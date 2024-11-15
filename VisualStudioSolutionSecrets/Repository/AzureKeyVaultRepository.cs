using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace VisualStudioSolutionSecrets.Repository
{
    internal class AzureKeyVaultRepository : IRepository
    {

        private readonly Dictionary<string, string> _clouds = new Dictionary<string, string>
        {
            {".vault.azure.net",            "AzureCloud"},
            {".vault.azure.cn",             "AzureChinaCloud" },
            {".vault.usgovcloudapi.net",    "AzureUSGovernment" },
            {".vault.microsoftazure.de",    "AzureGermanCloud" }
        };

        private const string DEFAULT_CLOUD = ".vault.azure.net";
        private const string SECRET_PREFIX = "vs-secrets--";

        public bool EncryptOnClient => false;
        public string RepositoryType => "AzureKV";

        private Uri? _repositoryUri;
        private string? _repositoryName;

        public string? RepositoryName
        {
            get => _repositoryName;
            set
            {
                if (value == null)
                {
                    _repositoryName = null;
                    _repositoryUri = null;
                }
                else
                {
                    string loweredValue = value.ToLowerInvariant();
                    if (Uri.TryCreate(loweredValue, UriKind.Absolute, out Uri? repositoryUri))
                    {
                        _repositoryName = null;
                        _repositoryUri = null;

                        int vaultIndex = loweredValue.IndexOf(".vault.", StringComparison.Ordinal);
                        if (vaultIndex >= 0)
                        {
                            string cloudDomain = loweredValue[vaultIndex..];
                            if (_clouds.ContainsKey(cloudDomain))
                            {
                                _repositoryName = repositoryUri.Scheme.Equals("https", StringComparison.Ordinal) ? loweredValue : null;
                                _repositoryUri = _repositoryName != null ? new Uri(_repositoryName) : null;
                            }
                        }
                    }
                    else
                    {
                        _repositoryName = $"https://{loweredValue}{DEFAULT_CLOUD}";
                        _repositoryUri = new Uri(_repositoryName);
                    }
                }

                if (_client != null && _client.VaultUri != _repositoryUri)
                {
                    // If the vault URI has changed, we need to re-authorize the client.
                    _client = null;
                }
            }
        }


        // Credentials settings

        private static readonly TokenCachePersistenceOptions _tokenCachePersistenceOptions =
            new TokenCachePersistenceOptions
            {
                Name = "vs-secrets",
                UnsafeAllowUnencryptedStorage = false
            };

        private static readonly InteractiveBrowserCredentialOptions _interactiveBrowserCredentialOptions =
            new InteractiveBrowserCredentialOptions
            {
                TokenCachePersistenceOptions = _tokenCachePersistenceOptions,
                AdditionallyAllowedTenants = { "*" }
            };


        // Credential selected for accessing to any Azure Key Vault repository
        private static ChainedTokenCredential? _credential;

        // Current client
        private SecretClient? _client;



        private static async Task<ChainedTokenCredential?> GetCredential()
        {
            if (_credential == null)
            {
                _credential = new ChainedTokenCredential(
                    new SharedTokenCacheCredential(new SharedTokenCacheCredentialOptions(_tokenCachePersistenceOptions)),
                    new InteractiveBrowserCredential(_interactiveBrowserCredentialOptions)
                );

                _ = await _credential.GetTokenAsync(new TokenRequestContext(/*scopes: new string[] { "https://vault.azure.net/.default" }*/));
            }
            return _credential;
        }


        public string? GetFriendlyName()
        {
            if (_repositoryName == null)
            {
                return null;
            }

            string name = _repositoryName;
            name = name[8..];
            name = name[..name.IndexOf(".vault.", StringComparison.Ordinal)];

            string cloudDomain = _repositoryName;
            cloudDomain = cloudDomain[cloudDomain.IndexOf(".vault.", StringComparison.Ordinal)..];

            return _clouds.TryGetValue(cloudDomain, out string? cloud)
                ? $"{name} ({cloud})"
                : _repositoryName;
        }


        public async Task AuthorizeAsync(bool batchMode = false)
        {
            if (_repositoryUri != null)
            {
                async Task AuthorizeClientAsync()
                {
                    _client = new SecretClient(_repositoryUri, await GetCredential());
                }

                await AuthorizeClientAsync();
                try
                {
                    _ = await _client!.GetSecretAsync("vs-secrets-fake");
                }
                catch (Azure.RequestFailedException ex)
                {
                    if (ex.Status == 401)
                    {
                        _client = null;
                        throw new UnauthorizedAccessException(ex.ErrorCode, ex);
                    }
                }
                catch
                {
                    // ignored
                }
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
            ArgumentNullException.ThrowIfNull(solution);

            List<(string name, string? content)> files = new List<(string name, string? content)>();

            if (_client == null)
            {
                return files;
            }

            string prefix = solution.Uid != Guid.Empty ?
                $"{SECRET_PREFIX}{solution.Uid}--" :
                $"{SECRET_PREFIX}{solution.Name}--";

            List<string> solutionSecretsName = new List<string>();
            await foreach (SecretProperties secretProperties in _client.GetPropertiesOfSecretsAsync())
            {
                if (secretProperties.Enabled == true && secretProperties.Name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    solutionSecretsName.Add(secretProperties.Name);
                }
            }

            foreach (string secretName in solutionSecretsName)
            {
                Azure.Response<KeyVaultSecret> response = await _client.GetSecretAsync(secretName);
                KeyVaultSecret? secret = response?.Value;
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
            ArgumentNullException.ThrowIfNull(solution);
            ArgumentNullException.ThrowIfNull(files);

            if (_client == null)
            {
                return false;
            }

            foreach ((string name, string? content) in files)
            {
                string fileName = name;
                if (fileName.Contains('\\', StringComparison.Ordinal))
                {
                    fileName = fileName[(fileName.IndexOf('\\', StringComparison.Ordinal) + 1)..];
                    fileName = fileName[..fileName.IndexOf('.', StringComparison.Ordinal)];
                }

                string secretName = solution.Uid != Guid.Empty ?
                    $"{SECRET_PREFIX}{solution.Uid}--{fileName}" :
                    $"{SECRET_PREFIX}{solution.Name}--{fileName}";

                int retry = 1;
                bool checkSecretEquality = true;
                while (true)
                {
                    if (checkSecretEquality)
                    {
                        try
                        {
                            // Read the current secret
                            Azure.Response<KeyVaultSecret> response = await _client.GetSecretAsync(secretName);
                            KeyVaultSecret? secret = response?.Value;
                            if (secret?.Value == content)
                            {
                                break;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    try
                    {
                        _ = await _client.SetSecretAsync(secretName, content);
                    }
                    catch (Azure.RequestFailedException aex)
                    {
                        if (aex.Status == 409)
                        {
                            // Try to purge eventually deleted secret
                            try
                            {
                                _ = await _client.PurgeDeletedSecretAsync(secretName);
                            }
                            catch (Azure.RequestFailedException aex2)
                            {
#pragma warning disable CA1508
                                if (aex2.Status == 403)
                                {
                                    Console.WriteLine($"\nERR: Cannot proceed with the operation.\n     Check if there is a secret named \"{secretName}\" that is deleted, but recoverable. In that case purge the secret or recover it before pushing local secrets.");
                                }
                                else
                                {
                                    Console.WriteLine($"\nERR: Cannot proceed with the operation.\n     There is a conflict with the secret named \"{secretName}\" that cannot be resolved. Contact the administrator of the Key Vault.");
                                }
#pragma warning restore CA1508
                                return false;
                            }
                            catch (Exception)
                            {
                                return false;
                            }

                            if (retry-- > 0)
                            {
                                checkSecretEquality = false;
                                continue;
                            }
                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERR: {ex.Message}");
                        return false;
                    }
                    break;
                }
            }

            return true;
        }


        public Task RefreshStatus()
        {
            return Task.CompletedTask;
        }


        public bool IsValid()
        {
            return _repositoryName != null;
        }
    }
}
