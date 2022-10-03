using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Pull solution secrets and decrypt them.")]
    internal class PullCommand : CommandBase
    {

        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        public async Task<int> OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            if (!await CanSync())
            {
                return 1;
            }

            string? path = EnsureFullyQualifiedPath(Path) ?? Context.Current.IO.GetCurrentDirectory();

            string[] solutionFiles = GetSolutionFiles(path, All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return 1;
            }

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, Context.Current.Cipher);

                var configFiles = solution.GetProjectsSecretSettingsFiles();
                if (configFiles.Count == 0)
                {
                    continue;
                }

                var synchronizationSettings = solution.CustomSynchronizationSettings;

                // Select the repository for the curront solution
                IRepository repository = Context.Current.GetRepository(synchronizationSettings) ?? Context.Current.Repository;

                // Ensure authorization on the selected repository
                if (!await repository.IsReady())
                {
                    await repository.AuthorizeAsync();
                }

                Console.Write($"Pulling secrets for solution: {solution.Name}... ");

                var repositoryFiles = await repository.PullFilesAsync(solution.Name);
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

                                    bool isFileOk = true;
                                    if (repository.EncryptOnClient)
                                    {
                                        isFileOk = configFile.Decrypt();
                                    }

                                    if (isFileOk)
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
            return 0;
        }

    }
}

