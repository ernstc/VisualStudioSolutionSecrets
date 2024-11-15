using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace VisualStudioSolutionSecrets
{

    [DebuggerDisplay("Name = {Name}")]
    internal class SolutionFile : ISolution
    {
        private const string ASPNET_MVC5_PROJECT_GUID = "{349c5851-65df-11da-9384-00065b846f21}";

        private readonly Regex _projRegex = new(".*proj$");
        private readonly string _filePath;
        private readonly string _solutionFolderPath;


        public string Name { get; }
        public Guid Uid { get; }



        private sealed class SecretFileInfo
        {
            public string SecretsId { get; set; } = null!;
            public string FilePath { get; set; } = null!;
        }



        public SolutionFile(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            _filePath = filePath;
            _solutionFolderPath = fileInfo.Directory?.FullName ?? String.Empty;
            Name = fileInfo.Name;

            if (fileInfo.Exists)
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string line in lines)
                {
                    if (line.Contains("SolutionGuid", StringComparison.Ordinal) && line.Contains('=', StringComparison.Ordinal))
                    {
                        string guid = line.Substring(line.IndexOf('=', StringComparison.Ordinal) + 1);
                        Uid = Guid.Parse(guid.Trim());
                        break;
                    }
                }
            }
        }


        public SolutionSynchronizationSettings? CustomSynchronizationSettings => SyncConfiguration.GetCustomSynchronizationSettings(Uid);


        private ICollection<SecretFile>? _projectsSecretFiles;

        public ICollection<SecretFile> GetProjectsSecretFiles()
        {
            if (_projectsSecretFiles != null)
            {
                return _projectsSecretFiles;
            }

            Dictionary<string, SecretFile> configFiles = new();

            IEnumerable<string> lines = File.ReadAllLines(_filePath)
                .Where(line => line.StartsWith("Project(", StringComparison.Ordinal));

            foreach (string line in lines)
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

                            SecretFileInfo? secrets = GetProjectSecretsFilePath(projectFileContent);
                            if (secrets == null && projectFile.Directory != null)
                            {
                                secrets = GetDotNetFrameworkProjectSecretFiles(projectFileContent, projectFile.Directory.FullName);
                            }

                            if (secrets != null)
                            {
                                string groupName = $"secrets\\{secrets.SecretsId}.json";
                                if (!configFiles.ContainsKey(secrets.FilePath))
                                {
                                    SecretFile configFile = new SecretFile(secrets.FilePath, groupName);
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

            return _projectsSecretFiles = configFiles.Values;
        }


        public string GetSolutionCompositeKey()
        {
            SolutionSynchronizationSettings? synchronizationSettings = CustomSynchronizationSettings;
            Repository.IRepository? repository = Context.Current.GetRepository(synchronizationSettings);

            string repositoryKey = repository == null
                ? string.Empty
                : $"{repository.RepositoryType}{repository.RepositoryName ?? string.Empty}";

            StringBuilder sb = new StringBuilder()
                .Append(repositoryKey)
                .Append('|')
                .Append(Uid.ToString("D", CultureInfo.InvariantCulture));

            foreach (SecretFile secretFile in GetProjectsSecretFiles())
            {
                _ = sb
                    .Append('|')
                    .Append(secretFile.SecretsId);
            }

            return sb.ToString();
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
                        string[] webConfigFiles = Directory.GetFiles(projectFolderPath, "web*.config", SearchOption.TopDirectoryOnly);
                        foreach (string webConfigFile in webConfigFiles)
                        {
                            XDocument xml = XDocument.Load(webConfigFile);
                            IEnumerable<XElement> addNodes = xml.Descendants(XName.Get("configBuilders"))
                                .Descendants(XName.Get("builders"))
                                .Descendants(XName.Get("add"));

                            foreach (XElement node in addNodes)
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


#pragma warning disable CA1822, S2325

        public void SaveSecretSettingsFile(SecretFile configFile)
        {
            ArgumentNullException.ThrowIfNull(configFile);

            string secretsId = configFile.ContainerName.Substring(8, 36);
            string filePath = GetSecretsFilePath(secretsId, configFile.Name);

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                _ = Directory.CreateDirectory(fileInfo.Directory.FullName);
            }
            File.WriteAllText(filePath, configFile.Content ?? String.Empty);
        }

#pragma warning restore CA1822, S2325


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
