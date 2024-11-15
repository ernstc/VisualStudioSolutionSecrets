using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    internal class GistRepository : IRepository
    {

        private const string APP_DATA_FILENAME = "github.json";

        private const string CLIENT_ID = "b0e87a43f71306d87649";
        private const string SCOPE = "gist";

        private const int GIST_PER_PAGE = 100;
        private const int GIST_PAGES_LIMIT = 1000;

        private string? _oAuthAccessToken;


        public bool EncryptOnClient => true;
        public string RepositoryType => "GitHub";
        public string? RepositoryName { get; set; }


        public string? GetFriendlyName()
        {
            return RepositoryType;
        }


        private readonly JsonSerializerOptions _jsonOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };



        #region GitHub Gists data model

        private sealed class DeviceFlowResponse
        {
            [JsonPropertyName("device_code")]
            public string DeviceCode { get; set; } = null!;

            [JsonPropertyName("user_code")]
            public string UserCode { get; set; } = null!;

            [JsonPropertyName("verification_uri")]
            public string VerificationUri { get; set; } = null!;

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("interval")]
            public int Interval { get; set; }
        }



        private sealed class AccessTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string? TokenType { get; set; }

            [JsonPropertyName("scope")]
            public string? Scope { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("error_description")]
            public string? ErrorDescription { get; set; }

            [JsonPropertyName("error_uri")]
            public string? ErrorUri { get; set; }
        }



        private sealed class GistFile
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }

            [JsonPropertyName("raw_url")]
            public string? RawUrl { get; set; }
        }



        private sealed class Gist
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("public")]
            public bool Public { get; set; }

            [JsonPropertyName("files")]
            public Dictionary<string, GistFile>? Files { get; set; }
        }



        private sealed class RepositoryAppData
        {
            [JsonPropertyName("access_token")]
            public string? AccessToken { get; set; }
        }

        #endregion



        public GistRepository()
        {
            RefreshStatus().Wait();
        }


        private bool _isReadyCalled;
        private bool _isReadyResult;

        public async Task<bool> IsReady()
        {
            if (!_isReadyCalled)
            {
                _isReadyCalled = true;
                await CheckAccessToken();
                _isReadyResult = _oAuthAccessToken != null;
            }
            return _isReadyResult;
        }


        public Task RefreshStatus()
        {
            RepositoryAppData? repositoryData = AppData.LoadData<RepositoryAppData>(APP_DATA_FILENAME);
            _oAuthAccessToken = repositoryData?.AccessToken;
            _isReadyCalled = false;
            _isReadyResult = false;
            return Task.CompletedTask;
        }


        public async Task AuthorizeAsync(bool batchMode = false)
        {
            if (batchMode)
            {
                throw new UnauthorizedAccessException($"{nameof(GistRepository)} does not support authorization during batch operations.");
            }

            DeviceFlowResponse? _deviceFlowResponse = await SendRequest<DeviceFlowResponse>(HttpMethod.Post, $"https://github.com/login/device/code?client_id={CLIENT_ID}&scope={SCOPE}");
            if (_deviceFlowResponse == null)
            {
                return;
            }

            string user_code = _deviceFlowResponse.UserCode;
            if (user_code == null)
            {
                return;
            }

            Console.WriteLine($"\nAuthenticate on GitHub with Device code = {user_code}\n");

            await CheckAccessToken();

            if (_oAuthAccessToken == null)
            {
                WebBrowser.OpenUrl(new Uri(_deviceFlowResponse.VerificationUri));

                for (int seconds = _deviceFlowResponse.ExpiresIn; seconds > 0; seconds -= _deviceFlowResponse.Interval)
                {
                    AccessTokenResponse? accessTokenResponse = await SendRequest<AccessTokenResponse>(
                        HttpMethod.Post,
                        $"https://github.com/login/oauth/access_token?client_id={CLIENT_ID}&device_code={_deviceFlowResponse.DeviceCode}&grant_type=urn:ietf:params:oauth:grant-type:device_code"
                        );

                    if (accessTokenResponse?.AccessToken != null)
                    {
                        _oAuthAccessToken = accessTokenResponse.AccessToken;

                        AppData.SaveData(APP_DATA_FILENAME, new RepositoryAppData
                        {
                            AccessToken = _oAuthAccessToken
                        });

                        break;
                    }

                    await Task.Delay(1000 * _deviceFlowResponse.Interval);
                }
            }
        }


        public async Task<ICollection<SolutionSettings>> PullAllSecretsAsync()
        {
            List<SolutionSettings> data = new();

            for (int page = 1; page < GIST_PAGES_LIMIT; page++)
            {
                List<Gist>? gists = await SendRequest<List<Gist>>(HttpMethod.Get, $"https://api.github.com/gists?per_page={GIST_PER_PAGE}&page={page}");
                if (gists == null || gists.Count == 0)
                {
                    break;
                }
                for (int i = 0; i < gists.Count; i++)
                {
                    Gist gist = gists[i];
                    if (gist.Files == null)
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
                        Console.WriteLine($"\n    ERR: Header file has incompatible version {header.VisualStudioSolutionSecretsVersion}");
                        Console.WriteLine($"\n         Consider to install an updated version of this tool.");
                        continue;
                    }

                    if (header.SolutionFile != null)
                    {
                        List<(string name, string? content)> files = new();

                        foreach (KeyValuePair<string, GistFile> file in gist.Files)
                        {
                            if (file.Key == "secrets")
                            {
                                continue;
                            }

                            string? content = file.Value.Content;
                            if (content == null && file.Value.RawUrl != null)
                            {
                                try
                                {
                                    content = await GetRawContent(file.Value.RawUrl);
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
                            SolutionSettings solutionSettings = new(files)
                            {
                                Name = header.SolutionFile
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
            ArgumentNullException.ThrowIfNull(solution);

            List<(string name, string? content)> files = new();
            Gist? gist = await GetGistAsync(solution);
            if (gist?.Files != null)
            {
                foreach (KeyValuePair<string, GistFile> file in gist.Files)
                {
                    string? content = file.Value.Content;
                    if (content == null && file.Value.RawUrl != null)
                    {
                        try
                        {
                            content = await GetRawContent(file.Value.RawUrl);
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
            ArgumentNullException.ThrowIfNull(solution);
            ArgumentNullException.ThrowIfNull(files);

            Gist? gist = await GetGistAsync(solution);
            if (gist != null)
            {
                _ = await DeleteGist(gist);
            }

            using HttpClientHandler httpHandler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new(httpHandler);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oAuthAccessToken);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);
            }

            using HttpRequestMessage request = new(HttpMethod.Post, "https://api.github.com/gists");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            string gistDescription = solution.Name;
            if (solution.Uid != Guid.Empty)
            {
                gistDescription += $" ({solution.Uid})";
            }

            Gist payload = new()
            {
                Description = gistDescription,
                Public = false,
                Files = new Dictionary<string, GistFile>()
            };

            foreach ((string name, string? content) in files)
            {
                payload.Files.Add(name, new GistFile
                {
                    Content = content
                });
            }

            try
            {
                string payloadJson = JsonSerializer.Serialize(payload, _jsonOptions);
                request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

                Debug.WriteLine($"{request.Method} {request.RequestUri}");
                HttpResponseMessage response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}");
                return false;
            }
        }


        public bool IsValid()
        {
            return true;
        }


        private async Task<Gist?> GetGistAsync(ISolution solution)
        {
            string gistDescription = solution.Name;
            if (solution.Uid != Guid.Empty)
            {
                gistDescription += $" ({solution.Uid})";
            }

            for (int page = 1; page < GIST_PAGES_LIMIT; page++)
            {
                List<Gist>? gists = await SendRequest<List<Gist>>(HttpMethod.Get, $"https://api.github.com/gists?per_page={GIST_PER_PAGE}&page={page}", useCache: true);
                if (gists == null || gists.Count == 0)
                {
                    break;
                }
                for (int i = 0; i < gists.Count; i++)
                {
                    Gist gist = gists[i];
                    if (
                        gist.Description == gistDescription
                        || gist.Description == solution.Name     // For compatibility with version 1.x.x format
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
            if (gist.Files != null)
            {
                foreach (KeyValuePair<string, GistFile> file in gist.Files)
                {
                    if (file.Key == "secrets")
                    {
                        if (file.Value.RawUrl != null)
                        {
                            string? content = null;
                            try
                            {
                                content = await GetRawContent(file.Value.RawUrl);
                                if (content != null)
                                {
                                    return JsonSerializer.Deserialize<HeaderFile>(content);
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                        break;
                    }
                }
            }
            return null;
        }


        private async Task<bool> DeleteGist(Gist gist)
        {
            ArgumentNullException.ThrowIfNull(gist);

            using HttpClientHandler httpHandler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new(httpHandler);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oAuthAccessToken);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);
            }

            using HttpRequestMessage request = new(HttpMethod.Delete, $"https://api.github.com/gists/{gist.Id}");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            try
            {
                Debug.WriteLine($"{request.Method} {request.RequestUri}");
                HttpResponseMessage response = await httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }


        private async Task CheckAccessToken()
        {
            if (_oAuthAccessToken == null)
            {
                return;
            }

            using HttpClientHandler httpHandler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                CheckCertificateRevocationList = true
            };

            using HttpClient httpClient = new(httpHandler);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oAuthAccessToken);

            if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);
            }

            using HttpRequestMessage message = new(HttpMethod.Get, "https://api.github.com/gists/00000000000000000000000000000000");
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                Debug.WriteLine($"{message.Method} {message.RequestUri}");
                HttpResponseMessage response = await httpClient.SendAsync(message).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    _oAuthAccessToken = null;
                    AppData.SaveData(APP_DATA_FILENAME, new RepositoryAppData
                    {
                        AccessToken = null
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}");
            }
        }


        private readonly Dictionary<string, string> _requestsCache = new();

        private async Task<T?> SendRequest<T>(HttpMethod method, string uri, bool useCache = false)
            where T : class, new()
        {
            string? content = null;
            string cacheKey = $"{method} {uri}";

            _ = useCache && _requestsCache.TryGetValue(cacheKey, out content);

            if (content == null)
            {
                using HttpClientHandler httpHandler = new()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                    CheckCertificateRevocationList = true
                };

                using HttpClient httpClient = new(httpHandler);

                if (_oAuthAccessToken != null)
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oAuthAccessToken);
                }

                if (httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.USER_AGENT))
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", Constants.USER_AGENT);
                }

                using HttpRequestMessage message = new(method, uri);
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                try
                {
                    Debug.WriteLine($"{message.Method} {message.RequestUri}");
                    HttpResponseMessage response = await httpClient.SendAsync(message).ConfigureAwait(false);
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
                    T? data = JsonSerializer.Deserialize<T>(content);
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
            using HttpClient httpClient = new();
            Debug.WriteLine($"GET {uri}");
            return await httpClient.GetStringAsync(new Uri(uri));
        }

    }
}
