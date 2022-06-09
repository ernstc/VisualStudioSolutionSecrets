using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class ChangeKeyCommand : EncryptionKeyCommand<ChangeKeyOptions>
	{

        /*
        * 1) Validate encryption key parameters
        * 2) Authenticate to repository
        * 3) Load existing secrets with the current key
        * 4) Generate the new encryption key
        * 5) Encrypt secrets with the new key
        * 6) Push encrypted secrets
        * 
        */

        protected override async Task Execute(ChangeKeyOptions options)
        {
            if (!await CanSync())
            {
                return;
            }

            if (!AreEncryptionKeyParametersValid(options.Passphrase, options.KeyFile))
            {
                return;
            }

            await AuthenticateRepositoryAsync();


            Console.Write("Loading existing secrets ...");
            var allSecrets = await Context.Repository.PullAllSecretsAsync();
            Console.WriteLine("Done\n");

            if (allSecrets.Count == 0)
            {
                Console.WriteLine("\nThere are no solution settings to that need to be encrypted with the new key.");
            }

            var successfulSolutionSecrets = new List<SolutionSettings>();

            foreach (var solutionSecrets in allSecrets)
            {
                bool decryptionSucceded = true;
                var decryptedSettings = new List<(string name, string? content)>();
                foreach (var settings in solutionSecrets.Settings)
                {
                    if (settings.content == null)
                        continue;

                    string? decryptedContent = Context.Cipher.Decrypt(settings.content);
                    if (decryptedContent == null)
                    {
                        decryptionSucceded = false;
                        break;
                    }
                    decryptedSettings.Add((settings.name, decryptedContent));
                }
                if (decryptionSucceded)
                {
                    solutionSecrets.Settings = decryptedSettings;
                    successfulSolutionSecrets.Add(solutionSecrets);
                }
            }

            if (successfulSolutionSecrets.Count != allSecrets.Count)
            {
                Console.WriteLine("\n    WARN: Some solution settings cannot be decrypted with the current encryption key.");
                if (!Confirm())
                {
                    return;
                }
            }

            GenerateEncryptionKey(options.Passphrase, options.KeyFile);

            Console.Write("Saving secrets with the new key ...");

            // TODO: Save secrets
            foreach (var solutionSecrets in successfulSolutionSecrets)
            {
                var headerFile = new HeaderFile
                {
                    visualStudioSolutionSecretsVersion = Context.VersionString!,
                    lastUpload = DateTime.UtcNow,
                    solutionFile = solutionSecrets.SolutionName
                };

                List<(string fileName, string? content)> files = new List<(string fileName, string? content)>();
                files.Add(("secrets", JsonSerializer.Serialize(headerFile)));



                Dictionary<string, Dictionary<string, string>> secrets = new Dictionary<string, Dictionary<string, string>>();

                bool failed = false;
                foreach (var settings in solutionSecrets.Settings)
                {
                    var settingFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(settings.content);
                    var encryptedSettingFiles = new Dictionary<string, string>();

                    foreach (var entry in settingFiles)
                    {
                        string? encryptedValue = Context.Cipher.Encrypt(entry.Value);
                        if (encryptedValue != null)
                        {
                            encryptedSettingFiles.Add(entry.Key, encryptedValue);
                        }
                    }

                    /*
                    if (configFile.content != null)
                    {
                        if (configFile.Encrypt())
                        {
                            if (!secrets.ContainsKey(configFile.GroupName))
                            {
                                secrets.Add(configFile.GroupName, new Dictionary<string, string>());
                            }
                            secrets[configFile.GroupName].Add(configFile.FileName, configFile.Content);
                        }
                        else
                        {
                            failed = true;
                            break;
                        }
                    }
                    */
                }

                foreach (var group in secrets)
                {
                    string groupContent = JsonSerializer.Serialize(group.Value);
                    files.Add((group.Key, groupContent));
                }

                if (!failed)
                {
                    if (!await Context.Repository.PushFilesAsync(files))
                    {
                        failed = true;
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");



            }

            Console.WriteLine("Done\n");
        }

    }
}

