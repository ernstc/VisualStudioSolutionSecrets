using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Commands;
using VisualStudioSolutionSecrets.Tests.Helpers;

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
            new SearchCommand
            {
                Path = Constants.SolutionFilesPath
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public void SearchWithNoResultsTest()
        {
            new SearchCommand
            {
                Path = Constants.SampleFilesPath,
                All = false
            }
            .OnExecute();

            VerifyOutput();
        }


        [Fact]
        public void SearchAllTest()
        {
            new SearchCommand
            {
                Path = Constants.SampleFilesPath,
                All = true
            }
            .OnExecute();

            VerifyOutput();
        }

    }
}
