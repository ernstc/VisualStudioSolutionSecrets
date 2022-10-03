using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Shows the status for the tool and the solutions.")]
    internal class StatusCommand : CommandBase
	{

        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        public async Task<int> OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            Console.WriteLine("Checking status...\n");

            bool isCipherReady = await Context.Current.Cipher.IsReady();
            bool isRepositoryReady = await Context.Current.Repository.IsReady();

            string encryptionKeyStatus = isCipherReady ? "OK" : "NOT DEFINED";
            string repositoryAuthorizationStatus = isRepositoryReady ? "OK" : "NOT AUTHORIZED";
            string defaultRepository;

            var repository = Context.Current.GetService<IRepository>();
            if (repository != null)
            {
                defaultRepository = repository.RepositoryType;
                if (!String.IsNullOrEmpty(repository.RepositoryName))
                {
                    defaultRepository += $" ({repository.RepositoryName})";
                }
            }
            else
            {
                defaultRepository = "None";
            }

            Console.WriteLine($"             Ecryption key: {encryptionKeyStatus}");
            Console.WriteLine($"  Repository authorization: {repositoryAuthorizationStatus}");
            Console.WriteLine($"        Default repository: {defaultRepository}\n\n");

            if (isCipherReady && isRepositoryReady)
            {
                string path = EnsureFullyQualifiedPath(Path) ?? Context.Current.IO.GetCurrentDirectory();
                string[] solutionFiles = GetSolutionFiles(path, All);
                if (solutionFiles.Length > 0)
                {
                    Console.WriteLine("Checking solutions synchronization status...\n");

                    Console.WriteLine("Solution                                          |  Version  |  Last Update          |  Repo     |  Status");
                    Console.WriteLine("----------------------------------------------------------------------------------------------------------------------");

                    foreach (string solutionFile in solutionFiles)
                    {
                        await GetSolutionStatus(solutionFile);
                    }

                    Console.WriteLine();
                }
            }

            return 0;
        }


        private async Task GetSolutionStatus(string solutionFile)
        {
            string version = String.Empty;
            string lastUpdate = String.Empty;
            string repoName = String.Empty;
            string status = String.Empty;

            var color = Console.ForegroundColor;

            ConsoleColor statusColor = color;
            ConsoleColor solutionColor = color;

            bool hasRemoteSecrets = false;
            bool? hasLocalSecrets = null;
            bool? isSynchronized = null;

            SolutionFile solution = new SolutionFile(solutionFile, Context.Current.Cipher);

            string solutionName = solution.Name;
            if (solutionName.Length > 48) solutionName = solutionName[..45] + "...";

            var synchronizationSettings = solution.CustomSynchronizationSettings;

            IRepository? repository = Context.Current.GetRepository(synchronizationSettings);

            if (repository == null)
            {
                repoName = "";
                repository = Context.Current.Repository;
            }
            else
            {
                repoName = repository.RepositoryType;
            }

            try
            {
                var configFiles = solution.GetProjectsSecretSettingsFiles();
                if (configFiles.Count == 0)
                {
                    // This solution has not projects with secrets.
                    status = "No secrests found";
                    statusColor = ConsoleColor.DarkGray;
                    solutionColor = ConsoleColor.DarkGray;
                }
                else
                {
                    hasLocalSecrets = configFiles.Any(c => c.Content != null);
                    isSynchronized = true;

                    repository.SolutionName = solution.Name;

                    // Ensure authorization on the selected repository
                    if (!await repository.IsReady())
                    {
                        await repository.AuthorizeAsync();
                    }

                    var remoteFiles = await repository.PullFilesAsync();
                    hasRemoteSecrets = remoteFiles.Count > 0;

                    if (!hasRemoteSecrets)
                    {
                        if (hasLocalSecrets.Value)
                        {
                            status = "Local only";
                            statusColor = ConsoleColor.DarkYellow;
                            solutionColor = ConsoleColor.White;
                        }
                        else
                        {
                            status = "No secrests found";
                            statusColor = ConsoleColor.DarkGray;
                            solutionColor = ConsoleColor.DarkGray;
                        }
                    }
                    else
                    {
                        repoName = repository.RepositoryType;

                        status = "OK";
                        statusColor = ConsoleColor.Cyan;
                        solutionColor = ConsoleColor.White;

                        HeaderFile? header = null;

                        foreach (var remoteFile in remoteFiles)
                        {
                            if (remoteFile.name == "secrets" && remoteFile.content != null)
                            {
                                header = JsonSerializer.Deserialize<HeaderFile>(remoteFile.content);
                                continue;
                            }

                            if (remoteFile.content == null)
                            {
                                status = "ERROR";
                                statusColor = ConsoleColor.Red;
                                break;
                            }

                            bool isFileOk = true;
                            var contents = JsonSerializer.Deserialize<Dictionary<string, string>>(remoteFile.content);
                            if (contents == null)
                            {
                                isFileOk = false;
                            }
                            else
                            {
                                foreach (var item in contents)
                                {
                                    string? content = item.Value;

                                    if (repository?.EncryptOnClient == true)
                                    {
                                        content = Context.Current.Cipher.Decrypt(content);
                                    }

                                    if (content == null)
                                    {
                                        isFileOk = false;
                                        break;
                                    }
                                    else if (hasLocalSecrets.Value)
                                    {
                                        var localFile = configFiles.FirstOrDefault(c => c.GroupName == remoteFile.name && c.FileName == item.Key);
                                        if (localFile != null)
                                        {
                                            if (localFile.Content == null)
                                            {
                                                isSynchronized = false;
                                            }
                                            else
                                            {
                                                if (localFile.Content != content)
                                                {
                                                    isSynchronized = false;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (!isFileOk)
                            {
                                status = "INVALID KEY!";
                                statusColor = ConsoleColor.Red;

                                break;
                            }
                        }

                        version = header?.visualStudioSolutionSecretsVersion ?? String.Empty;
                        lastUpdate = header?.lastUpload.ToString("yyyy-MM-dd HH:mm:ss") ?? String.Empty;
                    }
                }
            }
            catch
            {
                status = "ERROR loading status";
                statusColor = ConsoleColor.Red;
            }

            Console.ForegroundColor = solutionColor;
            Console.Write($"{solutionName,-48}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{version,-7}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{lastUpdate,-19}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{repoName,-7}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = statusColor;
            Console.Write($"{status}");

            if (hasRemoteSecrets)
            {
                if (hasLocalSecrets == false)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(" / ");

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Cloud only");
                }
                else if (isSynchronized == false)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(" / ");

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.Write("Not synchronized");
                }
            }

            Console.WriteLine();

            Console.ForegroundColor = color;
        }

    }
}
