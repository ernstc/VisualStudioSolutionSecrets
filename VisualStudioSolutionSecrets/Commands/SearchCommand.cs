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
                        Console.WriteLine("\n------------------------------------------------------------------------------------------------------------------------\n");
                    }
                    
                    Write("Solution: "); WriteLine(solution.Name, ConsoleColor.White);

                    string path = solutionFile;
                    if (path.EndsWith(solution.Name, StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - solution.Name.Length - 1);
                    Write("    Path: "); WriteLine(path, ConsoleColor.DarkGray);

                    Console.WriteLine("\nProjects that use secrets:");

                    int i = 0;
                    foreach (var configFile in configFiles)
                    {
                        string projectFileName = configFile.ProjectFileName!;
                        string projectRelativePath = String.Empty;
                        string projectName = projectFileName;

                        int separator = projectFileName.LastIndexOf("\\");
                        if (separator  > 0)
                        {
                            projectRelativePath = projectFileName.Substring(0, separator + 1);
                            projectName = projectFileName.Substring(separator + 1);
                        }

                        Write($"{++i,3}) ");
                        Write($"{configFile.SecretsId}", ConsoleColor.DarkGray);
                        Write($" - {projectRelativePath}", ConsoleColor.DarkGray);
                        WriteLine(projectName);

                        //Console.WriteLine($"{++i,3}) {configFile.SecretsId} - {configFile.ProjectFileName} ");
                    }
                }
            }
            Console.WriteLine();
            return 0;
        }

    }
}
