using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;


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
    internal class PushSecrectsOptions
    {
        [Option("path")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }


    [Verb("pull", HelpText = "Pull solution secrets and decrypt them.")]
    internal class PullSecrectsOptions
    {
        [Option("path", HelpText = "Path for searching solutions.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }



    [Verb("search", HelpText = "Search for solution secrets.")]
    internal class SearchSecrectsOptions
    {
        [Option("path", HelpText = "Path for searching solutions.")]
        public string? Path { get; set; }

        [Option("all", HelpText = "When true, search in the specified path and its sub-tree.", Default = false)]
        public bool All { get; set; }
    }


    [Verb("status", HelpText = "Shows initialization status for the tool.")]
    internal class StatusOptions
    {
    }

}
