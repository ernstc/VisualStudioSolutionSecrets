using System;
using System.IO;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class InitCommand : EncryptionKeyCommand<InitOptions>
	{

        public override async Task Execute(InitOptions options)
		{
			string? keyFile = EnsureFullyQualifiedPath(options.KeyFile);

			if (AreEncryptionKeyParametersValid(options.Passphrase, keyFile))
			{
				GenerateEncryptionKey(options.Passphrase, keyFile);
				await AuthenticateRepositoryAsync();
			}
		}

	}
}

