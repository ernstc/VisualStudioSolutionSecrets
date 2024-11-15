using System;
using System.IO;


namespace VisualStudioSolutionSecrets.Commands.Abstractions
{

    internal abstract class CommandBase
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
                ConsoleKeyInfo key = Context.Current.Input.ReadKey();
                Console.WriteLine();
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
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    return new string[] { fileInfo.FullName };
                }
                else if (fileInfo.Name == path)
                {
                    try
                    {
                        string localDir = Context.Current.IO.GetCurrentDirectory();
                        string[] files = Directory.GetFiles(localDir, fileInfo.Name, new EnumerationOptions
                        {
                            IgnoreInaccessible = true,
                            ReturnSpecialDirectories = false,
                            RecurseSubdirectories = true
                        });
                        if (files.Length == 1)
                        {
                            return files;
                        }
                        else if (files.Length > 1 && all)
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

            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                return File.Exists(path)
                    ? new string[] { path }
                    : Array.Empty<string>();
            }

            try
            {
                string[] files = Directory.GetFiles(path, "*.sln", new EnumerationOptions
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
            ConsoleColor currentColor = Console.ForegroundColor;
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
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = currentColor;
        }

    }
}
