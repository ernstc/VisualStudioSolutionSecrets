using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.IO;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class ChangeKeyCommandTests : CommandTests, IDisposable
    {

        private const string NEW_PASSPHRASE = "New" + Constants.PASSPHRASE;
        private readonly string PulledSecretsFilesPath = Constants.SecretFilesPath + "_pulled";


        public ChangeKeyCommandTests()
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
            foreach (var file in Directory.GetFiles(Constants.RepositoryFilesPath))
            {
                if (file.EndsWith(".json")) File.Delete(file);
            }
            if (Directory.Exists(PulledSecretsFilesPath))
            {
                Directory.Delete(PulledSecretsFilesPath, true);
            }
        }


        private void ChangeSecretsFilesPath()
        {
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);

            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(PulledSecretsFilesPath);

            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);

            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
        }


        private void VerifyTestResults()
        {
            string originalFilePath1 = Path.Combine(Constants.SecretFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.json");
            string originalFilePath2 = Path.Combine(Constants.SecretFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.xml");

            string pulledFilePath1 = Path.Combine(PulledSecretsFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.json");
            string pulledFilePath2 = Path.Combine(PulledSecretsFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.xml");

            Assert.True(File.Exists(pulledFilePath1));
            Assert.True(File.Exists(pulledFilePath2));

            Assert.Equal(File.ReadAllText(originalFilePath1), File.ReadAllText(pulledFilePath1));
            Assert.Equal(File.ReadAllText(originalFilePath2), File.ReadAllText(pulledFilePath2));
        }


        private static async Task PrepareTest()
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
        }


        [Fact]
        public async Task ChangeKeyWithoutParameters()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            string encryptionKeyFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
            string encryptionKey = File.ReadAllText(encryptionKeyFilePath);

            await new ChangeKeyCommand().OnExecute();

            Assert.Equal(encryptionKey, File.ReadAllText(encryptionKeyFilePath));
        }


        [Fact]
        public async Task ChangeKeyWithPassphrase()
        {
            await PrepareTest();

            await new ChangeKeyCommand
            {
                Passphrase = NEW_PASSPHRASE
            }
            .OnExecute();

            ChangeSecretsFilesPath();

            await new PullCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            VerifyTestResults();
        }


        [Fact]
        public async Task ChangeKeyWithKeyFile()
        {
            await PrepareTest();

            await new ChangeKeyCommand
            {
                KeyFile = Path.Combine(Constants.TestFilesPath, "initFile2.key")
            }
            .OnExecute();

            ChangeSecretsFilesPath();

            await new PullCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            VerifyTestResults();
        }


        [Fact]
        public async Task ChangeKeyWithKeyFileWithRelativePath()
        {
            await PrepareTest();

            await new ChangeKeyCommand
            {
                KeyFile = "initFile2.key"
            }
            .OnExecute();

            ChangeSecretsFilesPath();

            await new PullCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            VerifyTestResults();
        }

    }
}
