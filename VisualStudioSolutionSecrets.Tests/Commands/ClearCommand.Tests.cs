using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Tests.Commands
{

    public class ClearCommandTests : CommandTests, IDisposable
    {

        public ClearCommandTests()
        {
            ConfigureContext();
        }


        public void Dispose()
        {
            DisposeTempFolder();
        }


        protected void CopySecretsToTempFolder(string secretId)
        {
            string secretsFolder = Path.Combine(Constants.SecretFilesPath, secretId);
            string tempFolder = Path.Combine(Constants.TempFolderPath, secretId);
            Directory.CreateDirectory(tempFolder);
            string[] files = Directory.GetFiles(secretsFolder, "*.*");
            foreach (var filePath in files)
            {
                var fileInfo = new FileInfo(filePath);
                File.WriteAllText(
                    Path.Combine(tempFolder, fileInfo.Name),
                    File.ReadAllText(filePath)
                    );
            }
        }


        [Fact]
        public void ClearTest()
        {
            const string secretId = "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99";
            CopySecretsToTempFolder(secretId);

            MockFileSystem(secretsFolder: Constants.TempFolderPath);

            RunCommand("clear");

            var secretFile1 = new SecretFile(Path.Combine(Constants.TempFolderPath, secretId, "secrets.json"), String.Empty);
            var secretFile2 = new SecretFile(Path.Combine(Constants.TempFolderPath, secretId, "secrets.xml"), String.Empty);

            Assert.Null(secretFile1.Content);
            Assert.Null(secretFile2.Content);
        }

    }
}
