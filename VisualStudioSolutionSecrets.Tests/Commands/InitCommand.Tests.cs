using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
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
        public async Task InitWithoutParametersTest()
        {
            await new InitCommand().OnExecute();

            Assert.False(File.Exists(_generatedFilePath));
        }


        [Fact]
        public async Task InitWithPassphraseTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public async Task InitWithKeyFileTest()
        {
            await new InitCommand
            {
                KeyFile = Path.Combine(Constants.TestFilesPath, "initFile.key")
            }
            .OnExecute();

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public async Task InitWithKeyFileWithRelativePathTest()
        {
            await new InitCommand
            {
                KeyFile = Path.Combine("..", "testFiles", "initFile.key")
            }
            .OnExecute();

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }
        
    }
}
