using System;
using System.IO;


namespace VisualStudioSolutionSecrets.Tests
{

    public abstract class TestsBase
    {

        protected static void SetupTempFolder()
        {
            var tempFolder = new DirectoryInfo(Path.Combine(Constants.TempFolderPath));
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }
            tempFolder.Create();
        }


        protected void DisposeTempFolder()
        {
            var tempFolder = new DirectoryInfo(Path.Combine(Constants.TempFolderPath));
            if (tempFolder.Exists)
            {
                tempFolder.Delete(true);
            }
        }

    }
}
