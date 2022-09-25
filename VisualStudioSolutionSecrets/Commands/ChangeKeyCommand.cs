using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;
using static System.Net.WebRequestMethods;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class ChangeKeyCommand : EncryptionKeyCommand<ChangeKeyOptions>
	{

        protected override async Task Execute(ChangeKeyOptions options)
        {
            if (!await CanSync())
            {
                return;
            }

            string? keyFile = EnsureFullyQualifiedPath(options.KeyFile);

            if (!AreEncryptionKeyParametersValid(options.Passphrase, keyFile))
            {
                return;
            }

            await AuthenticateRepositoryAsync();

            Console.Write("Loading existing secrets... ");
            var allSecrets = await Context.Repository.PullAllSecretsAsync();
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

                        string? decryptedContent = Context.Cipher.Decrypt(secret.Value);
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
                        SolutionName = solutionSecrets.SolutionName,
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
                    return;
                }
            }

            GenerateEncryptionKey(options.Passphrase, keyFile);

            Console.WriteLine("Saving secrets with the new key...\n");

            foreach (var solutionSecrets in successfulSolutionSecrets)
            {
                Console.Write($"- {solutionSecrets.SolutionName}... ");
                
                var headerFile = new HeaderFile
                {
                    visualStudioSolutionSecretsVersion = Versions.VersionString!,
                    lastUpload = DateTime.UtcNow,
                    solutionFile = solutionSecrets.SolutionName
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
                        string? encryptedContent = Context.Cipher.Encrypt(secret.Value);
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
                    Context.Repository.SolutionName = solutionSecrets.SolutionName;
                    if (!await Context.Repository.PushFilesAsync(files))
                    {
                        failed = true;
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }

    }
}

