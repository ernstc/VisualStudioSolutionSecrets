using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Commands.Abstractions;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Commands
{

    public class EncryptionKeyCommandTests
    {

        private EncryptionKeyCommand _command;


        public EncryptionKeyCommandTests()
        {
            _command = new Mock<EncryptionKeyCommand>().Object;
        }


        [Theory]
        [InlineData("Password.1")]
        [InlineData("aA1!@#$%^&*()_+=[{]};:<>|./?,-")]
        [InlineData("Pàssword with 1 accent?")]
        [InlineData("More Words 3.")]
        public void ValidPassphraseTests(string passphrase)
        {
            Assert.True(_command.ValidatePassphrase(passphrase));
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Hello")]
        [InlineData("Passs.1")]
        [InlineData("Password1")]
        [InlineData("password.1")]
        [InlineData("Password~1")]
        [InlineData("LongPhrase")]
        [InlineData("1234567")]
        [InlineData("12345678")]
        [InlineData("!@#$%^&*()_+=[{]};:<>|./?,-")]
        public void NotValidPassphraseTests(string passphrase)
        {
            Assert.False(_command.ValidatePassphrase(passphrase));
        }

    }
}
