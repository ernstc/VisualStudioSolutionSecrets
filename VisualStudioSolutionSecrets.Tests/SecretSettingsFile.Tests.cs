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
    public class SecretsSettingsFileTests
    {

        public SecretsSettingsFileTests()
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
        public void SecretSettingsFile_Encryption_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Encrypt(It.IsAny<string>())).Returns("encrypted");

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretSettingsFile configFile = new SecretSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            bool succes = configFile.Encrypt();
            Assert.True(succes);
            Assert.Equal("encrypted", configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_EncryptionFailed_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string?)null);

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretSettingsFile configFile = new SecretSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            string? content = configFile.Content;
            bool success = configFile.Encrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_Decryption_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Decrypt(It.IsAny<string>())).Returns("decrypted");

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretSettingsFile configFile = new SecretSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            bool success = configFile.Decrypt();
            Assert.True(success);
            Assert.Equal("decrypted", configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_DecryptionFailed_Test()
        {
            var cipherMock = new Mock<ICipher>();
            cipherMock.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string?)null);

            Context.Current.AddService<ICipher>(cipherMock.Object);

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretSettingsFile configFile = new SecretSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            string? content = configFile.Content;
            bool success = configFile.Decrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_JsonEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "6030c231-73da-4823-adb8-5b919180a7ac", "secrets.json");
            SecretSettingsFile configFile = new SecretSettingsFile(filePath, "uniqueConfigFile.json", Context.Current.Cipher);
            Assert.Null(configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_XmlEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "6030c231-73da-4823-adb8-5b919180a7ac", "secrets.xml");
            SecretSettingsFile configFile = new SecretSettingsFile(filePath, "uniqueConfigFile.json", Context.Current.Cipher);
            Assert.Null(configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_JsonNotEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.json");
            SecretSettingsFile configFile = new SecretSettingsFile(filePath, "uniqueConfigFile.json", Context.Current.Cipher);
            Assert.NotNull(configFile.Content);
        }


        [Fact]
        public void SecretSettingsFile_XmlNotEmpy_Test()
        {
            string filePath = Path.Combine(Constants.SecretFilesPath, "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99", "secrets.xml");
            SecretSettingsFile configFile = new SecretSettingsFile(filePath, "uniqueConfigFile.json", Context.Current.Cipher);
            Assert.NotNull(configFile.Content);
        }

    }
}
