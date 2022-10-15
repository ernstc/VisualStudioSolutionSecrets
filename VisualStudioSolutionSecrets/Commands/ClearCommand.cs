using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Delete all the solution secrets from the local machine.")]
    internal class ClearCommand : CommandBaseWithPath
    {

        public int OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            string[] solutionFiles = GetSolutionFiles(Path, false);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return 1;
            }

            int solutionIndex = 0;
            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile);

                ICollection<SecretFile> secretFiles = solution.GetProjectsSecretFiles();
                if (secretFiles.Count > 0)
                {
                    solutionIndex++;
                    if (solutionIndex > 1)
                    {
                        Console.WriteLine("\n----------------------------------------\n");
                    }
                    Console.WriteLine($"Solution: {solution.Name}");
                    Console.WriteLine($"    Path: {solutionFile}\n");

                    Console.WriteLine($"You are about to clear all local secrets for the solution.");
                    if (Confirm())
                    {
                        Console.WriteLine("\nClearing secrets for projects:");

                        int i = 0;
                        foreach (var secretFile in secretFiles)
                        {
                            if (File.Exists(secretFile.Path))
                            {
                                var fileInfo = new FileInfo(secretFile.Path);
                                if (fileInfo.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                                {
                                    File.WriteAllText(secretFile.Path, "{ }");
                                }
                                else if (fileInfo.Extension.Equals(".xml", StringComparison.OrdinalIgnoreCase))
                                {
                                    File.WriteAllText(secretFile.Path, @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <secrets ver=""1.0"" />
</root>");
                                }
                            }
                            Console.WriteLine($"   {++i,3}) {secretFile.ProjectFileName}");
                        }
                    }
                }
            }
            Console.WriteLine();
            return 0;
        }

    }
}
