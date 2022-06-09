using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

    internal class PullSecretsCommand : Command<PullSecretsOptions>
    {

        protected override async Task Execute(PullSecretsOptions options)
        {
            if (!await CanSync())
            {
                return;
            }

            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            await AuthenticateRepositoryAsync();

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, Context.Cipher);

                var configFiles = solution.GetProjectsSecretConfigFiles();
                if (configFiles.Count == 0)
                    continue;

                Context.Repository.SolutionName = solution.Name;

                Console.Write($"Pulling secrets for solution: {solution.Name} ...");

                var files = await Context.Repository.PullFilesAsync();
                if (files.Count == 0)
                {
                    Console.WriteLine("Failed, secrets not found");
                    continue;
                }

                // Validate header file
                HeaderFile? header = null;
                foreach (var file in files)
                {
                    if (file.name == "secrets" && file.content != null)
                    {
                        header = JsonSerializer.Deserialize<HeaderFile>(file.content);
                        break;
                    }
                }

                if (header == null)
                {
                    Console.WriteLine("\n    ERR: Header file not found");
                    continue;
                }

                if (!header.IsVersionSupported())
                {
                    continue;
                }

                bool failed = false;
                foreach (var file in files)
                {
                    if (file.name != "secrets")
                    {
                        if (file.content == null)
                        {
                            Console.Write($"\n    ERR: File has no content: {file.name}");
                            continue;
                        }

                        Dictionary<string, string>? secretFiles = null;
                        try
                        {
                            secretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(file.content);
                        }
                        catch
                        {
                            Console.Write($"\n    ERR: File content cannot be read: {file.name}");
                        }

                        if (secretFiles == null)
                        {
                            failed = true;
                            break;
                        }

                        foreach (var secret in secretFiles)
                        {
                            string configFileName = secret.Key;

                            // This check is for compatibility with version 1.0.x
                            if (configFileName == "content")
                            {
                                configFileName = "secrets.json";
                            }

                            foreach (var configFile in configFiles)
                            {
                                if (configFile.GroupName == file.name
                                    && configFile.FileName == configFileName)
                                {
                                    configFile.Content = secret.Value;
                                    if (configFile.Decrypt())
                                    {
                                        solution.SaveConfigFile(configFile);
                                    }
                                    else
                                    {
                                        failed = true;
                                    }
                                    break;
                                }
                            }
                        }

                        if (failed)
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }

    }
}

