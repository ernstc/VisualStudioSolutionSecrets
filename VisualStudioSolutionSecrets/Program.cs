using System;
using System.Threading.Tasks;
using CommandLine;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    static class Program
    {

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowLogo();
            }

            CommandLine.Parser.Default.ParseArguments<
                InitOptions,
                ChangeKeyOptions,
                PushSecretsOptions,
                PullSecretsOptions,
                SearchSecretsOptions,
                StatusCheckOptions
                >(args)

            .WithNotParsed(err =>
            {
                CheckForUpdates().Wait();
                Console.WriteLine("\nUsage:");
                Console.WriteLine("     vs-secrets push --all");
                Console.WriteLine("     vs-secrets pull --all\n");
            })

            .MapResult(
                (InitOptions options) => { return Execute(new InitCommand(), options); },
                (ChangeKeyOptions options) => { return Execute(new ChangeKeyCommand(), options); },
                (PushSecretsOptions options) => { return Execute(new PushSecretsCommand(), options); },
                (PullSecretsOptions options) => { return Execute(new PullSecretsCommand(), options); },
                (SearchSecretsOptions options) => { return Execute(new SearchSecretsCommand(), options); },
                (StatusCheckOptions options) => { return Execute(new StatusCheckCommand(), options); },
                err => 1
                );
        }


        private static void InitDependencies()
        {
            Context.Create(
                fileSystem: new DefaultFileSystem(),
                cipher: new Cipher(),
                repository: new GistRepository()
                );
        }


        private static int Execute<TOptions>(Command<TOptions> command, TOptions options)
        {
            CheckForUpdates().Wait();
            InitDependencies();
            command.Execute(Context.Current, options).Wait();
            return 0;
        }


        private static bool _showedLogo = false;
        private static void ShowLogo()
        {
            if (_showedLogo) return;
            _showedLogo = true;
            Console.WriteLine(
                            @"
 __     ___                 _   ____  _             _ _                    
 \ \   / (_)___ _   _  __ _| | / ___|| |_ _   _  __| (_) ___               
  \ \ / /| / __| | | |/ _` | | \___ \| __| | | |/ _` | |/ _ \              
   \ V / | \__ \ |_| | (_| | |  ___) | |_| |_| | (_| | | (_) |             
  ____/  |_|___/\__,_|___,_|_| |____/ \__|_____|\__,_|_|\___/      _       
 / ___|  ___ | |_   _| |_(_) ___  _ __   / ___|  ___  ___ _ __ ___| |_ ___ 
 \___ \ / _ \| | | | | __| |/ _ \| '_ \  \___ \ / _ \/ __| '__/ _ \ __/ __|
  ___) | (_) | | |_| | |_| | (_) | | | |  ___) |  __/ (__| | |  __/ |_\__ \
 |____/ \___/|_|\__,_|\__|_|\___/|_| |_| |____/ \___|\___|_|  \___|\__|___/
"
                            );
        }


        static async Task CheckForUpdates()
        {
            if (Context.Current.CurrentVersion != null)
            {
                var lastVersion = await Versions.CheckForNewVersion();
                var currentVersion = Context.Current.CurrentVersion;

                var v1 = new Version(lastVersion.Major, lastVersion.Minor, lastVersion.Build);
                var v2 = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);

                if (v1 > v2)
                {
                    ShowLogo();
                    Console.WriteLine($"Current version: {currentVersion}\n");
                    Console.WriteLine($">>> New version available: {lastVersion} <<<");
                    Console.WriteLine("Use the command below for upgrading to the latest version:\n");
                    Console.WriteLine("    dotnet tool update vs-secrets --global\n");
                    Console.WriteLine("------------------------------------------------------------");
                }
            }
        }

    }
}
