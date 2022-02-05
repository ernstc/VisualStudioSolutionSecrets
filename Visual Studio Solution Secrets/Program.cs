using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
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
        static Version _toolVersion = null!;

        static ICipher _cipher = null!;
        static IRepository _repository = null!;



        static void Main(string[] args)
        {
            _versionString = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            _toolVersion = string.IsNullOrEmpty(_versionString) ? new Version() : new Version(_versionString);

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

            CommandLine.Parser.Default.ParseArguments<
                InitOptions,
                PushSecrectsOptions,
                PullSecrectsOptions,
                SearchSecrectsOptions
                >(args)
                
            .WithNotParsed(err => {
                Console.WriteLine("\nUsage:");
                Console.WriteLine("     vs-secrets push --all");
                Console.WriteLine("     vs-secrets pull --all\n");
                })

            .MapResult(
                (InitOptions options) => { Init(options).Wait(); return 0; },
                (PushSecrectsOptions options) => { PushSecrets(options).Wait(); return 0; },
                (PullSecrectsOptions options) => { PullSecrets(options).Wait(); return 0; },
                (SearchSecrectsOptions options) => { SearchSecrets(options); return 0; },
                err => 1
                );
        }


        static void InitDependencies()
        {
            _cipher = new Cipher();
            _repository = new GistRepository();
        }


        static bool CanSync()
        {
            if (!_cipher.IsReady)
            {
                Console.WriteLine("You need to create the encryption key before syncing secrets.");
                Console.WriteLine("For generating the encryption key, use the command below:\n\n    vs-secrets init\n");
                return false;
            }
            return true;
        }


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

            await _repository.AuthenticateAsync();
        }


        static async Task PushSecrets(PushSecrectsOptions options)
        {
            InitDependencies();

            if (!CanSync())
            {
                return;
            }

            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

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

                var configFiles = solution.GetProjectsSecretConfigFile();
                if (configFiles.Count == 0)
                    continue;

                await _repository.AuthenticateAsync(solution.Name);

                Console.Write($"Pushing secrets for solution: {solution.Name} ...");

                bool failed = false;
                foreach (var configFile in configFiles)
                {
                    if (configFile.Content != null)
                    {
                        if (configFile.Encrypt())
                        {
                            files.Add((configFile.UniqueFileName, configFile.Content));
                        }
                        else
                        {
                            failed = true;
                            break;
                        }
                    }
                }

                if (!failed)
                {
                    await _repository.PushFilesAsync(files);
                }

                Console.WriteLine(failed ? "Failed" : "Done");
            }

            Console.WriteLine("\nFinished.\n");
        }


        static async Task PullSecrets(PullSecrectsOptions options)
        {
            InitDependencies();

            if (!CanSync())
            {
                return;
            }

            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, _cipher);

                var configFiles = solution.GetProjectsSecretConfigFile();
                if (configFiles.Count == 0)
                    continue;

                await _repository.AuthenticateAsync(solution.Name);

                Console.Write($"Pulling secrets for solution: {solution.Name} ...");
                
                var files = await _repository.PullFilesAsync();

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
                if (headerVersion.Major > _toolVersion.Major)
                {
                    Console.WriteLine($"\n    ERR: Header file has incompatible version {header.visualStudioSolutionSecretsVersion}");
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

                        foreach (var configFile in configFiles)
                        {
                            if (configFile.UniqueFileName == file.name)
                            {
                                configFile.Content = file.content;
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


        static void SearchSecrets(SearchSecrectsOptions options)
        {
            string[] solutionFiles = GetSolutionFiles(options.Path, options.All);
            if (solutionFiles.Length == 0)
            {
                Console.WriteLine("Solution files not found.\n");
                return;
            }

            foreach (var solutionFile in solutionFiles)
            {
                SolutionFile solution = new SolutionFile(solutionFile, null);

                var configFiles = solution.GetProjectsSecretConfigFile();
                if (configFiles.Count > 0)
                {
                    Console.WriteLine("\n-----------------------------------");
                    Console.WriteLine($"Solution: {solution.Name}");
                    Console.WriteLine($"    Path: {solutionFile}\n");

                    Console.WriteLine("Projects that use secrets:");

                    foreach (var configFile in configFiles)
                    {
                        Console.WriteLine($"    - {configFile.ProjectFileName}");
                    }
                }
            }
            Console.WriteLine();
        }


        private static string[] GetSolutionFiles(string? path, bool all)
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
    }
}
