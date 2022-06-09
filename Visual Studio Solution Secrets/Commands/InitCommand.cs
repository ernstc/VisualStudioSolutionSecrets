using System;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class InitCommand : EncryptionKeyCommand<InitOptions>
	{

        protected override async Task Execute(InitOptions options)
		{
			if (AreEncryptionKeyParametersValid(options.Passphrase, options.KeyFile))
			{
				GenerateEncryptionKey(options.Passphrase, options.KeyFile);
				await AuthenticateRepositoryAsync();
			}
		}

    }
}

