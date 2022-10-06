using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    public class Context
	{
        public IConsoleInput Input => GetService<IConsoleInput>()!;
        public IFileSystem IO => GetService<IFileSystem>()!;
        public ICipher Cipher => GetService<ICipher>()!;
        public IRepository Repository => GetService<IRepository>()!;


        private static Context _current = null!;
        public static Context Current => _current ?? (_current = new Context());



        private IDictionary<string, object> _services;


        private Context()
        {
            _services = new Dictionary<string, object>();
            ResetToDefault();
        }


        public void AddService<T>(T service, string? label = null) where T: class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var key = typeof(T).FullName;
            if (key == null)
                throw new ArgumentNullException("The service cannot be added as a dependency.");

            if (!String.IsNullOrEmpty(label))
            {
                key += $"|{label}";
            }

            _services[key] = service;
        }


        public T? GetService<T>(string? label = null) where T: class
        {
            var key = typeof(T).FullName;
            if (key != null)
            {
                if (!String.IsNullOrEmpty(label))
                {
                    key += $"|{label}";
                }

                if (_services.TryGetValue(key, out var service))
                {
                    return (T)service;
                }
            }
            return null;
        }


        private static IConsoleInput defaultConsoleInput = new ConsoleInput();
        private static IFileSystem defaultIO = new DefaultFileSystem();

        public void ResetToDefault()
        {
            _services.Clear();
            AddService(defaultConsoleInput);
            AddService(defaultIO);
        }


        internal IRepository? GetRepository(SolutionSynchronizationSettings? settings)
        {
            if (settings != null)
            {
                var repository = GetService<IRepository>(settings.Repository.ToString());
                if (repository != null && settings.Repository == RepositoryTypesEnum.AzureKV)
                {
                    repository.RepositoryName = settings.AzureKeyVaultName;
                }
                return repository;
            }
            return null;
        }

    }
}
