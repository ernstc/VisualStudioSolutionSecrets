using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;

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

            Context.Configure(context =>
            {
                context.IO = fileSystem;
                context.Repository = repository;
                context.Cipher = new Cipher();
            });
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
                        string filePath = Path.Combine(Constants.RepositoryFilesPath, item.name);
                        File.WriteAllText(filePath, item.content);
                    }
                    return true;
                });

            repository
                .Setup(o => o.PullFilesAsync())
                .ReturnsAsync(() =>
                {
                    List<(string name, string? content)> files = new();
                    string[] filesPath = Directory.GetFiles(Constants.RepositoryFilesPath);
                    foreach (var filePath in filesPath)
                    {
                        files.Add((new FileInfo(filePath).Name, File.ReadAllText(filePath)));
                    }
                    return files;
                });

            return repository.Object;
        }

    }
}
