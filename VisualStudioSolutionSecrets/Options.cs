using System;
using System.Collections.Generic;
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
    }


    [Verb("changekey", HelpText = "Change the encryption key and encrypts all existing secrets with the new key.")]
    internal class ChangeKeyOptions
    {
        [Option('p', "passphrase", Group = "Key", HelpText = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option('f', "keyfile", Group = "Key", HelpText = "Key file path to use for creating the encryption key.")]
        public string? KeyFile { get; set; }
    }


    [Verb("push", HelpText = "Push encrypted solution secrets.")]
    internal class PushSecretsOptions
    {
        [Option("path")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }


    [Verb("pull", HelpText = "Pull solution secrets and decrypt them.")]
    internal class PullSecretsOptions
    {
        [Option("path", HelpText = "Path for searching solutions.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }



    [Verb("search", HelpText = "Search for solution secrets.")]
    internal class SearchSecretsOptions
    {
        [Option("path", HelpText = "Path for searching solutions.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }


    [Verb("status", HelpText = "Shows the status for the tool and the solutions.")]
    internal class StatusCheckOptions
    {
        [Option("path", HelpText = "Path for searching solutions. The status for the found solutions will be shown.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }


    [Verb("configure", HelpText = "Configure the repository to use by default or for the solution in the current directory.")]
    internal class ConfigureOptions
    {
        [Option("default", HelpText = "Changes the default configuration.", Default = false)]
        public bool Default { get; set; }

        [Option('r', "repo", Required = true, HelpText = "Repository type to use for the solution: \"github\" or \"azurekv\"")]
        public RepositoryTypesEnum RepositoryType { get; set; }

        [Option('n', "name", HelpText = "Repository name to use for the solution. This setting applies only for Azure Key Vault.")]
        public string? RepositoryName { get; set; }

        //[Usage]
        //public static IEnumerable<Example> UsageExamples
        //{
        //    get
        //    {
        //        return new List<Example>() {
        //            new Example("Configure the solution to use GitHub", new ConfigureOptions { RepositoryType = RepositoryTypesEnum.GitHub }),
        //            new Example("Configure the solution to use Azure Key Vault", new ConfigureOptions { RepositoryType = RepositoryTypesEnum.AzureKV, RepositoryName = "my-keyvault" }),
        //            new Example("Set GitHub as the default repository", new ConfigureOptions { RepositoryType = RepositoryTypesEnum.GitHub, Default = true }),
        //        };
        //    }
        //}
    }
}
