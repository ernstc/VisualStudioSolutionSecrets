using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class StatusCheckCommand : Command<StatusCheckOptions>
	{

        protected override async Task Execute(StatusCheckOptions options)
        {
            Console.WriteLine($"vs-secrets {Context.Current.VersionString}\n");
            Console.WriteLine("Checking status...\n");

            bool isCipherReady = await Context.Cipher.IsReady();
            bool isRepositoryReady = await Context.Repository.IsReady();

            string encryptionKeyStatus = isCipherReady ? "OK" : "NOT DEFINED";
            string repositoryAuthorizationStatus = isRepositoryReady ? "OK" : "NOT AUTHORIZED";

            Console.WriteLine($"             Ecryption key: {encryptionKeyStatus}");
            Console.WriteLine($"  Repository authorization: {repositoryAuthorizationStatus}\n");
            Console.WriteLine();

            if (isCipherReady && isRepositoryReady)
            {
                string? path = options.Path != null ? EnsureFullyQualifiedPath(options.Path) : Context.IO.GetCurrentDirectory();
                string[] solutionFiles = GetSolutionFiles(path, options.All);
                if (solutionFiles.Length > 0)
                {
                    Console.WriteLine("Checking solutions synchronization status...\n");

                    Console.WriteLine("Solution                                          |  Version   |  Last Update          |  Status");
                    Console.WriteLine("---------------------------------------------------------------------------------------------------------------");

                    foreach (string solutionFile in solutionFiles)
                    {
                        await GetSolutionStatus(solutionFile);
                    }

                    Console.WriteLine();
                }
            }
        }


        private async Task GetSolutionStatus(string solutionFile)
        {
            string version = String.Empty;
            string lastUpdate = String.Empty;
            string status;

            ConsoleColor statusColor;
            ConsoleColor solutionColor;

            bool hasRemoteSecrets = false;
            bool? hasLocalSecrets = null;
            bool? isSynchronized = null;

            SolutionFile solution = new SolutionFile(solutionFile, Context.Cipher);
            var configFiles = solution.GetProjectsSecretConfigFiles();
            if (configFiles.Count == 0)
            {
                // This solution has not projects with secrets.
                status = "No secrests found";
                statusColor = ConsoleColor.DarkGray;
                solutionColor = ConsoleColor.DarkGray;
            }
            else
            {
                hasLocalSecrets = configFiles.Any(c => !String.IsNullOrWhiteSpace(c.Content));
                isSynchronized = true;

                solutionColor = ConsoleColor.White;

                Context.Repository.SolutionName = solution.Name;

                var remoteFiles = await Context.Repository.PullFilesAsync();
                hasRemoteSecrets = remoteFiles.Count > 0;

                if (!hasRemoteSecrets)
                {
                    if (hasLocalSecrets.Value)
                    {
                        status = "Local only";
                        statusColor = ConsoleColor.Gray;
                    }
                    else
                    {
                        status = "No secrests found";
                        statusColor = ConsoleColor.DarkGray;
                    }
                }
                else
                {
                    status = "OK";
                    statusColor = ConsoleColor.Cyan;

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
                                string? decryptedContent = Context.Current.Cipher.Decrypt(item.Value);
                                if (decryptedContent == null)
                                {
                                    isFileOk = false;
                                    break;
                                }
                                else if (hasLocalSecrets.Value)
                                {
                                    var localFile = configFiles.FirstOrDefault(c => c.GroupName == remoteFile.name && c.FileName == item.Key);
                                    if (localFile != null)
                                    {
                                        string localContent = File.ReadAllText(localFile.FilePath);
                                        if (localContent != decryptedContent)
                                        {
                                            isSynchronized = false;
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

            string solutionName = solution.Name;
            if (solutionName.Length > 48) solutionName = solutionName[..45] + "...";

            var color = Console.ForegroundColor;

            Console.ForegroundColor = solutionColor;
            Console.Write($"{solutionName,-48}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{version,-8}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = color;
            Console.Write($"{lastUpdate,-19}");

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
