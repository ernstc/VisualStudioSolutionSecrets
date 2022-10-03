using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace VisualStudioSolutionSecrets.Repository
{
    public class AzureKeyVaultRepository : IRepository
    {

        private const string SECRET_PREFIX = "vs-secrets--";

        public bool EncryptOnClient => false;
        public string RepositoryType => "AzureKV";
        public string? RepositoryName { get; set; }


        private SecretClient? _client;


        public Task AuthorizeAsync()
        {
            var kvUri = "https://" + RepositoryName + ".vault.azure.net";
            var credential = new ChainedTokenCredential(new DefaultAzureCredential(), new InteractiveBrowserCredential());
            _client = new SecretClient(new Uri(kvUri), credential);
            return Task.CompletedTask;
        }


        public Task<bool> IsReady()
        {
            return Task.FromResult(_client != null);
        }


        public Task<ICollection<SolutionSettings>> PullAllSecretsAsync()
        {
            return Task.FromResult<ICollection<SolutionSettings>>(new List<SolutionSettings>());
        }


        public async Task<ICollection<(string name, string? content)>> PullFilesAsync(string solutionName)
        {
            var files = new List<(string name, string? content)>();

            if (_client == null)
            {
                return files;
            }

            var asyncPagedResults = _client.GetPropertiesOfSecretsAsync();

            solutionName = solutionName.Substring(0, solutionName.IndexOf('.'));
            string prefix = $"{SECRET_PREFIX}{solutionName}--";

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


        public async Task<bool> PushFilesAsync(string solutionName, ICollection<(string name, string? content)> files)
        {
            if (_client == null)
            {
                return false;
            }

            solutionName = solutionName.Substring(0, solutionName.IndexOf('.'));
            foreach (var item in files)
            {
                string fileName = item.name;
                if (fileName.Contains('\\'))
                {
                    fileName = fileName.Substring(fileName.IndexOf('\\') + 1);
                    fileName = fileName.Substring(0, fileName.IndexOf('.'));
                }    
                string secretName = $"{SECRET_PREFIX}{solutionName}--{fileName}";
                try
                {
                    await _client.SetSecretAsync(secretName, item.content);
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
