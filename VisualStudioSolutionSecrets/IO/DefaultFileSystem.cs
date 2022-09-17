using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.IO
{
    public class DefaultFileSystem : IFileSystem
    {
        private const string APP_DATA_FOLDER = @"Visual Studio Solution Secrets";

        public DirectoryInfo CreateDirectory(string path)
        {
            return Directory.CreateDirectory(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public FileStream FileOpenRead(string path)
        {
            return File.OpenRead(path);
        }

        public string[] FileReadAllLines(string path)
        {
            return File.ReadAllLines(path);
        }

        public string FileReadAllText(string path)
        {
            return File.ReadAllText(path);
        }

        public void FileWriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public virtual string GetApplicationDataFolderPath()
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (String.IsNullOrEmpty(appDataFolder) && Environment.OSVersion.Platform == System.PlatformID.Unix)
            {
                appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            }
            return Path.Combine(appDataFolder, APP_DATA_FOLDER);
        }

        public virtual string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        public FileInfo GetFileInfo(string fileName)
        {
            return new FileInfo(fileName);
        }

        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(path, searchPattern, searchOption);
        }

        public virtual string GetSecretsFolderPath()
        {
            return (Environment.OSVersion.Platform == System.PlatformID.Win32NT) ?
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Roaming\\Microsoft\\UserSecrets") :
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft/usersecrets");
        }
    }
}
