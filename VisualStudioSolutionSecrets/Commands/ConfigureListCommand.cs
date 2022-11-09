﻿using System;
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
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(repository.RepositoryType);
                if (!String.IsNullOrEmpty(repository.RepositoryName))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($" {repository.RepositoryName}");
                }
            }
            else
            {
                Console.Write("None");
            }
            Console.ForegroundColor = color;
            Console.WriteLine("\n");

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

                Console.WriteLine("Solution                                    | Uid                                  | Repo    | Cloud / Name");
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

            string repoName;
            ConsoleColor solutionColor;

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

            ShowHeader();

            Console.ForegroundColor = solutionColor;
            Console.Write($"{solutionName,-(MAX_SOLUTION_LENGTH + 3)}");

            Console.ForegroundColor = color;
            Console.Write(" | ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{solution.Uid,-36}");

            Console.ForegroundColor = color;
            Console.Write(" | ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{repository?.RepositoryType ?? String.Empty,-7}");

            Console.ForegroundColor = color;
            Console.Write(" | ");

            Console.ForegroundColor = solutionColor;
            Console.Write($"{repository?.GetFriendlyName() ?? String.Empty}");

            Console.WriteLine();

            Console.ForegroundColor = color;
        }

    }
}