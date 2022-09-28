using System;
using System.IO;
using System.Text.RegularExpressions;


namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

	internal abstract class EncryptionKeyCommand<TOptions> : Command<TOptions>
	{

        internal bool ValidatePassphrase(string passphrase)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                return false;
            }

            var startsWithSpace = new Regex(@"^\s");
            var endsWithSpace = new Regex(@"\s$");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasNumber = new Regex(@"[0-9]+");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]+");

            return
                !startsWithSpace.IsMatch(passphrase)
                && !endsWithSpace.IsMatch(passphrase)
                && hasLowerChar.IsMatch(passphrase)
                && hasUpperChar.IsMatch(passphrase)
                && hasNumber.IsMatch(passphrase)
                && hasSymbols.IsMatch(passphrase)
                && passphrase.Length >= 8;
        }


        protected bool AreEncryptionKeyParametersValid(string? passphrase, string? keyFile)
        {
            if (string.IsNullOrEmpty(passphrase) && string.IsNullOrEmpty(keyFile))
            {
                Console.WriteLine("\nYou must specify a passphrase or key file to create the encryption key.");
                return false;
            }
            if (!string.IsNullOrEmpty(passphrase))
            {
                if (!string.IsNullOrEmpty(keyFile))
                {
                    Console.WriteLine("\n    WARN: You have specified passphrase and key file, but only passphrase will be used.");
                }

                if (!ValidatePassphrase(passphrase))
                {
                    Console.WriteLine("\n    WARN: The passphrase is weak. It should contains at least 8 characters in upper and lower case, at least one digit and at least one symbol between !@#$%^&*()_+=[{]};:<>|./?,-\n");
                    if (!Confirm())
                    {
                        return false;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(keyFile))
            {
                if (!File.Exists(keyFile))
                {
                    Console.WriteLine("\n    ERR: Cannot create the encryption key. Key file not found.");
                    return false;
                }
            }
            return true;
        }


        protected void GenerateEncryptionKey(string? passphrase, string? keyFile)
        {
            Console.Write("\nGenerating encryption key... ");
            if (!string.IsNullOrEmpty(passphrase))
            {
                Context.Current.Cipher.Init(passphrase);
            }
            else if (!string.IsNullOrEmpty(keyFile))
            {
                using var file = File.OpenRead(keyFile);
                Context.Current.Cipher.Init(file);
                file.Close();
            }
            Console.WriteLine("Done\n");
        }

    }
}

