using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Tests
{
    public class SolutionFileTests
    {


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

            Context.Create(
                fileSystemMock.Object,
                new Mock<ICipher>().Object,
                new Mock<IRepository>().Object
                );
        }


        [Fact]
        public void GetProjectsSecretConfigFiles()
        {
            CreateContext();

            var solutionFilePath = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln");
            SolutionFile solutionFile = new SolutionFile(solutionFilePath);

            var secretConfigFiles = solutionFile.GetProjectsSecretConfigFiles();

            Assert.NotNull(secretConfigFiles);
            Assert.Equal(2, secretConfigFiles.Count);
            Assert.Contains(secretConfigFiles, item => item.FileName.EndsWith(".json"));
            Assert.Contains(secretConfigFiles, item => item.FileName.EndsWith(".xml"));
            Assert.Equal(
                secretConfigFiles.ElementAt(0).GroupName,
                secretConfigFiles.ElementAt(1).GroupName
                );
        }


        [Fact]
        public void SaveConfigFile()
        {
            // Phase 1: Load config files.
            CreateContext();

            var solutionFilePath = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln");
            SolutionFile solutionFile = new SolutionFile(solutionFilePath);

            var secretConfigFiles = solutionFile.GetProjectsSecretConfigFiles();
            var configFile = secretConfigFiles.ElementAt(0);

            // Phase 2: Save the first config file found in a subfolder and check that the files has been saved.
            const string TEST_SUBFOLDER_NAME = "configFileSaveTest";
            string destinationSecretsFolderPath = $"{TEST_SUBFOLDER_NAME}{Path.DirectorySeparatorChar}c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99";

            CreateContext(TEST_SUBFOLDER_NAME);

            solutionFile.SaveConfigFile(configFile);

            string savedConfigFilePath = Path.Combine(Constants.SecretFilesPath, destinationSecretsFolderPath, configFile.FileName);
            Assert.True(File.Exists(savedConfigFilePath));

            string savedConfigFileContet = File.ReadAllText(savedConfigFilePath);
            Assert.Equal(configFile.Content, savedConfigFileContet);
        }

    }
}
