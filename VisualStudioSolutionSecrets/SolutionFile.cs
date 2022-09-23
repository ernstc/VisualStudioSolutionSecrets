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
            FileInfo fileInfo = new FileInfo(filePath);
            _filePath = filePath;
            _solutionFolderPath = fileInfo.Directory?.FullName ?? String.Empty;
            _cipher = cipher;
            _name = fileInfo.Name;
        }


        public ICollection<ConfigFile> GetProjectsSecretConfigFiles()
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
                            string projectFileRelativePath = Path.Combine(Path.Combine(value.Split(@"\")));
                            string projectFilePath = Path.Combine(_solutionFolderPath, projectFileRelativePath);
                            string projectFileContent;

                            FileInfo projectFile = new FileInfo(projectFilePath);
                            if (projectFile.Exists)
                            {
                                try
                                {
                                    projectFileContent = File.ReadAllText(projectFilePath);
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
                                        configFile.ProjectFileName = projectFileRelativePath;
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
                    return new SecretFileInfo
                    {
                        SecretsId = secretsId,
                        FilePath = GetSecretsFilePath(secretsId, "secrets.json")
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
                        var webConfigFiles = Directory.GetFiles(projectFolderPath, "web*.config", SearchOption.TopDirectoryOnly);
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
                                    return new SecretFileInfo
                                    {
                                        SecretsId = userSecretsId,
                                        FilePath = GetSecretsFilePath(userSecretsId, "secrets.xml")
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
            string filePath = GetSecretsFilePath(secretsId, configFile.FileName);

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            File.WriteAllText(filePath, configFile.Content ?? String.Empty);
        }


        private static string GetSecretsFilePath(string secretsId, string fileName)
        {
            return Path.Combine(
                Context.Current.IO.GetSecretsFolderPath(),
                secretsId,
                fileName
                );
        }
    }
}
