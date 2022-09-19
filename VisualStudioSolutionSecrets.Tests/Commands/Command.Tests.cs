using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;
using VisualStudioSolutionSecrets.Tests.Helpers;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class CommandTests
    {
        protected void ConfigureContext()
        {
            // Suppress output on standart out
            Console.SetOut(new StringWriter());

            // Mock dependencies
            IFileSystem fileSystem = MockFileSystem();
            IRepository repository = MockRepository();

            // Configure mocked dependencies
            Context.Configure(context =>
            {
                context.IO = fileSystem;
                context.Repository = repository;
                context.Cipher = new Cipher();
            });
        }


        protected async Task InitializeCipher()
        {
            await CallCommand.Init(new InitOptions { Passphrase = Constants.PASSPHRASE });
        }


        protected void DisposeCipherFiles()
        {
            string cipherFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
            if (File.Exists(cipherFilePath))
            {
                File.Delete(cipherFilePath);
            }
        }


        private static IFileSystem MockFileSystem()
        {
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.ConfigFilesPath);

            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(Constants.SecretFilesPath);

            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(Constants.SolutionFilesPath);

            return fileSystemMock.Object;
        }


        private static IRepository MockRepository()
        {
            var repository = new Mock<IRepository>();

            repository
                .Setup(o => o.IsReady())
                .ReturnsAsync(true);

            repository
                .Setup(o => o.PushFilesAsync(It.IsAny<ICollection<(string name, string? content)>>()))
                .ReturnsAsync((ICollection<(string name, string? content)> collection) =>
                {
                    foreach (var item in collection)
                    {
                        string fileName = item.name;
                        if (!fileName.EndsWith(".json")) fileName = fileName + ".json";
                        string filePath = Path.Combine(Constants.RepositoryFilesPath, fileName);
                        var fileInfo = new FileInfo(filePath);
                        Directory.CreateDirectory(fileInfo.DirectoryName!);
                        File.WriteAllText(filePath, item.content);
                    }
                    return true;
                });

            repository
                .Setup(o => o.PullFilesAsync())
                .ReturnsAsync(() =>
                {
                    List<(string name, string? content)> files = new();
                    string[] filesPath = Directory.GetFiles(Constants.RepositoryFilesPath, "*.json", SearchOption.AllDirectories);
                    foreach (var filePath in filesPath)
                    {
                        string fileName = new FileInfo(filePath).Name;
                        if (!fileName.StartsWith("secrets")) fileName = "secrets\\" + fileName;
                        files.Add((fileName, File.ReadAllText(filePath)));
                    }
                    return files;
                });

            return repository.Object;
        }

    }
}
