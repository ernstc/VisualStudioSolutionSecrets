using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

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

            bool isCipherReady = await Context.Current.Cipher.IsReady();
            
            if (!isCipherReady)
            {
                string? keyFile = KeyFile != null ? EnsureFullyQualifiedPath(KeyFile) : null;
            
                if (AreEncryptionKeyParametersValid(Passphrase, keyFile))
                {
                    GenerateEncryptionKey(Passphrase, keyFile);
                    await Context.Current.Cipher.RefreshStatus();
                    isCipherReady = await Context.Current.Cipher.IsReady();
                }
            }
            else
            {
                Console.WriteLine($"The encryption key is already defined. For changing the encryption key, use the command \"change-key\".");
            }

            if (isCipherReady)
            {
                // Check if any repositories that needs encryption on the client need to be authorized.
                IEnumerable<IRepository> repositories = Context.Current.GetServices<IRepository>().Where(r => r.EncryptOnClient);
                foreach (IRepository repository in repositories)
                {
                    if (!await repository.IsReady())
                    {
                        Console.WriteLine($"\nAccess to {repository.GetFriendlyName()} needs be authorized.");
                        if (Confirm())
                        {
                            await repository.AuthorizeAsync();
                            await repository.RefreshStatus();

                            if (await repository.IsReady())
                            {
                                Console.WriteLine($"Authorization to {repository.GetFriendlyName()} succeeded.");
                            }
                        }
                    }
                }
            }

            return 0;
        }

	}
}
