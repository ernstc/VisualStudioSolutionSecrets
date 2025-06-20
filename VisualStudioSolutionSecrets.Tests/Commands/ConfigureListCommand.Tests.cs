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
    public class ConfigureListCommandTests : CommandTests, IClassFixture<TemporaryFolder>
    {
        private const string KEY_VAULT_NAME = "keyvault-name";

        private readonly TemporaryFolder _temporaryFolder;


        public ConfigureListCommandTests(TemporaryFolder temporaryFolder) 
            : base(temporaryFolder)
        {
            _temporaryFolder = temporaryFolder;
            
            ConfigureContext();

            MockFileSystem(
                applicationDataFolder: temporaryFolder.Path
                );
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
            RunCommand($"configure SolutionSample.sln --repo {nameof(RepositoryType.AzureKV)} --name {KEY_VAULT_NAME}");

            SyncConfiguration.Refresh();
            
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
