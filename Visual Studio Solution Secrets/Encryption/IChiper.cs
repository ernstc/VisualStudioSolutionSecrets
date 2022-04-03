using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Encryption
{
    public interface ICipher : IService
    {
        void Init(string passphrase);
        void Init(Stream keyfile);
        string? Encrypt(string fileName, string plainText);
        string? Decrypt(string fileName, string encrypted);
    }
}
