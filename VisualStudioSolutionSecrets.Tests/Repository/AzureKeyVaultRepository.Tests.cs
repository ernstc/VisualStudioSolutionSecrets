using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualStudioSolutionSecrets.Repository;

namespace VisualStudioSolutionSecrets.Tests.Repository
{
    public class AzureKeyVaultRepositoryTests
    {

        [Fact]
        public void RepositoryName_1_Test()
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = "https://my-kv-name.vault.azure.net";
            Assert.Equal("https://my-kv-name.vault.azure.net", repository.RepositoryName);
        }


        [Fact]
        public void RepositoryName_2_Test()
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = "my-kv-name";
            Assert.Equal("https://my-kv-name.vault.azure.net", repository.RepositoryName);
        }


        [Fact]
        public void RepositoryName_3_Test()
        {
            var repository = new AzureKeyVaultRepository();
            repository.RepositoryName = "my-kv-name";
            Assert.Equal("AzureCloud (my-kv-name)", repository.GetFriendlyName());
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
