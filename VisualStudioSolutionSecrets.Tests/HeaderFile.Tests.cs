using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;


namespace VisualStudioSolutionSecrets.Tests
{
    [Collection("vs-secrets Tests")]
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

        public void IsVersionSupported_Tests(string version)
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

        public void IsVersionNotSupported_Tests(string version)
        {
            HeaderFile headerFile = new HeaderFile();
            headerFile.visualStudioSolutionSecretsVersion = version;
            bool isSupported = headerFile.IsVersionSupported();

            Assert.False(isSupported);
        }

    }
}
