using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.IO;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    [Collection("vs-secrets Tests")]
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


        private void PrepareTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Constants.SolutionFilesPath}'");
        }


        [Fact]
        public void ChangeKeyWithoutParameters()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");

            string encryptionKeyFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
            string encryptionKey = File.ReadAllText(encryptionKeyFilePath);

            RunCommand("change-key");

            Assert.Equal(encryptionKey, File.ReadAllText(encryptionKeyFilePath));
        }


        [Fact]
        public void ChangeKeyWithPassphrase()
        {
            PrepareTest();

            RunCommand($"change-key -p {NEW_PASSPHRASE}");

            ChangeSecretsFilesPath();

            RunCommand($"pull '{Constants.SolutionFilesPath}'");

            VerifyTestResults();
        }


        [Fact]
        public void ChangeKeyWithKeyFile()
        {
            PrepareTest();

            RunCommand($"change-key -f '{Path.Combine(Constants.TestFilesPath, "initFile2.key")}'");

            ChangeSecretsFilesPath();

            RunCommand($"pull '{Constants.SolutionFilesPath}'");

            VerifyTestResults();
        }


        [Fact]
        public void ChangeKeyWithKeyFileWithRelativePath()
        {
            PrepareTest();

            RunCommand("change-key -f initFile2.key");

            ChangeSecretsFilesPath();

            RunCommand($"pull '{Constants.SolutionFilesPath}'");

            VerifyTestResults();
        }

    }
}
