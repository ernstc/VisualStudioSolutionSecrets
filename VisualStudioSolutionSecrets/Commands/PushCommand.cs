﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using NuGet.Protocol.Core.Types;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Push encrypted solution secrets.")]
    internal class PushCommand : CommandBaseWithPath
    {

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        public async Task<int> OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            string[] solutionFiles = GetSolutionFiles(Path, All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return 1;
            }

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile);

                var synchronizationSettings = solution.CustomSynchronizationSettings;

                // Select the repository for the curront solution
                IRepository repository = Context.Current.GetRepository(synchronizationSettings) ?? Context.Current.Repository;

                try
                {
                    var headerFile = new HeaderFile
                    {
                        visualStudioSolutionSecretsVersion = Versions.VersionString!,
                        lastUpload = DateTime.UtcNow,
                        solutionFile = solution.Name,
                        solutionGuid = solution.Uid
                    };

                    var files = new List<(string fileName, string? content)>
                    {
                        ("secrets", JsonSerializer.Serialize(headerFile))
                    };

                    var secretFiles = solution.GetProjectsSecretFiles();
                    if (secretFiles.Count == 0)
                    {
                        continue;
                    }

                    Write($"Pushing secrets to {repository.RepositoryType} for solution: ");
                    Write(solution.Name, ConsoleColor.White);
                    Write("... ");

                    // Ensure authorization on the selected repository
                    if (!await repository.IsReady())
                    {
                        await repository.AuthorizeAsync(batchMode: true);
                    }

                    var secrets = new Dictionary<string, Dictionary<string, string>>();

                    bool isEmpty = true;
                    bool failed = false;
                    foreach (var secretFile in secretFiles)
                    {
                        if (secretFile.Content != null)
                        {
                            isEmpty = false;
                            bool isFileOk = true;

                            if (repository.EncryptOnClient)
                            {
                                isFileOk = secretFile.Encrypt();
                            }

                            if (isFileOk)
                            {
                                secrets.TryAdd(secretFile.ContainerName, new Dictionary<string, string>());
                                secrets[secretFile.ContainerName].Add(secretFile.Name, secretFile.Content);
                            }
                            else
                            {
                                failed = true;
                                break;
                            }
                        }
                    }

                    foreach (var group in secrets)
                    {
                        string groupContent = JsonSerializer.Serialize(group.Value);
                        files.Add((group.Key, groupContent));
                    }

                    if (!isEmpty && !failed)
                    {
                        if (!await repository.PushFilesAsync(solution, files))
                        {
                            failed = true;
                        }
                    }

                    if (isEmpty)
                    {
                        WriteLine("Skipped.\n    Warning: Cannot find local secrets for this solution.\n", ConsoleColor.Yellow);
                    }
                    else if (failed)
                    {
                        WriteLine("Failed.", ConsoleColor.Red);
                    }
                    else
                    {
                        WriteLine("Done.", ConsoleColor.Green);
                    }
                }
                catch (Azure.Identity.AuthenticationFailedException)
                {
                    WriteLine("Authentication failed", ConsoleColor.Red);
                }
                catch (UnauthorizedAccessException)
                {
                    WriteLine("Unauthorized access", ConsoleColor.Red);
                }
                catch (Exception)
                {
                    WriteLine("Error", ConsoleColor.Red);
                }
            }

            Console.WriteLine("\nFinished.\n");
            return 0;
        }

    }
}
