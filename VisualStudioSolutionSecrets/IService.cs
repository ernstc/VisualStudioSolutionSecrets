using System;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets
{
	public interface IService
	{
		Task<bool> IsReady();
		Task RefreshStatus();
	}
}

