using System;
using System.IO;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

	public abstract class Command<TOptions>
	{

        protected Context Context = null!;


		public Task Execute(Context context, TOptions options)
        {
            Context = context;
            return Execute(options);
        }


        protected abstract Task Execute(TOptions options);


        protected string? EnsureFullyQualifiedPath(string? path)
        {
            string? fullyQualifiedPath = path;
            if (fullyQualifiedPath != null && !Path.IsPathFullyQualified(fullyQualifiedPath))
            {
                fullyQualifiedPath = Path.Combine(Context.IO.GetCurrentDirectory(), fullyQualifiedPath);
            }

            return fullyQualifiedPath;
        }


        protected bool Confirm()
        {
            while (true)
            {
                Console.WriteLine("    Do you want to continue? [Y] Yes, [N] No");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Y)
                {
                    return true;
                }
                else if (key.Key == ConsoleKey.N)
                {
                    return false;
                }
            }
        }


        protected async Task AuthenticateRepositoryAsync()
        {
            if (!await Context.Repository.IsReady())
            {
                string? user_code = await Context.Repository.StartDeviceFlowAuthorizationAsync();
                Console.WriteLine($"\nAuthenticate on GitHub with Device code = {user_code}\n");
                await Context.Repository.CompleteDeviceFlowAuthorizationAsync();
            }
        }


        protected string[] GetSolutionFiles(string? path, bool all)
        {
            var directory = path ?? Context.Current.IO.GetCurrentDirectory();
            try
            {
                return Context.Current.IO.GetFiles(directory, "*.sln", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}\n");
                return new string[0];
            }
        }


        protected async Task<bool> CanSync()
        {
            if (!await Context.Cipher.IsReady())
            {
                Console.WriteLine("You need to create the encryption key before syncing secrets.");
                Console.WriteLine("For generating the encryption key, use the command below:\n\n    vs-secrets init\n");
                return false;
            }
            return true;
        }

    }
}

