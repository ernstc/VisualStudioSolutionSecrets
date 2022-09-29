using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Tests.Helpers;

namespace VisualStudioSolutionSecrets.Tests.Commands
{
    public class StatusCommandTests : CommandTests, IDisposable
    {

        public StatusCommandTests()
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


        [Fact]
        public async Task StatusTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand().OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsPathTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Constants.SampleFilesPath
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsFilePathTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Path.Combine(Constants.SolutionFilesPath, "SolutionSample.sln")
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsPathAllTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = Constants.SampleFilesPath,
                All = true
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsRelativePathTest()
        {
            await new InitCommand
            {
                Passphrase = Constants.PASSPHRASE
            }
            .OnExecute();

            await new PushCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            ClearOutput();

            await new StatusCommand
            {
                Path = ".",
                All = false
            }
            .OnExecute();

            VerifyOutput();
        }

    }
}
