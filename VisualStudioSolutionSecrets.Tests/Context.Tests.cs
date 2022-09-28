using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NuGet.Protocol.Core.Types;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Tests
{
    public class ContextTests : IDisposable
    {

        public ContextTests()
        {
            Context.Current.ResetToDefault();
        }


        public void Dispose()
        {
            Context.Current.ResetToDefault();
        }


        [Fact]
        public void DefaultContextTest()
        {
            Assert.NotNull(Context.Current);
        }


        [Fact]
        public void DefaultIOTest()
        {
            Assert.NotNull(Context.Current.IO);
        }


        [Fact]
        public void SetIODependencyTest()
        {
            // Test the dependency assignment
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock
                .Setup(o => o.GetCurrentDirectory())
                .Returns("current");

            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);

            var currentDirectory = Context.Current.IO.GetCurrentDirectory();
            Assert.Equal("current", currentDirectory);
        }


        [Fact]
        public void SetCipherDependencyTest()
        {
            // Test the dependency assignment
            var dependency = new Mock<ICipher>().Object;
            Context.Current.AddService(dependency);
            Assert.Equal(dependency, Context.Current.Cipher);
        }


        [Fact]
        public void SetRepositoryDependencyTest()
        {
            // Test the dependency assignment
            var dependency = new Mock<IRepository>().Object;
            Context.Current.AddService(dependency);
            Assert.Equal(dependency, Context.Current.Repository);
        }


        [Fact]
        public void AddServiceWithNullTest()
        {
            // Check that null assignment throws an exception
            Assert.Throws<ArgumentNullException>(() => Context.Current.AddService<IFileSystem>(null));
        }


        [Fact]
        public void AddServiceWithLabelTest()
        {
            var dependency = new Mock<IRepository>().Object;
            Context.Current.AddService(dependency, "label");

            Assert.Null(Context.Current.GetService<IRepository>());
            Assert.Equal(dependency, Context.Current.GetService<IRepository>("label"));
        }

    }
}
