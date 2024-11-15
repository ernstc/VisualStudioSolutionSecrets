using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace VisualStudioSolutionSecrets
{
    internal static class Versions
    {

        public const string MinFileFormatSupported = "1.0.0";
        public const string MaxFileFormatSupported = "2.0.0";


        public static string? VersionString { get; }
        public static string? CommitHash { get; }
        public static Version CurrentVersion { get; }


        static Versions()
        {
            string? version = typeof(Versions).Assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            string[]? versionParts = version?.Split('+');

            VersionString = versionParts?.Length > 0 ? versionParts[0] : "unknown";
            CurrentVersion = String.IsNullOrEmpty(version) ? new Version() : new Version(VersionString.Split('-')[0]);
            CommitHash = versionParts?.Length > 1 ? versionParts[1].Substring(0, 8) : null;
        }


        public static async Task<Version> CheckForNewVersion()
        {
            Version lastVersion = new Version();
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;
            try
            {
                using SourceCacheContext cache = new SourceCacheContext();
                SourceRepository repository = NuGet.Protocol.Core.Types.Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
                FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();
                IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                    "vs-secrets",
                    cache,
                    logger,
                    cancellationToken);

                foreach (NuGetVersion version in versions.Where(v => v.Version > lastVersion))
                {
                    lastVersion = version.Version;
                }
            }
            catch
            {
                // ignored
            }

            return lastVersion;
        }

    }
}