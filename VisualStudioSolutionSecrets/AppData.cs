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
            string filePath = Path.Combine(Context.Current.IO.GetApplicationDataFolderPath(), APP_DATA_FOLDER, fileName);
            if (Context.Current.IO.FileExists(filePath))
            {
                try
                {
                    return JsonSerializer.Deserialize<T>(Context.Current.IO.FileReadAllText(filePath));
                }
                catch
                {
                }
            }
            return null;
        }


        public static void SaveData<T>(string fileName, T data) where T : class, new()
        {
            string folderPath = Path.Combine(Context.Current.IO.GetApplicationDataFolderPath(), APP_DATA_FOLDER);
            Context.Current.IO.CreateDirectory(folderPath);
            string filePath = Path.Combine(folderPath, fileName);
            try
            {
                string json = JsonSerializer.Serialize<T>(data, new JsonSerializerOptions 
                {
                    WriteIndented = true
                });
                Context.Current.IO.FileWriteAllText(filePath, JsonSerializer.Serialize<T>(data));
            }
            catch
            {
            }
        }

    }
}
