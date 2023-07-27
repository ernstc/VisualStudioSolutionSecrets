using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    [Collection("vs-secrets Tests")]
    public class StatusCommandTests : CommandTests, IDisposable
    {

        public StatusCommandTests()
        {
            ConfigureContext();
        }


        public void Dispose()
        {
            DisposeTempFolder();
            DisposeCipherFiles();
            foreach (var directory in Directory.GetDirectories(Constants.RepositoryFilesPath))
            {
                Directory.Delete(directory, true);
            }
            foreach (var file in Directory.GetFiles(Constants.RepositoryFilesPath, "*.json"))
            {
                File.Delete(file);
            }
        }


        [Fact]
        public void StatusTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Constants.SolutionFilesPath}'");

            ClearOutput();

            RunCommand("status");

            VerifyOutput();
        }


        [Fact]
        public void StatusWithSolutionsPathTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Constants.SolutionFilesPath}'");

            ClearOutput();

            RunCommand($"status '{Constants.SampleFilesPath}'");

            VerifyOutput();
        }


        [Fact]
        public void StatusWithSolutionsFilePathTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput();
        }


        [Fact]
        public void StatusWithSolutionsPathAllTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Constants.SolutionFilesPath}'");

            ClearOutput();

            RunCommand($"status --all '{Constants.SampleFilesPath}'");

            VerifyOutput("StatusTest", s => s);
        }


        [Fact]
        public void StatusWithSolutionsRelativePathTest()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Constants.SolutionFilesPath}'");

            ClearOutput();

            RunCommand($"status . --all");

            VerifyOutput("StatusTest", s => s);
        }


        [Fact]
        public void Status_Synchronized_1_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            ClearOutput();

            // Configuration files are both equal.

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Synchronized"));
        }


        [Fact]
        public void Status_Synchronized_2_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            string filePath = Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json");
            File.Delete(filePath);

            // Fake file system for hiding local settings
            MockFileSystem(secretsFolder: Constants.TempFolderPath);

            ClearOutput();

            // No secrets on the local machine, only header file on the repository

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Synchronized"));
        }


        [Fact]
        public void Status_NoSecretFound_1_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");

            ClearOutput();

            // No secrets on the local machine, no secrets on the repository, no header on the repository.

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample-WithEmptySecrets.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Secrets not setted"));
        }


        [Fact]
        public void Status_Unmanaged_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "ReallySuperVeryVeryVeryLongNameForSolutionFileSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Unmanaged"));
        }


        [Fact]
        public void Status_HeaderError_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            File.WriteAllText(
                Path.Combine(Constants.RepositoryFilesPath, "secrets.json"),
                "-- Not JSON file --"
                );

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "ERROR: Header"));
        }


        [Fact]
        public void Status_ContentError_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            File.WriteAllText(
                Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json"),
                "-- Not JSON file --"
                );

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "ERROR: Content"));
        }


        [Fact]
        public void Status_LocalOnly_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Local only"));
        }


        [Fact]
        public void Status_CloundOnly_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            // Fake file system for hiding local settings
            MockFileSystem(secretsFolder: Constants.TempFolderPath);

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Cloud only"));
        }


        [Fact]
        public void Status_CloudOnly_InvalidKey_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            // Fake file system for hiding local settings
            MockFileSystem(secretsFolder: Constants.TempFolderPath);

            // Invalidate the key
            RunCommand($"change-key -s -p New{Constants.PASSPHRASE}");

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Cloud only / Invalid key"));
        }


        [Fact]
        public void Status_InvalidKey_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");
            RunCommand($"change-key -s -p New{Constants.PASSPHRASE}");

            ClearOutput();

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", "Invalid key"));
        }


        [Fact]
        public void Status_NotSynchronized_1_Test()
        {
            UseRepositoryEncryption = false;

            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            string filePath = Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json");
            string fileContent = File.ReadAllText(filePath);
            fileContent = fileContent.Replace("secret value", "secret value updated");
            File.WriteAllText(filePath, fileContent);

            ClearOutput();

            // Configuration files are both different.

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", $"Not synchronized 2{StatusCommand.CHAR_DIFF}"));
        }


        [Fact]
        public void Status_NotSynchronized_2_Test()
        {
            UseRepositoryEncryption = false;

            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            string filePath = Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json");
            string fileContent = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(fileContent);
            settings?.Remove("secrets.xml");
            fileContent = JsonSerializer.Serialize(settings);
            File.WriteAllText(filePath, fileContent);

            ClearOutput();

            // Local machine has 1 file that does not exist in the repository.

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", $"Not synchronized 1{StatusCommand.CHAR_DOWN}"));
        }


        [Fact]
        public void Status_NotSynchronized_3_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            string filePath = Path.Combine(Constants.RepositoryFilesPath, "secrets", "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99.json");
            File.Delete(filePath);

            ClearOutput();

            // The repository contains only the header file but not the configuration files.

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", $"Not synchronized 2{StatusCommand.CHAR_DOWN}"));
        }


        [Fact]
        public void Status_NotSynchronized_4_Test()
        {
            RunCommand($"init -p {Constants.PASSPHRASE}");
            RunCommand($"push '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            const string secretId = "c5dd8aa7-f3ef-4757-8f36-7b3135e3ac99";
            string filePath = Path.Combine(secretId, "secrets.json");
            Directory.CreateDirectory(Path.Combine(Constants.TempFolderPath, secretId));
            File.WriteAllText(
                Path.Combine(Constants.TempFolderPath, filePath),
                File.ReadAllText(Path.Combine(Constants.SecretFilesPath, filePath))
                );

            // Fake file system for hiding local settings
            MockFileSystem(secretsFolder: Constants.TempFolderPath);

            ClearOutput();

            // The local machine has not 1 file that exists in the repository.

            RunCommand($"status '{Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")}'");

            VerifyOutput("status_name", l => l.Replace("{status}", $"Not synchronized 1{StatusCommand.CHAR_UP}"));
        }

    }
}
