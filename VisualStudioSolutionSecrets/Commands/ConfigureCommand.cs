using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;

namespace VisualStudioSolutionSecrets.Commands
{

    internal class ConfigureCommand : Command<ConfigureOptions>
    {

        protected override Task Execute(ConfigureOptions options)
        {
            if (options.Default)
            {
                switch (options.RepositoryType)
                {
                    case Repository.RepositoryTypesEnum.GitHub:
                        {
                            Configuration.Default.Repository = options.RepositoryType;
                            Configuration.Default.AzureKeyVaultName = null;
                            Configuration.Save();
                            break;
                        }
                    case Repository.RepositoryTypesEnum.AzureKV:
                        {
                            Configuration.Default.Repository = options.RepositoryType;
                            Configuration.Default.AzureKeyVaultName = options.RepositoryName;
                            Configuration.Save();
                            break;
                        }
                }
            }
            else
            {
                string path = Context.IO.GetCurrentDirectory();

                string[] solutionFiles = GetSolutionFiles(path, false);
                if (solutionFiles.Length == 0)
                {
                    Console.WriteLine("Solution files not found.\n");
                    return Task.CompletedTask;
                }

                var settings = new SolutionSynchronizationSettings();

                switch (options.RepositoryType)
                {
                    case Repository.RepositoryTypesEnum.GitHub:
                        {
                            settings.Repository = options.RepositoryType;
                            settings.AzureKeyVaultName = null;
                            Configuration.Save();
                            break;
                        }
                    case Repository.RepositoryTypesEnum.AzureKV:
                        {
                            settings.Repository = options.RepositoryType;
                            settings.AzureKeyVaultName = options.RepositoryName;
                            Configuration.Save();
                            break;
                        }
                    default:
                        {
                            return Task.CompletedTask;
                        }
                }

                SolutionFile solution = new SolutionFile(solutionFiles[0]);
                Configuration.SetSynchronizationSettings(solution.SolutionGuid, settings);
                Configuration.Save();
            }

            return Task.CompletedTask;
        }

    }
}
