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
        public static T? LoadData<T>(string fileName) where T: class, new()
        {
            string filePath = Path.Combine(Context.Current.IO.GetApplicationDataFolderPath(), fileName);
            if (File.Exists(filePath))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath));
                }
                catch
                { }
            }
            return null;
        }


        public static void SaveData<T>(string fileName, T data) where T : class, new()
        {
            string folderPath = Context.Current.IO.GetApplicationDataFolderPath();
            Directory.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                string json = JsonSerializer.Serialize<T>(data, new JsonSerializerOptions 
                {
                    WriteIndented = true
                });
                File.WriteAllText(filePath, json);
            }
            catch
            { }
        }

    }
}
