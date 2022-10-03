using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using VisualStudioSolutionSecrets.Encryption;


namespace VisualStudioSolutionSecrets
{

    [DebuggerDisplay("Secret = {GroupName}")]
    public class SecretSettingsFile
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



        public SecretSettingsFile(string configFilePath, string uniqueFileName, ICipher? cipher)
        {
            FileInfo fileInfo = new FileInfo(configFilePath);

            _fileName = fileInfo.Name;
            _configFilePath = configFilePath;
            _uniqueFileName = uniqueFileName;
            _cipher = cipher;

            if (fileInfo.Exists)
            {
                string content = File.ReadAllText(_configFilePath);
                if (String.Equals(".json", fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Check if the file does not contains an empty JSON object.
                        var contentTest = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                        if (contentTest.Count > 0)
                        {
                            Content = content;
                        }
                    }
                    catch
                    { }
                }
                else if (String.Equals(".xml", fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Check if the XML file contains some secrets or not.
                        XDocument xdoc = XDocument.Parse(content);
                        XElement xmlSecrets = xdoc.Descendants("secrets").First();
                        if (xmlSecrets.Descendants("secret").Any())
                        {
                            Content = content;
                        }
                    }
                    catch
                    { }
                }
                else
                {
                    Content = content;
                }
            }
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
                var decryptedContent = _cipher.Decrypt(Content);
                if (decryptedContent != null)
                {
                    Content = decryptedContent;
                    return true;
                }
            }
            return false;
        }

    }
}
