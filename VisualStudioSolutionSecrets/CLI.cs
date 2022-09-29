using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;

namespace VisualStudioSolutionSecrets
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EncryptionKeyParametersValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is InitCommand_ initCommand)
            {
                if (!String.IsNullOrEmpty(initCommand.Passphrase) && !String.IsNullOrEmpty(initCommand.KeyFile))
                {
                    return new ValidationResult("\nSpecify -p|--passphrase or -f|--key-file, not both.\n");
                }
            }
            else if (value is ChangeKeyCommand_ changeKeyCommand)
            {
                if (!String.IsNullOrEmpty(changeKeyCommand.Passphrase) && !String.IsNullOrEmpty(changeKeyCommand.KeyFile))
                {
                    return new ValidationResult("\nSpecify -p|--passphrase or -f|--key-file, not both.\n");
                }
            }
            return ValidationResult.Success;
        }
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigureCommandValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is ConfigureCommand_ command)
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




    [Command("vs-secrets")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(
         typeof(InitCommand_),
         typeof(ChangeKeyCommand_),
         typeof(PushCommand),
         typeof(PullCommand),
         typeof(SearchCommand),
         typeof(StatusCommand),
         typeof(ConfigureCommand_)
    )]
    internal class VsSecrets
    {
        private string? GetVersion()
            => $"vs-secrets {typeof(VsSecrets).Assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}";


        static void Main(string[] args)
        {
            CommandLineApplication.Execute<VsSecrets>(args);
        }


        protected int OnExecute(CommandLineApplication app)
        {
            Console.WriteLine(">>> Logo <<<\n");
            // this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 1;
        }
    }


    [Command(Description = "Create the encryption key.")]
    [EncryptionKeyParametersValidation]
    internal class InitCommand_
    {
        [Option("-p|--passphrase", Description = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option("-f|--keyfile <path>", Description = "Key file path to use for creating the encryption key.")]
        [FileExists]
        public string? KeyFile { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            //// this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }


    [Command(Description = "Change the encryption key and encrypts all existing secrets with the new key.")]
    [EncryptionKeyParametersValidation]
    internal class ChangeKeyCommand_
    {
        [Option("-p|--passphrase", Description = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option("-f|--keyfile <path>", Description = "Key file path to use for creating the encryption key.")]
        [FileExists]
        public string? KeyFile { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            //// this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }


    [Command(Description = "Push encrypted solution secrets.")]
    internal class PushCommand
    {
        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            //// this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }


    [Command(Description = "Pull solution secrets and decrypt them.")]
    internal class PullCommand
    {
        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            //// this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }


    [Command(Description = "Search for solution secrets.")]
    internal class SearchCommand
    {
        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            //// this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }


    [Command(Description = "Shows the status for the tool and the solutions.")]
    internal class StatusCommand
    {
        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        protected int OnExecute(CommandLineApplication app)
        {
            //// this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }


    [Command(Description = "Configure the repository to use by default or for the solution in the current directory.")]
    [ConfigureCommandValidation]
    internal class ConfigureCommand_
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


        protected int OnExecute(CommandLineApplication app)
        {
            // this shows help even if the --help option isn't specified
            app.ShowHelp();
            return 0;
        }
    }
}
