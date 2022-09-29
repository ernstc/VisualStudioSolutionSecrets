using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Repository;

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
        public void ConfigureGitHubByDefaultTest()
        {
            new ConfigureCommand
            {
                Default = true,
                RepositoryType = nameof(RepositoryTypesEnum.GitHub)
            }
            .OnExecute();

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
            Assert.Equal(RepositoryTypesEnum.GitHub, Configuration.Default.Repository);
            Assert.Null(Configuration.Default.AzureKeyVaultName);
        }


        [Fact]
        public void ConfigureAzureKVByDefaultTest()
        {
            new ConfigureCommand
            {
                Default = true,
                RepositoryType = nameof(RepositoryTypesEnum.AzureKV),
                RepositoryName = KEY_VAULT_NAME
            }
            .OnExecute();

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
            Assert.Equal(RepositoryTypesEnum.AzureKV, Configuration.Default.Repository);
            Assert.Equal(KEY_VAULT_NAME, Configuration.Default.AzureKeyVaultName);
        }


        [Fact]
        public void ConfigureGitHubForProject()
        {
            new ConfigureCommand
            {
                RepositoryType = nameof(RepositoryTypesEnum.GitHub)
            }
            .OnExecute();

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.NotNull(settings);
            Assert.Equal(RepositoryTypesEnum.GitHub, settings.Repository);
            Assert.Null(settings.AzureKeyVaultName);
        }


        [Fact]
        public void ConfigureAzureKVForProject()
        {
            new ConfigureCommand
            {
                RepositoryType = nameof(RepositoryTypesEnum.AzureKV),
                RepositoryName = KEY_VAULT_NAME
            }
            .OnExecute();

            Configuration.Refresh();

            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.NotNull(settings);
            Assert.Equal(RepositoryTypesEnum.AzureKV, settings.Repository);
            Assert.Equal(KEY_VAULT_NAME, settings.AzureKeyVaultName);
        }

    }
}
