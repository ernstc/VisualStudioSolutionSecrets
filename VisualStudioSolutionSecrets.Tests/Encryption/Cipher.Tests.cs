﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Encryption
{
    public sealed class CipherTests : IDisposable
    {

        private readonly string _generatedFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
        private readonly string _sampleFilePath = Path.Combine(Constants.ConfigFilesPath, "sample.cipher.json");


        public CipherTests()
        {
            // Mock dependencies
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);

            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);

            // Configure mocked dependencies
            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
        }


        public void Dispose()
        {
            if (File.Exists(_generatedFilePath))
            {
                File.Delete(_generatedFilePath);
            }
        }


        [Fact]
        public void InitWithPassphrase_Test()
        {
            var cipher = new Cipher();
            cipher.Init(Constants.PASSPHRASE);

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public void InitWithFile_Test()
        {
            var cipher = new Cipher();

            string keyFile = Path.Combine(Constants.TestFilesPath, "initFile.key");
            cipher.Init(File.OpenRead(keyFile));

            Assert.True(File.Exists(_generatedFilePath));
            Assert.Equal(File.ReadAllLines(_sampleFilePath), File.ReadAllLines(_generatedFilePath));
        }


        [Fact]
        public void Encryption_Decryption_Test()
        {
            const string sample = "plain text";

            var cipher1 = new Cipher();
            cipher1.Init(Constants.PASSPHRASE);
            string? encryptedText = cipher1.Encrypt(sample);

            Assert.NotNull(encryptedText);

            var cipher2 = new Cipher();
            cipher2.Init(Constants.PASSPHRASE);
            string? decryptedText = cipher2.Decrypt(encryptedText);

            Assert.NotNull(decryptedText);

            Assert.Equal(sample, decryptedText);
        }

    }
}
