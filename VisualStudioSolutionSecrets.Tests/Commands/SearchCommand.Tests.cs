using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public async Task SearchTest()
        {
            await CallCommand.Search(new SearchSecretsOptions
            {
                Path = Constants.SolutionFilesPath
            });

            VerifyOutput();
        }


        [Fact]
        public async Task SearchWithNoResultsTest()
        {
            await CallCommand.Search(new SearchSecretsOptions
            {
                Path = Constants.SampleFilesPath,
                All = false
            });

            VerifyOutput();
        }


        [Fact]
        public async Task SearchAllTest()
        {
            await CallCommand.Search(new SearchSecretsOptions
            {
                Path = Constants.SampleFilesPath,
                All = true
            });

            VerifyOutput();
        }

    }
}
