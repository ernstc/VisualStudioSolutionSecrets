using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Tests
{
    public class ContextTests
    {

        [Fact]
        public void DefaultContextTest()
        {
            Assert.NotNull(Context.Current);
        }


        [Fact]
        public void SetIODependencyTest()
        {
            // Test the dependency assignment
            var dependency = new Mock<IFileSystem>().Object;
            Context.Configure(context => context.IO = dependency);
            Assert.Equal(dependency, Context.Current.IO);

            // Check that null assignment does not change the dependency
            Context.Configure(context => context.IO = null);
            Assert.Equal(dependency, Context.Current.IO);
        }


        [Fact]
        public void SetCipherDependencyTest()
        {
            // Test the dependency assignment
            var dependency = new Mock<ICipher>().Object;
            Context.Configure(context => context.Cipher = dependency);
            Assert.Equal(dependency, Context.Current.Cipher);

            // Check that null assignment does not change the dependency
            Context.Configure(context => context.Cipher = null);
            Assert.Equal(dependency, Context.Current.Cipher);
        }


        [Fact]
        public void SetRepositoryDependencyTest()
        {
            // Test the dependency assignment
            var dependency = new Mock<IRepository>().Object;
            Context.Configure(context => context.Repository = dependency);
            Assert.Equal(dependency, Context.Current.Repository);

            // Check that null assignment does not change the dependency
            Context.Configure(context => context.Repository = null);
            Assert.Equal(dependency, Context.Current.Repository);
        }


        [Fact]
        public void VersionStringTest()
        {
            Assert.False(String.IsNullOrWhiteSpace(Context.Current.VersionString));
        }


        [Fact]
        public void CurrentVersionTest()
        {
            Assert.NotNull(Context.Current.CurrentVersion);
        }

    }
}
