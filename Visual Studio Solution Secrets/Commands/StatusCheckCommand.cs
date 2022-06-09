using System;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

	internal class StatusCheckCommand : Command<StatusCheckOptions>
	{

        protected override async Task Execute(StatusCheckOptions options)
        {
            Console.WriteLine("\nChecking status...\n");
            string encryptionKeyStatus = await Context.Cipher.IsReady() ? "OK" : "NOT DEFINED";
            string repositoryAuthorizationStatus = await Context.Repository.IsReady() ? "OK" : "NOT AUTHORIZED";
            Console.WriteLine($"             Ecryption key status: {encryptionKeyStatus}");
            Console.WriteLine($"  Repository authorization status: {repositoryAuthorizationStatus}\n");
            Console.WriteLine();
        }

    }
}

