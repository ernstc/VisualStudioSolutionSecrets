﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.IO;


namespace VisualStudioSolutionSecrets.Tests.Commands
{

    public class PullSecretsCommandTests : CommandTests, IDisposable
    {

        private readonly string PulledSecretsFilesPath = Constants.SecretFilesPath + "_pulled";


        public PullSecretsCommandTests()
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

            ChangeSecretsFilesPath();
        }


        [Fact]
        public void PullPathTest()
        {
            PrepareTest();

            RunCommand($"pull '{Constants.SolutionFilesPath}'");

            VerifyTestResults();
        }


        [Fact]
        public void PullRelativePathTest()
        {
            PrepareTest();

            RunCommand("pull .");

            VerifyTestResults();
        }


        [Fact]
        public void PullPathWithoutSolutionTest()
        {
            PrepareTest();

            RunCommand("pull unknown");

            string pulledFilePath1 = Path.Combine(PulledSecretsFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.json");
            string pulledFilePath2 = Path.Combine(PulledSecretsFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.xml");

            Assert.False(File.Exists(pulledFilePath1));
            Assert.False(File.Exists(pulledFilePath2));
        }


        [Fact]
        public void PullAllWithinPathTest()
        {
            PrepareTest();

            RunCommand($"pull --all '{Constants.SampleFilesPath}'");

            VerifyTestResults();
        }

    }
}
