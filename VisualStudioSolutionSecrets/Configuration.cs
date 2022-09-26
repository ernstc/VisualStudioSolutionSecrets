using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    public class SolutionSynchronizationSettings
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepositoryTypesEnum Repository { get; set; }
        public string? AzureKeyVaultName { get; set; }
    }


    public class Configuration : Dictionary<string, SolutionSynchronizationSettings>
    {

        public static SolutionSynchronizationSettings DefaultSettings = new SolutionSynchronizationSettings
        {
            Repository = RepositoryTypesEnum.GitHub
        };


        [JsonIgnore]
        public SolutionSynchronizationSettings Default
        {
            get
            {
                if (TryGetValue("default", out var settings))
                {
                    return settings;
                }
                else
                {
                    return DefaultSettings;
                }
            }
            set
            {
                if (value != null)
                {
                    this["default"] = value;
                }
            }
        }


        private static Configuration _current = null!;

        public static Configuration Current
        {
            get
            {
                if (_current == null)
                {
                    var configuration = AppData.LoadData<Configuration>("configuration.json");
                    if (configuration == null)
                    {
                        configuration = new Configuration();
                        configuration.Default = Configuration.DefaultSettings;
                        AppData.SaveData("configuration.json", configuration);
                    }
                    _current = configuration;
                }
                return _current;
            }
        }


        public SolutionSynchronizationSettings GetSynchronizationSettings(Guid solutionGuid)
        {
            string key = solutionGuid.ToString();
            if (ContainsKey(key))
            {
                return this[key];
            }
            else
            {
                return Default;
            }
        }

    }
}
