using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets
{
    internal interface IService
    {
        Task<bool> IsReady();
        Task RefreshStatus();
    }
}
