using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            RunCommand("configure list");
            VerifyOutput();
        }


        [Fact]
        public void ConfigureList_Project_Test()
        {
            RunCommand($"configure SolutionSample.sln --repo {nameof(RepositoryTypesEnum.AzureKV)} --name {KEY_VAULT_NAME}");

            Configuration.Refresh();
            
            ClearOutput();
            
            RunCommand("configure list");
            
            VerifyOutput();
        }


        [Fact]
        public void ConfigureList_WithPath_Test()
        {
            RunCommand($"configure list '{Path.Combine(Constants.SolutionFilesPath)}'");
            
            VerifyOutput();
        }


        [Fact]
        public void ConfigureList_WithPathAll_Test()
        {
            RunCommand($"configure list --all '{Path.Combine(Constants.SolutionFilesPath)}'");
            
            VerifyOutput();
        }

    }
}
