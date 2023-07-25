using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;
using Xunit;


namespace VisualStudioSolutionSecrets.Tests
{

    [Collection("vs-secrets Tests")]
    public sealed class ContextTests : TestsBase, IDisposable
    {

        public ContextTests()
        {
            SetupTempFolder();
            Context.Current.ResetToDefault();
        }


        public void Dispose()
        {
            DisposeTempFolder();
            Context.Current.ResetToDefault();
        }


        [Fact]
        public void Context_DefaultContext_Test()
        {
            Assert.NotNull(Context.Current);
        }


        [Fact]
        public void Context_DefaultIO_Test()
        {
            Assert.NotNull(Context.Current.IO);
        }


        [Fact]
        public void Context_SetIODependency_Test()
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
        public void Context_SetCipherDependency_Test()
        {
            // Test the dependency assignment
            var dependency = new Mock<ICipher>().Object;
            Context.Current.AddService(dependency);
            Assert.Equal(dependency, Context.Current.Cipher);
        }


        [Fact]
        public void Context_SetRepositoryDependency_Test()
        {
            // Test the dependency assignment
            var dependency = new Mock<IRepository>().Object;
            Context.Current.AddService(dependency);
            Assert.Equal(dependency, Context.Current.Repository);
        }


        [Fact]
        public void Context_AddServiceWithNull_Test()
        {
            // Check that null assignment throws an exception
            Assert.Throws<ArgumentNullException>(() => Context.Current.AddService<IFileSystem>(null!));
        }


        [Fact]
        public void Context_AddServiceWithLabel_Test()
        {
            var dependency = new Mock<IRepository>().Object;
            Context.Current.AddService(dependency, "label");

            Assert.Null(Context.Current.GetService<IRepository>());
            Assert.Equal(dependency, Context.Current.GetService<IRepository>("label"));
        }


        private void Context_GetDefaultRepository_Setup(string? configurationContent = null)
        {
            SetupTempFolder();

            // Test the dependency assignment
            var fileSystemMock = new Mock<IFileSystem>();

            fileSystemMock
                .Setup(o => o.GetApplicationDataFolderPath())
                .Returns(Constants.TempFolderPath);

            if (configurationContent != null)
            {
                File.WriteAllText(Path.Combine(Constants.TempFolderPath, "configuration.json"), configurationContent);
            }

            Context.Current.AddService<IFileSystem>(fileSystemMock.Object);
            Context.Current.AddService<IRepository>(new GistRepository(), nameof(RepositoryType.GitHub));
            Context.Current.AddService<IRepository>(new AzureKeyVaultRepository(), nameof(RepositoryType.AzureKV));

            SyncConfiguration.Refresh();
        }


        [Fact]
        public void Context_GetDefaultRepository_1_Test()
        {
            Context_GetDefaultRepository_Setup();

            var repository = Context.Current.Repository;

            Assert.NotNull(repository);
            Assert.IsType<GistRepository>(repository);

            DisposeTempFolder();
        }


        [Fact]
        public void Context_GetDefaultRepository_2_Test()
        {
            Context_GetDefaultRepository_Setup(
@"{
  ""default"": {
    ""repository"": ""GitHub"",
  }
}");
            var repository = Context.Current.Repository;

            Assert.NotNull(repository);
            Assert.IsType<GistRepository>(repository);

            DisposeTempFolder();
        }


        [Fact]
        public void Context_GetDefaultRepository_3_Test()
        {
            Context_GetDefaultRepository_Setup(
@"{
  ""default"": {
    ""repository"": ""AzureKV"",
    ""azureKeyVaultName"": ""https://___.vault.azure.net""
  }
}");
            var repository = Context.Current.Repository;

            Assert.NotNull(repository);
            Assert.IsType<AzureKeyVaultRepository>(repository);

            var azureKeyVaultRepository = repository as AzureKeyVaultRepository;
            Assert.Equal("https://___.vault.azure.net", azureKeyVaultRepository?.RepositoryName);

            DisposeTempFolder();
        }

    }
}
