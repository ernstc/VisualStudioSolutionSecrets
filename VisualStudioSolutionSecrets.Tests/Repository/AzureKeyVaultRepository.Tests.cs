using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Repository;
using Xunit;

namespace VisualStudioSolutionSecrets.Tests.Repository
{
    public class AzureKeyVaultRepositoryTests
    {

        [Theory]
        [InlineData("https://my-kv-name.vault.azure.net")]
        [InlineData("HTTPS://MY-KV-NAME.VAULT.AZURE.NET")]
        public void RepositoryName_1_Test(string repositoryName)
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = repositoryName;
            Assert.Equal("https://my-kv-name.vault.azure.net", repository.RepositoryName);
            Assert.Equal("my-kv-name (AzureCloud)", repository.GetFriendlyName());
        }


        [Fact]
        public void RepositoryName_2_Test()
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = "my-kv-name";
            Assert.Equal("https://my-kv-name.vault.azure.net", repository.RepositoryName);
        }


        [Theory]
        [InlineData("my-kv-name")]
        [InlineData("MY-KV-NAME")]
        public void RepositoryName_3_Test(string repositoryName)
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = repositoryName;
            Assert.Equal("my-kv-name (AzureCloud)", repository.GetFriendlyName());
        }


        [Fact]
        public void RepositoryName_4_Test()
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = "https://my-kv-name.vault.azure.com";
            Assert.Null(repository.RepositoryName);
        }

    }
}
