using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    internal class PullSecretsCommand : Command<PullSecretsOptions>
    {

        protected override async Task Execute(PullSecretsOptions options)
        {
            if (!await CanSync())
            {
                return;
            }

            string? path = EnsureFullyQualifiedPath(options.Path);

            string[] solutionFiles = GetSolutionFiles(path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            await AuthenticateRepositoryAsync();

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, Context.Cipher);

                var synchronizationSettings = solution.SynchronizationSettings;
                IRepository? repository = Context.GetRepository(synchronizationSettings);
                if (repository == null)
                {
                    Console.Write($"Skipping solution \"{solution.Name}\". Wrong repository.");
                    continue;
                }

                var configFiles = solution.GetProjectsSecretSettingsFiles();
                if (configFiles.Count == 0)
                    continue;

                repository.SolutionName = solution.Name;

                Console.Write($"Pulling secrets for solution: {solution.Name}... ");

                var repositoryFiles = await repository.PullFilesAsync();
                if (repositoryFiles.Count == 0)
                {
                    Console.WriteLine("Failed, secrets not found");
                    continue;
                }

                // Validate header file
                HeaderFile? header = null;
                foreach (var file in repositoryFiles)
                {
                    if (file.name == "secrets" && file.content != null)
                    {
                        header = JsonSerializer.Deserialize<HeaderFile>(file.content);
                        break;
                    }
                }

                if (header == null)
                {
                    Console.WriteLine("\n    ERR: Header file not found");
                    continue;
                }

                if (!header.IsVersionSupported())
                {
                    Console.WriteLine($"\n    ERR: Header file has incompatible version {header.visualStudioSolutionSecretsVersion}");
                    Console.WriteLine("\n         Consider to install an updated version of this tool.");
                    continue;
                }

                bool failed = false;
                foreach (var repositoryFile in repositoryFiles)
                {
                    if (repositoryFile.name != "secrets")
                    {
                        if (repositoryFile.content == null)
                        {
                            Console.Write($"\n    ERR: File has no content: {repositoryFile.name}");
                            continue;
                        }

                        Dictionary<string, string>? secretFiles = null;
                        try
                        {
                            secretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(repositoryFile.content);
                        }
                        catch
                        {
                            Console.Write($"\n    ERR: File content cannot be read: {repositoryFile.name}");
                        }

                        if (secretFiles == null)
                        {
                            failed = true;
                            break;
                        }

                        foreach (var secret in secretFiles)
                        {
                            string configFileName = secret.Key;

                            // This check is for compatibility with version 1.0.x
                            if (configFileName == "content")
                            {
                                configFileName = "secrets.json";
                            }

                            foreach (var configFile in configFiles)
                            {
                                if (configFile.GroupName == repositoryFile.name
                                    && configFile.FileName == configFileName)
                                {
                                    configFile.Content = secret.Value;
                                    if (configFile.Decrypt())
                                    {
                                        solution.SaveSecretSettingsFile(configFile);
                                    }
                                    else
                                    {
                                        failed = true;
                                    }
                                    break;
                                }
                            }
                        }

                        if (failed)
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }

    }
}

