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
            var repository = new Mock<IRepository>();
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);

            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);

            // Configure mocked dependencies
            Context.Configure(context =>
            {
                context.IO = fileSystemMock.Object;
                context.Repository = repository.Object;
            });
        }


        [Fact]
        public void SecretsSettingsFileEncryptionTest()
        {
            var cipher = new Mock<ICipher>();
            cipher.Setup(o => o.Encrypt(It.IsAny<string>())).Returns("encrypted");

            Context.Configure(context =>
            {
                context.Cipher = cipher.Object;
            });

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretsSettingsFile configFile = new SecretsSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            bool succes = configFile.Encrypt();
            Assert.True(succes);
            Assert.Equal("encrypted", configFile.Content);
        }


        [Fact]
        public void SecretsSettingsFileEncryptionFailedTest()
        {
            var cipher = new Mock<ICipher>();
            cipher.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string?)null);

            Context.Configure(context =>
            {
                context.Cipher = cipher.Object;
            });

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretsSettingsFile configFile = new SecretsSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            string? content = configFile.Content;
            bool success = configFile.Encrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }


        [Fact]
        public void SecretsSettingsFileDecryptionTest()
        {
            var cipher = new Mock<ICipher>();
            cipher.Setup(o => o.Decrypt(It.IsAny<string>())).Returns("decrypted");

            Context.Configure(context =>
            {
                context.Cipher = cipher.Object;
            });

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretsSettingsFile configFile = new SecretsSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            bool success = configFile.Decrypt();
            Assert.True(success);
            Assert.Equal("decrypted", configFile.Content);
        }


        [Fact]
        public void SecretsSettingsFileDecryptionFailedTest()
        {
            var cipher = new Mock<ICipher>();
            cipher.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string?)null);

            Context.Configure(context =>
            {
                context.Cipher = cipher.Object;
            });

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            SecretsSettingsFile configFile = new SecretsSettingsFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            string? content = configFile.Content;
            bool success = configFile.Decrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }

    }
}
