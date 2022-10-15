using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Create the encryption key.")]
    [EncryptionKeyParametersValidation]
    internal class InitCommand : EncryptionKeyCommand
	{
        [Option("-p|--passphrase", Description = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option("-f|--keyfile <path>", Description = "Key file path to use for creating the encryption key.")]
        public string? KeyFile { get; set; }


        public async Task<int> OnExecute(CommandLineApplication? app = null)
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            if (
                Passphrase == null
                && KeyFile == null
                )
            {
                app?.ShowHelp();
                return 1;
            }

            string? keyFile = KeyFile != null ? EnsureFullyQualifiedPath(KeyFile) : null;

			if (AreEncryptionKeyParametersValid(Passphrase, keyFile))
			{
				GenerateEncryptionKey(Passphrase, keyFile);

                // Ensure authorization on the default repository
                if (!await Context.Current.Repository.IsReady())
                {
                    await Context.Current.Repository.AuthorizeAsync();
                }
            }

            await Context.Current.Cipher.RefreshStatus();

            return 0;
        }

	}
}
