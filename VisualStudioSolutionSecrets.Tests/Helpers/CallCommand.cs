using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using static VisualStudioSolutionSecrets.Tests.Commands.EncryptionKeyCommandTests;

namespace VisualStudioSolutionSecrets.Tests.Helpers
{
    internal static class CallCommand
    {

        public static Task Configure(ConfigureOptions options)
        {
            var command = new ConfigureCommand
            {
                Default = options.Default,
                Path = options.Path,
                RepositoryName = options.RepositoryName,
                RepositoryType = options.RepositoryType,
                Reset = options.Reset
            };
            command.OnExecute();
            return Task.CompletedTask;
        }


        public static async Task Init(InitOptions options)
        {
            var command = new InitCommand
            {
                KeyFile = options.KeyFile,
                Passphrase = options.Passphrase
            };
            await command.OnExecute();
            await Context.Current.Cipher.RefreshStatus();
        }


        public static async Task Push(PushSecretsOptions options)
        {
            var command = new PushCommand
            {
                 All = options.All,
                 Path = options.Path
            };
            await command.OnExecute();
        }


        public static async Task Pull(PullSecretsOptions options)
        {
            var command = new PullCommand
            {
                All = options.All,
                Path = options.Path
            };
            await command.OnExecute();
        }


        public static async Task ChangeKey(ChangeKeyOptions options)
        {
            var command = new ChangeKeyCommand
            {
                KeyFile = options.KeyFile,
                Passphrase = options.Passphrase
            };
            await command.OnExecute();
            await Context.Current.Cipher.RefreshStatus();
        }


        public static Task Search(SearchSecretsOptions options)
        {
            var command = new SearchCommand
            {
                All = options.All,
                Path = options.Path
            };
            command.OnExecute();
            return Task.CompletedTask;
        }


        public static async Task Status(StatusCheckOptions options)
        {
            var command = new StatusCommand
            {
                All = options.All,
                Path = options.Path
            };
            await command.OnExecute();
        }

    }
}
