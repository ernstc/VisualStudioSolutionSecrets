using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Repository;
using VisualStudioSolutionSecrets.Tests.Helpers;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class ConfigureCommandTests : CommandTests, IDisposable
    {

        private const string KEY_VAULT_NAME = "keyvault-name";
        private const string SOLUTION_GUID = "D16E70F9-E206-4047-8A33-01710EBD0EEB";


        public ConfigureCommandTests()
        {
            ConfigureContext();
        }


        public void Dispose()
        {
            string configurationFilePath = Path.Combine(Constants.ConfigFilesPath, "configuration.json");
            if (File.Exists(configurationFilePath))
            {
                File.Delete(configurationFilePath);
            }
        }


        [Fact]
        public async Task ConfigureGitHubByDefaultTest()
        {
            await CallCommand.Configure(new ConfigureOptions
            {
                Default = true,
                RepositoryType = RepositoryTypesEnum.GitHub
            });

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
            Assert.Equal(RepositoryTypesEnum.GitHub, Configuration.Default.Repository);
            Assert.Null(Configuration.Default.AzureKeyVaultName);
        }


        [Fact]
        public async Task ConfigureAzureKVByDefaultTest()
        {
            await CallCommand.Configure(new ConfigureOptions
            {
                Default = true,
                RepositoryType = Repository.RepositoryTypesEnum.AzureKV,
                RepositoryName = KEY_VAULT_NAME
            });

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
            Assert.Equal(RepositoryTypesEnum.AzureKV, Configuration.Default.Repository);
            Assert.Equal(KEY_VAULT_NAME, Configuration.Default.AzureKeyVaultName);
        }


        [Fact]
        public async Task ConfigureGitHubForProject()
        {
            await CallCommand.Configure(new ConfigureOptions
            {
                RepositoryType = Repository.RepositoryTypesEnum.GitHub
            });

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));
            Assert.Equal(RepositoryTypesEnum.GitHub, settings.Repository);
            Assert.Null(settings.AzureKeyVaultName);
        }


        [Fact]
        public async Task ConfigureAzureKVForProject()
        {
            await CallCommand.Configure(new ConfigureOptions
            {
                RepositoryType = Repository.RepositoryTypesEnum.AzureKV,
                RepositoryName = KEY_VAULT_NAME
            });

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));
            Assert.Equal(RepositoryTypesEnum.AzureKV, settings.Repository);
            Assert.Equal(KEY_VAULT_NAME, settings.AzureKeyVaultName);
        }

    }
}
