using System;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;

namespace VisualStudioSolutionSecrets.Commands
{

    [Command(Description = "Search for solution secrets.")]
    internal class SearchCommand : CommandBaseWithPath
    {

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        public int OnExecute()
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            string[] solutionFiles = GetSolutionFiles(Path, All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return 1;
            }

            int solutionIndex = 0;
            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile);

                var configFiles = solution.GetProjectsSecretFiles();
                if (configFiles.Count > 0)
                {
                    solutionIndex++;
                    if (solutionIndex > 1)
                    {
                        Console.WriteLine("\n----------------------------------------\n");
                    }
                    Console.WriteLine($"Solution: {solution.Name}");
                    Console.WriteLine($"    Path: {solutionFile}\n");

                    Console.WriteLine("Projects that use secrets:");

                    int i = 0;
                    foreach (var configFile in configFiles)
                    {
                        Console.WriteLine($"   {++i,3}) [{configFile.SecretsId}] - {configFile.ProjectFileName} ");
                    }
                }
            }
            Console.WriteLine();
            return 0;
        }

    }
}
