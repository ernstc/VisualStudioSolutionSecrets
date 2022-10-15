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
    internal class StatusCommand : CommandBaseWithPath
    {

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
                Console.WriteLine("Checking solutions synchronization status...");

                string[] solutionFiles = GetSolutionFiles(Path, All);
                if (solutionFiles.Length > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Solution                                          |  Version  |  Last Update          |  Repo     |  Status");
                    Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------------");

                    foreach (string solutionFile in solutionFiles)
                    {
                        await GetSolutionStatus(solutionFile);
                    }
                }
                else
                {
                    Console.WriteLine("... no solution found.");
                }
                Console.WriteLine();
            }

            return 0;
        }


        [Flags]
        internal enum SyncStatus
        {
            Unknown = 0x0,
            Synchronized = 0x1,
            NoSecretsFound = 0x2,
            HeaderError = 0x4,
            ContentError = 0x8,
            LocalOnly = 0x10,
            CloudOnly = 0x20,
            NotSynchronized = 0x40,
            InvalidKey = 0x80,
            CannotLoadStatus = 0x200,
        }


        /*
       *  
       *  Possible SyncStatus values and description:
       *  
       *  Synchronized                  Local and remote settings are synchronized
       *  NoSecretsFound                No secrets found in the solution
       *  HeaderError                   Found errors in the header file
       *  ContentError                  Remote file is empty or it is not in the corret format.
       *  LocalOnly                     Settings found only on the local machine.
       *  CloudOnly                     Settings found only on the remote repository.
       *  CloudOnly | Invalid Key       Settings found only on the remote repository, they cannot be read decrypted.
       *  NotSynchronized               Local and remote settings are not synchronized.
       *  InvalidKey                    Local settings cannot be compared with remote settings because they cannot be decrypted.
       *  CannotLoadStatus              It is not possible to determine the synchronization status.
       * 
       */

        private void WriteStatus(SyncStatus status, ConsoleColor defaultColor)
        {
            if ((status & SyncStatus.Synchronized) == SyncStatus.Synchronized)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Synchronized");
            }
            else if ((status & SyncStatus.NoSecretsFound) == SyncStatus.NoSecretsFound)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("No secrets found");
            }
            else if ((status & SyncStatus.HeaderError) == SyncStatus.HeaderError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: Header");
            }
            else if ((status & SyncStatus.ContentError) == SyncStatus.ContentError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: Content");
            }
            else if ((status & SyncStatus.LocalOnly) == SyncStatus.LocalOnly)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Local only");
            }
            else if ((status & SyncStatus.CloudOnly) == SyncStatus.CloudOnly)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Cloud only");

                if ((status & SyncStatus.InvalidKey) == SyncStatus.InvalidKey)
                {
                    Console.ForegroundColor = defaultColor;
                    Console.Write(" / ");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Invalid key");
                }
            }
            else if ((status & SyncStatus.NotSynchronized) == SyncStatus.NotSynchronized)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("Not synchronized");
            }
            else if ((status & SyncStatus.InvalidKey) == SyncStatus.InvalidKey)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Invalid key");
            }
            else if ((status & SyncStatus.CannotLoadStatus) == SyncStatus.CannotLoadStatus)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: Cannot load status");
            }
        }


        private async Task GetSolutionStatus(string solutionFile)
        {
            string version = String.Empty;
            string lastUpdate = String.Empty;
            string repositoryType = String.Empty;
            SyncStatus status = SyncStatus.Unknown;

            bool foundContentError = false;

            var color = Console.ForegroundColor;
            ConsoleColor solutionColor = color;

            SolutionFile solution = new SolutionFile(solutionFile);

            string solutionName = solution.Name;
            if (solutionName.Length > 48) solutionName = solutionName[..45] + "...";

            var synchronizationSettings = solution.CustomSynchronizationSettings;

            IRepository? repository = Context.Current.GetRepository(synchronizationSettings);
            repository ??= Context.Current.Repository;

            bool tryToDecrypt = repository.EncryptOnClient;

            try
            {
                // Ensure authorization on the selected repository
                if (!await repository.IsReady())
                {
                    await repository.AuthorizeAsync();
                }

                var remoteFiles = await repository.PullFilesAsync(solution);

                bool hasRemoteSecrets = remoteFiles.Count > 0;

                var configFiles = solution.GetProjectsSecretFiles().Where(c => c.Content != null).ToList();
                if (configFiles.Count == 0 && !hasRemoteSecrets)
                {
                    // This solution has not projects with secrets.
                    status = SyncStatus.NoSecretsFound;
                    solutionColor = ConsoleColor.DarkGray;
                }
                else
                {
                    solutionColor = ConsoleColor.White;

                    if (configFiles.Count > 0 && !hasRemoteSecrets)
                    {
                        status = SyncStatus.LocalOnly;
                    }
                    else
                    {
                        // Here hasRemoteSecrets is true
                        repositoryType = repository.RepositoryType;

                        if (configFiles.Count == 0)
                        {
                            status = SyncStatus.CloudOnly;
                            solutionColor = ConsoleColor.White;
                        }

                        HeaderFile? header = null;
                        try
                        {
                            string? headerContent = remoteFiles.First(f => f.name == "secrets").content;
                            if (headerContent != null)
                            {
                                header = JsonSerializer.Deserialize<HeaderFile>(headerContent);
                            }
                        }
                        catch
                        { }

                        if (header == null)
                        {
                            status = SyncStatus.HeaderError;
                            foundContentError = true;
                        }
                        else
                        {
                            var remoteSecretFiles = new List<SecretFile>();
                            foreach (var remoteFile in remoteFiles)
                            {
                                if (remoteFile.name == "secrets")
                                {
                                    continue;
                                }

                                if (remoteFile.content == null)
                                {
                                    status = SyncStatus.ContentError;
                                    foundContentError = true;
                                    break;
                                }

                                Dictionary<string, string>? contents = null;
                                try
                                {
                                    contents = JsonSerializer.Deserialize<Dictionary<string, string>>(remoteFile.content);
                                }
                                catch
                                { }

                                if (contents == null)
                                {
                                    status = SyncStatus.ContentError;
                                    foundContentError = true;
                                    break;
                                }
                                else
                                {
                                    foreach (var item in contents)
                                    {
                                        string? content = item.Value;

                                        if (content == null)
                                        {
                                            status = SyncStatus.ContentError;
                                            foundContentError = true;
                                            break;
                                        }

                                        if (tryToDecrypt)
                                        {
                                            content = Context.Current.Cipher.Decrypt(content);
                                            if (content == null)
                                            {
                                                status |= SyncStatus.InvalidKey;
                                                foundContentError = true;
                                                break;
                                            }
                                        }

                                        remoteSecretFiles.Add(new SecretFile
                                        {
                                            Name = item.Key,
                                            ContainerName = remoteFile.name,
                                            Content = content
                                        });
                                    }

                                    if (foundContentError)
                                    {
                                        break;
                                    }
                                }
                            }

                            if (!foundContentError && status == SyncStatus.Unknown)
                            {
                                // Check if the local and remote settings are synchronized
                                if (configFiles.Count != remoteSecretFiles.Count)
                                {
                                    status = SyncStatus.NotSynchronized;
                                }
                                else
                                {
                                    foreach (var remoteFile in remoteSecretFiles)
                                    {
                                        var localFile = configFiles.FirstOrDefault(c => c.ContainerName == remoteFile.ContainerName && c.Name == remoteFile.Name);
                                        if (localFile != null)
                                        {
                                            if (localFile.Content == null)
                                            {
                                                status = SyncStatus.NotSynchronized;
                                                break;
                                            }
                                            else
                                            {
                                                if (localFile.Content != remoteFile.Content)
                                                {
                                                    status = SyncStatus.NotSynchronized;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            version = header.visualStudioSolutionSecretsVersion ?? String.Empty;
                            lastUpdate = header.lastUpload.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? String.Empty;
                        }
                    }
                }
            }
            catch
            {
                status = SyncStatus.CannotLoadStatus;
                foundContentError = true;
            }

            if (status == SyncStatus.Unknown)
            {
                status = SyncStatus.Synchronized;
                solutionColor = ConsoleColor.White;
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
            Console.Write($"{repositoryType,-7}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            WriteStatus(status, color);

            Console.WriteLine();

            Console.ForegroundColor = color;
        }

    }
}
