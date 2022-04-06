using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{


    class HeaderFile
    {
        public string visualStudioSolutionSecretsVersion { get; set; } = null!;
        public DateTime lastUpload { get; set; }
        public string solutionFile { get; set; } = null!;
    }



    static class Program
    {

        static string? _versionString;
        static Version? _currentVersion;

        static ICipher _cipher = null!;
        static IRepository _repository = null!;



        static void Main(string[] args)
        {
            _versionString = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            _currentVersion = string.IsNullOrEmpty(_versionString) ? new Version() : new Version(_versionString);

            if (args.Length == 0)
            {
                ShowLogo();
            }

            CommandLine.Parser.Default.ParseArguments<
                InitOptions,
                PushSecrectsOptions,
                PullSecrectsOptions,
                SearchSecrectsOptions,
                StatusOptions
                >(args)

            .WithNotParsed(err =>
            {
                CheckForUpdates().Wait();
                Console.WriteLine("\nUsage:");
                Console.WriteLine("     vs-secrets push --all");
                Console.WriteLine("     vs-secrets pull --all\n");
            })

            .MapResult(
                (InitOptions options) => { return Execute(Init, options); },
                (PushSecrectsOptions options) => { return Execute(PushSecrets, options); },
                (PullSecrectsOptions options) => { return Execute(PullSecrets, options); },
                (SearchSecrectsOptions options) => { return Execute(SearchSecrets, options); },
                (StatusOptions options) => { return Execute(StatusCheck, options); },
                err => 1
                );
        }


        private static int Execute<T>(Func<T, Task> action, T options)
        {
            CheckForUpdates().Wait();
            action(options).Wait();
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



        #region Utilities

        static async Task CheckForUpdates()
        {
            if (_currentVersion != null)
            {
                var lastVersion = await Versions.CheckForNewVersion();

                var v1 = new Version(lastVersion.Major, lastVersion.Minor, lastVersion.Build);
                var v2 = new Version(_currentVersion.Major, _currentVersion.Minor, _currentVersion.Build);

                if (v1 > v2)
                {
                    ShowLogo();
                    Console.WriteLine($"Current version: {_currentVersion}\n");
                    Console.WriteLine($">>> New version available: {lastVersion} <<<");
                    Console.WriteLine("Use the command below for upgrading to the latest version:\n");
                    Console.WriteLine("    dotnet tool update vs-secrets --global\n");
                    Console.WriteLine("------------------------------------------------------------");
                }
            }
        }


        static void InitDependencies()
        {
            _cipher = new Cipher();
            _repository = new GistRepository();
        }


        static async Task<bool> CanSync()
        {
            if (!await _cipher.IsReady())
            {
                Console.WriteLine("You need to create the encryption key before syncing secrets.");
                Console.WriteLine("For generating the encryption key, use the command below:\n\n    vs-secrets init\n");
                return false;
            }
            return true;
        }


        static bool ValidatePassphrase(string passphrase)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                return false;
            }

            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasMiniMaxChars = new Regex(@".{8,}");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasSymbols = new Regex(@"[!@#$%^&*()_+=\[{\]};:<>|./?,-]");

            return
                hasLowerChar.IsMatch(passphrase)
                && hasUpperChar.IsMatch(passphrase)
                && hasMiniMaxChars.IsMatch(passphrase)
                && hasNumber.IsMatch(passphrase)
                && hasSymbols.IsMatch(passphrase);
        }


        static async Task AuthenticateRepositoryAsync()
        {
            if (!await _repository.IsReady())
            {
                string? user_code = await _repository.StartDeviceFlowAuthorizationAsync();
                Console.WriteLine($"\nAuthenticate on GitHub with Device code = {user_code}\n");
                await _repository.CompleteDeviceFlowAuthorizationAsync();
            }
        }


        static string[] GetSolutionFiles(string? path, bool all)
        {
            var directory = path ?? Directory.GetCurrentDirectory();
            try
            {
                return Directory.GetFiles(directory, "*.sln", all ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERR: {ex.Message}\n");
                return new string[0];
            }
        }

        #endregion



        #region Commands

        static async Task Init(InitOptions options)
        {
            InitDependencies();

            Console.Write("Generating encryption key ...");
            if (!string.IsNullOrEmpty(options.Passphrase))
            {
                if (!string.IsNullOrEmpty(options.KeyFile))
                {
                    Console.WriteLine("\n    WARN: You have specified passphrase and keyfile, but only passphrase will be used.");
                }

                if (!ValidatePassphrase(options.Passphrase))
                {
                    Console.WriteLine("\n    WARN: The passphrase is weak. It should contains at least 8 characters in upper and lower case, at least one digit and at least one symbol between !@#$%^&*()_+=[{]};:<>|./?,-\n");
                }

                _cipher.Init(options.Passphrase);
            }
            else if (!string.IsNullOrEmpty(options.KeyFile))
            {
                if (!File.Exists(options.KeyFile))
                {
                    Console.WriteLine("\n    ERR: Cannot create encryption key. Key file not found.");
                    return;
                }

                using var file = File.OpenRead(options.KeyFile);
                _cipher.Init(file);
                file.Close();
            }
            Console.WriteLine("Done\n");

            await AuthenticateRepositoryAsync();
        }


        static async Task PushSecrets(PushSecrectsOptions options)
        {
            InitDependencies();

            if (!await CanSync())
            {
                return;
            }

            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            await AuthenticateRepositoryAsync();

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, _cipher);

                var headerFile = new HeaderFile
                {
                    visualStudioSolutionSecretsVersion = _versionString!,
                    lastUpload = DateTime.UtcNow,
                    solutionFile = solution.Name
                };

                List<(string fileName, string? content)> files = new List<(string fileName, string? content)>();
                files.Add(("secrets", JsonSerializer.Serialize(headerFile)));

                var configFiles = solution.GetProjectsSecretConfigFiles();
                if (configFiles.Count == 0)
                {
                    continue;
                }

                _repository.SolutionName = solution.Name;

                Console.Write($"Pushing secrets for solution: {solution.Name} ...");

                Dictionary<string, Dictionary<string, string>> secrets = new Dictionary<string, Dictionary<string, string>>();

                bool failed = false;
                foreach (var configFile in configFiles)
                {
                    if (configFile.Content != null)
                    {
                        if (configFile.Encrypt())
                        {
                            if (!secrets.ContainsKey(configFile.GroupName))
                            {
                                secrets.Add(configFile.GroupName, new Dictionary<string, string>());
                            }
                            secrets[configFile.GroupName].Add(configFile.FileName, configFile.Content);
                        }
                        else
                        {
                            failed = true;
                            break;
                        }
                    }
                }

                foreach (var group in secrets)
                {
                    string groupContent = JsonSerializer.Serialize(group.Value);
                    files.Add((group.Key, groupContent));
                }

                if (!failed)
                {
                    if (!await _repository.PushFilesAsync(files))
                    {
                        failed = true;
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }


        static async Task PullSecrets(PullSecrectsOptions options)
        {
            InitDependencies();

            if (!await CanSync())
            {
                return;
            }

            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            await AuthenticateRepositoryAsync();

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, _cipher);

                var configFiles = solution.GetProjectsSecretConfigFiles();
                if (configFiles.Count == 0)
                    continue;

                _repository.SolutionName = solution.Name;

                Console.Write($"Pulling secrets for solution: {solution.Name} ...");

                var files = await _repository.PullFilesAsync();
                if (files.Count == 0)
                {
                    Console.WriteLine("Failed, secrets not found");
                    continue;
                }

                // Validate header file
                HeaderFile? header = null;
                foreach (var file in files)
                {
                    if (file.name == "secrets" && file.content != null)
                    {
                        header = JsonSerializer.Deserialize<HeaderFile>(file.content);
                        break;
                    }
                }

                if (header == null)
                {
                    Console.WriteLine("\n    ERR: Header file not found");
                    continue;
                }

                Version headerVersion = new Version(header.visualStudioSolutionSecretsVersion);
                Version minVersion = new Version(Versions.MinimumFileFormatSupported);
                if (headerVersion.Major > minVersion.Major)
                {
                    Console.WriteLine($"\n    ERR: Header file has incompatible version {header.visualStudioSolutionSecretsVersion}");
                    Console.WriteLine($"\n         Consider to install an updated version of this tool.");
                    continue;
                }

                bool failed = false;
                foreach (var file in files)
                {
                    if (file.name != "secrets")
                    {
                        if (file.content == null)
                        {
                            Console.Write($"\n    ERR: File has no content: {file.name}");
                            continue;
                        }

                        Dictionary<string, string>? secretFiles = null;
                        try
                        {
                            secretFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(file.content);
                        }
                        catch
                        {
                            Console.Write($"\n    ERR: File content cannot be read: {file.name}");
                        }

                        if (secretFiles == null)
                        {
                            failed = true;
                            break;
                        }

                        foreach (var secret in secretFiles)
                        {
                            string configFileName = secret.Key;
                            
                            // This check is for compatibility with version 1.0.x
                            if (configFileName == "content")
                            {
                                configFileName = "secrets.json";
                            }

                            foreach (var configFile in configFiles)
                            {
                                if (configFile.GroupName == file.name
                                    && configFile.FileName == configFileName)
                                {
                                    configFile.Content = secret.Value;
                                    if (configFile.Decrypt())
                                    {
                                        solution.SaveConfigFile(configFile);
                                    }
                                    else
                                    {
                                        failed = true;
                                    }
                                    break;
                                }
                            }
                        }

                        if (failed)
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }


        static Task SearchSecrets(SearchSecrectsOptions options)
        {
            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
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


        static async Task StatusCheck(StatusOptions options)
        {
            InitDependencies();

            Console.WriteLine("\nChecking status...\n");
            string encryptionKeyStatus = await _cipher.IsReady() ? "OK" : "NOT DEFINED";
            string repositoryAuthorizationStatus = await _repository.IsReady() ? "OK" : "NOT AUTHORIZED";
            Console.WriteLine($"             Ecryption key status: {encryptionKeyStatus}");
            Console.WriteLine($"  Repository authorization status: {repositoryAuthorizationStatus}\n");
            Console.WriteLine();
        }

        #endregion

    }
}
