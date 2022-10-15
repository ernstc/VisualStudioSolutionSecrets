using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

    public abstract class CommandBase
    {

        protected string EnsureFullyQualifiedPath(string? path)
        {
            string fullyQualifiedPath = path ?? Context.Current.IO.GetCurrentDirectory();
            if (!Path.IsPathFullyQualified(fullyQualifiedPath))
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
                var key = Context.Current.Input.ReadKey();
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
            path ??= Context.Current.IO.GetCurrentDirectory();

            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    return new string[] { fileInfo.FullName };
                }
                else if (fileInfo.Name == path)
                {
                    try
                    {
                        string localDir = Context.Current.IO.GetCurrentDirectory();
                        var files = Directory.GetFiles(localDir, fileInfo.Name, SearchOption.AllDirectories);
                        if (files.Length == 1)
                        {
                            return files;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERR: {ex.Message}\n");
                        return Array.Empty<string>();
                    }
                }
            }

            path = EnsureFullyQualifiedPath(path) ?? Context.Current.IO.GetCurrentDirectory();

            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                return new string[] { path };

            var directory = path ?? Context.Current.IO.GetCurrentDirectory();
            try
            {
                var files = Directory.GetFiles(directory, "*.sln", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.Ordinal);
                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}\n");
                return Array.Empty<string>();
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
