using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

    public abstract class CommandBase
    {

        protected static string EnsureFullyQualifiedPath(string? path)
        {
            string fullyQualifiedPath = path ?? Context.Current.IO.GetCurrentDirectory();
            if (!Path.IsPathFullyQualified(fullyQualifiedPath))
            {
                fullyQualifiedPath = Path.Combine(Context.Current.IO.GetCurrentDirectory(), fullyQualifiedPath);
            }
            return fullyQualifiedPath;
        }


        protected static bool Confirm()
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


        protected static string[] GetSolutionFiles(string? path, bool all)
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
                        var files = Directory.GetFiles(localDir, fileInfo.Name, new EnumerationOptions
                        {
                            IgnoreInaccessible = true,
                            ReturnSpecialDirectories = false,
                            RecurseSubdirectories = true
                        });
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
                var files = Directory.GetFiles(directory, "*.sln", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false,
                    RecurseSubdirectories = all
                });
                Array.Sort(files, StringComparer.Ordinal);
                return files;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}\n");
                return Array.Empty<string>();
            }
        }


        protected static void Write(string message)
        {
            Console.Write(message);
        }


        protected static void Write(string message, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(message);
            Console.ForegroundColor = currentColor;
        }


        protected static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }


        protected static void WriteLine(string message, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = currentColor;
        }

    }
}
