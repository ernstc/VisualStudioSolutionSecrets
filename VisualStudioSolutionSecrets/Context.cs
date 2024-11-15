using System;
using System.Collections.Generic;
using VisualStudioSolutionSecrets.Encryption;
using VisualStudioSolutionSecrets.IO;
using VisualStudioSolutionSecrets.Repository;


namespace VisualStudioSolutionSecrets
{

    internal class Context
    {
        public IConsoleInput Input => GetService<IConsoleInput>()!;
        public IFileSystem IO => GetService<IFileSystem>()!;
        public ICipher Cipher => GetService<ICipher>()!;
        public IRepository Repository => GetRepository(SyncConfiguration.Default) ?? GetService<IRepository>()!;


        private static Context _current = null!;
        public static Context Current => _current ??= new Context();


        private readonly Dictionary<string, object> _services;
        private readonly Dictionary<Type, ISet<object>> _servicesByType;


        private Context()
        {
            _services = new Dictionary<string, object>();
            _servicesByType = new Dictionary<Type, ISet<object>>();
            ResetToDefault();
        }


        public void AddService<T>(T service, string? label = null) where T : class
        {
            Type type = typeof(T);

            string? key = type.FullName ?? throw new InvalidOperationException("The service cannot be added as a dependency.");

            if (!String.IsNullOrEmpty(label))
            {
                key += $"|{label}";
            }

            _services[key] = service ?? throw new ArgumentNullException(nameof(service));

            if (!_servicesByType.TryGetValue(type, out ISet<object>? servicesByType))
            {
                servicesByType = new HashSet<object>();
                _servicesByType[type] = servicesByType;
            }
            _ = servicesByType.Add(service);
        }


        public T? GetService<T>(string? label = null) where T : class
        {
            string? key = typeof(T).FullName;
            if (key != null)
            {
                if (!String.IsNullOrEmpty(label))
                {
                    key += $"|{label}";
                }

                if (_services.TryGetValue(key, out object? service))
                {
                    return (T)service;
                }
            }
            return null;
        }


        public ISet<T> GetServices<T>() where T : class
        {
            Type type = typeof(T);
            HashSet<T> services = new();
            if (_servicesByType.TryGetValue(type, out ISet<object>? servicesForType))
            {
                foreach (object service in servicesForType)
                {
                    _ = services.Add((T)service);
                }
            }
            return services;
        }


        public void Clear()
        {
            _services.Clear();
            _servicesByType.Clear();
        }


        private static readonly IConsoleInput defaultConsoleInput = new ConsoleInput();
        private static readonly IFileSystem defaultIO = new DefaultFileSystem();

        public void ResetToDefault()
        {
            Clear();
            AddService(defaultConsoleInput);
            AddService(defaultIO);
        }


        internal IRepository? GetRepository(SolutionSynchronizationSettings? settings)
        {
            if (settings != null)
            {
                IRepository? repository = GetService<IRepository>(settings.Repository.ToString());
                if (repository != null && settings.Repository == RepositoryType.AzureKV)
                {
                    repository.RepositoryName = settings.AzureKeyVaultName;
                }
                return repository;
            }
            return null;
        }

    }
}
