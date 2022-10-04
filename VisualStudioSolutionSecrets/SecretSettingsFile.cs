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
        private readonly string _filePath = null!;
        private readonly string _containerName = null!;


        public string FileName => _fileName;
        public string FilePath => _filePath;
        public string ContainerName => _containerName;

        public string? Content { get; set; }
        public string? ProjectFileName { get; set; }



        public SecretSettingsFile(string filePath, string containerName)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            _fileName = fileInfo.Name;
            _filePath = filePath;
            _containerName = containerName;

            if (fileInfo.Exists)
            {
                string content = File.ReadAllText(_filePath);
                if (String.Equals(".json", fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Check if the file does not contains an empty JSON object.
                        var contentTest = JsonSerializer.Deserialize<Dictionary<string, object?>>(content, new JsonSerializerOptions
                        {
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        });

                        if (contentTest?.Count > 0)
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
            var cipher = Context.Current.Cipher;
            if (cipher != null && Content != null)
            {
                var encryptedContent = cipher.Encrypt(Content);
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
            var cipher = Context.Current.Cipher;
            if (cipher != null && Content != null)
            {
                var decryptedContent = cipher.Decrypt(Content);
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
