using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets
{
    public static class AppData
    {

        private const string APP_DATA_FOLDER = @"Visual Studio Solution Secrets";


        public static T? LoadData<T>(string fileName) where T: class, new()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_DATA_FOLDER, fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath));
                }
                catch
                {
                }
            }
            return null;
        }


        public static void SaveData<T>(string fileName, T data) where T : class, new()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APP_DATA_FOLDER);
            Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                string json = JsonSerializer.Serialize<T>(data, new JsonSerializerOptions 
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, JsonSerializer.Serialize<T>(data));
            }
            catch
            {
            }
        }

    }
}
