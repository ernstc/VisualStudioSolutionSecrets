using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
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
        public void InitWithoutParametersTest()
        {
            CommandLineApplication.Execute<InitCommand>();

            Assert.False(File.Exists(_generatedFilePath));
        }


        [Fact]
        public void InitWithPassphraseTest()
        {
            CommandLineApplication.Execute<InitCommand>(
                "-p", Constants.PASSPHRASE
                );

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public void InitWithKeyFileTest()
        {
            CommandLineApplication.Execute<InitCommand>(
                "-f", Path.Combine(Constants.TestFilesPath, "initFile.key")
                );

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public void InitWithKeyFileWithRelativePathTest()
        {
            int result = CommandLineApplication.Execute<InitCommand>(
                "-f", Path.Combine("..", "testFiles", "initFile.key")
                );

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }
        
    }
}
