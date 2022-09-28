using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Tests
{
    public class HeaderFileTests
    {

        [Theory]
        [InlineData("1.0")]
        [InlineData("1.0.0")]
        [InlineData("1.1.2")]
        [InlineData("1.1.3")]
        [InlineData("1.9.1")]
        [InlineData("1.9.1.1")]
        [InlineData("2.0.0")]

        public void IsVersionSupportedTests(string version)
        {
            HeaderFile headerFile = new HeaderFile();
            headerFile.visualStudioSolutionSecretsVersion = version;
            bool isSupported = headerFile.IsVersionSupported();

            Assert.True(isSupported);
        }


        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("1.9.1.1.1")]
        [InlineData("3.0.0")]

        public void IsVersionNotSupportedTests(string version)
        {
            HeaderFile headerFile = new HeaderFile();
            headerFile.visualStudioSolutionSecretsVersion = version;
            bool isSupported = headerFile.IsVersionSupported();

            Assert.False(isSupported);
        }

    }
}
