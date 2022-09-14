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
    public class ConfigFileTests
    {

        [Fact]
        public void ConfigFileEncryption()
        {
            var cipher = new Mock<ICipher>();
            var repository = new Mock<IRepository>();

            cipher.Setup(o => o.Encrypt(It.IsAny<string>())).Returns("encrypted");

            Context.Create(
                new DefaultFileSystem(),
                cipher.Object,
                repository.Object
                );

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            ConfigFile configFile = new ConfigFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            bool succes = configFile.Encrypt();
            Assert.True(succes);
            Assert.Equal("encrypted", configFile.Content);
        }


        [Fact]
        public void ConfigFileEncryptionFailed()
        {
            var cipher = new Mock<ICipher>();
            var repository = new Mock<IRepository>();

            cipher.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string)null);

            Context.Create(
                new DefaultFileSystem(),
                cipher.Object,
                repository.Object
                );

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            ConfigFile configFile = new ConfigFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            string? content = configFile.Content;
            bool success = configFile.Encrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }


        [Fact]
        public void ConfigFileDecryption()
        {
            var cipher = new Mock<ICipher>();
            var repository = new Mock<IRepository>();

            cipher.Setup(o => o.Decrypt(It.IsAny<string>())).Returns("decrypted");

            Context.Create(
                new DefaultFileSystem(),
                cipher.Object,
                repository.Object
                );

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            ConfigFile configFile = new ConfigFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            bool success = configFile.Decrypt();
            Assert.True(success);
            Assert.Equal("decrypted", configFile.Content);
        }


        [Fact]
        public void ConfigFileDecryptionFailed()
        {
            var cipher = new Mock<ICipher>();
            var repository = new Mock<IRepository>();

            cipher.Setup(o => o.Encrypt(It.IsAny<string>())).Returns((string)null);

            Context.Create(
                new DefaultFileSystem(),
                cipher.Object,
                repository.Object
                );

            string configFilePath = Path.Combine(Constants.ConfigFilesPath, "configFile.json");

            ConfigFile configFile = new ConfigFile(configFilePath, "uniqueConfigFile.json", Context.Current.Cipher);
            string? content = configFile.Content;
            bool success = configFile.Decrypt();
            Assert.False(success);
            Assert.Equal(content, configFile.Content);
        }
    }
}
