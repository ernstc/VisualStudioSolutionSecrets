using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
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


        private static void PrepareTest()
        {
            CommandLineApplication.Execute<InitCommand>(
                "-p", Constants.PASSPHRASE
                );

            CommandLineApplication.Execute<PushCommand>(
                Constants.SolutionFilesPath
                );
        }


        [Fact]
        public void ChangeKeyWithoutParameters()
        {
            CommandLineApplication.Execute<InitCommand>(
                "-p", Constants.PASSPHRASE
            );

            string encryptionKeyFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
            string encryptionKey = File.ReadAllText(encryptionKeyFilePath);

            CommandLineApplication.Execute<ChangeKeyCommand>();

            Assert.Equal(encryptionKey, File.ReadAllText(encryptionKeyFilePath));
        }


        [Fact]
        public void ChangeKeyWithPassphrase()
        {
            PrepareTest();

            CommandLineApplication.Execute<ChangeKeyCommand>(
                "-p", NEW_PASSPHRASE
                );

            ChangeSecretsFilesPath();

            CommandLineApplication.Execute<PullCommand>(
                Constants.SolutionFilesPath
                );

            VerifyTestResults();
        }


        [Fact]
        public void ChangeKeyWithKeyFile()
        {
            PrepareTest();

            CommandLineApplication.Execute<ChangeKeyCommand>(
                "-f", Path.Combine(Constants.TestFilesPath, "initFile2.key")
                );

            ChangeSecretsFilesPath();

            CommandLineApplication.Execute<PullCommand>(
                Constants.SolutionFilesPath
                );

            VerifyTestResults();
        }


        [Fact]
        public void ChangeKeyWithKeyFileWithRelativePath()
        {
            PrepareTest();

            CommandLineApplication.Execute<ChangeKeyCommand>(
                "-f", "initFile2.key"
                );

            ChangeSecretsFilesPath();

            CommandLineApplication.Execute<PullCommand>(
                Constants.SolutionFilesPath
                );

            VerifyTestResults();
        }

    }
}
