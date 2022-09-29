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

    [Command(Description = "Push encrypted solution secrets.")]
    internal class PushCommand : CommandBase
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

                var synchronizationSettings = solution.CustomSynchronizationSettings;

                // Select the repository for the curront solution
                IRepository repository = Context.Current.GetRepository(synchronizationSettings) ?? Context.Current.Repository;

                // Ensure authorization on the selected repository
                if (!await repository.IsReady())
                {
                    await repository.AuthorizeAsync();
                }

                var headerFile = new HeaderFile
                {
                    visualStudioSolutionSecretsVersion = Versions.VersionString!,
                    lastUpload = DateTime.UtcNow,
                    solutionFile = solution.Name
                };

                var files = new List<(string fileName, string? content)>
                {
                    ("secrets", JsonSerializer.Serialize(headerFile))
                };

                var configFiles = solution.GetProjectsSecretSettingsFiles();
                if (configFiles.Count == 0)
                {
                    continue;
                }

                repository.SolutionName = solution.Name;

                Console.Write($"Pushing secrets for solution: {solution.Name}... ");

                Dictionary<string, Dictionary<string, string>> secrets = new Dictionary<string, Dictionary<string, string>>();

                bool isEmpty = true;
                bool failed = false;
                foreach (var configFile in configFiles)
                {
                    if (configFile.Content != null)
                    {
                        isEmpty = false;
                        bool isFileOk = true;

                        if (repository.EncryptOnClient)
                        {
                            isFileOk = configFile.Encrypt();
                        }

                        if (isFileOk)
                        {
                            if (!secrets.ContainsKey(configFile.GroupName))
                            {
                                secrets.Add(configFile.GroupName, new Dictionary<string, string>());
                            }
                            secrets[configFile.GroupName].Add(configFile.FileName, configFile.Content);
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
                    if (!await repository.PushFilesAsync(files))
                    {
                        failed = true;
                    }
                }

                Console.WriteLine(isEmpty ? "Skipped.\n    Warning: Cannot find local secrets for this solution.\n" : failed ? "Failed." : "Done.");
            }

            Console.WriteLine("\nFinished.\n");
            return 0;
        }

    }
}
