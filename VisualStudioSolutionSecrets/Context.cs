using System;
using System.Reflection;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

	public sealed class Context
	{
        private string? _versionString;
        private Version? _currentVersion;

        public string? VersionString { get; }
        public Version? CurrentVersion { get; }


        public ICipher Cipher { get; set; } = null!;
        public IRepository Repository { get; set; } = null!;


        public Context()
        {
            _versionString = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            _currentVersion = string.IsNullOrEmpty(_versionString) ? new Version() : new Version(_versionString);
        }

    }
}

