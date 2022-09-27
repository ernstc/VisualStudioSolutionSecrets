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

        private Configuration() { }



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


        private static Configuration? _current;

        public static Configuration Current
        {
            get
            {
                if (_current == null)
                {
                    var loadedConfiguration = AppData.LoadData<Dictionary<string, SolutionSynchronizationSettings>>("configuration.json");
                    if (loadedConfiguration == null)
                    {
                        _current = new Configuration();
                        _current.Default = Configuration.DefaultSettings;
                        AppData.SaveData<Dictionary<string, SolutionSynchronizationSettings>>("configuration.json", _current);
                    }
                    else
                    {
                        _current = (Configuration)loadedConfiguration;
                    }
                }
                return _current;
            }
        }


        public static void Refresh()
        {
            _current = null;
        }

    }
}
