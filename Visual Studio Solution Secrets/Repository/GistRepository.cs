using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Repository
{

    public class GistRepository : IRepository
    {

        private const string USER_AGENT = "VisualStudioSolutionSecrets/1.0";

        private const string APP_DATA_FILENAME = "github.json";

        private const string CLIENT_ID = "b0e87a43f71306d87649";
        private const string SCOPE = "gist";

        private string? _oauthAccessToken;
        private string? _repositoryName;
        private Gist? _gist;



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



        public GistRepository()
        {
            var repositoryData = AppData.LoadData<RepositoryAppData>(APP_DATA_FILENAME);
            if (repositoryData != null)
            {
                _oauthAccessToken = repositoryData.access_token;
            }
        }


        public async Task AuthenticateAsync(string? repositoryName = null)
        {
            _repositoryName = repositoryName;

            await CheckAccessToken();

            if (_oauthAccessToken == null)
            {
                var deviceCodeData = await SendRequest<DeviceFlowResponse>(HttpMethod.Post, $"https://github.com/login/device/code?client_id={CLIENT_ID}&scope={SCOPE}");
                if (deviceCodeData != null)
                {
                    Console.WriteLine($"\nAuthenticate on GitHub with Device code = {deviceCodeData.user_code}\n");
                    OpenBrowser(deviceCodeData.verification_uri);

                    for (int seconds = deviceCodeData.expires_in; seconds > 0; seconds -= deviceCodeData.interval)
                    {
                        var accessTokenResponse = await SendRequest<AccessTokenResponse>(
                            HttpMethod.Post,
                            $"https://github.com/login/oauth/access_token?client_id={CLIENT_ID}&device_code={deviceCodeData.device_code}&grant_type=urn:ietf:params:oauth:grant-type:device_code"
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

                        await Task.Delay(1000 * deviceCodeData.interval);
                    }
                }
            }

            _gist = await GetGist();
        }


        private async Task<Gist?> GetGist()
        {
            if (_repositoryName != null)
            {
                const int GIST_PER_PAGE = 100;
                for (int page = 1; page < 1000; page++)
                {
                    var gists = await SendRequest<List<Gist>>(HttpMethod.Get, $"https://api.github.com/gists?per_page={GIST_PER_PAGE}&page={page}");
                    if (gists == null || gists.Count == 0)
                    {
                        break;
                    }
                    for (int i = 0; i < gists.Count; i++)
                    {
                        var gist = gists[i];
                        if (gist.description == _repositoryName)
                        {
                            return gist;
                        }
                    }
                    if (gists.Count < GIST_PER_PAGE)
                    {
                        break;
                    }
                }
            }
            return null;
        }


        public async Task<ICollection<(string name, string? content)>> PullFilesAsync()
        {
            List<(string name, string? content)> files = new List<(string name, string? content)>();
            if (_gist != null && _gist.files != null)
            {
                foreach (var file in _gist.files)
                {
                    string? content = file.Value.content;
                    if (content == null && file.Value.raw_url != null)
                    {
                        HttpClient httpClient = new HttpClient();
                        try
                        {
                            content = await httpClient.GetStringAsync(file.Value.raw_url);
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


        public async Task PushFilesAsync(ICollection<(string name, string? content)> files)
        {
            HttpClient client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            });

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

            if (client.DefaultRequestHeaders.UserAgent.TryParseAdd(USER_AGENT))
                client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            HttpRequestMessage request = _gist == null ?
                new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/gists") :
                new HttpRequestMessage(HttpMethod.Patch, $"https://api.github.com/gists/{_gist.id}");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var payload = new Gist
            {
                description = _repositoryName,
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

#if NET5_0_OR_GREATER
            string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
#else
            string payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { IgnoreNullValues = true });
#endif
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();
            }
            catch
            {
            }
        }


        private async Task CheckAccessToken()
        {
            if (_oauthAccessToken == null)
            {
                return;
            }

            HttpClient client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            });

            if (_oauthAccessToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

            if (client.DefaultRequestHeaders.UserAgent.TryParseAdd(USER_AGENT))
                client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            var message = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/gists/00000000000000000000000000000000");
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                var response = await client.SendAsync(message).ConfigureAwait(false);
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


        private async Task<T?> SendRequest<T>(HttpMethod method, string uri)
            where T : class, new()
        {
            HttpClient client = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            });

            if (_oauthAccessToken != null)
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthAccessToken);

            if (client.DefaultRequestHeaders.UserAgent.TryParseAdd(USER_AGENT))
                client.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

            var message = new HttpRequestMessage(method, uri);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                var response = await client.SendAsync(message).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var data = JsonSerializer.Deserialize<T>(jsonContent);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}");
            }
            return null;
        }


        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

    }
}
