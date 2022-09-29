using System;
using System.IO;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

	public abstract class Command<TOptions>
	{

        public abstract Task Execute(TOptions options);


        protected string? EnsureFullyQualifiedPath(string? path)
        {
            string? fullyQualifiedPath = path;
            if (fullyQualifiedPath != null && !Path.IsPathFullyQualified(fullyQualifiedPath))
            {
                fullyQualifiedPath = Path.Combine(Context.Current.IO.GetCurrentDirectory(), fullyQualifiedPath);
            }
            return fullyQualifiedPath;
        }


        protected bool Confirm()
        {
            while (true)
            {
                Console.Write("    Do you want to continue? [Y] Yes, [N] No : ");
                var key = Console.ReadKey();
                Console.WriteLine();
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


        protected string[] GetSolutionFiles(string? path, bool all)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                return new string[] { path };

            var directory = path ?? Context.Current.IO.GetCurrentDirectory();
            try
            {
                return Directory.GetFiles(directory, "*.sln", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}\n");
                return new string[0];
            }
        }


        protected async Task<bool> CanSync()
        {
            if (!await Context.Current.Cipher.IsReady())
            {
                Console.WriteLine("You need to create the encryption key before syncing secrets.");
                Console.WriteLine("For generating the encryption key, use the command below:\n\n    vs-secrets init\n");
                return false;
            }
            return true;
        }

    }
}

