using System;
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
            Console.WriteLine("\nChecking status...\n");

            bool isCipherReady = await Context.Cipher.IsReady();
            bool isRepositoryReady = await Context.Repository.IsReady();

            string encryptionKeyStatus = isCipherReady ? "OK" : "NOT DEFINED";
            string repositoryAuthorizationStatus = isRepositoryReady ? "OK" : "NOT AUTHORIZED";

            Console.WriteLine($"             Ecryption key status: {encryptionKeyStatus}");
            Console.WriteLine($"  Repository authorization status: {repositoryAuthorizationStatus}\n");
            Console.WriteLine();

            if (isCipherReady && isRepositoryReady && options.Path != null)
            {
                Console.WriteLine("Checking solutions status...\n");

                string? path = options.Path;
                if (path != null && !Path.IsPathFullyQualified(path))
                {
                    path = Path.Combine(Context.IO.GetCurrentDirectory(), path);
                }

                Console.WriteLine("Solution                                |  Version                 | Last Update    | Status");
                Console.WriteLine("---------------------------------------------------------------------------------------------");

                string[] solutionFiles = GetSolutionFiles(path, options.All);
                foreach (string solutionFile in solutionFiles)
                {
                    await GetSolutionStatus(solutionFile);
                }
            }
        }


        private async Task GetSolutionStatus(string solutionFile)
        {
            string version;
            string lastUpdate;
            string status;

            SolutionFile solution = new SolutionFile(solutionFile, Context.Cipher);

            Context.Repository.SolutionName = solution.Name;

            var repositoryFiles = await Context.Repository.PullFilesAsync();
            if (repositoryFiles.Count == 0)
            {
                version = String.Empty;
                lastUpdate = String.Empty;
                status = "ERROR";
            }
            else
            {
                status = "Valid";
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
                        break;
                    }

                    string? decryptedContent = Context.Current.Cipher.Decrypt(file.content!);
                    if (decryptedContent == null)
                    {
                        status = "ERROR";
                        break;
                    }
                }

                version = header?.visualStudioSolutionSecretsVersion ?? String.Empty;
                lastUpdate = header?.lastUpload.ToString("dd/MM/yyyy HH:mm:ss") ?? String.Empty;
            }
            Console.WriteLine($"{solution.Name:40}| {version:10} | {lastUpdate} | {status}");
        }

    }
}
