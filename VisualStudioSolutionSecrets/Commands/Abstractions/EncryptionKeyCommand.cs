using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;


namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class EncryptionKeyParametersValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is InitCommand initCommand)
            {
                return CheckParameters(initCommand.Passphrase, initCommand.KeyFile);
            }
            else if (value is ChangeKeyCommand changeKeyCommand)
            {
                return CheckParameters(changeKeyCommand.Passphrase, changeKeyCommand.KeyFile);
            }
            return ValidationResult.Success;
        }


        private static ValidationResult? CheckParameters(string? passphrase, string? keyFile)
        {
            return !String.IsNullOrEmpty(passphrase) && !String.IsNullOrEmpty(keyFile)
                ? new ValidationResult("\nSpecify -p|--passphrase or -f|--key-file, not both.\n")
                : ValidationResult.Success;
        }
    }



    internal abstract class EncryptionKeyCommand : CommandBase
    {

        private static readonly Regex startsWithSpace = new(@"^\s");
        private static readonly Regex endsWithSpace = new(@"\s$");
        private static readonly Regex hasLowerChar = new(@"[a-z]+");
        private static readonly Regex hasUpperChar = new(@"[A-Z]+");
        private static readonly Regex hasNumber = new(@"[0-9]+");
        private static readonly Regex hasSymbols = new(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]+");


        internal static bool ValidatePassphrase(string passphrase)
        {
            return !string.IsNullOrWhiteSpace(passphrase)
                   && !startsWithSpace.IsMatch(passphrase)
                   && !endsWithSpace.IsMatch(passphrase)
                   && hasLowerChar.IsMatch(passphrase)
                   && hasUpperChar.IsMatch(passphrase)
                   && hasNumber.IsMatch(passphrase)
                   && hasSymbols.IsMatch(passphrase)
                   && passphrase.Length >= 8;
        }


        protected static bool AreEncryptionKeyParametersValid(string? passphrase, string? keyFile)
        {
            if (string.IsNullOrEmpty(passphrase) && string.IsNullOrEmpty(keyFile))
            {
                Console.WriteLine("You must specify a passphrase or key file to create the encryption key.\n");
                return false;
            }
            if (!string.IsNullOrEmpty(passphrase))
            {
                if (!string.IsNullOrEmpty(keyFile))
                {
                    Console.WriteLine("    WARN: You have specified passphrase and key file, but only passphrase will be used.");
                }

                if (!ValidatePassphrase(passphrase))
                {
                    Console.WriteLine("    WARN: The passphrase is weak. It should contains at least 8 characters in upper and lower case, at least one digit and at least one symbol between !@#$%^&*()_+=[{]};:<>|./?,-\n");
                    if (!Confirm())
                    {
                        return false;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(keyFile) && !File.Exists(keyFile))
            {
                Console.WriteLine("    ERR: Cannot create the encryption key. Key file not found.");
                return false;
            }
            return true;
        }


        protected static void GenerateEncryptionKey(string? passphrase, string? keyFile)
        {
            Console.Write("Generating encryption key... ");
            if (!string.IsNullOrEmpty(passphrase))
            {
                Context.Current.Cipher.Init(passphrase);
            }
            else if (!string.IsNullOrEmpty(keyFile))
            {
                using FileStream file = File.OpenRead(keyFile);
                Context.Current.Cipher.Init(file);
                file.Close();
            }
            Console.WriteLine("Done\n");
        }

    }
}