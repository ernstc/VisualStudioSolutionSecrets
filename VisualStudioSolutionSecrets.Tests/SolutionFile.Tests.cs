using System;
using System.Collections.Generic;
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

        private IFileSystem _fileSystem;

        public SolutionFileTests()
        {
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.SampleFilesPath);

            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(Constants.SecretFilesPath);

            _fileSystem = fileSystemMock.Object;
        }


        [Fact]
        public void GetProjectsSecretConfigFiles()
        {
            Context.Create(
                _fileSystem,
                new Mock<ICipher>().Object,
                new Mock<IRepository>().Object
                );

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
            Assert.Fail("Not implemented.");
        }

    }
}
