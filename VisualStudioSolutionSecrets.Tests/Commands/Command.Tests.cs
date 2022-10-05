﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class CommandTests
    {

        // Here you will find the console output generated by the test.
        private StringBuilder _consoleOutput = new StringBuilder();


        protected string Output => _consoleOutput.ToString();


        protected void ConfigureContext()
        {
            _consoleOutput.Clear();

            // Suppress output on standart out
            Console.SetOut(new StringWriter(_consoleOutput));

            // Mock dependencies
            IFileSystem fileSystem = MockFileSystem();
            IRepository repository = MockRepository();

            // Configure mocked dependencies
            Context.Current.AddService<IFileSystem>(fileSystem);
            Context.Current.AddService<IRepository>(repository);
            Context.Current.AddService<IRepository>(repository, nameof(RepositoryTypesEnum.AzureKV));
            Context.Current.AddService<IRepository>(repository, nameof(RepositoryTypesEnum.GitHub));
            Context.Current.AddService<ICipher>(new Cipher());

            Configuration.Refresh();
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
                .Setup(o => o.EncryptOnClient)
                .Returns(true);

            repository
                .Setup(o => o.RepositoryType)
                .Returns("Mock");

            repository
               .Setup(o => o.RepositoryName)
               .Returns("Name");

            repository
                .Setup(o => o.IsReady())
                .ReturnsAsync(true);

            repository
                .Setup(o => o.PushFilesAsync(It.IsAny<ISolution>(), It.IsAny<ICollection<(string name, string? content)>>()))
                .ReturnsAsync((ISolution _, ICollection<(string name, string? content)> collection) =>
                {
                    foreach (var item in collection)
                    {
                        string fileName = item.name;
                        if (!fileName.EndsWith(".json")) fileName += ".json";
                        string filePath = Path.Combine(Constants.RepositoryFilesPath, fileName.Replace('\\', Path.DirectorySeparatorChar));
                        var fileInfo = new FileInfo(filePath);
                        Directory.CreateDirectory(fileInfo.DirectoryName!);
                        File.WriteAllText(filePath, item.content);
                    }
                    return true;
                });

            repository
                .Setup(o => o.PullFilesAsync(It.IsAny<ISolution>()))
                .ReturnsAsync((ISolution solution) =>
                {
                    List<(string name, string? content)> files = new();
                    string[] filesPath = Directory.GetFiles(Constants.RepositoryFilesPath, "*.json", SearchOption.AllDirectories);
                    foreach (var filePath in filesPath)
                    {
                        string fileName = new FileInfo(filePath).Name;
                        string fileContent = File.ReadAllText(filePath);

                        if (fileName == "secrets.json")
                        {
                            fileName = "secrets";
                            var header = JsonSerializer.Deserialize<HeaderFile>(fileContent);
                            if (header != null && header.solutionFile != solution.Name)
                            {
                                files.Clear();
                                break;
                            }
                        }
                        else if (!fileName.StartsWith("secrets"))
                        {
                            fileName = "secrets\\" + fileName;
                        }

                        files.Add((fileName, fileContent));
                    }
                    return files;
                });

            repository
                .Setup(o => o.PullAllSecretsAsync())
                .ReturnsAsync(() =>
                {

                    List<(string name, string? content)> files = new();
                    string[] filesPath = Directory.GetFiles(Constants.RepositoryFilesPath, "*.json", SearchOption.AllDirectories);
                    foreach (var filePath in filesPath)
                    {
                        string fileName = new FileInfo(filePath).Name;
                        if (fileName != "secrets.json")
                        {
                            if (!fileName.StartsWith("secrets")) fileName = "secrets\\" + fileName;
                            files.Add((fileName, File.ReadAllText(filePath)));
                        }
                    }

                    List<SolutionSettings> secrets = new List<SolutionSettings>
                    {
                        new SolutionSettings
                        {
                            Name = "SolutionSample.sln",
                            Settings = files
                        }
                    };

                    return secrets;
                });

            return repository.Object;
        }


        protected void ClearOutput()
        {
            _consoleOutput.Clear();
        }


        protected void VerifyOutput([CallerMemberName] string? caller = null)
        {
            if (caller == null)
                throw new ArgumentNullException(caller);

            string sampleFile = Path.Combine(Constants.TestFilesPath, caller + ".output.txt");

            bool foundFirstLine = false;

            List<string> generatedLines = new List<string>();
            foreach (var line in Output.Split("\n"))
            {
                if (!foundFirstLine && String.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                foundFirstLine = true;
                generatedLines.Add(line.TrimEnd());
            }
            for (int i = generatedLines.Count - 1; i >= 0; i--)
            {
                if (generatedLines[i].Length == 0)
                    generatedLines.RemoveAt(i);
                else
                    break;
            }

            foundFirstLine = false;

            List<string> expectedLines = new List<string>();
            foreach (var line in File.ReadAllLines(sampleFile))
            {
                if (!foundFirstLine && String.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                foundFirstLine = true;
                expectedLines.Add(line.TrimEnd());
            }

            Assert.Equal(expectedLines.Count, generatedLines.Count);
            for (int i = 0; i < expectedLines.Count; i++)
            {
                string pattern = "^" + expectedLines[i]
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace(@"\", @"\\")
                    .Replace("|", @"\|")
                    .Replace("#", @"\d")
                    .Replace(".", @"\.")
                    .Replace("*", @".*")
                    .Replace("(", @"\(")
                    .Replace(")", @"\)")
                    + "$";

                Assert.Matches(pattern, generatedLines[i]);
            }
        }

    }
}
