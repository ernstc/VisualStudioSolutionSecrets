﻿using System;
using System.IO;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class InitCommand : EncryptionKeyCommand<InitOptions>
	{

        protected override async Task Execute(InitOptions options)
		{
			string? keyFile = options.KeyFile;
			if (keyFile != null && !Path.IsPathFullyQualified(keyFile))
			{
				keyFile = Path.Combine(Context.IO.GetCurrentDirectory(), keyFile);
            }

            if (AreEncryptionKeyParametersValid(options.Passphrase, keyFile))
			{
				GenerateEncryptionKey(options.Passphrase, keyFile);
				await AuthenticateRepositoryAsync();
			}
		}

    }
}

