using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class PushSecretsCommand : Command<PushSecretsOptions>
	{

        protected override async Task Execute(PushSecretsOptions options)
        {
            if (!await CanSync())
            {
                return;
            }

            string? path = options.Path;
            if (path != null && !Path.IsPathFullyQualified(path))
            {
                path = Path.Combine(Context.IO.GetCurrentDirectory(), path);
            }

            string[] solutionFiles = GetSolutionFiles(path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            await AuthenticateRepositoryAsync();

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, Context.Cipher);

                var headerFile = new HeaderFile
                {
                    visualStudioSolutionSecretsVersion = Context.VersionString!,
                    lastUpload = DateTime.UtcNow,
                    solutionFile = solution.Name
                };

                List<(string fileName, string? content)> files = new List<(string fileName, string? content)>();
                files.Add(("secrets", JsonSerializer.Serialize(headerFile)));

                var configFiles = solution.GetProjectsSecretConfigFiles();
                if (configFiles.Count == 0)
                {
                    continue;
                }

                Context.Repository.SolutionName = solution.Name;

                Console.Write($"Pushing secrets for solution: {solution.Name} ...");

                Dictionary<string, Dictionary<string, string>> secrets = new Dictionary<string, Dictionary<string, string>>();

                bool failed = false;
                foreach (var configFile in configFiles)
                {
                    if (configFile.Content != null)
                    {
                        if (configFile.Encrypt())
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

                if (!failed)
                {
                    if (!await Context.Repository.PushFilesAsync(files))
                    {
                        failed = true;
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }

    }
}
