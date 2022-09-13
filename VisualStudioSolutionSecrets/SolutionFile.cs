using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using VisualStudioSolutionSecrets.Encryption;

namespace VisualStudioSolutionSecrets
{
    public class SolutionFile
    {
        private const string ASPNET_MVC5_PROJECT_GUID = "{349c5851-65df-11da-9384-00065b846f21}";

        private readonly Regex _projRegex = new Regex(".*proj$");

        private string _name;
        private string _filePath;
        private string _solutionFolderPath;
        private ICipher? _cipher;

        public string Name => _name;



        class SecretFileInfo
        {
            public string SecretsId { get; set; } = null!;
            public string FilePath { get; set; } = null!;
        }



        public SolutionFile(string filePath, ICipher? cipher = null)
        {
            FileInfo fileInfo = Context.Current.IO.GetFileInfo(filePath);
            _filePath = filePath;
            _solutionFolderPath = fileInfo.Directory?.FullName ?? String.Empty;
            _cipher = cipher;
            _name = fileInfo.Name;
        }


        public ICollection<ConfigFile> GetProjectsSecretConfigFiles()
        {
            Dictionary<string, ConfigFile> configFiles = new Dictionary<string, ConfigFile>();

            string[] lines = Context.Current.IO.FileReadAllLines(_filePath);
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
                            string projectFilePath = Context.Current.IO.PathCombine(_solutionFolderPath, Context.Current.IO.PathCombine(value.Split('\\')));
                            string projectFileContent;

                            FileInfo projectFile = Context.Current.IO.GetFileInfo(projectFilePath);
                            if (projectFile.Exists)
                            {
                                try
                                {
                                    projectFileContent = Context.Current.IO.FileReadAllText(projectFilePath);
                                }
                                catch
                                {
                                    Console.WriteLine("    ERR: Error loading project file.");
                                    break;
                                }

                                var secrects = GetProjectSecretsFilePath(projectFileContent);
                                if (secrects == null && projectFile.Directory != null)
                                {
                                    secrects = GetDotNetFrameworkProjectSecretFiles(projectFileContent, projectFile.Directory.FullName);
                                }

                                if (secrects != null)
                                {
                                    string groupName = $"secrets\\{secrects.SecretsId}.json";
                                    if (!configFiles.ContainsKey(secrects.FilePath))
                                    {
                                        var configFile = new ConfigFile(secrects.FilePath, groupName, _cipher);
                                        configFile.ProjectFileName = value;
                                        configFiles.Add(secrects.FilePath, configFile);
                                    }
                                }
                                break;
                            }
                        }
                        idx = line.IndexOf('"', endIdx + 1);
                    }
                }
            }
            return configFiles.Values;
        }


        private SecretFileInfo? GetProjectSecretsFilePath(string projectFileContent)
        {
            const string openTag = "<UserSecretsId>";
            const string closeTag = "</UserSecretsId>";

            int idx = projectFileContent.IndexOf(openTag, StringComparison.InvariantCultureIgnoreCase);
            if (idx >= 0)
            {
                int endIdx = projectFileContent.IndexOf(closeTag, idx + 1);
                if (endIdx > idx)
                {
                    string secretsId = projectFileContent.Substring(idx + openTag.Length, endIdx - idx - openTag.Length);
                    string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return new SecretFileInfo
                    {
                        SecretsId = secretsId,
                        FilePath = GetSecretsFilePath(secretsId, userProfileFolder, "secrets.json")
                    };
                }
            }

            return null;
        }


        private SecretFileInfo? GetDotNetFrameworkProjectSecretFiles(string projectFileContent, string projectFolderPath)
        {
            const string openTag = "<ProjectTypeGuids>";
            const string closeTag = "</ProjectTypeGuids>";
            const string secretsBuilderTypeName = "Microsoft.Configuration.ConfigurationBuilders.UserSecretsConfigBuilder";

            int idx = projectFileContent.IndexOf(openTag, StringComparison.InvariantCultureIgnoreCase);
            if (idx >= 0)
            {
                int endIdx = projectFileContent.IndexOf(closeTag, idx + 1);
                if (endIdx > idx)
                {
                    string[] projectGuids = projectFileContent.Substring(idx + openTag.Length, endIdx - idx - openTag.Length).ToLower().Split(';');
                    if (projectGuids.Contains(ASPNET_MVC5_PROJECT_GUID))
                    {
                        var webConfigFiles = Context.Current.IO.GetFiles(projectFolderPath, "web*.config", SearchOption.TopDirectoryOnly);
                        foreach (var webConfigFile in webConfigFiles)
                        {
                            XDocument xml = XDocument.Load(webConfigFile);
                            var addNodes = xml.Descendants(XName.Get("configBuilders"))
                                .Descendants(XName.Get("builders"))
                                .Descendants(XName.Get("add"));

                            foreach (var node in addNodes)
                            {
                                string? name = node.Attribute(XName.Get("name"))?.Value;
                                string? userSecretsId = node.Attribute(XName.Get("userSecretsId"))?.Value;
                                string? type = node.Attribute(XName.Get("type"))?.Value;

                                if (name == "Secrets"
                                    && userSecretsId != null 
                                    && type != null
                                    && type.StartsWith(secretsBuilderTypeName, StringComparison.OrdinalIgnoreCase))
                                {
                                    string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                                    return new SecretFileInfo
                                    {
                                        SecretsId = userSecretsId,
                                        FilePath = GetSecretsFilePath(userSecretsId, userProfileFolder, "secrets.xml")
                                    };
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }


        public void SaveConfigFile(ConfigFile configFile)
        {
            string secretsId = configFile.GroupName.Substring(8, 36);
            string userProfileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = GetSecretsFilePath(secretsId, userProfileFolder, configFile.FileName);

            FileInfo fileInfo = Context.Current.IO.GetFileInfo(filePath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                Context.Current.IO.CreateDirectory(fileInfo.Directory.FullName);
            }
            Context.Current.IO.FileWriteAllText(filePath, configFile.Content ?? String.Empty);
        }


        private static string GetSecretsFilePath(string secretsId, string userProfileFolder, string fileName)
        {
            return (Environment.OSVersion.Platform == System.PlatformID.Win32NT) ?
                $"{userProfileFolder}\\AppData\\Roaming\\Microsoft\\UserSecrets\\{secretsId}\\{fileName}" :
                $"{userProfileFolder}/.microsoft/usersecrets/{secretsId}/{fileName}";
        }
    }
}
