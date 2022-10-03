using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Repository
{

    public interface IRepository : IService
    {
        bool EncryptOnClient { get; }
        string RepositoryType { get; }
        string? RepositoryName { get; set; }
        string? GetFriendlyName();
        Task AuthorizeAsync();
        Task<bool> PushFilesAsync(string solutionName, ICollection<(string name, string? content)> files);
        Task<ICollection<(string name, string? content)>> PullFilesAsync(string solutionName);
        Task<ICollection<SolutionSettings>> PullAllSecretsAsync();
    }
}
