using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Tests.Commands
{

    public class PushSecretsCommandTests : CommandTests, IDisposable
    {

        public PushSecretsCommandTests()
        {
            ConfigureContext();
        }


        public void Dispose()
        {
            DisposeCipherFiles();
            foreach (var directory in Directory.GetDirectories(Constants.RepositoryFilesPath))
            {
                Directory.Delete(directory, true);
            }
            foreach (var file in Directory.GetFiles(Constants.RepositoryFilesPath))
            {
                if (file.EndsWith(".json")) File.Delete(file);
            }
        }


        private static void VerifyTestResults()
        {
            string headerPath = Path.Combine(Constants.RepositoryFilesPath, "secrets.json");
            string encryptedSecretsPath = Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json");

            Assert.True(File.Exists(headerPath));
            Assert.True(File.Exists(encryptedSecretsPath));

            var header = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(headerPath))!;

            Assert.True(header.ContainsKey("visualStudioSolutionSecretsVersion"));
            Assert.True(header.ContainsKey("lastUpload"));
            Assert.True(header.ContainsKey("solutionFile"));

            Assert.Equal("2.0.0", header["visualStudioSolutionSecretsVersion"]);
            Assert.Equal("SolutionSample.sln", header["solutionFile"]);

            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(encryptedSecretsPath))!;

            Assert.True(secrets.ContainsKey("secrets.json"));
            Assert.True(secrets.ContainsKey("secrets.xml"));

            Assert.True(secrets["secrets.json"].Length > 0);
            Assert.True(secrets["secrets.xml"].Length > 0);
        }


        [Fact]
        public void PushPathTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Constants.SolutionFilesPath}'");

            VerifyTestResults();
        }


        [Fact]
        public void PushRelativePathTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand("push .");

            VerifyTestResults();
        }


        [Fact]
        public void PushPathWithoutSolutionTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand("push unknown");

            string headerPath = Path.Combine(Constants.RepositoryFilesPath, "secrets.json");
          
            Assert.False(File.Exists(headerPath));
        }


        [Fact]
        public void PushAllWithinPathTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push --all '{Constants.SampleFilesPath}'");

            VerifyTestResults();
        }

    }
}