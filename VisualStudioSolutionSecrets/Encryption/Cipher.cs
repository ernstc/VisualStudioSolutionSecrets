using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Encryption
{
    internal sealed class Cipher : ICipher
    {

        private const string APP_DATA_FILENAME = "cipher.json";

        private string? _key;


        private sealed class CipherAppData
        {
            [JsonPropertyName("key")]
            public string Key { get; set; } = null!;
        }


        public Task<bool> IsReady()
        {
            return Task.FromResult(!string.IsNullOrEmpty(_key));
        }


        public Task RefreshStatus()
        {
            CipherAppData? cipherAppData = AppData.LoadData<CipherAppData>(APP_DATA_FILENAME);
            _key = cipherAppData?.Key;
            return Task.CompletedTask;
        }


        public void Init(string passPhrase)
        {
            if (String.IsNullOrWhiteSpace(passPhrase))
            {
                // Remove the Key
                AppData.SaveData(APP_DATA_FILENAME, new { });
                return;
            }

            byte[] data = UTF8Encoding.UTF8.GetBytes(passPhrase);
            GenerateEncryptionKey(data);
        }


        public void Init(Stream keyFile)
        {
            if (keyFile == null)
            {
                // Remove the Key
                AppData.SaveData(APP_DATA_FILENAME, new { });
                return;
            }

            byte[] data = new byte[keyFile.Length];
            int read = keyFile.Read(data, 0, data.Length);
            if (read != data.Length)
            {
                Console.WriteLine("    ERR: Error loading key file.");
                return;
            }
            GenerateEncryptionKey(data);
        }


        private void GenerateEncryptionKey(byte[] data)
        {
            byte[] result;
            result = SHA512.HashData(data);
            _key = Convert.ToBase64String(result);

            AppData.SaveData(APP_DATA_FILENAME, new CipherAppData
            {
                Key = _key
            });
        }


        public string? Encrypt(string plainText)
        {
            if (
                !string.IsNullOrEmpty(plainText)
                && !string.IsNullOrEmpty(_key)
                )
            {
                try
                {
                    byte[] encrypted;
                    byte[] Key = new byte[32];

                    byte[] keyBytes = Convert.FromBase64String(_key);
                    for (int i = 0; i < Key.Length; i++) { Key[i] = keyBytes[i]; }

                    // Create an Aes object with the specified Key and a random IV.
                    using Aes aesAlg = Aes.Create();
                    aesAlg.Key = Key;
                    aesAlg.GenerateIV();
                    byte[] IV = aesAlg.IV;

                    // Create an encryptor to perform the stream transform.
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    using MemoryStream msEncrypt = new();
                    using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
                    using (StreamWriter swEncrypt = new(csEncrypt))
                    {
                        //Write all data to the stream.
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();

                    // Combine IV and encrypted data
                    byte[] combinedData = new byte[IV.Length + encrypted.Length];
                    Array.Copy(IV, 0, combinedData, 0, IV.Length);
                    Array.Copy(encrypted, 0, combinedData, IV.Length, encrypted.Length);

                    // Return the combined IV and encrypted bytes from the memory stream.
                    return Convert.ToBase64String(combinedData);
                }
                catch
                {
                    // ignored
                }
            }
            return null;
        }


        public string? Decrypt(string encrypted)
        {
            if (
                !string.IsNullOrEmpty(encrypted)
                && !string.IsNullOrEmpty(_key)
                )
            {
                try
                {
                    byte[] Key = new byte[32];

                    byte[] keyBytes = Convert.FromBase64String(_key);
                    for (int i = 0; i < Key.Length; i++) { Key[i] = keyBytes[i]; }

                    // Extract IV and encrypted data
                    byte[] combinedData = Convert.FromBase64String(encrypted);
                    byte[] IV = new byte[16];
                    byte[] encryptedData = new byte[combinedData.Length - IV.Length];
                    Array.Copy(combinedData, 0, IV, 0, IV.Length);
                    Array.Copy(combinedData, IV.Length, encryptedData, 0, encryptedData.Length);

                    // Declare the string used to hold the decrypted text.
                    string plaintext;

                    // Create an Aes object with the specified Key and IV.
                    using (Aes aesAlg = Aes.Create())
                    {
                        aesAlg.Key = Key;
                        aesAlg.IV = IV;

                        // Create a decryptor to perform the stream transform.
                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                        // Create the streams used for decryption.
                        using MemoryStream msDecrypt = new(encryptedData);
                        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
                        using StreamReader srDecrypt = new(csDecrypt);

                        // Read the decrypted bytes from the decrypting stream and place them in a string.
                        plaintext = srDecrypt.ReadToEnd();
                    }

                    return plaintext;
                }
                catch
                {
                    // ignored
                }
            }
            return null;
        }

    }
}
