﻿using System;
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

        public static async Task Configure(ConfigureOptions options)
        {
            var command = new ConfigureCommand();
            await command.Execute(options);
        }


        public static async Task Init(InitOptions options)
        {
            var command = new InitCommand();
            await command.Execute(options);
            await Context.Current.Cipher.RefreshStatus();
        }


        public static async Task Push(PushSecretsOptions options)
        {
            var command = new PushSecretsCommand();
            await command.Execute(options);
        }


        public static async Task Pull(PullSecretsOptions options)
        {
            var command = new PullSecretsCommand();
            await command.Execute(options);
        }


        public static async Task ChangeKey(ChangeKeyOptions options)
        {
            var command = new ChangeKeyCommand();
            await command.Execute(options);
            await Context.Current.Cipher.RefreshStatus();
        }


        public static async Task Search(SearchSecretsOptions options)
        {
            var command = new SearchSecretsCommand();
            await command.Execute(options);
        }


        public static async Task Status(StatusCheckOptions options)
        {
            var command = new StatusCheckCommand();
            await command.Execute(options);
        }

    }
}
