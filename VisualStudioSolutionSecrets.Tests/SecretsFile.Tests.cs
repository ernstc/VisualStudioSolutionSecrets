using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;
using Xunit;


namespace VisualStudioSolutionSecrets.Tests
{
    public class SecretsFileTests
    {

        public SecretsFileTests()
        {
            // Mock dependencies
            var repositoryMock = new Mock<IRepository>();
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);

            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);

            // Configure mocked dependencies
            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
            Context.Current.AddService<IRepository>(repositoryMock.Object);
        }


        [Fact]
        public void SecretsFile_Encryption_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Encrypt(It.IsAny<string>())).Returns("encrypted");

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretFile configFile = new SecretFile(configFilePath, "uniqueConfigFile.json");
            bool succes = configFile.Encrypt();
            Assert.True(succes);
            Assert.Equal("encrypted", configFile.Content);
        }


        [Fact]
        public void SecretsFile_EncryptionFailed_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string?)null);

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretFile configFile = new SecretFile(configFilePath, "uniqueConfigFile.json");
            string? content = configFile.Content;
            bool success = configFile.Encrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }


        [Fact]
        public void SecretsFile_Decryption_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Decrypt(It.IsAny<string>())).Returns("decrypted");

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretFile configFile = new SecretFile(configFilePath, "uniqueConfigFile.json");
            bool success = configFile.Decrypt();
            Assert.True(success);
            Assert.Equal("decrypted", configFile.Content);
        }


        [Fact]
        public void SecretsFile_DecryptionFailed_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string?)null);

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretFile configFile = new SecretFile(configFilePath, "uniqueConfigFile.json");
            string? content = configFile.Content;
            bool success = configFile.Decrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }


        [Fact]
        public void SecretsFile_JsonEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "6030c231-73da-4823-adb8-5b919180a7ac", "secrets.json");
            SecretFile configFile = new SecretFile(filePath, "uniqueConfigFile.json");
            Assert.Null(configFile.Content);
        }


        [Fact]
        public void SecretsFile_XmlEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "6030c231-73da-4823-adb8-5b919180a7ac", "secrets.xml");
            SecretFile configFile = new SecretFile(filePath, "uniqueConfigFile.json");
            Assert.Null(configFile.Content);
        }


        [Fact]
        public void SecretsFile_JsonNotEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.json");
            SecretFile configFile = new SecretFile(filePath, "uniqueConfigFile.json");
            Assert.NotNull(configFile.Content);
        }


        [Fact]
        public void SecretsFile_XmlNotEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.xml");
            SecretFile configFile = new SecretFile(filePath, "uniqueConfigFile.json");
            Assert.NotNull(configFile.Content);
        }

    }
}
