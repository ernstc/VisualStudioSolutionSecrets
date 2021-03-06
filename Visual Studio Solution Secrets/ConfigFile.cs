using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Encryption;


namespace VisualStudioSolutionSecrets
{

    public class ConfigFile
    {

        private readonly string _fileName = null!;
        private readonly string _configFilePath = null!;
        private readonly string _uniqueFileName = null!;
        private readonly ICipher? _cipher;


        public string GroupName => _uniqueFileName;
        public string FileName => _fileName;
        public string FilePath => _configFilePath;
        public string? Content { get; set; }
        public string? ProjectFileName { get; set; }



        public ConfigFile(string configFilePath, string uniqueFileName, ICipher? cipher)
        {
            FileInfo fileInfo = new FileInfo(configFilePath);

            _fileName = fileInfo.Name;
            _configFilePath = configFilePath;
            _uniqueFileName = uniqueFileName;
            _cipher = cipher;

            if (fileInfo.Exists)
            {
                Content = File.ReadAllText(_configFilePath);
            }
        }


        public ConfigFile(string configFilePath, string uniqueFileName, string content, ICipher? cipher)
        {
            FileInfo fileInfo = new FileInfo(configFilePath);

            _fileName = fileInfo.Name;
            _configFilePath = configFilePath;
            _uniqueFileName = uniqueFileName;
            Content = content;
            _cipher = cipher;
        }


        public bool Encrypt()
        {
            if (_cipher != null && Content != null)
            {
                var encryptedContent = _cipher.Encrypt(Content);
                if (encryptedContent != null)
                {
                    Content = encryptedContent;
                    return true;
                }
            }
            return false;
        }


        public bool Decrypt()
        {
            if (_cipher != null && Content != null)
            {
                Content = _cipher.Decrypt(Content);
                return Content != null;
            }
            return false;
        }

    }
}
