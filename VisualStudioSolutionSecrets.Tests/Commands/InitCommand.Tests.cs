﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Tests.Helpers;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class InitCommandTests : CommandTests, IDisposable
    {

        private string _generatedFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
        private string _sampleFilePath = Path.Combine(Constants.ConfigFilesPath, "sample.cipher.json");


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
            ConfigureContext();

            await CallCommand.Init(new InitOptions
            {
                Passphrase = "Passphrase.1"
            });

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllText(_sampleFilePath), File.ReadAllText(_generatedFilePath));
        }


        [Fact]
        public async void InitWithKeyFileTest()
        {
            ConfigureContext();

            await CallCommand.Init(new InitOptions
            {
                KeyFile = "initFile.key"
            });

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllText(_sampleFilePath), File.ReadAllText(_generatedFilePath));
        }
        
    }
}
