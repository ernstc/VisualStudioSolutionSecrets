using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
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
        public void Configure_InvalidParams_1_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--default",
                "--reset"
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_2_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--repo", "unknown"
                );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_3_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--default",
                "--repo", nameof(RepositoryTypesEnum.GitHub),
                "--name", KEY_VAULT_NAME
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_4_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--repo", nameof(RepositoryTypesEnum.GitHub),
                "--name", KEY_VAULT_NAME
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_5_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--name", KEY_VAULT_NAME
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_InvalidParams_6_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--default",
                "--path", "path"
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_GitHubByDefault_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--default",
                "--repo", nameof(RepositoryTypesEnum.GitHub)
            );

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            Configuration.Refresh();

            Assert.Equal(RepositoryTypesEnum.GitHub, Configuration.Default.Repository);
            Assert.Null(Configuration.Default.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_AzureKVByDefault_1_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--default",
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", KEY_VAULT_NAME
            );

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            Configuration.Refresh();

            Assert.Equal(RepositoryTypesEnum.AzureKV, Configuration.Default.Repository);
            Assert.Equal(KEY_VAULT_NAME, Configuration.Default.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_AzureKVByDefault_2_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--default",
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", "https://my-domain.com"
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_GitHubForProject_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--repo", nameof(RepositoryTypesEnum.GitHub)
            );

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            Configuration.Refresh();

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.NotNull(settings);
            Assert.Equal(RepositoryTypesEnum.GitHub, settings.Repository);
            Assert.Null(settings.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_GitHubForProject_WithoutGuid_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "SolutionSample-WithoutGuid.sln",
                "--repo", nameof(RepositoryTypesEnum.GitHub)
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_AzureKVForProject_1_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", KEY_VAULT_NAME
            );

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            Configuration.Refresh();

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.NotNull(settings);
            Assert.Equal(RepositoryTypesEnum.AzureKV, settings.Repository);
            Assert.Equal(KEY_VAULT_NAME, settings.AzureKeyVaultName);
        }


        [Fact]
        public void Configure_AzureKVForProject_2_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "SolutionSample.sln",
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", "http://my-domain.com"
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_AzureKVForProject_WithoutGuid_Test()
        {
            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "SolutionSample-WithoutGuid.sln",
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", KEY_VAULT_NAME
            );

            Assert.Equal(1, result);
            Assert.False(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));
        }


        [Fact]
        public void Configure_Reset_Test()
        {
            CommandLineApplication.Execute<ConfigureCommand>(
                "SolutionSample.sln",
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", KEY_VAULT_NAME
            );

            var result = CommandLineApplication.Execute<ConfigureCommand>(
                "SolutionSample.sln",
                "--reset"
            );

            Assert.Equal(0, result);
            Assert.True(File.Exists(Path.Combine(Constants.ConfigFilesPath, "configuration.json")));

            var settings = Configuration.GetCustomSynchronizationSettings(new Guid(SOLUTION_GUID));

            Assert.Null(settings);
        }

    }
}
