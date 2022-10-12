using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace VisualStudioSolutionSecrets.Tests.Commands
{

    public class SearchCommandTests : CommandTests
    {

        public SearchCommandTests()
        {
            ConfigureContext();
        }


        [Fact]
        public void SearchTest()
        {
            RunCommand($"search '{Constants.SolutionFilesPath}'");

            VerifyOutput();
        }


        [Fact]
        public void SearchWithNoResultsTest()
        {
            RunCommand($"search '{Constants.SampleFilesPath}'");

            VerifyOutput();
        }


        [Fact]
        public void SearchAllTest()
        {
            RunCommand($"search --all '{Constants.SampleFilesPath}'");

            VerifyOutput();
        }

    }
}
