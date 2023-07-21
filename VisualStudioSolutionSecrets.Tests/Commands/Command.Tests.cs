﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    public class CommandTests
    {

        protected bool UseRepositoryEncryption { get; set; } = true;

        // Here you will find the console output generated by the test.
        private readonly StringBuilder _consoleOutput = new StringBuilder();


        protected string Output => _consoleOutput.ToString();


        protected void ConfigureContext()
        {
            UseRepositoryEncryption = true;

            _consoleOutput.Clear();

            // Suppress output on standart out
            Console.SetOut(new StringWriter(_consoleOutput));

            // Temp folder setup
            var tempFolder = new DirectoryInfo(Path.Combine(Constants.TempFolderPath));
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }
            tempFolder.Create();

            // Mock dependencies
            MockConsoleInput();
            MockFileSystem();
            MockRepository();

            SyncConfiguration.Refresh();
        }


        protected void DisposeTempFolder()
        {
            var tempFolder = new DirectoryInfo(Path.Combine(Constants.TempFolderPath));
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }
        }


        protected void DisposeCipherFiles()
        {
            string cipherFilePath = Path.Combine(Constants.ConfigFilesPath, "cipher.json");
            if (File.Exists(cipherFilePath))
            {
                File.Delete(cipherFilePath);
            }
        }


        protected void MockConsoleInput()
        {
            var consoleInputMock = new Mock<IConsoleInput>();

            consoleInputMock
                .Setup(o => o.ReadKey())
                .Returns(new ConsoleKeyInfo('y', ConsoleKey.Y, false, false, false));

            // Configure mocked dependencies
            Context.Current.AddService<IConsoleInput>(consoleInputMock.Object);
        }


        protected void MockFileSystem(
            string? applicationDataFolder = null,
            string? secretsFolder = null,
            string? solutionFilesFolder = null
            )
        {
            var fileSystemMock = new Mock<DefaultFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(applicationDataFolder ?? Constants.ConfigFilesPath);

            fileSystemMock
                .Setup(o => o.GetSecretsFolderPath())
                .Returns(secretsFolder ?? Constants.SecretFilesPath);

            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns(solutionFilesFolder ?? Constants.SolutionFilesPath);

            // Configure mocked dependencies
            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
        }


        private void MockRepository()
        {
            var repositoryMock = new Mock<IRepository>();

            repositoryMock
                .Setup(o => o.EncryptOnClient)
                .Returns(() => UseRepositoryEncryption);

            repositoryMock
                .Setup(o => o.RepositoryType)
                .Returns("Mock");

            repositoryMock
               .Setup(o => o.RepositoryName)
               .Returns("Name");

            repositoryMock
                .Setup(o => o.GetFriendlyName())
                .Returns("Name");

            repositoryMock
                .Setup(o => o.IsReady())
                .ReturnsAsync(true);

            repositoryMock
                .Setup(o => o.IsValid())
                .Returns(true);

            repositoryMock
                .Setup(o => o.PushFilesAsync(It.IsAny<ISolution>(), It.IsAny<ICollection<(string name, string? content)>>()))
                .ReturnsAsync((ISolution _, ICollection<(string name, string? content)> collection) =>
                {
                    foreach (var (name, content) in collection)
                    {
                        string fileName = name;
                        if (!fileName.EndsWith(".json")) fileName += ".json";
                        string filePath = Path.Combine(Constants.RepositoryFilesPath, fileName.Replace('\\', Path.DirectorySeparatorChar));
                        var fileInfo = new FileInfo(filePath);
                        Directory.CreateDirectory(fileInfo.DirectoryName!);
                        File.WriteAllText(filePath, content);
                    }
                    return true;
                });

            repositoryMock
                .Setup(o => o.PullFilesAsync(It.IsAny<ISolution>()))
                .ReturnsAsync((ISolution solution) =>
                {
                    List<(string name, string? content)> files = new List<(string name, string? content)>();
                    string[] filesPath = Directory.GetFiles(Constants.RepositoryFilesPath, "*.json", new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        ReturnSpecialDirectories = false,
                        RecurseSubdirectories = true
                    });
                    foreach (var filePath in filesPath)
                    {
                        string fileName = new FileInfo(filePath).Name;
                        string fileContent = File.ReadAllText(filePath);

                        if (fileName == "secrets.json")
                        {
                            fileName = "secrets";
                            HeaderFile? header = null;
                            try
                            {
                                header = JsonSerializer.Deserialize<HeaderFile>(fileContent);
                            }
                            catch
                            { }

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

            repositoryMock
                .Setup(o => o.PullAllSecretsAsync())
                .ReturnsAsync(() =>
                {

                    List<(string name, string? content)> files = new List<(string name, string? content)>();
                    string[] filesPath = Directory.GetFiles(Constants.RepositoryFilesPath, "*.json", new EnumerationOptions
                    {
                        IgnoreInaccessible = true,
                        ReturnSpecialDirectories = false,
                        RecurseSubdirectories = true
                    });
                    foreach (var filePath in filesPath)
                    {
                        string fileName = new FileInfo(filePath).Name;
                        if (fileName != "secrets.json")
                        {
                            if (!fileName.StartsWith("secrets")) fileName = "secrets\\" + fileName;
                            files.Add((fileName, File.ReadAllText(filePath)));
                        }
                    }

                    return new List<SolutionSettings>
                    {
                        new SolutionSettings(files)
                        {
                            Name = "SolutionSample.sln"
                        }
                    };
                });

            // Configure mocked dependencies
            var repository = repositoryMock.Object;
            Context.Current.AddService<IRepository>(repository);
            Context.Current.AddService<IRepository>(repository, nameof(RepositoryType.AzureKV));
            Context.Current.AddService<IRepository>(repository, nameof(RepositoryType.GitHub));
            Context.Current.AddService<ICipher>(new Cipher());
        }


        protected void ClearOutput()
        {
            _consoleOutput.Clear();
        }


        protected int RunCommand(string arguments)
        {
            const char multiWordParamDelimiter = '\'';

            arguments = arguments.Trim();
            List<string> args = new List<string>();

            bool capturing = true;
            bool aggregating = false;
            int startIndex = 0;
            for (int i = 0; i < arguments.Length; i++)
            {
                switch (arguments[i])
                {
                    case multiWordParamDelimiter:
                        {
                            aggregating = !aggregating;
                            if (aggregating)
                            {
                                capturing = true;
                                startIndex = i + 1;
                            }
                            else
                            {
                                capturing = false;
                                args.Add(arguments[startIndex..i]);
                            }
                            break;
                        }
                    case ' ':
                        {
                            if (!aggregating && capturing)
                            {
                                capturing = false;
                                args.Add(arguments[startIndex..i]);
                            }
                            break;
                        }
                    default:
                        {
                            if (!capturing)
                            {
                                startIndex = i;
                                capturing = true;
                            }
                            break;
                        }
                }
            }

            if (capturing)
            {
                args.Add(arguments[startIndex..]);
            }

            return CommandLineApplication.Execute<Program>(args.ToArray());
        }


        protected void VerifyOutput([CallerMemberName] string? caller = null)
        {
            if (caller == null)
                throw new ArgumentNullException(caller);

            VerifyOutput(caller, transform: null);
        }


        protected void VerifyOutput(string testName, Func<string, string>? transform = null)
        {
            string sampleFile = Path.Combine(Constants.TestFilesPath, testName + ".output.txt");

            List<string> expectedLines = new List<string>();

            bool foundFirstLine = false;

            foreach (var line in File.ReadAllLines(sampleFile))
            {
                if (!foundFirstLine && String.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                foundFirstLine = true;
                string expectedLine = line.TrimEnd();
                if (transform != null)
                {
                    expectedLine = transform(expectedLine);
                }
                expectedLines.Add(expectedLine);
            }

            VerifyOutput(expectedLines);
        }


        private void VerifyOutput(List<string> expectedLines)
        {
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

            Assert.Equal(expectedLines.Count, generatedLines.Count);
            for (int i = 0; i < expectedLines.Count; i++)
            {
                string pattern = "^" + expectedLines[i]
                    .Replace('\\', Path.DirectorySeparatorChar)
                    .Replace(@"\", @"\\")
                    .Replace("|", @"\|")
                    .Replace("#", @"\d")
                    .Replace(".", @"\.")
                    .Replace("%", ".")
                    .Replace("*", ".*")
                    .Replace("(", @"\(")
                    .Replace(")", @"\)")
                    .Replace("[", @"\[")
                    .Replace("]", @"\]")
                    + "$";

                Assert.Matches(pattern, generatedLines[i]);
            }
        }

    }
}
