using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
            await CallCommand.Init(new InitOptions
            {
                Passphrase = Constants.PASSPHRASE
            });

            ClearOutput();

            await CallCommand.Status(new StatusCheckOptions());

            VerifyOutput();
        }


        [Fact]
        public async Task StatusWithSolutionsPathTest()
        {
            await CallCommand.Init(new InitOptions
            {
                Passphrase = Constants.PASSPHRASE
            });

            await CallCommand.Push(new PushSecretsOptions
            {
                Path = Constants.SolutionFilesPath
            });

            ClearOutput();

            await CallCommand.Status(new StatusCheckOptions
            {
                Path = "."
            });

            VerifyOutput();
        }

    }
}
