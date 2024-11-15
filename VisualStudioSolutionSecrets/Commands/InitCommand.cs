using System;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Create the encryption key and setup all needed authorizations to remote repositories.")]
    [EncryptionKeyParametersValidation]
    internal class InitCommand : EncryptionKeyCommand
    {
        [Option("-p|--passphrase", Description = "Passphrase for creating the encryption key.")]
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
                else
                {
                    app?.ShowHint();
                    return 1;
                }
            }
            else
            {
                Console.WriteLine($"The encryption key is already defined. For changing the encryption key, use the command \"change-key\".\n");
            }

            if (isCipherReady)
            {
                // Check if any repositories that needs encryption on the client need to be authorized.
                foreach (IRepository repository in Context.Current.GetServices<IRepository>().Where(r => r.EncryptOnClient))
                {
                    if (await repository.IsReady())
                    {
                        Console.WriteLine($"Access to {repository.GetFriendlyName()} is authorized.\n");
                    }
                    else
                    {
                        Console.WriteLine($"Access to {repository.GetFriendlyName()} needs be authorized.");
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
