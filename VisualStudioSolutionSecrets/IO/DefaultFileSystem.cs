using System;
using System.IO;

namespace VisualStudioSolutionSecrets.IO
{
    internal class DefaultFileSystem : IFileSystem
    {
        private const string APP_DATA_FOLDER = @"Visual Studio Solution Secrets";

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

        public virtual string GetSecretsFolderPath()
        {
            return (Environment.OSVersion.Platform == System.PlatformID.Win32NT) ?
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\Roaming\\Microsoft\\UserSecrets") :
               Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft/usersecrets");
        }
    }
}
