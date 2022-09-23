using System;
using System.Dynamic;
using System.Reflection;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    public class ContextConfiguration
    {
        public IFileSystem? IO;
        public ICipher? Cipher;
        public IRepository? Repository;
    }


    public sealed class Context
	{
        public string? VersionString { get; }
        public Version? CurrentVersion { get; }

        public IFileSystem IO { get; private set; } = new DefaultFileSystem();
        public ICipher Cipher { get; private set; } = null!;
        public IRepository Repository { get; private set; } = null!;


        private static Context _current = null!;
        public static Context Current => _current ?? new Context();

        
        public static void Configure(Action<ContextConfiguration> configureAction)
        {
            if (configureAction == null)
                throw new ArgumentNullException(nameof(configureAction));

            ContextConfiguration configuration = new ContextConfiguration();
            configureAction(configuration);

            if (_current == null) _current = new Context();

            if (configuration.IO != null) _current.IO = configuration.IO;
            if (configuration.Cipher != null) _current.Cipher = configuration.Cipher;
            if (configuration.Repository != null) _current.Repository = configuration.Repository;
        }


        private Context()
        {
            string? version = this.GetType().Assembly?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            VersionString = version != null ? version.Split('-')[0] : null;
            CurrentVersion = string.IsNullOrEmpty(VersionString) ? new Version() : new Version(VersionString);
        }

    }
}
