using System;
using System.IO;

namespace VisualStudioSolutionSecrets.Tests
{
    public class TemporaryFolder : IDisposable
    {

        public string Path { get; init; }


        public TemporaryFolder()
        {
            Path = System.IO.Path.Combine(Constants.TempFolderPath, $"{Guid.NewGuid():n}");
            Reset();
        }


        public void Dispose()
        {
            Delete();
        }


        public void Reset()
        {
            Delete();
            Directory.CreateDirectory(Path);
        }


        private void Delete()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }

    }
}
