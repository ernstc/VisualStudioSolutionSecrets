using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Change the encryption key and encrypts all existing secrets with the new key.")]
    [EncryptionKeyParametersValidation]
    internal class ChangeKeyCommand : EncryptionKeyCommand
	{
        [Option("-p|--passphrase", Description = "Passphare for creating the encryption key.")]
        public string? Passphrase { get; set; }

        [Option("-f|--keyfile <path>", Description = "Key file path to use for creating the encryption key.")]
        [FileExists]
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

            if (!await CanSync())
            {
                return 1;
            }

            string? keyFile = EnsureFullyQualifiedPath(KeyFile);

            if (!AreEncryptionKeyParametersValid(Passphrase, keyFile))
            {
                return 1;
            }

            // Ensure authorization on the default repository
            if (!await Context.Current.Repository.IsReady())
            {
                await Context.Current.Repository.AuthorizeAsync();
            }

            Console.Write("Loading existing secrets... ");
            ICollection<SolutionSettings> allSecrets = await Context.Current.Repository.PullAllSecretsAsync();

            Console.WriteLine("Done");

            if (allSecrets.Count == 0)
            {
                Console.WriteLine("\nThere are no solution settings that need to be encrypted with the new key.");
            }

            var successfulSolutionSecrets = new List<SolutionSettings>();

            foreach (var solutionSecrets in allSecrets)
            {
                bool decryptionSucceded = true;
                var decryptedSettings = new List<(string name, string? content)>();

                foreach (var settings in solutionSecrets.Settings)
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

                    var decryptedFiles = new Dictionary<string, string>();
                    foreach (var secret in secretFiles)
                    {
                        string configFileName = secret.Key;

                        // This check is for compatibility with version 1.0.x
                        if (configFileName == "content")
                        {
                            configFileName = "secrets.json";
                        }

                        var decryptedContent = Context.Current.Cipher.Decrypt(secret.Value);
                        if (decryptedContent == null)
                        {
                            decryptionSucceded = false;
                            break;
                        }
                        decryptedFiles.Add(secret.Key, decryptedContent);
                    }

                    decryptedSettings.Add((settings.name, JsonSerializer.Serialize(decryptedFiles)));
                }

                if (decryptionSucceded)
                {
                    successfulSolutionSecrets.Add(new SolutionSettings
                    {
                        Name = solutionSecrets.Name,
                        Settings = decryptedSettings
                    });
                }
            }

            if (successfulSolutionSecrets.Count != allSecrets.Count)
            {
                Console.WriteLine("\n    Attention!");
                Console.WriteLine("    Some solution settings cannot be decrypted with the current encryption key.");
                Console.WriteLine("    Only some solutions will be re-encrypted with the new key.\n");
                if (!Confirm())
                {
                    return 1;
                }
            }

            GenerateEncryptionKey(Passphrase, keyFile);

            Console.WriteLine("Saving secrets with the new key...\n");

            foreach (var solutionSecrets in successfulSolutionSecrets)
            {
                Console.Write($"- {solutionSecrets.Name}... ");
                
                var headerFile = new HeaderFile
                {
                    visualStudioSolutionSecretsVersion = Versions.VersionString!,
                    lastUpload = DateTime.UtcNow,
                    solutionFile = solutionSecrets.Name
                };

                var files = new List<(string fileName, string? content)>
                {
                    ("secrets", JsonSerializer.Serialize(headerFile))
                };

                bool failed = false;
                foreach (var settings in solutionSecrets.Settings)
                {
                    if (settings.content == null)
                        continue;

                    var secretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.content);
                    if (secretFiles == null)
                    {
                        failed = true;
                        break;
                    }

                    var encryptedFiles = new Dictionary<string, string>();
                    foreach (var secret in secretFiles)
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

                if (!failed)
                {
                    if (!await Context.Current.Repository.PushFilesAsync(solutionSecrets, files))
                    {
                        failed = true;
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");

            await Context.Current.Cipher.RefreshStatus();

            return 0;
        }

    }
}

