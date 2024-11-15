using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace VisualStudioSolutionSecrets
{
    internal static class AppData
    {

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };


        public static T? LoadData<T>(string fileName) where T : class, new()
        {
            string filePath = Path.Combine(Context.Current.IO.GetApplicationDataFolderPath(), fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<T>(json, jsonSerializerOptions);
                }
                catch
                {
                    // ignored
                }
            }
            return null;
        }


        public static void SaveData<T>(string fileName, T data) where T : class, new()
        {
            string folderPath = Context.Current.IO.GetApplicationDataFolderPath();
            _ = Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                string json = JsonSerializer.Serialize<T>(data, jsonSerializerOptions);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // ignored
            }
        }

    }
}
