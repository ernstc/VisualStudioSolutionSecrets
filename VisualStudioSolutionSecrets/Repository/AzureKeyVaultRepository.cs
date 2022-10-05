using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace VisualStudioSolutionSecrets.Repository
{
    public class AzureKeyVaultRepository : IRepository
    {

        private readonly IDictionary<string, string> _clouds = new Dictionary<string, string>
        {
            {"AzureCloud",         ".vault.azure.net" },
            {"AzureChinaCloud",    ".vault.azure.cn" },
            {"AzureUSGovernment",  ".vault.usgovcloudapi.net" },
            {"AzureGermanCloud",   ".vault.microsoftazure.de" }
        };

        private const string DEFAULT_CLOUD = "AzureCloud";
        private const string SECRET_PREFIX = "vs-secrets--";

        public bool EncryptOnClient => false;
        public string RepositoryType => "AzureKV";
        
        private string? _repositoryName;

        public string? RepositoryName
        {
            get => _repositoryName;
            set
            {
                if (value == null)
                {
                    _repositoryName = null;
                }
                else
                {
                    string loweredValue = value.ToLower();
                    if (Uri.TryCreate(loweredValue, UriKind.Absolute, out Uri? repositoryUri) && repositoryUri != null)
                    {
                        string cloudDomain = loweredValue[loweredValue.IndexOf(".vault.")..];
                        if (_clouds.Any(item => item.Value == cloudDomain))
                        {
                            _repositoryName = repositoryUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? loweredValue : null;
                        }
                        else
                        {
                            _repositoryName = null;
                        }
                    }
                    else
                    {
                        _repositoryName = $"https://{loweredValue}{_clouds[DEFAULT_CLOUD]}";
                    }
                }
            }
        }


        private SecretClient? _client;



        public string? GetFriendlyName()
        {
            if (RepositoryName == null)
            {
                return null;
            }

            string name = RepositoryName;
            name = name[8..];
            name = name[..name.IndexOf(".vault.")];

            string cloudDomain = RepositoryName;
            cloudDomain = cloudDomain[cloudDomain.IndexOf(".vault.")..];

            var cloud = _clouds.First(entry => entry.Value.Equals(cloudDomain, StringComparison.OrdinalIgnoreCase));

            return $"{cloud.Key} ({name})";
        }


        public async Task AuthorizeAsync()
        {
            if (_repositoryName != null)
            {
                var tokenCachePersistenceOptions = new TokenCachePersistenceOptions
                {
                    Name = "vs-secrets",
                    UnsafeAllowUnencryptedStorage = false
                };

                var interactiveBrowserCredentialOptions = new InteractiveBrowserCredentialOptions();
                interactiveBrowserCredentialOptions.TokenCachePersistenceOptions = tokenCachePersistenceOptions;
                interactiveBrowserCredentialOptions.AdditionallyAllowedTenants.Add("*");

                async Task AuthorizeClientAsync()
                {
                    var credential = new ChainedTokenCredential(
                        new SharedTokenCacheCredential(new SharedTokenCacheCredentialOptions(tokenCachePersistenceOptions)),
                        new InteractiveBrowserCredential(interactiveBrowserCredentialOptions)
                    );

                    var accessToken = await credential.GetTokenAsync(new TokenRequestContext(/*scopes: new string[] { "https://vault.azure.net/.default" }*/));

                    _client = new SecretClient(new Uri(_repositoryName!), credential);
                }

                await AuthorizeClientAsync();
                try
                {
                    var _ = await _client!.GetSecretAsync("vs-secrets-fake");
                }
                catch (Azure.Identity.AuthenticationFailedException)
                {
                    await AuthorizeClientAsync();
                }
                catch
                { }
            }
        }


        public Task<bool> IsReady()
        {
            return Task.FromResult(_client != null);
        }


        public Task<ICollection<SolutionSettings>> PullAllSecretsAsync()
        {
            // No needs to implement this method.
            return Task.FromResult<ICollection<SolutionSettings>>(new List<SolutionSettings>());
        }


        public async Task<ICollection<(string name, string? content)>> PullFilesAsync(ISolution solution)
        {
            var files = new List<(string name, string? content)>();

            if (_client == null)
            {
                return files;
            }

            var asyncPagedResults = _client.GetPropertiesOfSecretsAsync();

            string prefix = $"{SECRET_PREFIX}{solution.Uid}--";

            List<string> solutionSecretsName = new List<string>();

            await foreach (var secretProperties in asyncPagedResults)
            {
                if (secretProperties.Enabled == true && secretProperties.Name.StartsWith(prefix))
                {
                    solutionSecretsName.Add(secretProperties.Name);
                }
            }

            foreach (var secretName in solutionSecretsName)
            {
                var response = await _client.GetSecretAsync(secretName);
                var secret = response?.Value;
                if (secret == null)
                {
                    continue;
                }

                string[] nameParts = secretName.Split("--");
                if (nameParts.Length == 3)
                {
                    string fileNamePart = nameParts[2];
                    string fileName = fileNamePart == "secrets" ?
                            "secrets" :
                            $"secrets\\{fileNamePart}.json";
                    files.Add((name: fileName, content: secret.Value));
                }
            }

            return files;
        }


        public async Task<bool> PushFilesAsync(ISolution solution, ICollection<(string name, string? content)> files)
        {
            if (_client == null)
            {
                return false;
            }

            foreach (var (name, content) in files)
            {
                string fileName = name;
                if (fileName.Contains('\\'))
                {
                    fileName = fileName[(fileName.IndexOf('\\') + 1)..];
                    fileName = fileName[..fileName.IndexOf('.')];
                }
                string secretName = $"{SECRET_PREFIX}{solution.Uid}--{fileName}";
                try
                {
                    await _client.SetSecretAsync(secretName, content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERR: {ex.Message}");
                    return false;
                }
            }

            return true;
        }


        public Task RefreshStatus()
        {
            return Task.CompletedTask;
        }

    }
}
