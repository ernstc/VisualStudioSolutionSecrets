using System;
using System.Collections.Generic;
using System.Globalization;
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

        const int MAX_SOLUTION_LENGTH = 40;
        const char CHAR_UP = '\u2191';
        const char CHAR_DOWN = '\u2193';
        const char CHAR_DIFF = '\u2260';


        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        public async Task<int> OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            Console.WriteLine("Checking status...\n");

            bool isCipherReady = await Context.Current.Cipher.IsReady();
            bool isRepositoryReady = await Context.Current.Repository.IsReady();

            string encryptionKeyStatus = isCipherReady ? "OK" : "NOT DEFINED";

            Console.WriteLine($"Ecryption Key: {encryptionKeyStatus}\n");

            Console.WriteLine("Checking solutions synchronization status...");

            string[] solutionFiles = GetSolutionFiles(Path, All);
            if (solutionFiles.Length > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Solution                                    | Version | Last Update         | Repo    | Secrets Status");
                Console.WriteLine("--------------------------------------------|---------|---------------------|---------|---------------------------------");

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
            AuthenticationFailed = 0x400,
            Unauthorized = 0x800,
            Unmanaged = 0x1000
        }


        /*
        *  
        *  Possible SyncStatus values and description:
        *  
        *  Synchronized                  Local and remote settings are synchronized
        *  NoSecretsFound                The solution uses user secrets but they are not setted.
        *  HeaderError                   Found errors in the header file
        *  ContentError                  Remote file is empty or it is not in the corret format.
        *  LocalOnly                     Settings found only on the local machine.
        *  CloudOnly                     Settings found only on the remote repository.
        *  CloudOnly | Invalid Key       Settings found only on the remote repository, they cannot be read decrypted.
        *  NotSynchronized               Local and remote settings are not synchronized.
        *  InvalidKey                    Local settings cannot be compared with remote settings because they cannot be decrypted.
        *  CannotLoadStatus              It is not possible to determine the synchronization status.
        *  AuthenticationFailed          User authentication has failed or user has canceled authentication.
        *  Unauthorized                  User credentials are missing or not valid.
        *  Unmanaged                     The solution does not manage user secrets.
        * 
        */

        private static void WriteStatus(SyncStatus status, ConsoleColor defaultColor)
        {
            if ((status & SyncStatus.Synchronized) == SyncStatus.Synchronized)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Synchronized");
            }
            else if ((status & SyncStatus.Unmanaged) == SyncStatus.Unmanaged)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("Unmanaged");
            }
            else if ((status & SyncStatus.NoSecretsFound) == SyncStatus.NoSecretsFound)
            {
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("Secrets not setted");
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
            else if ((status & SyncStatus.AuthenticationFailed) == SyncStatus.AuthenticationFailed)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: Authentication failed");
            }
            else if ((status & SyncStatus.Unauthorized) == SyncStatus.Unauthorized)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: Unauthorized");
            }
            else if ((status & SyncStatus.CannotLoadStatus) == SyncStatus.CannotLoadStatus)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("ERROR: Cannot load status");
            }
        }


        private static async Task GetSolutionStatus(string solutionFile)
        {
            string version = String.Empty;
            string lastUpdate = String.Empty;
            string repositoryType = String.Empty;
            SyncStatus status = SyncStatus.Unknown;
            string statusDetails = String.Empty;

            bool foundContentError = false;

            var color = Console.ForegroundColor;
            ConsoleColor solutionColor = color;

            SolutionFile solution = new SolutionFile(solutionFile);

            string solutionName = solution.Name;
            if (solutionName.Length > (MAX_SOLUTION_LENGTH + 3)) solutionName = solutionName[..MAX_SOLUTION_LENGTH] + "...";

            try
            {
                var secretFiles = solution.GetProjectsSecretFiles();
                if (secretFiles.Count == 0)
                {
                    // This solution has not projects with secrets.
                    status = SyncStatus.Unmanaged;
                    solutionColor = ConsoleColor.DarkGray;
                }
                else
                {
                    var synchronizationSettings = solution.CustomSynchronizationSettings;

                    IRepository? repository = Context.Current.GetRepository(synchronizationSettings);
                    repository ??= Context.Current.Repository;

                    repositoryType = repository.RepositoryType;
                    bool tryToDecrypt = repository.EncryptOnClient;

                    // Ensure authorization on the selected repository
                    if (!await repository.IsReady())
                    {
                        await repository.AuthorizeAsync(batchMode: true);
                    }

                    var remoteFiles = await repository.PullFilesAsync(solution);

                    HeaderFile? header = null;
                    try
                    {
                        string? headerContent = remoteFiles.First(f => f.name == "secrets").content;
                        if (headerContent != null)
                        {
                            header = JsonSerializer.Deserialize<HeaderFile>(headerContent);
                            if (header != null)
                            {
                                version = header.visualStudioSolutionSecretsVersion ?? String.Empty;
                                lastUpdate = header.lastUpload.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat) ?? String.Empty;
                            }
                        }
                    }
                    catch
                    { }

                    int remoteSecretsCount = remoteFiles.Count(item => !String.Equals("secrets", item.name, StringComparison.OrdinalIgnoreCase));

                    bool hasRemoteSecrets = remoteSecretsCount > 0;

                    var configFiles = secretFiles.Where(c => c.Content != null).ToList();
                    if (configFiles.Count == 0 && !hasRemoteSecrets)
                    {
                        if (header != null)
                        {
                            // The remote repository exists but it's empty
                            status = SyncStatus.Synchronized;
                            solutionColor = ConsoleColor.White;
                        }
                        else
                        {
                            // This solution manage secrets but files cannot be foundò
                            status = SyncStatus.NoSecretsFound;
                            solutionColor = ConsoleColor.DarkGray;
                        }
                    }
                    else
                    {
                        solutionColor = ConsoleColor.White;

                        if (configFiles.Count > 0 && remoteFiles.Count == 0)
                        {
                            status = SyncStatus.LocalOnly;
                        }
                        else
                        {
                            // Here hasRemoteSecrets is true
                            if (configFiles.Count == 0)
                            {
                                status = SyncStatus.CloudOnly;
                                solutionColor = ConsoleColor.White;
                            }

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
                                    HashSet<string> localNames = new HashSet<string>(configFiles.Select(f => $"{f.ContainerName}-{f.Name}"));
                                    HashSet<string> remoteNames = new HashSet<string>(remoteSecretFiles.Select(f => $"{f.ContainerName}-{f.Name}"));

                                    int countLocalOnly = localNames.Count(n => !remoteNames.Contains(n));
                                    int countRemoteOnly = remoteNames.Count(n => !localNames.Contains(n));
                                    int countDifferences = 0;

                                    foreach (var remoteFile in remoteSecretFiles)
                                    {
                                        var localFile = configFiles.FirstOrDefault(c => c.ContainerName == remoteFile.ContainerName && c.Name == remoteFile.Name);
                                        if (localFile != null)
                                        {
                                            if (localFile.Content == null)
                                            {
                                                countDifferences++;
                                            }
                                            else
                                            {
                                                if (localFile.Content != remoteFile.Content)
                                                {
                                                    countDifferences++;
                                                }
                                            }
                                        }
                                    }

                                    if (countLocalOnly != 0
                                        || countRemoteOnly != 0
                                        || countDifferences != 0)
                                    {
                                        status = SyncStatus.NotSynchronized;
                                        if (countLocalOnly != 0) statusDetails += $" {countLocalOnly}{CHAR_DOWN}";
                                        if (countRemoteOnly != 0) statusDetails += $" {countRemoteOnly}{CHAR_UP}";
                                        if (countDifferences != 0) statusDetails += $" {countDifferences}{CHAR_DIFF}";
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Azure.Identity.AuthenticationFailedException)
            {
                status = SyncStatus.AuthenticationFailed;
                foundContentError = true;
            }
            catch (UnauthorizedAccessException)
            {
                status = SyncStatus.Unauthorized;
                foundContentError = true;
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

            Write($"{solutionName,-(MAX_SOLUTION_LENGTH + 3)}", solutionColor);
            Write(" | ", color);
            Write($"{version,-7}", solutionColor);
            Write(" | ", color);
            Write($"{lastUpdate,-19}", solutionColor);
            Write(" | ", color);
            Write($"{repositoryType,-7}", solutionColor);
            Write(" | ", color);
            WriteStatus(status, color);
            Write($"{statusDetails}\n");

            Console.ForegroundColor = color;
        }

    }
}
