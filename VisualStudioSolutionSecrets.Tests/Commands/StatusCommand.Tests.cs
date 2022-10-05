using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Tests.Helpers;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class StatusCommandTests : CommandTests, IDisposable
    {

        public StatusCommandTests()
        {
            ConfigureContext();
        }


        public void Dispose()
        {
            DisposeCipherFiles();
            foreach (var directory in Directory.GetDirectories(Constants.RepositoryFilesPath))
            {
                Directory.Delete(directory, true);
            }
            foreach (var file in Directory.GetFiles(Constants.RepositoryFilesPath, "*.json"))
            {
                File.Delete(file);
            }
        }


        [Fact]
        public async Task StatusTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand().OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsPathTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Constants.SampleFilesPath
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsFilePathTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsPathAllTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Constants.SampleFilesPath,
                All = true
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsRelativePathTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = ".",
                All = false
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task Status_Synchronized_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
           .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "Synchronized"));
        }


        [Fact]
        public async Task Status_NoSecretFound_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample-WithEmptySecrets.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "No secrets found"));
        }


        [Fact]
        public async Task Status_HeaderError_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            File.WriteAllText(
                Path.Combine(Constants.RepositoryFilesPath, "secrets.json"),
                "-- Not JSON file --"
                );

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "ERROR: Header"));
        }


        [Fact]
        public async Task Status_ContentError_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            File.WriteAllText(
                Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json"),
                "-- Not JSON file --"
                );

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "ERROR: Content"));
        }


        [Fact]
        public async Task Status_LocalOnly_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "Local only"));
        }


        [Fact]
        public async Task Status_CloundOnly_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            // Fake file system for hiding local settings
            var fileSystemMock = new Mock<DefaultFileSystem>();
            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);
            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(Constants.ConfigFilesPath);
            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);
            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "Cloud only"));
        }


        [Fact]
        public async Task Status_CloudOnly_InvalidKey_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            // Fake file system for hiding local settings
            var fileSystemMock = new Mock<DefaultFileSystem>();
            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);
            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(Constants.ConfigFilesPath);
            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);
            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);

            // Invalidate the key
            await new InitCommand
            {
                Passphrase = "New" + Constants.PASSPHRASE
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "Cloud only / Invalid key"));
        }


        [Fact]
        public async Task Status_NotSynchronized_Test()
        {
            UseRepositoryEncryption = false;

            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            string filePath = Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json");
            string fileContent = File.ReadAllText(filePath);
            fileContent = fileContent.Replace("secret value", "secret value updated");
            File.WriteAllText(filePath, fileContent);

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "Not synchronized"));
        }


        [Fact]
        public async Task Status_InvalidKey_Test()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            await new InitCommand
            {
                Passphrase = "New" + Constants.PASSPHRASE
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput("status_name", l => l.Replace("{status}", "Invalid key"));
        }

    }
}
