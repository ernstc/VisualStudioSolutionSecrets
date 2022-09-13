using System;
using System.Text.RegularExpressions;


namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

	internal abstract class EncryptionKeyCommand<TOptions> : Command<TOptions>
	{

        private bool ValidatePassphrase(string passphrase)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                return false;
            }

            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMiniMaxChars = new Regex(@".{8,}");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

            return
                hasLowerChar.IsMatch(passphrase)
                && hasUpperChar.IsMatch(passphrase)
                && hasMiniMaxChars.IsMatch(passphrase)
                && hasNumber.IsMatch(passphrase)
                && hasSymbols.IsMatch(passphrase);
        }


        protected bool AreEncryptionKeyParametersValid(string? passphrase, string? keyFile)
        {
            if (!string.IsNullOrEmpty(passphrase))
            {
                if (!string.IsNullOrEmpty(keyFile))
                {
                    Console.WriteLine("\n    WARN: You have specified passphrase and keyfile, but only passphrase will be used.");
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
                if (!Context.Current.IO.FileExists(keyFile))
                {
                    Console.WriteLine("\n    ERR: Cannot create encryption key. Key file not found.");
                    return false;
                }
            }
            return true;
        }


        protected void GenerateEncryptionKey(string? passphrase, string? keyFile)
        {
            Console.Write("Generating encryption key ...");
            if (!string.IsNullOrEmpty(passphrase))
            {
                Context.Cipher.Init(passphrase);
            }
            else if (!string.IsNullOrEmpty(keyFile))
            {
                using var file = Context.Current.IO.FileOpenRead(keyFile);
                Context.Cipher.Init(file);
                file.Close();
            }
            Console.WriteLine("Done\n");
        }

    }
}

