using System;
using System.IO;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands.Abstractions;


namespace VisualStudioSolutionSecrets.Commands
{

    internal class SearchSecretsCommand : Command<SearchSecretsOptions>
    {

        protected override Task Execute(SearchSecretsOptions options)
        {
            string? path = EnsureFullyQualifiedPath(options.Path);

            string[] solutionFiles = GetSolutionFiles(path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return Task.CompletedTask;
            }

            int solutionIndex = 0;
            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, null);

                var configFiles = solution.GetProjectsSecretConfigFiles();
                if (configFiles.Count > 0)
                {
                    solutionIndex++;
                    if (solutionIndex > 1)
                    {
                        Console.WriteLine("\n----------------------------------------");
                    }
                    Console.WriteLine($"\nSolution: {solution.Name}");
                    Console.WriteLine($"    Path: {solutionFile}\n");

                    Console.WriteLine("Projects that use secrets:");

                    int i = 0;
                    foreach (var configFile in configFiles)
                    {
                        Console.WriteLine($"   {++i,3}) {configFile.ProjectFileName}");
                    }
                }
            }
            Console.WriteLine();
            return Task.CompletedTask;
        }

    }
}

