using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Repository;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    [Collection("vs-secrets Tests")]
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
        public void Configure_InvalidParams_1_Test()
        {
            var result = RunCommand("configure --default --reset");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_2_Test()
        {
            var result = RunCommand("configure --repo unknown");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_3_Test()
        {
            var result = RunCommand($"configure --default --repo {nameof(RepositoryType.GitHub)} --name {KEY_VAULT_NAME}");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_4_Test()
        {
            var result = RunCommand($"configure --repo {nameof(RepositoryType.GitHub)} --name {KEY_VAULT_NAME}");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_5_Test()
        {
            var result = RunCommand($"configure --name {KEY_VAULT_NAME}");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_6_Test()
        {
            var result = RunCommand("configure --default --path path");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_GitHubByDefault_Test()
        {
            var result = RunCommand($"configure --default --repo {nameof(RepositoryType.GitHub)}");

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            SyncConfiguration.Refresh();

            Assert.Equal(RepositoryType.GitHub, SyncConfiguration.Default.Repository);
            Assert.Null(SyncConfiguration.Default.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_AzureKVByDefault_1_Test()
        {
            var result = RunCommand($"configure --default --repo {nameof(RepositoryType.AzureKV)} --name {KEY_VAULT_NAME}");

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            SyncConfiguration.Refresh();

            Assert.Equal(RepositoryType.AzureKV, SyncConfiguration.Default.Repository);
            Assert.Equal(KEY_VAULT_NAME, SyncConfiguration.Default.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_AzureKVByDefault_2_Test()
        {
            var result = RunCommand($"configure --default --repo {nameof(RepositoryType.AzureKV)} --name https://my-domain.com");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_GitHubForProject_Test()
        {
            var result = RunCommand($"configure --repo {nameof(RepositoryType.GitHub)}");

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            SyncConfiguration.Refresh();

            var settings = SyncConfiguration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.NotNull(settings);
            Assert.Equal(RepositoryType.GitHub, settings.Repository);
            Assert.Null(settings.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_GitHubForProject_WithoutGuid_Test()
        {
            var result = RunCommand($"configure SolutionSample-WithoutGuid.sln --repo {nameof(RepositoryType.GitHub)}");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_AzureKVForProject_1_Test()
        {
            var result = RunCommand($"configure --repo {nameof(RepositoryType.AzureKV)} --name {KEY_VAULT_NAME}");

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            SyncConfiguration.Refresh();

            var settings = SyncConfiguration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.NotNull(settings);
            Assert.Equal(RepositoryType.AzureKV, settings.Repository);
            Assert.Equal(KEY_VAULT_NAME, settings.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_AzureKVForProject_2_Test()
        {
            var result = RunCommand($"configure SolutionSample.sln --repo {nameof(RepositoryType.AzureKV)} --name http://my-domain.com");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_AzureKVForProject_WithoutGuid_Test()
        {
            var result = RunCommand($"configure SolutionSample-WithoutGuid.sln --repo {nameof(RepositoryType.AzureKV)} --name {KEY_VAULT_NAME}");

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_Reset_Test()
        {
            RunCommand($"configure SolutionSample.sln --repo {nameof(RepositoryType.AzureKV)} --name {KEY_VAULT_NAME}");

            var result = RunCommand("configure SolutionSample.sln --reset");

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            var settings = SyncConfiguration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.Null(settings);
        }

    }
}
