using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Commands
{
    [Command("list", Description = "List the configuration for solutions.")]
    public class ConfigureListCommand : CommandBase
    {

        [Option("--path", Description = "Path for searching solutions or single solution file path.")]
        public string? Path { get; set; }

        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        public int OnExecute(CommandLineApplication? app = null)
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            string path = EnsureFullyQualifiedPath(Path) ?? Context.Current.IO.GetCurrentDirectory();
            string[] solutionFiles = GetSolutionFiles(path, All);

            if (solutionFiles.Length > 0)
            {
                Console.WriteLine("List of solutions configuration\n");

                Console.WriteLine("Solution                                          |  Repo     |  Name                                                |  Cloud");
                Console.WriteLine("------------------------------------------------------------------------------------------------------------------------------------------------------");

                foreach (string solutionFile in solutionFiles)
                {
                    GetSolutionConfiguration(solutionFile);
                }

                Console.WriteLine();
            }

            return 0;
        }


        private void GetSolutionConfiguration(string solutionFile)
        {
            string repoName = String.Empty;

            var color = Console.ForegroundColor;
            ConsoleColor solutionColor = color;


            SolutionFile solution = new SolutionFile(solutionFile, Context.Current.Cipher);

            string solutionName = solution.Name;
            if (solutionName.Length > 48) solutionName = solutionName[..45] + "...";

            var synchronizationSettings = solution.CustomSynchronizationSettings;

            IRepository? repository = synchronizationSettings != null ? Context.Current.GetRepository(synchronizationSettings) : null;

            if (repository == null)
            {
                repoName = "";
                solutionColor = ConsoleColor.DarkGray;
            }
            else
            {
                repoName = repository.RepositoryName ?? String.Empty;
                solutionColor = ConsoleColor.White;
            }

            if (repoName.Length > 50) repoName = repoName[..47] + "...";

            Console.ForegroundColor = solutionColor;
            Console.Write($"{solutionName,-48}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{repository?.RepositoryType ?? String.Empty,-7}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{repoName,-50}");

            Console.ForegroundColor = color;
            Console.Write("  |  ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{repository?.GetFriendlyName() ?? String.Empty}");

            Console.WriteLine();

            Console.ForegroundColor = color;

        }
    }
}
