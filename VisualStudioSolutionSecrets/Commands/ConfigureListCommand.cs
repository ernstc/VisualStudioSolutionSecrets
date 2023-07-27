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
    internal class ConfigureListCommand : CommandBaseWithPath
    {

        const int MAX_SOLUTION_LENGTH = 40;


        [Option("--all", Description = "When true, search in the specified path and its sub-tree.")]
        public bool All { get; set; }


        bool _renderedTableHeader;


        public int OnExecute(CommandLineApplication? app = null)
        {
            Console.WriteLine($"vs-secrets {Versions.VersionString}\n");

            var color = Console.ForegroundColor;

            Console.Write("Default repository: ");
            var repository = Context.Current.GetRepository(SyncConfiguration.Default);
            if (repository != null)
            {
                Write(repository.RepositoryType, ConsoleColor.White);
                if (!String.IsNullOrEmpty(repository.RepositoryName))
                {
                    Write($" {repository.RepositoryName}", ConsoleColor.Cyan);
                }
            }
            else
            {
                Console.Write("None");
            }
            WriteLine("\n", color);

            string[] solutionFiles = GetSolutionFiles(Path, All);

            if (solutionFiles.Length > 0)
            {
                Console.WriteLine("Solutions with custom configuration...\n");
                foreach (string solutionFile in solutionFiles)
                {
                    GetSolutionConfiguration(solutionFile);
                }
                if (!_renderedTableHeader)
                {
                    Console.WriteLine("...none\n");
                }
                Console.WriteLine();
            }

            return 0;
        }


        private void ShowHeader()
        {
            if (!_renderedTableHeader)
            {
                _renderedTableHeader = true;

                Console.WriteLine("Solution                                    | Uid                                  | Repo    | Name / Cloud");
                Console.WriteLine("--------------------------------------------|--------------------------------------|---------|--------------------------");
            }
        }


        private void GetSolutionConfiguration(string solutionFile)
        {
            var color = Console.ForegroundColor;

            SolutionFile solution = new SolutionFile(solutionFile);

            string solutionName = solution.Name;
            if (solutionName.Length > (MAX_SOLUTION_LENGTH + 3)) solutionName = solutionName[..MAX_SOLUTION_LENGTH] + "...";

            var synchronizationSettings = solution.CustomSynchronizationSettings;
            if (synchronizationSettings == null)
            {
                return;
            }

            IRepository? repository = Context.Current.GetRepository(synchronizationSettings);
            ConsoleColor solutionColor = repository == null ? ConsoleColor.DarkGray : ConsoleColor.White;

            ShowHeader();

            Write($"{solutionName,-(MAX_SOLUTION_LENGTH + 3)}", solutionColor);
            Write(" | ", color);
            Write($"{solution.Uid,-36}", solutionColor);
            Write(" | ", color);
            Write($"{repository?.RepositoryType ?? String.Empty,-7}", solutionColor);
            Write(" | ", color);
            Write($"{repository?.GetFriendlyName() ?? String.Empty}", solutionColor);

            Console.WriteLine();
            Console.ForegroundColor = color;
        }

    }
}
