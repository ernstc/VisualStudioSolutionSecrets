using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using VisualStudioSolutionSecrets.Commands;


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
            CommandLineApplication.Execute<SearchCommand>(
                Constants.SolutionFilesPath
                );

            VerifyOutput();
        }


        [Fact]
        public void SearchWithNoResultsTest()
        {
            CommandLineApplication.Execute<SearchCommand>(
                Constants.SampleFilesPath
                );

            VerifyOutput();
        }


        [Fact]
        public void SearchAllTest()
        {
            CommandLineApplication.Execute<SearchCommand>(
                Constants.SampleFilesPath,
                "--all"
                );

            VerifyOutput();
        }

    }
}
