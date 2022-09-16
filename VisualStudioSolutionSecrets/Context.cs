using System;
using System.Dynamic;
using System.Reflection;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

	public sealed class Context
	{
        private string? _versionString;
        private Version? _currentVersion;

        public string? VersionString { get; }
        public Version? CurrentVersion { get; }


        public IFileSystem IO { get; private set; } = null!;
        public ICipher Cipher { get; private set; } = null!;
        public IRepository Repository { get; private set; } = null!;


        private Context()
        {
            _versionString = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            _currentVersion = string.IsNullOrEmpty(_versionString) ? new Version() : new Version(_versionString);
        }


        private static Context _current = null!;
        public static Context Current => _current;


        public static void Create(
            IFileSystem fileSystem,
            ICipher cipher,
            IRepository repository
            )
        {
            _current = new Context() 
            { 
                IO = fileSystem, 
                Cipher = cipher, 
                Repository = repository 
            };
        }

    }
}

