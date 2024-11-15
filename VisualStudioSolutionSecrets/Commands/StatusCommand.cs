using System;
using System.Collections.Generic;
using System.Globalization;
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

        private const int MAX_SOLUTION_LENGTH = 40;

        internal const char CHAR_UP = '\u2191';
        internal const char CHAR_DOWN = '\u2193';
        internal const char CHAR_DIFF = '\u2260';
        internal const char CHAR_EQUAL = '=';
        internal const char CHAR_NOT_SETTED = '?';



        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }

        [Option("-d|--duplicates", Description = "When true, shows also duplicate solutions.")]
        public bool ShowDuplicates { get; set; }


        public async Task<int> OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            Console.WriteLine("Checking status...\n");

            bool isCipherReady = await Context.Current.Cipher.IsReady();

            string encryptionKeyStatus = isCipherReady ? "OK" : "NOT DEFINED";

            Console.WriteLine($"Encryption Key: {encryptionKeyStatus}\n");

            Console.WriteLine("Checking solutions synchronization status...");

            string[] solutionFiles = GetSolutionFiles(Path, All);
            if (solutionFiles.Length > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Solution                                    | Version | Last Update         | Repo    | Secrets Status");
                Console.WriteLine("--------------------------------------------|---------|---------------------|---------|---------------------------------");

                List<string> processedSolutions = new();
                foreach (string solutionFile in solutionFiles)
                {
                    SolutionFile solution = new(solutionFile);
                    string solutionCompositeKey = solution.GetSolutionCompositeKey();
                    if (
                        ShowDuplicates
                        || !processedSolutions.Contains(solutionCompositeKey)
                        )
                    {
                        processedSolutions.Add(solutionCompositeKey);
                        await GetSolutionStatus(solution);
                    }
                }

                Console.WriteLine();
                Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
                Write($"{CHAR_UP} ", ConsoleColor.Blue); WriteLine("= # projects with secrets only on the cloud", ConsoleColor.DarkGray);
                Write($"{CHAR_DOWN} ", ConsoleColor.Blue); WriteLine("= # projects with secrets only on the local", ConsoleColor.DarkGray);
                Write($"{CHAR_EQUAL} ", ConsoleColor.Blue); WriteLine("= # projects with same secrets on the cloud and local", ConsoleColor.DarkGray);
                Write($"{CHAR_DIFF} ", ConsoleColor.Blue); WriteLine("= # projects with secrets with differences between cloud and local", ConsoleColor.DarkGray);
                Write($"{CHAR_NOT_SETTED} ", ConsoleColor.Blue); WriteLine("= # projects with secrets, but secrets not set", ConsoleColor.DarkGray);
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
            None = 0x0,
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
        *  Possible SyncStatus values and Description:
        *  
        *  Synchronized                  Local and remote settings are synchronized
        *  NoSecretsFound                The solution uses user secrets but they are not set.
        *  HeaderError                   Found errors in the header file
        *  ContentError                  Remote file is empty or it is not in the correct format.
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

        private static string WriteStatus(SyncStatus status, ConsoleColor defaultColor)
        {
            string statusText = String.Empty;
            if ((status & SyncStatus.Synchronized) == SyncStatus.Synchronized)
            {
                statusText = "Synchronized";
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.Unmanaged) == SyncStatus.Unmanaged)
            {
                statusText = "Unmanaged";
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.NoSecretsFound) == SyncStatus.NoSecretsFound)
            {
                statusText = "Secrets not set";
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.HeaderError) == SyncStatus.HeaderError)
            {
                statusText = "ERROR: Header";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.ContentError) == SyncStatus.ContentError)
            {
                statusText = "ERROR: Content";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.LocalOnly) == SyncStatus.LocalOnly)
            {
                statusText = "Local only";
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.CloudOnly) == SyncStatus.CloudOnly)
            {
                statusText = "Cloud only";
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write(statusText);

                if ((status & SyncStatus.InvalidKey) == SyncStatus.InvalidKey)
                {
                    Console.ForegroundColor = defaultColor;
                    Console.Write(" / ");

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("Invalid key");

                    statusText += " / Invalid key";
                }
            }
            else if ((status & SyncStatus.NotSynchronized) == SyncStatus.NotSynchronized)
            {
                statusText = "Not synchronized";
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.InvalidKey) == SyncStatus.InvalidKey)
            {
                statusText = "Invalid key";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.AuthenticationFailed) == SyncStatus.AuthenticationFailed)
            {
                statusText = "ERROR: Authentication failed";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.Unauthorized) == SyncStatus.Unauthorized)
            {
                statusText = "ERROR: Unauthorized";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(statusText);
            }
            else if ((status & SyncStatus.CannotLoadStatus) == SyncStatus.CannotLoadStatus)
            {
                statusText = "ERROR: Cannot load status";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(statusText);
            }
            return statusText;
        }


        private static async Task GetSolutionStatus(SolutionFile solution)
        {
            string version = String.Empty;
            string lastUpdate = String.Empty;
            string repositoryType = String.Empty;
            SyncStatus status = SyncStatus.None;
            string statusDetails = String.Empty;
            int countLocalOnly = 0;
            int countRemoteOnly = 0;
            int countDifferences = 0;
            int countEquals = 0;
            int countUnmanaged = 0;

            bool foundContentError = false;

            ConsoleColor color = Console.ForegroundColor;
            ConsoleColor solutionColor = 0;

            string solutionName = solution.Name;
            if (solutionName.Length > (MAX_SOLUTION_LENGTH + 3))
            {
                solutionName = solutionName[..MAX_SOLUTION_LENGTH] + "...";
            }

            try
            {
                SolutionSynchronizationSettings? synchronizationSettings = solution.CustomSynchronizationSettings;
                IRepository? repository = Context.Current.GetRepository(synchronizationSettings);
                repositoryType = repository?.RepositoryType ?? String.Empty;

                ICollection<SecretFile> secretFiles = solution.GetProjectsSecretFiles();
                if (secretFiles.Count == 0)
                {
                    // This solution has not projects with secrets.
                    status = SyncStatus.Unmanaged;
                    solutionColor = ConsoleColor.DarkGray;
                }
                else
                {
                    repository ??= Context.Current.Repository;

                    repositoryType = repository.RepositoryType;
                    bool tryToDecrypt = repository.EncryptOnClient;

                    // Ensure authorization on the selected repository
                    if (!await repository.IsReady())
                    {
                        await repository.AuthorizeAsync(batchMode: true);
                    }

                    ICollection<(string name, string? content)> remoteFiles = await repository.PullFilesAsync(solution);

                    HeaderFile? header = null;
                    try
                    {
                        string? headerContent = remoteFiles.First(f => f.name == "secrets").content;
                        if (headerContent != null)
                        {
                            header = JsonSerializer.Deserialize<HeaderFile>(headerContent);
                            if (header != null)
                            {
                                version = header.VisualStudioSolutionSecretsVersion ?? String.Empty;
                                lastUpdate = header.LastUpload.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture.DateTimeFormat) ?? String.Empty;
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    remoteFiles = remoteFiles.Where(f =>
                        f.name == "secrets"
                        || secretFiles.Any(
                            sf => sf.SecretsId != null
                            && f.name.Contains(sf.SecretsId, StringComparison.OrdinalIgnoreCase)
                            )
                        ).ToList();

                    int remoteSecretsCount = remoteFiles.Count(item => !String.Equals("secrets", item.name, StringComparison.OrdinalIgnoreCase));

                    bool hasRemoteSecrets = remoteSecretsCount > 0;

                    List<SecretFile> configFiles = secretFiles.Where(c => c.Content != null).ToList();
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
                            // This solution manage secrets but Files cannot be found
                            status = SyncStatus.NoSecretsFound;
                            solutionColor = ConsoleColor.DarkGray;
                        }
                    }
                    else
                    {
                        solutionColor = ConsoleColor.White;

                        if (configFiles.Count > 0 && remoteSecretsCount == 0)
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
                            }
                            else
                            {
                                List<SecretFile> remoteSecretFiles = new();
                                foreach ((string name, string? content) remoteFile in remoteFiles)
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
                                    {
                                        // ignored
                                    }

                                    if (contents == null)
                                    {
                                        status = SyncStatus.ContentError;
                                        foundContentError = true;
                                        break;
                                    }
                                    else
                                    {
                                        foreach (KeyValuePair<string, string> item in contents)
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

                                if (!foundContentError && status == SyncStatus.None)
                                {
                                    // Check if the local and remote settings are synchronized
                                    HashSet<string> localNames = new(configFiles.Select(f => $"{f.ContainerName}-{f.Name}"));
                                    HashSet<string> remoteNames = new(remoteSecretFiles.Select(f => $"{f.ContainerName}-{f.Name}"));

                                    countLocalOnly = localNames.Count(n => !remoteNames.Contains(n));
                                    countRemoteOnly = remoteNames.Count(n => !localNames.Contains(n));
                                    countDifferences = 0;
                                    countUnmanaged = secretFiles.Count(f =>
                                        !String.IsNullOrEmpty(f.SecretsId)
                                        && !localNames.Any(x => x.Contains(f.SecretsId, StringComparison.OrdinalIgnoreCase))
                                        && !remoteNames.Any(x => x.Contains(f.SecretsId, StringComparison.OrdinalIgnoreCase))
                                        );

                                    foreach (SecretFile remoteFile in remoteSecretFiles)
                                    {
                                        SecretFile? localFile = configFiles.Find(c => c.ContainerName == remoteFile.ContainerName && c.Name == remoteFile.Name);
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
                                                else
                                                {
                                                    countEquals++;
                                                }
                                            }
                                        }
                                    }

                                    if (countLocalOnly != 0
                                        || countRemoteOnly != 0
                                        || countDifferences != 0)
                                    {
                                        status = SyncStatus.NotSynchronized;
                                    }
                                }

                            }
                        }
                    }
                }

                if (countLocalOnly != 0)
                {
                    statusDetails += $" {countLocalOnly}{CHAR_DOWN}";
                }

                if (countRemoteOnly != 0)
                {
                    statusDetails += $" {countRemoteOnly}{CHAR_UP}";
                }

                if (countEquals != 0)
                {
                    statusDetails += $" {countEquals}{CHAR_EQUAL}";
                }

                if (countDifferences != 0)
                {
                    statusDetails += $" {countDifferences}{CHAR_DIFF}";
                }

                if (countUnmanaged != 0)
                {
                    statusDetails += $" {countUnmanaged}{CHAR_NOT_SETTED}";
                }
            }
            catch (Azure.Identity.AuthenticationFailedException)
            {
                status = SyncStatus.AuthenticationFailed;
            }
            catch (UnauthorizedAccessException)
            {
                status = SyncStatus.Unauthorized;
            }
            catch
            {
                status = SyncStatus.CannotLoadStatus;
            }

            if (status == SyncStatus.None)
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
            string statusText = WriteStatus(status, color);

            int totalWidth = 32 - statusText.Length;
            if (totalWidth < 0)
            {
                totalWidth = 0;
            }

            Write($"{statusDetails.PadLeft(totalWidth)}\n", ConsoleColor.Blue);

            Console.ForegroundColor = color;
        }

    }
}
