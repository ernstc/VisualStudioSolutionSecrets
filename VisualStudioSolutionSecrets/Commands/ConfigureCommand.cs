using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Configuration;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    internal class ConfigureCommand : Command<ConfigureOptions>
    {

        public override Task Execute(ConfigureOptions options)
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            if (options.Default)
            {
                if (String.Equals(nameof(RepositoryTypesEnum.GitHub), options.RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    Configuration.Default.Repository = RepositoryTypesEnum.GitHub;
                    Configuration.Default.AzureKeyVaultName = null;
                    Configuration.Save();

                    Console.WriteLine("Configured GitHub Gist as the default repository.\n");
                }
                else if (String.Equals(nameof(RepositoryTypesEnum.AzureKV), options.RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    Configuration.Default.Repository = RepositoryTypesEnum.AzureKV;
                    Configuration.Default.AzureKeyVaultName = options.RepositoryName;
                    Configuration.Save();

                    Console.WriteLine($"Configured Azure Key Vault (https://{options.RepositoryName}.vault.azure.net) as the default repository.\n");
                }
            }
            else
            {
                string path = Context.Current.IO.GetCurrentDirectory();

                string[] solutionFiles = GetSolutionFiles(path, false);
                if (solutionFiles.Length == 0)
                {
                    Console.WriteLine("Solution files not found.\n");
                    return Task.CompletedTask;
                }

                var solutionFilePath = solutionFiles[0];
                SolutionFile solution = new SolutionFile(solutionFilePath);

                if (options.Reset)
                {
                    Configuration.SetCustomSynchronizationSettings(solution.SolutionGuid, null);
                    Configuration.Save();

                    Console.WriteLine($"Removed custom configuration for the solution \"{solution.Name}\" ({solution.SolutionGuid}).\n");
                }
                else
                {
                    var settings = new SolutionSynchronizationSettings();

                    if (String.Equals(nameof(RepositoryTypesEnum.GitHub), options.RepositoryType, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Repository = RepositoryTypesEnum.GitHub;
                        settings.AzureKeyVaultName = null;
                        Configuration.SetCustomSynchronizationSettings(solution.SolutionGuid, settings);
                        Configuration.Save();

                        Console.WriteLine($"Configured GitHub Gist as the repository for the solution \"{solution.Name}\" ({solution.SolutionGuid}).\n");
                    }
                    else if (String.Equals(nameof(RepositoryTypesEnum.AzureKV), options.RepositoryType, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Repository = RepositoryTypesEnum.AzureKV;
                        settings.AzureKeyVaultName = options.RepositoryName;
                        Configuration.SetCustomSynchronizationSettings(solution.SolutionGuid, settings);
                        Configuration.Save();

                        Console.WriteLine($"Configured Azure Key Vault (https://{options.RepositoryName}.vault.azure.net) as the repository for the solution \"{solution.Name}\" ({solution.SolutionGuid}).\n");
                    }
                }
            }

            return Task.CompletedTask;
        }

    }
}
