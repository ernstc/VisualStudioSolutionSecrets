using System;
using System.Reflection;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    [Command("vs-secrets")]
    [VersionOptionFromMember("--version", MemberName = nameof(GetVersion))]
    [Subcommand(
        typeof(InitCommand),
        typeof(ChangeKeyCommand),
        typeof(PushCommand),
        typeof(PullCommand),
        typeof(SearchCommand),
        typeof(StatusCommand),
        typeof(ConfigureCommand),
        typeof(ClearCommand)
    )]
    internal class Program
    {

        static void Main(string[] args)
        {
            CheckForUpdates().Wait();

            // Register cipher
            var cipher = new Cipher();
            cipher.RefreshStatus().Wait();
            Context.Current.AddService<ICipher>(cipher);

            // Register GitHub repository
            var gistRepository = new GistRepository();
            Context.Current.AddService<IRepository>(gistRepository);
            Context.Current.AddService<IRepository>(gistRepository, nameof(RepositoryType.GitHub));

            // Register Azure Key Vault repository
            Context.Current.AddService<IRepository>(new AzureKeyVaultRepository(), nameof(RepositoryType.AzureKV));

            CommandLineApplication.Execute<Program>(args);
        }


#pragma warning disable CA1822

        protected int OnExecute(CommandLineApplication app)
        {
            ShowLogo();
            app.ShowHelp();
            return 0;
        }

#pragma warning restore CA1822


        private static string GetVersion()
        {
            var assembly = typeof(Versions).Assembly;
            var copyright = assembly?.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            string platform;
#if NET7_0
            platform = ".NET 7.0";
#elif NET6_0
            platform = ".NET 6.0";
#elif NETCOREAPP3_1
            platform = ".Net Core 3.1";
#else
            platform = String.Empty;
#endif
            string details = Versions.CommitHash != null 
                ? $" ({platform}, commit {Versions.CommitHash})" 
                : $" ({platform})";

            return $"vs-secrets {Versions.CurrentVersion}{details}\n{copyright}";
        }


        private static bool _showedLogo;
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


        private static async Task CheckForUpdates()
        {
            if (Versions.CurrentVersion != null)
            {
                var lastVersion = await Versions.CheckForNewVersion();
                var currentVersion = Versions.CurrentVersion;

                var v1 = new Version(lastVersion.Major, lastVersion.Minor, lastVersion.Build);
                var v2 = new Version(currentVersion.Major, currentVersion.Minor, currentVersion.Build);

                if (v1 > v2)
                {
                    Console.WriteLine($@"
------------------------------------------------------------

>>> New version available: {lastVersion.ToString(lastVersion.Revision == 0 ? 3 : 4)} <<<
Use the command below for upgrading to the latest version:

    dotnet tool update vs-secrets --global

------------------------------------------------------------
");
                }
            }
        }

    }
}
