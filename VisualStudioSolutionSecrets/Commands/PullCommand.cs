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
    internal class PullCommand : CommandBaseWithPath
    {

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
                SolutionFile solution = new SolutionFile(solutionFile);

                ICollection<SecretFile> secretFiles = solution.GetProjectsSecretFiles();
                if (secretFiles.Count == 0)
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

                var repositoryFiles = await repository.PullFilesAsync(solution);
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
                        try
                        {
                            header = JsonSerializer.Deserialize<HeaderFile>(file.content);
                        }
                        catch
                        { }
                        break;
                    }
                }

                if (header == null)
                {
                    Console.WriteLine("\n    ERR: Header file not found or not valid");
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

                        Dictionary<string, string>? remoteSecretFiles = null;
                        
                        try
                        {
                            remoteSecretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(repositoryFile.content);
                        }
                        catch
                        {
                            Console.Write($"\n    ERR: Remote file content cannot be read: {repositoryFile.name}");
                        }

                        if (remoteSecretFiles == null)
                        {
                            failed = true;
                            break;
                        }

                        foreach (var remoteSecretFile in remoteSecretFiles)
                        {
                            string secretFileName = remoteSecretFile.Key;

                            // This check is for compatibility with version 1.0.x
                            if (secretFileName == "content")
                            {
                                secretFileName = "secrets.json";
                            }

                            foreach (var localSecretFile in secretFiles)
                            {
                                if (localSecretFile.ContainerName == repositoryFile.name
                                    && localSecretFile.Name == secretFileName)
                                {
                                    localSecretFile.Content = remoteSecretFile.Value;

                                    bool isFileOk = true;
                                    if (repository.EncryptOnClient)
                                    {
                                        isFileOk = localSecretFile.Decrypt();
                                    }

                                    if (isFileOk)
                                    {
                                        solution.SaveSecretSettingsFile(localSecretFile);
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

