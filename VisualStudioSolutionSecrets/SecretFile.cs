using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;


namespace VisualStudioSolutionSecrets
{

    [DebuggerDisplay("Container = {ContainerName}; Name = {Name}")]
    internal class SecretFile
    {
        public string Path { get; } = null!;
        public string Name { get; set; } = null!;
        public string ContainerName { get; set; } = null!;
        public string? Content { get; set; }
        public string? ProjectFileName { get; set; }
        public string? SecretsId { get; set; }


        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };



        public SecretFile()
        {
        }


        public SecretFile(string filePath, string containerName)
        {
            FileInfo fileInfo = new(filePath);

            Path = filePath;
            Name = fileInfo.Name;
            ContainerName = containerName;

            if (fileInfo.Exists)
            {
                string content = File.ReadAllText(Path);
                if (String.Equals(".json", fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Check if the file does not contains an empty JSON object.
                        Dictionary<string, object?>? contentTest = JsonSerializer.Deserialize<Dictionary<string, object?>>(content, _jsonOptions);
                        if (contentTest?.Count > 0)
                        {
                            Content = content;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else if (String.Equals(".xml", fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Check if the XML file contains some secrets or not.
                        XDocument xDoc = XDocument.Parse(content);
                        XElement xmlSecrets = xDoc.Descendants("secrets").First();
                        if (xmlSecrets.Descendants("secret").Any())
                        {
                            Content = content;
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
                else
                {
                    Content = content;
                }
            }
        }


        public bool Encrypt()
        {
            Encryption.ICipher cipher = Context.Current.Cipher;
            if (cipher != null && Content != null)
            {
                string? encryptedContent = cipher.Encrypt(Content);
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
            Encryption.ICipher cipher = Context.Current.Cipher;
            if (cipher != null && Content != null)
            {
                string? decryptedContent = cipher.Decrypt(Content);
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
