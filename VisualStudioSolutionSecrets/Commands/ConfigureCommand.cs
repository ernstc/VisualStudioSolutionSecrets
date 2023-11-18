using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;
using AllowedValuesAttribute = McMaster.Extensions.CommandLineUtils.AllowedValuesAttribute;

namespace VisualStudioSolutionSecrets.Commands
{

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConfigureCommandValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is ConfigureCommand command)
            {
                bool paramSetRepo =
                    command.Default
                    || command.RepositoryType != null
                    || command.RepositoryName != null;

                bool paramSetReset =
                    command.Reset;

                if (paramSetRepo && paramSetReset)
                {
                    return new ValidationResult("\nThe option --reset is not compatible with --default, -r|--repo and -n|--name options.\n");
                }

                if (command.Default && command.Path != null)
                {
                    return new ValidationResult("\nThe option --default is not compatible with the option --path.\n");
                }

                if (command.RepositoryType != null)
                {
                    if (String.Equals(command.RepositoryType, nameof(RepositoryType.GitHub), StringComparison.OrdinalIgnoreCase))
                    {
                        if (!String.IsNullOrEmpty(command.RepositoryName))
                        {
                            return new ValidationResult("\nFor repository of type \"github\" you cannot specify the option -n|--name.\n");
                        }
                    }
                    else if (String.Equals(command.RepositoryType, nameof(RepositoryType.AzureKV), StringComparison.OrdinalIgnoreCase))
                    {
                        if (String.IsNullOrEmpty(command.RepositoryName))
                        {
                            return new ValidationResult("\nFor repository of type \"azurekv\" you need to specify the option -n|--name.\n");
                        }

                        var repository = new AzureKeyVaultRepository
                        {
                            RepositoryName = command.RepositoryName
                        };

                        if (!repository.IsValid())
                        {
                            return new ValidationResult($"The repository name is not valid: {command.RepositoryName}\n");
                        }
                    }
                    else
                    {
                        return new ValidationResult("\nThe option -r|--repo is not valid.\n");
                    }
                }
                else
                {
                    if (command.RepositoryName != null)
                    {
                        return new ValidationResult("\nThe option -n|--name cannot be used without the option -r|--repo.\n");
                    }
                }
            }
            return ValidationResult.Success;
        }
    }



    [Command(Description = "Configure the repository to use by default or for the solution in the current directory.")]
    [ConfigureCommandValidation]
    [Subcommand(
       typeof(ConfigureListCommand)
    )]
    internal class ConfigureCommand : CommandBaseWithPath
    {

        [Option("--default", Description = "Changes the default configuration.")]
        public bool Default { get; set; }

        [Option("-r|--repo", Description = "Repository type to use for the solution.")]
        [AllowedValues("github", "azurekv", IgnoreCase = true)]
        public string? RepositoryType { get; set; }

        [Option("-n|--name", Description = "Repository name to use for the solution. This setting applies only for Azure Key Vault.")]
        public string? RepositoryName { get; set; }

        [Option("--reset", Description = "Reset the configuration of the solution.")]
        public bool Reset { get; set; }


        public int OnExecute(CommandLineApplication? app = null)
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            bool invalidParams =
                (
                    !Default
                    && RepositoryType == null
                    && RepositoryName == null
                    && !Reset
                    && Path == null
                )
                || (
                    Default
                    && Reset
                )
                || (
                    Default
                    && Path != null
                );

            if (invalidParams)
            {
                app?.ShowHelp();
                return 1;
            }

            if (Default)
            {
                if (string.Equals(nameof(Repository.RepositoryType.GitHub), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    SyncConfiguration.Default.Repository = Repository.RepositoryType.GitHub;
                    SyncConfiguration.Default.AzureKeyVaultName = null;
                    SyncConfiguration.Save();

                    Console.WriteLine("Configured GitHub Gist as the default repository.\n");
                    return 0;
                }
                else if (string.Equals(nameof(Repository.RepositoryType.AzureKV), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    SyncConfiguration.Default.Repository = Repository.RepositoryType.AzureKV;
                    SyncConfiguration.Default.AzureKeyVaultName = RepositoryName;
                    SyncConfiguration.Save();

                    Console.WriteLine($"Configured Azure Key Vault ({RepositoryName}) as the default repository.\n");
                    return 0;
                }
                return 1;
            }
            else
            {
                string[] solutionFiles = GetSolutionFiles(Path, false);
                if (solutionFiles.Length == 0)
                {
                    Console.WriteLine("Solution file not found.\n");
                    return 1;
                }

                bool foundError = false;
                foreach (string solutionFilePath in solutionFiles)
                {
                    int result = ConfigureSolution(solutionFilePath);
                    foundError = foundError || result != 0;
                }

                return (foundError && solutionFiles.Length == 1) ? 1 : 0;
            }
        }


        private int ConfigureSolution(string solutionFilePath)
        {
            SolutionFile solution = new SolutionFile(solutionFilePath);

            if (solution.Uid == Guid.Empty)
            {
                Console.WriteLine($"The solution \"{solution.Name}\" has not a unique identifier. You need to upgrade the solution file.\n");
                return 1;
            }

            if (Reset)
            {
                SyncConfiguration.SetCustomSynchronizationSettings(solution.Uid, null);
                SyncConfiguration.Save();

                Console.WriteLine($"Removed custom configuration for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                return 0;
            }
            else
            {
                if (string.Equals(nameof(Repository.RepositoryType.GitHub), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    var settings = new SolutionSynchronizationSettings
                    {
                        Repository = Repository.RepositoryType.GitHub,
                        AzureKeyVaultName = null
                    };
                    SyncConfiguration.SetCustomSynchronizationSettings(solution.Uid, settings);
                    SyncConfiguration.Save();

                    Console.WriteLine($"Configured GitHub Gist as the repository for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                    return 0;
                }
                else if (string.Equals(nameof(Repository.RepositoryType.AzureKV), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    var settings = new SolutionSynchronizationSettings
                    {
                        Repository = Repository.RepositoryType.AzureKV,
                        AzureKeyVaultName = RepositoryName
                    };
                    SyncConfiguration.SetCustomSynchronizationSettings(solution.Uid, settings);
                    SyncConfiguration.Save();

                    Console.WriteLine($"Configured Azure Key Vault ({RepositoryName}) as the repository for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                    return 0;
                }
            }
            return 1;
        }

    }
}
