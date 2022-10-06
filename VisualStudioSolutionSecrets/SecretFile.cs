using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace VisualStudioSolutionSecrets
{

    [DebuggerDisplay("Container = {ContainerName}; Name = {Name}")]
    public class SecretFile
    {
        private readonly string _path = null!;

        public string Path => _path;
        public string Name { get; set; } = null!;
        public string ContainerName { get; set; } = null!;
        public string? Content { get; set; }
        public string? ProjectFileName { get; set; }



        public SecretFile()
        {
        }


        public SecretFile(string filePath, string containerName)
        {
            FileInfo fileInfo = new FileInfo(filePath);

            _path = filePath;
            Name = fileInfo.Name;
            ContainerName = containerName;

            if (fileInfo.Exists)
            {
                string content = File.ReadAllText(_path);
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
