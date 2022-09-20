using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Tests.Helpers;

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
        public async void InitWithPassphraseTest()
        {
            await CallCommand.Init(new InitOptions
            {
                Passphrase = Constants.PASSPHRASE
            });

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllText(_sampleFilePath), File.ReadAllText(_generatedFilePath));
        }


        [Fact]
        public async void InitWithKeyFileTest()
        {
            await CallCommand.Init(new InitOptions
            {
                KeyFile = Path.Combine(Constants.TestFilesPath, "initFile.key")
            });

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllText(_sampleFilePath), File.ReadAllText(_generatedFilePath));
        }


        [Fact]
        public async void InitWithKeyFileWithRelativePathTest()
        {
            await CallCommand.Init(new InitOptions
            {
                KeyFile = Path.Combine("..", "testFiles", "initFile.key")
            });

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllText(_sampleFilePath), File.ReadAllText(_generatedFilePath));
        }
        
    }
}
