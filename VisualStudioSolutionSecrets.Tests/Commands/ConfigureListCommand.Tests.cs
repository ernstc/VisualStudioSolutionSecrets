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

    public class ConfigureListCommandTests : CommandTests, IDisposable
    {
        private const string KEY_VAULT_NAME = "keyvault-name";


        public ConfigureListCommandTests()
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
        public void ConfigureList_None_Test()
        {
            CommandLineApplication.Execute<ConfigureListCommand>();

            VerifyOutput();
        }


        [Fact]
        public void ConfigureList_Project_Test()
        {
            CommandLineApplication.Execute<ConfigureCommand>(
                "SolutionSample.sln",
                "--repo", nameof(RepositoryTypesEnum.AzureKV),
                "--name", KEY_VAULT_NAME
            );

            Configuration.Refresh();

            ClearOutput();

            CommandLineApplication.Execute<ConfigureListCommand>();

            VerifyOutput();
        }

    }
}
