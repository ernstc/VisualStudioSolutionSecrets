using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Repository
{

    public interface IRepository : IService
    {
        string RepositoryType { get; }
        string? RepositoryName { get; }
        string? SolutionName { get; set; }
        Task AuthorizeAsync();
        Task<bool> PushFilesAsync(ICollection<(string name, string? content)> files);
        Task<ICollection<(string name, string? content)>> PullFilesAsync();
        Task<ICollection<SolutionSettings>> PullAllSecretsAsync();
    }
}
