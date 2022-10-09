using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigureCommandValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
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
                    return new ValidationResult("\nThe --reset option is not compatible with --default, -r|--repo and -n|--name options.\n");
                }

                if (command.Default && command.Path != null)
                {
                    return new ValidationResult("\nThe --default option is not compatible with --path option.\n");
                }

                if (String.Equals(command.RepositoryType, "github", StringComparison.OrdinalIgnoreCase)
                    && !String.IsNullOrEmpty(command.RepositoryName))
                {
                    return new ValidationResult("\nFor repository of type \"github\" you cannot specify the option -n|--name.\n");
                }

                if (String.Equals(command.RepositoryType, "azurekv", StringComparison.OrdinalIgnoreCase)
                    && String.IsNullOrEmpty(command.RepositoryName))
                {
                    return new ValidationResult("\nFor repository of type \"azurekv\" you need to specify the option -n|--name.\n");
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
    internal class ConfigureCommand : CommandBase
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

        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }


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
                if (String.Equals(nameof(RepositoryTypesEnum.GitHub), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    if (RepositoryName != null)
                    {
                        Console.WriteLine("The option --name cannot be used with the GitHub repository.\n");
                        return 1;
                    }

                    Configuration.Default.Repository = RepositoryTypesEnum.GitHub;
                    Configuration.Default.AzureKeyVaultName = null;
                    Configuration.Save();

                    Console.WriteLine("Configured GitHub Gist as the default repository.\n");
                    return 0;
                }
                else if (String.Equals(nameof(RepositoryTypesEnum.AzureKV), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    var repository = new AzureKeyVaultRepository
                    {
                        RepositoryName = RepositoryName
                    };

                    if (repository.IsValid())
                    {
                        Configuration.Default.Repository = RepositoryTypesEnum.AzureKV;
                        Configuration.Default.AzureKeyVaultName = RepositoryName;
                        Configuration.Save();

                        Console.WriteLine($"Configured Azure Key Vault ({RepositoryName}) as the default repository.\n");
                        return 0;
                    }
                    else
                    {
                        Console.WriteLine($"The repository name is not valid: {RepositoryName}\n");
                        return 1;
                    }
                }
            }
            else
            {
                string path = EnsureFullyQualifiedPath(Path) ?? Context.Current.IO.GetCurrentDirectory();
                string[] solutionFiles = GetSolutionFiles(path, false);
                if (solutionFiles.Length == 0)
                {
                    Console.WriteLine("Solution file not found.\n");
                    return 1;
                }

                var solutionFilePath = solutionFiles[0];
                SolutionFile solution = new SolutionFile(solutionFilePath);

                if (solution.Uid == Guid.Empty)
                {
                    Console.WriteLine($"The solution \"{solution.Name}\" has not a unique identifier. You need to upgrade the solution file.\n");
                    return 1;
                }

                if (Reset)
                {
                    Configuration.SetCustomSynchronizationSettings(solution.Uid, null);
                    Configuration.Save();

                    Console.WriteLine($"Removed custom configuration for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                    return 0;
                }
                else
                {
                    if (String.Equals(nameof(RepositoryTypesEnum.GitHub), RepositoryType, StringComparison.OrdinalIgnoreCase))
                    {
                        if (RepositoryName != null)
                        {
                            Console.WriteLine("The option --name cannot be used with the GitHub repository.\n");
                            return 1;
                        }

                        var settings = new SolutionSynchronizationSettings
                        {
                            Repository = RepositoryTypesEnum.GitHub,
                            AzureKeyVaultName = null
                        };
                        Configuration.SetCustomSynchronizationSettings(solution.Uid, settings);
                        Configuration.Save();

                        Console.WriteLine($"Configured GitHub Gist as the repository for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                        return 0;
                    }
                    else if (String.Equals(nameof(RepositoryTypesEnum.AzureKV), RepositoryType, StringComparison.OrdinalIgnoreCase))
                    {
                        var repository = new AzureKeyVaultRepository
                        {
                            RepositoryName = RepositoryName
                        };

                        if (repository.IsValid())
                        {
                            var settings = new SolutionSynchronizationSettings
                            {
                                Repository = RepositoryTypesEnum.AzureKV,
                                AzureKeyVaultName = RepositoryName
                            };
                            Configuration.SetCustomSynchronizationSettings(solution.Uid, settings);
                            Configuration.Save();

                            Console.WriteLine($"Configured Azure Key Vault ({RepositoryName}) as the repository for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                            return 0;
                        }
                        else
                        {
                            Console.WriteLine($"The repository name is not valid: {RepositoryName}\n");
                        }
                    }
                }
            }
            return 1;
        }

    }
}
