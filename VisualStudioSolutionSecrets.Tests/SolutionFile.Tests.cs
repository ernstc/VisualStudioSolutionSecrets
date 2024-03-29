﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.IO;
using Xunit;


namespace VisualStudioSolutionSecrets.Tests
{
    [Collection("vs-secrets Tests")]
    public class SolutionFileTests : IDisposable
    {

        const string TEST_SUBFOLDER_NAME = "configFileSaveTest";


        public void Dispose()
        {
            string testFolderPath = Path.Combine(Constants.SecretFilesPath, TEST_SUBFOLDER_NAME);
            if (Directory.Exists(testFolderPath))
            {
                Directory.Delete(testFolderPath, true);
            }
        }


        private void CreateContext(string? secretsSubFolderPath = null)
        {
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.SampleFilesPath);

            string secretsFilesPath = Constants.SecretFilesPath;
            if (secretsSubFolderPath != null)
                secretsFilesPath = Path.Combine(secretsFilesPath, secretsSubFolderPath);

            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(secretsFilesPath);

            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
        }


        [Fact]
        public void GetProjectsSecretConfigFiles_Test()
        {
            CreateContext(secretsSubFolderPath: null);

            var solutionFilePath = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln");
            SolutionFile solutionFile = new SolutionFile(solutionFilePath);

            var secretConfigFiles = solutionFile.GetProjectsSecretFiles();

            Assert.NotNull(secretConfigFiles);
            Assert.Equal(2, secretConfigFiles.Count);
            Assert.Contains(secretConfigFiles, item => item.Name.EndsWith(".json"));
            Assert.Contains(secretConfigFiles, item => item.Name.EndsWith(".xml"));
            Assert.Equal(
                secretConfigFiles.ElementAt(0).ContainerName,
                secretConfigFiles.ElementAt(1).ContainerName
                );
        }


        [Fact]
        public void SaveConfigFile_Test()
        {
            // Phase 1: Load config files.
            CreateContext(secretsSubFolderPath: null);

            var solutionFilePath = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln");
            SolutionFile solutionFile = new SolutionFile(solutionFilePath);

            var secretConfigFiles = solutionFile.GetProjectsSecretFiles();
            var configFile = secretConfigFiles.ElementAt(0);

            // Phase 2: Save the first config file found in a subfolder and check that the files has been saved.
            string destinationSecretsFolderPath = $"{TEST_SUBFOLDER_NAME}{Path.DirectorySeparatorChar}c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99";

            CreateContext(secretsSubFolderPath: TEST_SUBFOLDER_NAME);

            solutionFile.SaveSecretSettingsFile(configFile);

            string savedConfigFilePath = Path.Combine(Constants.SecretFilesPath, destinationSecretsFolderPath, configFile.Name);
            Assert.True(File.Exists(savedConfigFilePath));

            string savedConfigFileContet = File.ReadAllText(savedConfigFilePath);
            Assert.Equal(configFile.Content, savedConfigFileContet);
        }

    }
}
