using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Encryption;

namespace VisualStudioSolutionSecrets
{
    public class SolutionFile
    {
        private Regex _projRegex = new Regex(".*proj$");

        private string _name;
        private string _filePath;
        private string _solutionFolderPath;
        private ICipher? _cipher;


        public string Name => _name;


        public SolutionFile(string filePath, ICipher? cipher = null)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            _filePath = filePath;
            _solutionFolderPath = fileInfo.Directory?.FullName ?? String.Empty;
            _cipher = cipher;
            _name = fileInfo.Name;
        }


        public ICollection<ConfigFile> GetProjectsSecretConfigFile()
        {
            Dictionary<string, ConfigFile> configFiles = new Dictionary<string, ConfigFile>();

            string[] lines = File.ReadAllLines(_filePath);
            foreach (string line in lines)
            {
                if (line.StartsWith("Project("))
                {
                    int idx = line.IndexOf('"');
                    while (idx >= 0)
                    {
                        int endIdx = line.IndexOf('"', idx + 1);
                        string value = line.Substring(idx + 1, endIdx - idx - 1);

                        if (_projRegex.IsMatch(value))
                        {
                            string projectFilePath = Path.Combine(_solutionFolderPath, value);
                            var secrects = GetProjectSecretsFilePath(projectFilePath);
                            if (secrects != null)
                            {
                                string uniqueFileName = $"secrets\\{secrects.Value.secrectsId}.json";
                                if (!configFiles.ContainsKey(uniqueFileName))
                                {
                                    var configFile = new ConfigFile(secrects.Value.filePath, uniqueFileName, _cipher);
                                    configFile.ProjectFileName = value;
                                    configFiles.Add(uniqueFileName, configFile);
                                }
                            }
                            break;
                        }
                        idx = line.IndexOf('"', endIdx + 1);
                    }
                }
            }
            return configFiles.Values;
        }


        private (string secrectsId, string filePath)? GetProjectSecretsFilePath(string projectFilePath)
        {
            const string openTag = "<UserSecretsId>";
            const string endTag = "</UserSecretsId>";
            if (File.Exists(projectFilePath))
            {
                string content;
                try
                {
                    content = File.ReadAllText(projectFilePath);
                }
                catch
                {
                    Console.WriteLine("    ERR: Error loading project file.");
                    return null;
                }

                int idx = content.IndexOf(openTag, StringComparison.InvariantCultureIgnoreCase);
                if (idx >= 0)
                {
                    int endIdx = content.IndexOf(endTag, idx + 1);
                    string secretsId = content.Substring(idx + openTag.Length, endIdx - idx - openTag.Length);
                    string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return (secretsId, $"{userProfileFolder}\\AppData\\Roaming\\Microsoft\\UserSecrets\\{secretsId}\\secrets.json");
                }
            }
            return null;
        }


        public void SaveConfigFile(ConfigFile configFile)
        {
            string secretsId = configFile.UniqueFileName.Substring(8, 36);
            string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = $"{userProfileFolder}\\AppData\\Roaming\\Microsoft\\UserSecrets\\{secretsId}\\secrets.json";
            FileInfo fileInfo = new FileInfo(filePath);
            if (!fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            File.WriteAllText(filePath, configFile.Content);
        }
    }
}
