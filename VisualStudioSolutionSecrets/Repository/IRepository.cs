using System.Collections.Generic;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Repository
{

    internal interface IRepository : IService
    {
        bool EncryptOnClient { get; }
        string RepositoryType { get; }
        string? RepositoryName { get; set; }
        string? GetFriendlyName();
        Task AuthorizeAsync(bool batchMode = false);
        Task<bool> PushFilesAsync(ISolution solution, ICollection<(string name, string? content)> files);
        Task<ICollection<(string name, string? content)>> PullFilesAsync(ISolution solution);
        Task<ICollection<SolutionSettings>> PullAllSecretsAsync();
        bool IsValid();
    }
}
