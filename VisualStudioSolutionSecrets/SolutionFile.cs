using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace VisualStudioSolutionSecrets
{

    [DebuggerDisplay("Name = {Name}")]
    public class SolutionFile : ISolution
    {
        private const string ASPNET_MVC5_PROJECT_GUID = "{349c5851-65df-11da-9384-00065b846f21}";

        private readonly Regex _projRegex = new Regex(".*proj$");

        private string _name;
        private Guid _uid;
        private string _filePath;
        private string _solutionFolderPath;


        public string Name => _name;
        public Guid Uid => _uid;



        class SecretFileInfo
        {
            public string SecretsId { get; set; } = null!;
            public string FilePath { get; set; } = null!;
        }



        public SolutionFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            _filePath = filePath;
            _solutionFolderPath = fileInfo.Directory?.FullName ?? String.Empty;
            _name = fileInfo.Name;

            if (fileInfo.Exists)
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    if (line.Contains("SolutionGuid", StringComparison.Ordinal) && line.Contains('=', StringComparison.Ordinal))
                    {
                        string guid = line.Substring(line.IndexOf('=', StringComparison.Ordinal) + 1);
                        _uid = Guid.Parse(guid.Trim());
                        break;
                    }
                }
            }
        }


        public SolutionSynchronizationSettings? CustomSynchronizationSettings
        {
            get
            {
                return SyncConfiguration.GetCustomSynchronizationSettings(_uid);
            }
        }


        public ICollection<SecretFile> GetProjectsSecretFiles()
        {
            Dictionary<string, SecretFile> configFiles = new Dictionary<string, SecretFile>();

            string[] lines = File.ReadAllLines(_filePath);
            foreach (string line in lines)
            {
                if (line.StartsWith("Project(", StringComparison.Ordinal))
                {
                    int idx = line.IndexOf('"', StringComparison.Ordinal);
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

                                var secrets = GetProjectSecretsFilePath(projectFileContent);
                                if (secrets == null && projectFile.Directory != null)
                                {
                                    secrets = GetDotNetFrameworkProjectSecretFiles(projectFileContent, projectFile.Directory.FullName);
                                }

                                if (secrets != null)
                                {
                                    string groupName = $"secrets\\{secrets.SecretsId}.json";
                                    if (!configFiles.ContainsKey(secrets.FilePath))
                                    {
                                        var configFile = new SecretFile(secrets.FilePath, groupName);
                                        configFile.ProjectFileName = projectFileRelativePath;
                                        configFile.SecretsId = secrets.SecretsId;
                                        configFiles.Add(secrets.FilePath, configFile);
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


        private static SecretFileInfo? GetProjectSecretsFilePath(string projectFileContent)
        {
            const string openTag = "<UserSecretsId>";
            const string closeTag = "</UserSecretsId>";

            int idx = projectFileContent.IndexOf(openTag, StringComparison.InvariantCultureIgnoreCase);
            if (idx >= 0)
            {
                int endIdx = projectFileContent.IndexOf(closeTag, idx + 1, StringComparison.Ordinal);
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


        private static SecretFileInfo? GetDotNetFrameworkProjectSecretFiles(string projectFileContent, string projectFolderPath)
        {
            const string openTag = "<ProjectTypeGuids>";
            const string closeTag = "</ProjectTypeGuids>";
            const string secretsBuilderTypeName = "Microsoft.Configuration.ConfigurationBuilders.UserSecretsConfigBuilder";

            int idx = projectFileContent.IndexOf(openTag, StringComparison.InvariantCultureIgnoreCase);
            if (idx >= 0)
            {
                int endIdx = projectFileContent.IndexOf(closeTag, idx + 1, StringComparison.Ordinal);
                if (endIdx > idx)
                {
                    string[] projectGuids = projectFileContent.Substring(idx + openTag.Length, endIdx - idx - openTag.Length).ToLowerInvariant().Split(';');
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


#pragma warning disable CA1822

        public void SaveSecretSettingsFile(SecretFile configFile)
        {
            if (configFile == null)
                throw new ArgumentNullException(nameof(configFile));

            string secretsId = configFile.ContainerName.Substring(8, 36);
            string filePath = GetSecretsFilePath(secretsId, configFile.Name);

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            File.WriteAllText(filePath, configFile.Content ?? String.Empty);
        }

#pragma warning restore CA1822


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
