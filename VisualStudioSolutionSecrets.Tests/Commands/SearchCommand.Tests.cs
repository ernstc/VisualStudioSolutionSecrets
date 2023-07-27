using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    [Collection("vs-secrets Tests")]
    public class SearchCommandTests : CommandTests
    {

        public SearchCommandTests()
        {
            ConfigureContext();
        }


        [Fact]
        public void Search_Test()
        {
            RunCommand($"search '{Constants.SolutionFilesPath}'");

            VerifyOutput();
        }


        [Fact]
        public void Search_WithNoResults_Test()
        {
            RunCommand($"search '{Constants.SampleFilesPath}'");

            VerifyOutput();
        }


        [Fact]
        public void Search_All_Test()
        {
            RunCommand($"search --all '{Constants.SampleFilesPath}'");

            VerifyOutput();
        }

    }
}
