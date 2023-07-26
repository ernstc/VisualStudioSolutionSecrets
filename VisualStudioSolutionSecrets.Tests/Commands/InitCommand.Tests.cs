using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    [Collection("vs-secrets Tests")]
    public class InitCommandTests : CommandTests, IDisposable
    {

        private readonly string _generatedFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
        private readonly string _sampleFilePath = Path.Combine(Constants.ConfigFilesPath, "sample.cipher.json");


        public InitCommandTests()
        {
            ConfigureContext();
        }


        public void Dispose()
        {
            if (File.Exists(_generatedFilePath))
            {
                File.Delete(_generatedFilePath);
            }
        }


        [Fact]
        public void InitWithoutParametersTest()
        {
            RunCommand("init");

            Assert.False(File.Exists(_generatedFilePath));
        }


        [Fact]
        public void InitWithPassphraseTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public void InitWithKeyFileTest()
        {
            RunCommand($"init -f '{Path.Combine(Constants.TestFilesPath, "initFile.key")}'");

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public void InitWithKeyFileWithRelativePathTest()
        {
            RunCommand($"init -f '{Path.Combine("..", "testFiles", "initFile.key")}'");

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }
        
    }
}
