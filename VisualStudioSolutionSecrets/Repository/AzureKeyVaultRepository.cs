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

        public string RepositoryType => "AzureKV";
        public string? RepositoryName { get; }
        public string? SolutionName { get; set; }


        private SecretClient _client;


        public Task AuthorizeAsync()
        {
            var kvUri = "https://" + RepositoryName + ".vault.azure.net";
            var credential = new InteractiveBrowserCredential();
            _client = new SecretClient(new Uri(kvUri), credential);
            return Task.CompletedTask;
        }


        public Task<bool> IsReady()
        {
            return Task.FromResult(true);
        }


        public async Task<ICollection<SolutionSettings>> PullAllSecretsAsync()
        {
            return null;
        }


        public async Task<ICollection<(string name, string? content)>> PullFilesAsync()
        {
            return null;
        }


        public Task<bool> PushFilesAsync(ICollection<(string name, string? content)> files)
        {
            return Task.FromResult(true);
        }


        public Task RefreshStatus()
        {
            return Task.CompletedTask;
        }

    }
}
