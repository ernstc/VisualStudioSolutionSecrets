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

        public string RepositoryType => "AzureKV";
        public string? RepositoryName { get; }
        public string? SolutionName { get; set; }


        private SecretClient? _client;


        public Task AuthorizeAsync()
        {
            var kvUri = "https://" + RepositoryName + ".vault.azure.net";
            var credential = new InteractiveBrowserCredential();
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


        public async Task<ICollection<(string name, string? content)>> PullFilesAsync()
        {
            var files = new List<(string name, string? content)>();

            if (_client == null || SolutionName == null)
            {
                return files;
            }

            var asyncPagedResults = _client.GetPropertiesOfSecretsAsync();

            string solutionName = SolutionName.Substring(0, SolutionName.IndexOf('.'));
            string prefix = $"{SECRET_PREFIX}{solutionName}";

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
                    string fileName = $"secrets\\{nameParts[2]}.json";
                    files.Add((name: fileName, content: secret.Value));
                }
            }

            return files;
        }


        public async Task<bool> PushFilesAsync(ICollection<(string name, string? content)> files)
        {
            if (_client == null || SolutionName == null)
            {
                return false;
            }

            string solutionName = SolutionName.Substring(0, SolutionName.IndexOf('.'));
            foreach (var item in files)
            {
                string fileName = item.name;
                if (fileName.Contains('\\'))
                {
                    fileName = fileName.Substring(fileName.IndexOf('\\'));
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
