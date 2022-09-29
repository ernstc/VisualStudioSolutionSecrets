using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets
{

    [Verb("init", HelpText = "Create the encryption key.")]
    internal class InitOptions
    {
        [Option('p', "passphrase", Group = "Key", HelpText = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option('f', "keyfile", Group = "Key", HelpText = "Key file path to use for creating the encryption key.")]
        public string? KeyFile { get; set; }

        [Usage]
        public static IEnumerable<Example> UsageExamples
        {
            get
            {
                return new List<Example>() {
                    new Example("Create encryption key with a passphrase", new InitOptions { Passphrase = "my passphrase" }),
                    new Example("Create encryption key from a file", new InitOptions { KeyFile = Path.Combine(".", "key-file.txt") }),
                };
            }
        }
    }


    [Verb("changekey", HelpText = "Change the encryption key and encrypts all existing secrets with the new key.")]
    internal class ChangeKeyOptions
    {
        [Option('p', "passphrase", Group = "Key", HelpText = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option('f', "keyfile", Group = "Key", HelpText = "Key file path to use for creating the encryption key.")]
        public string? KeyFile { get; set; }

        [Usage]
        public static IEnumerable<Example> UsageExamples
        {
            get
            {
                return new List<Example>() {
                    new Example("Change the encryption key with a passphrase", new ChangeKeyOptions { Passphrase = "my passphrase" }),
                    new Example("Change the encryption key from a file", new ChangeKeyOptions { KeyFile = Path.Combine(".", "key-file.txt") }),
                };
            }
        }
    }


    [Verb("push", HelpText = "Push encrypted solution secrets.")]
    internal class PushSecretsOptions
    {
        [Option("path", HelpText = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }

        [Usage]
        public static IEnumerable<Example> UsageExamples
        {
            get
            {
                return new List<Example>() {
                    new Example("Push secrets for all solutions found in the current folder", new PushSecretsOptions()),
                    new Example("Push secrets for all solutions found in the specified folder", new PushSecretsOptions { Path = System.IO.Path.Combine(".", "MySolution") }),
                    new Example("Push secrets for all solutions found in the specified folder tree", new PushSecretsOptions { Path = System.IO.Path.Combine(".", "MySolution"), All = true }),
                    new Example("Push secrets for the specified solution file", new PushSecretsOptions { Path = System.IO.Path.Combine(".", "MySolution", "MySolution.sln") }),
                };
            }
        }
    }


    [Verb("pull", HelpText = "Pull solution secrets and decrypt them.")]
    internal class PullSecretsOptions
    {
        [Option("path", HelpText = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }
    }



    [Verb("search", HelpText = "Search for solution secrets.")]
    internal class SearchSecretsOptions
    {
        [Option("path", HelpText = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }
    }


    [Verb("status", HelpText = "Shows the status for the tool and the solutions.")]
    internal class StatusCheckOptions
    {
        [Option("path", HelpText = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }
    }


    [Verb("configure", HelpText = "Configure the repository to use by default or for the solution in the current directory.")]
    internal class ConfigureOptions
    {
        [Option("default", SetName = "set", HelpText = "Changes the default configuration.")]
        public bool Default { get; set; }

        [Option('r', "repo", SetName = "set", HelpText = "Repository type to use for the solution: \"github\" or \"azurekv\"")]
        public string? RepositoryType { get; set; }

        [Option('n', "name", SetName = "set", HelpText = "Repository name to use for the solution. This setting applies only for Azure Key Vault.")]
        public string? RepositoryName { get; set; }

        [Option("reset", SetName = "reset", HelpText = "Reset the configuration of the solution.")]
        public bool Reset { get; set; }

        [Option("path", HelpText = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Usage]
        public static IEnumerable<Example> UsageExamples
        {
            get
            {
                return new List<Example>() {
                    new Example("Configure the solution to use GitHub", new ConfigureOptions { RepositoryType = nameof(RepositoryTypesEnum.GitHub) }),
                    new Example("Configure the solution to use Azure Key Vault", new ConfigureOptions { RepositoryType = nameof(RepositoryTypesEnum.AzureKV), RepositoryName = "my-keyvault" }),
                    new Example("Set GitHub as the default repository", new ConfigureOptions { RepositoryType = nameof(RepositoryTypesEnum.GitHub), Default = true }),
                    new Example("Reset the solution configuration", new ConfigureOptions { Reset = true }),
                };
            }
        }
    }
}
