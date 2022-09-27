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
        public static SolutionSynchronizationSettings Default
        {
            get
            {
                if (Current.TryGetValue("default", out var settings))
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
                    Current["default"] = value;
                }
            }
        }


        public static SolutionSynchronizationSettings GetSynchronizationSettings(Guid solutionGuid)
        {
            string key = solutionGuid.ToString();
            if (Current.ContainsKey(key))
            {
                return Current[key];
            }
            else
            {
                return Default;
            }
        }


        public static void SetSynchronizationSettings(Guid solutionGuid, SolutionSynchronizationSettings settings)
        {
            string key = solutionGuid.ToString();
            Current[key] = settings;
        }


        private static Configuration? _current;

        private static Configuration Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new Configuration();

                    var loadedConfiguration = AppData.LoadData<Dictionary<string, SolutionSynchronizationSettings>>("configuration.json");
                    if (loadedConfiguration == null)
                    {
                        Default = Configuration.DefaultSettings;
                        AppData.SaveData<Dictionary<string, SolutionSynchronizationSettings>>("configuration.json", _current);
                    }
                    else
                    {
                        foreach (var item in loadedConfiguration)
                        {
                            _current.Add(item.Key, item.Value);
                        }
                    }
                }
                return _current;
            }
        }


        public static void Refresh()
        {
            _current = null;
        }


        public static void Save()
        {
            AppData.SaveData<Dictionary<string, SolutionSynchronizationSettings>>("configuration.json", Current);
        }

    }
}
