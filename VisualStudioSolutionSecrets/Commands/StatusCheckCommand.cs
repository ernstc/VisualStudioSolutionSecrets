using System;
using System.Collections.Generic;
using System.IO;
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

            Console.WriteLine($"             Ecryption key status: {encryptionKeyStatus}");
            Console.WriteLine($"  Repository authorization status: {repositoryAuthorizationStatus}\n");
            Console.WriteLine();

            if (isCipherReady && isRepositoryReady && options.Path != null)
            {
                Console.WriteLine("Checking solutions synchronization status...\n");

                string? path = EnsureFullyQualifiedPath(options.Path);

                Console.WriteLine("Solution                                          |  Version   |  Last Update          |  Status");
                Console.WriteLine("------------------------------------------------------------------------------------------------------------");

                string[] solutionFiles = GetSolutionFiles(path, options.All);
                foreach (string solutionFile in solutionFiles)
                {
                    await GetSolutionStatus(solutionFile);
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
                solutionColor = ConsoleColor.White;

                Context.Repository.SolutionName = solution.Name;

                var repositoryFiles = await Context.Repository.PullFilesAsync();
                if (repositoryFiles.Count == 0)
                {
                    status = "Local only";
                    statusColor = ConsoleColor.Gray;
                }
                else
                {
                    status = "OK";
                    statusColor = ConsoleColor.Cyan;

                    HeaderFile? header = null;

                    foreach (var file in repositoryFiles)
                    {
                        if ((
                            file.name == "secrets.json"
                            || file.name == "secrets"   // This check is for compatibility with versions <= 1.1.x
                            )
                            && file.content != null)
                        {
                            header = JsonSerializer.Deserialize<HeaderFile>(file.content);
                            continue;
                        }

                        if (file.content == null)
                        {
                            status = "ERROR";
                            statusColor = ConsoleColor.Red;
                            break;
                        }

                        bool isFileOk = true;
                        var contents = JsonSerializer.Deserialize<Dictionary<string, string>>(file.content);
                        foreach (var item in contents)
                        {
                            string? decryptedContent = Context.Current.Cipher.Decrypt(item.Value);
                            if (decryptedContent == null)
                            {
                                isFileOk = false;
                                break;
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
            if (solutionName.Length > 48) solutionName = solutionName.Substring(0, 45) + "...";

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
            Console.WriteLine($"{status}");

            Console.ForegroundColor = color;
        }

    }
}
