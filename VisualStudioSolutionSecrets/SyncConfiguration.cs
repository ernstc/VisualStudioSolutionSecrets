using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    internal class SolutionSynchronizationSettings
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public RepositoryType Repository { get; set; }
        public string? AzureKeyVaultName { get; set; }
    }


    internal class SyncConfiguration : Dictionary<string, SolutionSynchronizationSettings>
    {

        private const string APP_DATA_FILENAME = "configuration.json";


        private SyncConfiguration() { }



        public static readonly SolutionSynchronizationSettings DefaultSettings = new SolutionSynchronizationSettings
        {
            Repository = RepositoryType.GitHub
        };


        [JsonIgnore]
        public static SolutionSynchronizationSettings Default
        {
            get => Current.TryGetValue("default", out SolutionSynchronizationSettings? settings)
                    ? settings
                    : DefaultSettings;
            set
            {
                if (value != null)
                {
                    Current["default"] = value;
                }
            }
        }


        public static SolutionSynchronizationSettings? GetCustomSynchronizationSettings(Guid solutionGuid)
        {
            string key = solutionGuid.ToString();
            return Current.TryGetValue(key, out SolutionSynchronizationSettings? value)
                ? value
                : null;
        }


        public static void SetCustomSynchronizationSettings(Guid solutionGuid, SolutionSynchronizationSettings? settings)
        {
            string key = solutionGuid.ToString();
            if (settings != null)
            {
                Current[key] = settings;
            }
            else
            {
                _ = Current.Remove(key);
            }
        }


        private static SyncConfiguration? _current;

        private static SyncConfiguration Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new SyncConfiguration();

                    Dictionary<string, SolutionSynchronizationSettings>? loadedConfiguration = AppData.LoadData<Dictionary<string, SolutionSynchronizationSettings>>(APP_DATA_FILENAME);
                    if (loadedConfiguration == null)
                    {
                        Default = SyncConfiguration.DefaultSettings;
                        AppData.SaveData<Dictionary<string, SolutionSynchronizationSettings>>(APP_DATA_FILENAME, _current);
                    }
                    else
                    {
                        foreach (KeyValuePair<string, SolutionSynchronizationSettings> item in loadedConfiguration)
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
            AppData.SaveData<Dictionary<string, SolutionSynchronizationSettings>>(APP_DATA_FILENAME, Current);
        }

    }
}
