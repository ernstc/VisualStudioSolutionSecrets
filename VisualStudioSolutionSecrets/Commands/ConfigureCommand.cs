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

            if (
                !Default
                && RepositoryType == null
                && RepositoryName == null
                && !Reset
                && Path == null
                )
            {
                app?.ShowHelp();
                return 1;
            }

            if (Default)
            {
                if (String.Equals(nameof(RepositoryTypesEnum.GitHub), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    Configuration.Default.Repository = RepositoryTypesEnum.GitHub;
                    Configuration.Default.AzureKeyVaultName = null;
                    Configuration.Save();

                    Console.WriteLine("Configured GitHub Gist as the default repository.\n");
                }
                else if (String.Equals(nameof(RepositoryTypesEnum.AzureKV), RepositoryType, StringComparison.OrdinalIgnoreCase))
                {
                    Configuration.Default.Repository = RepositoryTypesEnum.AzureKV;
                    Configuration.Default.AzureKeyVaultName = RepositoryName;
                    Configuration.Save();

                    Console.WriteLine($"Configured Azure Key Vault ({RepositoryName}) as the default repository.\n");
                }
            }
            else
            {
                string path = Context.Current.IO.GetCurrentDirectory();

                string[] solutionFiles = GetSolutionFiles(path, false);
                if (solutionFiles.Length == 0)
                {
                    Console.WriteLine("Solution file not found.\n");
                    return 1;
                }

                var solutionFilePath = solutionFiles[0];
                SolutionFile solution = new SolutionFile(solutionFilePath);

                if (Reset)
                {
                    Configuration.SetCustomSynchronizationSettings(solution.Uid, null);
                    Configuration.Save();

                    Console.WriteLine($"Removed custom configuration for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                }
                else
                {
                    var settings = new SolutionSynchronizationSettings();

                    if (String.Equals(nameof(RepositoryTypesEnum.GitHub), RepositoryType, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Repository = RepositoryTypesEnum.GitHub;
                        settings.AzureKeyVaultName = null;
                        Configuration.SetCustomSynchronizationSettings(solution.Uid, settings);
                        Configuration.Save();

                        Console.WriteLine($"Configured GitHub Gist as the repository for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                    }
                    else if (String.Equals(nameof(RepositoryTypesEnum.AzureKV), RepositoryType, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Repository = RepositoryTypesEnum.AzureKV;
                        settings.AzureKeyVaultName = RepositoryName;
                        Configuration.SetCustomSynchronizationSettings(solution.Uid, settings);
                        Configuration.Save();

                        Console.WriteLine($"Configured Azure Key Vault ({RepositoryName}) as the repository for the solution \"{solution.Name}\" ({solution.Uid}).\n");
                    }
                }
            }

            return 0;
        }

    }
}
