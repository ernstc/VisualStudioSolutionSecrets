using System.IO;

namespace VisualStudioSolutionSecrets.Encryption
{
    internal interface ICipher : IService
    {
        void Init(string passPhrase);
        void Init(Stream keyFile);
        string? Encrypt(string plainText);
        string? Decrypt(string encrypted);
    }
}
