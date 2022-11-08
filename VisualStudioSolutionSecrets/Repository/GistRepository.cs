using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Utilities;

namespace VisualStudioSolutionSecrets.Repository
{

    public class GistRepository : IRepository
    {

        private const string APP_DATA_FILENAME = "github.json";

        private const string CLIENT_ID = "b0e87a43f71306d87649";
        private const string SCOPE = "gist";

        private const int GIST_PER_PAGE = 100;
        private const int GIST_PAGES_LIMIT = 1000;


        private DeviceFlowResponse? _deviceFlowResponse;

        private string? _oauthAccessToken;


        public bool EncryptOnClient => true;
        public string RepositoryType => "GitHub";
        public string? RepositoryName { get; set; }

        public string? GetFriendlyName() => RepositoryName;



        #region GitHub Gists data model

        class DeviceFlowResponse
        {
            public string device_code { get; set; } = null!;
            public string user_code { get; set; } = null!;
            public string verification_uri { get; set; } = null!;
            public int expires_in { get; set; }
            public int interval { get; set; }
        }



        class AccessTokenResponse
        {
            public string? access_token { get; set; }
            public string? token_type { get; set; }
            public string? scope { get; set; }
            public string? error { get; set; }
            public string? error_description { get; set; }
            public string? error_uri { get; set; }
        }



        class GistFile
        {
            public string? content { get; set; }
            public string? raw_url { get; set; }
        }



        class Gist
        {
            public string? id { get; set; }
            public string? description { get; set; }
            public bool @public { get; set; }
            public Dictionary<string, GistFile>? files { get; set; }
        }



        class RepositoryAppData
        {
            public string? access_token { get; set; }
        }

        #endregion



        public GistRepository()
        {
            RefreshStatus();
        }


        private bool _isReadyCalled;
        private bool _isReadyResult;

        public async Task<bool> IsReady()
        {
            if (!_isReadyCalled)
            {
                _isReadyCalled = true;
                await CheckAccessToken();
                _isReadyResult = _oauthAccessToken != null;
            }
            return _isReadyResult;
        }


        public Task RefreshStatus()
        {
            var repositoryData = AppData.LoadData<RepositoryAppData>(APP_DATA_FILENAME);
            _oauthAccessToken = repositoryData?.access_token;
            _isReadyCalled = false;
            _isReadyResult = false;
            return Task.CompletedTask;
        }


        public async Task AuthorizeAsync()
        {
            _deviceFlowResponse = await SendRequest<DeviceFlowResponse>(HttpMethod.Post, $"https://github.com/login/device/code?client_id={CLIENT_ID}&scope={SCOPE}");
            if (_deviceFlowResponse == null)
            {
                return;
            }

            string user_code = _deviceFlowResponse.user_code;
            if (user_code == null)
            {
                return;
            }

            Console.WriteLine($"\nAuthenticate on GitHub with Device code = {user_code}\n");

            await CheckAccessToken();

            if (_oauthAccessToken == null)
            {
                WebBrowser.OpenUrl(new Uri(_deviceFlowResponse.verification_uri));

                for (int seconds = _deviceFlowResponse.expires_in; seconds > 0; seconds -= _deviceFlowResponse.interval)
                {
                    var accessTokenResponse = await SendRequest<AccessTokenResponse>(
                        HttpMethod.Post,
                        $"https://github.com/login/oauth/access_token?client_id={CLIENT_ID}&device_code={_deviceFlowResponse.device_code}&grant_type=urn:ietf:params:oauth:grant-type:device_code"
                        );

                    if (accessTokenResponse?.access_token != null)
                    {
                        _oauthAccessToken = accessTokenResponse.access_token;

                        AppData.SaveData(APP_DATA_FILENAME, new RepositoryAppData
                        {
                            access_token = _oauthAccessToken
                        });

                        break;
                    }

                    await Task.Delay(1000 * _deviceFlowResponse.interval);
                }
            }
        }


        public async Task<ICollection<SolutionSettings>> PullAllSecretsAsync()
        {
            var data = new List<SolutionSettings>();

            for (int page = 1; page < GIST_PAGES_LIMIT; page++)
            {
                var gists = await SendRequest<List<Gist>>(HttpMethod.Get, $"https://api.github.com/gists?per_page={GIST_PER_PAGE}&page={page}");
                if (gists == null || gists.Count == 0)
                {
                    break;
                }
                for (int i = 0; i < gists.Count; i++)
                {
                    var gist = gists[i];
                    if (gist.files == null)
                    {
                        continue;
                    }

                    // Check if the gist is a solution secrets
                    HeaderFile? header = await GetHeaderFile(gist);
                    if (header == null)
                    {
                        continue;
                    }
                    else if (!header.IsVersionSupported())
                    {
                        Console.WriteLine($"\n    ERR: Header file has incompatible version {header.visualStudioSolutionSecretsVersion}");
                        Console.WriteLine($"\n         Consider to install an updated version of this tool.");
                        continue;
                    }

                    if (header.solutionFile != null)
                    {
                        var files = new List<(string name, string? content)>();

                        foreach (var file in gist.files)
                        {
                            if (file.Key == "secrets")
                            {
                                continue;
                            }

                            string? content = file.Value.content;
                            if (content == null && file.Value.raw_url != null)
                            {
                                try
                                {
                                    content = await GetRawContent(file.Value.raw_url);
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                            files.Add((file.Key, content));
                        }

                        if (files.Count > 0)
                        {
                            var solutionSettings = new SolutionSettings(files)
                            {
                                Name = header.solutionFile
                            };

                            data.Add(solutionSettings);
                        }
                    }
                }
                if (gists.Count < GIST_PER_PAGE)
                {
                    break;
                }
            }

            return data;
        }


        public async Task<ICollection<(string name, string? content)>> PullFilesAsync(ISolution solution)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            var files = new List<(string name, string? content)>();
            var gist = await GetGistAsync(solution);
            if (gist?.files != null)
            {
                foreach (var file in gist.files)
                {
                    string? content = file.Value.content;
                    if (content == null && file.Value.raw_url != null)
                    {
                        try
                        {
                            content = await GetRawContent(file.Value.raw_url);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    files.Add((file.Key, content));
                }
            }
            return files;
        }


        public async Task<bool> PushFilesAsync(ISolution solution, ICollection<(string name, string? content)> files)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (files == null)
                throw new ArgumentNullException(nameof(files));

            var gist = await GetGistAsync(solution);
            if (gist != null)
            {
                await DeleteGist(gist);
            }

            using var httpHandler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new HttpClient(httpHandler);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
                httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/gists");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            string gistDescription = solution.Name;
            if (solution.Uid != Guid.Empty) gistDescription += $" ({solution.Uid})";

            var payload = new Gist
            {
                description = gistDescription,
                @public = false,
                files = new Dictionary<string, GistFile>()
            };

            foreach (var file in files)
            {
                payload.files.Add(file.name, new GistFile
                {
                    content = file.content
                });
            }

            try
            {
#if NET5_0_OR_GREATER
                string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
#else
                string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { IgnoreNullValues = true });
#endif
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                Debug.WriteLine($"{request.Method} {request.RequestUri}");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}");
                return false;
            }
        }


        public bool IsValid() => true;


        private async Task<Gist?> GetGistAsync(ISolution solution)
        {
            string gistDescription = solution.Name;
            if (solution.Uid != Guid.Empty) gistDescription += $" ({solution.Uid})";

            for (int page = 1; page < GIST_PAGES_LIMIT; page++)
            {
                var gists = await SendRequest<List<Gist>>(HttpMethod.Get, $"https://api.github.com/gists?per_page={GIST_PER_PAGE}&page={page}", useCache: true);
                if (gists == null || gists.Count == 0)
                {
                    break;
                }
                for (int i = 0; i < gists.Count; i++)
                {
                    var gist = gists[i];
                    if (
                        gist.description == gistDescription
                        || gist.description == solution.Name     // For compatibility with version 1.x.x format
                        )
                    {
                        return gist;
                    }
                }
                if (gists.Count < GIST_PER_PAGE)
                {
                    break;
                }
            }
            return null;
        }


        private static async Task<HeaderFile?> GetHeaderFile(Gist gist)
        {
            if (gist.files != null)
            {
                foreach (var file in gist.files)
                {
                    if (file.Key == "secrets")
                    {
                        if (file.Value.raw_url != null)
                        {
                            string? content = null;
                            try
                            {
                                content = await GetRawContent(file.Value.raw_url);
                                if (content != null)
                                {
                                    return JsonSerializer.Deserialize<HeaderFile>(content);
                                }
                            }
                            catch
                            { }
                        }
                        break;
                    }
                }
            }
            return null;
        }


        private async Task<bool> DeleteGist(Gist gist)
        {
            if (gist == null)
                throw new ArgumentNullException(nameof(gist));

            using var httpHandler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new HttpClient(httpHandler);
            
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
                httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);

            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"https://api.github.com/gists/{gist.id}");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            try
            {
                Debug.WriteLine($"{request.Method} {request.RequestUri}");
                var response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }


        private async Task CheckAccessToken()
        {
            if (_oauthAccessToken == null)
            {
                return;
            }

            using var httpHandler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new HttpClient(httpHandler);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
                httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);

            using var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/gists/00000000000000000000000000000000");
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                Debug.WriteLine($"{message.Method} {message.RequestUri}");
                var response = await httpClient.SendAsync(message).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    _oauthAccessToken = null;
                    AppData.SaveData(APP_DATA_FILENAME, new RepositoryAppData
                    {
                        access_token = null
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}");
            }
        }


        private Dictionary<string, string> _requestsCache = new Dictionary<string, string>();

        private async Task<T?> SendRequest<T>(HttpMethod method, string uri, bool useCache = false)
            where T : class, new()
        {
            string? content = null;
            string cacheKey = $"{method} {uri}";

            if (useCache && _requestsCache.ContainsKey(cacheKey))
            {
                content = _requestsCache[cacheKey];
            }

            if (content == null)
            {
                using var httpHandler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    CheckCertificateRevocationList = true
                };

                using HttpClient httpClient = new HttpClient(httpHandler);

                if (_oauthAccessToken != null)
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

                if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
                    httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);

                using var message = new HttpRequestMessage(method, uri);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    Debug.WriteLine($"{message.Method} {message.RequestUri}");
                    var response = await httpClient.SendAsync(message).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (content != null)
                        {
                            _requestsCache[cacheKey] = content;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERR: {ex.Message}");
                }
            }

            if (content != null)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<T>(content);
                    return data;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERR: {ex.Message}");
                }
            }

            return null;
        }


        private static async Task<string?> GetRawContent(string uri)
        {
            using HttpClient httpClient = new HttpClient();
            Debug.WriteLine($"GET {uri}");
            try
            {
                return await httpClient.GetStringAsync(new Uri(uri));
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
