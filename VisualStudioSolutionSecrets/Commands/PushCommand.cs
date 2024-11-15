using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Push encrypted solution secrets.")]
    internal class PushCommand : CommandBaseWithPath
    {

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }

        [Option("-o|--overwrite", Description = "Does not merge remote secrets with local ones, but overwrites them with locals.")]
        public bool Overwrite { get; set; }


        public async Task<int> OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            string[] solutionFiles = GetSolutionFiles(Path, All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return 1;
            }

            foreach (string solutionFile in solutionFiles)
            {
                SolutionFile solution = new(solutionFile);

                SolutionSynchronizationSettings? synchronizationSettings = solution.CustomSynchronizationSettings;

                // Select the repository for the current solution
                IRepository repository = Context.Current.GetRepository(synchronizationSettings) ?? Context.Current.Repository;

                try
                {
                    HeaderFile headerFile = new()
                    {
                        VisualStudioSolutionSecretsVersion = Versions.CurrentVersion.ToString(),
                        LastUpload = DateTime.UtcNow,
                        SolutionFile = solution.Name,
                        SolutionGuid = solution.Uid
                    };

                    List<(string name, string? content)> files = new()
                    {
                        ("secrets", JsonSerializer.Serialize(headerFile))
                    };

                    ICollection<SecretFile> secretFiles = solution.GetProjectsSecretFiles();
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

                    ICollection<(string name, string? content)> repositoryFiles = await repository.PullFilesAsync(solution);

                    Dictionary<string, Dictionary<string, string>> secrets = new();

                    bool isEmpty = true;
                    bool failed = false;
                    foreach (SecretFile secretFile in secretFiles)
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
                                _ = secrets.TryAdd(secretFile.ContainerName, new Dictionary<string, string>());
                                secrets[secretFile.ContainerName].Add(secretFile.Name, secretFile.Content);
                            }
                            else
                            {
                                failed = true;
                                break;
                            }
                        }
                    }

                    foreach (KeyValuePair<string, Dictionary<string, string>> group in secrets)
                    {
                        string groupContent = JsonSerializer.Serialize(group.Value);
                        files.Add((group.Key, groupContent));
                    }

                    if (!Overwrite)
                    {
                        // Merge remote Files with local ones. Local Files have priority.
                        files.AddRange(
                            repositoryFiles.Where(rf => !files.Exists(f => f.name == rf.name))
                            );
                    }

                    bool isChanged = false;
                    if (!isEmpty)
                    {
                        if (files.Count != repositoryFiles.Count)
                        {
                            isChanged = true;
                        }
                        else
                        {
                            foreach ((string name, string? content) file in files)
                            {
                                if (file.name == "secrets")
                                {
                                    continue;
                                }

                                Dictionary<string, string>? fileContent = JsonSerializer.Deserialize<Dictionary<string, string>>(file.content!);
                                if (fileContent != null)
                                {
                                    foreach ((string name, string? content) repositoryFile in repositoryFiles)
                                    {
                                        if (repositoryFile.name == file.name)
                                        {
                                            Dictionary<string, string>? repositoryFileContent = JsonSerializer.Deserialize<Dictionary<string, string>>(repositoryFile.content!);
                                            if (repositoryFileContent != null)
                                            {
                                                bool contentChanged = repositoryFileContent.Any(secret => !fileContent.TryGetValue(secret.Key, out string? value) || value != secret.Value);
                                                if (contentChanged)
                                                {
                                                    isChanged = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    if (isChanged)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!isEmpty && isChanged && !failed && !await repository.PushFilesAsync(solution, files))
                    {
                        failed = true;
                    }

                    if (isEmpty)
                    {
                        WriteLine("Skipped.\n    Warning: Cannot find local secrets for this solution.\n", ConsoleColor.Yellow);
                    }
                    else if (!isChanged)
                    {
                        WriteLine("Already updated.", ConsoleColor.Cyan);
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
