using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol;
using NuGet.Versioning;

namespace VisualStudioSolutionSecrets
{
	public static class Versions
	{

		public const string CurrentFileFormat = "1.0.2";
		public const string MinimumFileFormatSupported = "1.0.0";


		public static async Task<Version> CheckForNewVersion()
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = NuGet.Protocol.Core.Types.Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
            IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                "vs-secrets",
                cache,
                logger,
                cancellationToken);

            Version lastVersion = new Version();

            foreach (NuGetVersion version in versions)
            {
                if (version.Version > lastVersion)
                    lastVersion = version.Version;
            }
            return lastVersion;
        }

	}
}

