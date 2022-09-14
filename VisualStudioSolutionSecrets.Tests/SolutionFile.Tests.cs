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
            var defaultFileSystem = new DefaultFileSystem();
            var fileSystemMock = new Mock<IFileSystem>();

            fileSystemMock
                .Setup(o => o.GetFileInfo(It.IsAny<string>()))
                .Returns((string path) => defaultFileSystem.GetFileInfo(path));

            fileSystemMock
                .Setup(o => o.FileReadAllLines(It.IsAny<string>()))
                .Returns((string path) => defaultFileSystem.FileReadAllLines(path));

            fileSystemMock
                .Setup(o => o.FileReadAllText(It.IsAny<string>()))
                .Returns((string path) => defaultFileSystem.FileReadAllText(path));

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.SampleFilesPath);

            fileSystemMock
                .Setup(o => o.GetUserProfileFolderPath())
                .Returns(Constants.SampleFilesPath);

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
        }


        [Fact]
        public void SaveConfigFile()
        {
            Assert.Fail("Not implemented.");
        }

    }
}
