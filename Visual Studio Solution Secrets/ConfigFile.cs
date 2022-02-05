using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Encryption;


namespace VisualStudioSolutionSecrets
{

    public class ConfigFile
    {

        private readonly string _configFilePath = null!;
        private readonly string _uniqueFileName = null!;
        private readonly ICipher? _cipher;


        public string UniqueFileName => _uniqueFileName;
        public string? Content { get; set; }
        public string? ProjectFileName { get; set; }


        class EncryptedContent
        {
            public string content { get; set; } = null!;
        }


        public ConfigFile(string configFilePath, string uniqueFileName, ICipher? cipher)
        {
            _configFilePath = configFilePath;
            _uniqueFileName = uniqueFileName;
            _cipher = cipher;

            if (File.Exists(_configFilePath))
            {
                Content = File.ReadAllText(_configFilePath);
            }
        }


        public ConfigFile(string configFilePath, string uniqueFileName, string content, ICipher? cipher)
        {
            _configFilePath = configFilePath;
            _uniqueFileName = uniqueFileName;
            Content = content;
            _cipher = cipher;
        }


        internal bool Encrypt()
        {
            if (_cipher != null && Content != null)
            {
                var encryptedContent = _cipher.Encrypt(_uniqueFileName, Content);
                if (encryptedContent != null)
                {
                    Content = JsonSerializer.Serialize(new EncryptedContent
                    {
                        content = encryptedContent
                    });
                    return true;
                }
            }
            return false;
        }


        internal bool Decrypt()
        {
            if (_cipher != null && Content != null)
            {
                var encryptedContent = JsonSerializer.Deserialize<EncryptedContent>(Content);
                Content = _cipher.Decrypt(_uniqueFileName, encryptedContent!.content);
                return Content != null;
            }
            return false;
        }

    }
}
