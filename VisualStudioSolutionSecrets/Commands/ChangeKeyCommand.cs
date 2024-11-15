using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Change the encryption key. By default re-encrypts all existing secrets with the new key.")]
    [EncryptionKeyParametersValidation]
    internal class ChangeKeyCommand : EncryptionKeyCommand
    {
        [Option("-p|--passphrase", Description = "Passphrase for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option("-f|--keyfile <path>", Description = "Key file path to use for creating the encryption key.")]
        [FileExists]
        public string? KeyFile { get; set; }

        [Option("-s|--skipencryption", Description = "Skip the re-encryption of secrets encrypted with the old key.")]
        public bool SkipEncryption { get; set; }



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

            if (!await Context.Current.Cipher.IsReady())
            {
                Console.WriteLine("Encryption key cannot be found.");
                Console.WriteLine("For generating the encryption key, use the command below:\n\n    vs-secrets init\n");
                return 1;
            }

            string? keyFile = KeyFile != null ? EnsureFullyQualifiedPath(KeyFile) : null;

            if (!AreEncryptionKeyParametersValid(Passphrase, keyFile))
            {
                return 1;
            }

            Dictionary<IRepository, IList<SolutionSettings>> successfulSolutionSecretsByRepository = new Dictionary<IRepository, IList<SolutionSettings>>();

            if (!SkipEncryption)
            {
                bool canDecryptAllSecrets = true;
                IEnumerable<IRepository> repositories = Context.Current.GetServices<IRepository>().Where(r => r.EncryptOnClient);

                // Read the existing secrets if the repository encrypts on the client
                foreach (IRepository repository in repositories)
                {
                    try
                    {
                        // Ensure authorization on the default repository
                        if (!await repository.IsReady())
                        {
                            await repository.AuthorizeAsync();
                        }

                        Console.Write("Loading existing secrets... ");
                        ICollection<SolutionSettings> allSecrets = await repository.PullAllSecretsAsync();

                        Console.WriteLine("Done");

                        if (allSecrets.Count == 0)
                        {
                            Console.WriteLine("\nThere are no solution settings that need to be encrypted with the new key.");
                        }

                        List<SolutionSettings> successfulSolutionSecrets = new List<SolutionSettings>();

                        foreach (SolutionSettings solutionSecrets in allSecrets)
                        {
                            bool decryptionSucceeded = true;
                            List<(string name, string? content)> decryptedSettings = new List<(string name, string? content)>();

                            foreach ((string name, string? content) settings in solutionSecrets.Settings)
                            {
                                if (settings.content == null)
                                {
                                    continue;
                                }

                                Dictionary<string, string>? secretFiles = null;
                                try
                                {
                                    secretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.content);
                                }
                                catch
                                {
                                    Console.Write($"\n    ERR: File content cannot be read: {settings.name}");
                                }

                                if (secretFiles == null)
                                {
                                    break;
                                }

                                Dictionary<string, string> decryptedFiles = new Dictionary<string, string>();
                                foreach (KeyValuePair<string, string> secret in secretFiles)
                                {
                                    string? decryptedContent = Context.Current.Cipher.Decrypt(secret.Value);
                                    if (decryptedContent == null)
                                    {
                                        decryptionSucceeded = false;
                                        break;
                                    }
                                    decryptedFiles.Add(secret.Key, decryptedContent);
                                }

                                decryptedSettings.Add((settings.name, JsonSerializer.Serialize(decryptedFiles)));
                            }

                            if (decryptionSucceeded)
                            {
                                successfulSolutionSecrets.Add(new SolutionSettings(decryptedSettings)
                                {
                                    Name = solutionSecrets.Name
                                });
                            }
                        }

                        if (successfulSolutionSecrets.Count != allSecrets.Count)
                        {
                            canDecryptAllSecrets = false;
                        }

                        if (successfulSolutionSecrets.Count > 0)
                        {
                            successfulSolutionSecretsByRepository.Add(repository, successfulSolutionSecrets);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }

                if (!canDecryptAllSecrets)
                {
                    Console.WriteLine("\n    Attention!");
                    Console.WriteLine("    Some solution settings cannot be decrypted with the current encryption key.");
                    Console.WriteLine("    Only some solutions will be re-encrypted with the new key.\n");
                    if (!Confirm())
                    {
                        return 1;
                    }
                }
            }

            // Generate the new encryption Key
            GenerateEncryptionKey(Passphrase, keyFile);

            // Re-encrypt the secrets with the new Key, if the repository encrypts on the server
            if (successfulSolutionSecretsByRepository.Count > 0)
            {
                Console.WriteLine("Saving secrets with the new key...\n");

                foreach (KeyValuePair<IRepository, IList<SolutionSettings>> item in successfulSolutionSecretsByRepository)
                {
                    IRepository repository = item.Key;
                    IList<SolutionSettings> successfulSolutionSecrets = item.Value;

                    foreach (SolutionSettings solutionSecrets in successfulSolutionSecrets)
                    {
                        Console.Write($"- {solutionSecrets.Name}... ");

                        HeaderFile headerFile = new HeaderFile
                        {
                            VisualStudioSolutionSecretsVersion = Versions.VersionString!,
                            LastUpload = DateTime.UtcNow,
                            SolutionFile = solutionSecrets.Name
                        };

                        List<(string fileName, string? content)> files = new List<(string fileName, string? content)>
                    {
                        ("secrets", JsonSerializer.Serialize(headerFile))
                    };

                        bool failed = false;
                        foreach ((string name, string? content) settings in solutionSecrets.Settings)
                        {
                            if (settings.content == null)
                            {
                                continue;
                            }

                            Dictionary<string, string>? secretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.content);
                            if (secretFiles == null)
                            {
                                failed = true;
                                break;
                            }

                            Dictionary<string, string> encryptedFiles = new Dictionary<string, string>();
                            foreach (KeyValuePair<string, string> secret in secretFiles)
                            {
                                string? encryptedContent = Context.Current.Cipher.Encrypt(secret.Value);
                                if (encryptedContent == null)
                                {
                                    failed = true;
                                    break;
                                }

                                // This change is for upgrading the secrets file to the 1.2.x+ format.
                                string fileName = secret.Key;
                                if (fileName == "content")
                                {
                                    fileName = "secrets.json";
                                }

                                encryptedFiles.Add(fileName, encryptedContent);
                            }

                            if (failed)
                            {
                                break;
                            }

                            files.Add((settings.name, JsonSerializer.Serialize(encryptedFiles)));
                        }

                        if (!failed && !await repository.PushFilesAsync(solutionSecrets, files))
                        {
                            failed = true;
                        }

                        Console.WriteLine(failed ? "Failed" : "Done");
                    }
                }

                Console.WriteLine("\nFinished.\n");
            }

            await Context.Current.Cipher.RefreshStatus();

            return 0;
        }

    }
}
