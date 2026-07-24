using System.Collections.Generic;
using UnityEngine;

namespace Dylanng
{
    public class DataStorageService : IDataStorageService
    {
        private readonly Dictionary<StorageType, IDataRepository> _repositories;

        public DataStorageService()
        {
            ServiceLocator.Register<IDataStorageService>(this, ServiceScope.Global);
            _repositories = new Dictionary<StorageType, IDataRepository>();
        }

        public void Initialize()
        {
            RegisterRepository(StorageType.PlayerPrefs, new PlayerPrefsRepository());
        }

        public void RegisterRepository(StorageType type, IDataRepository repository)
        {
            if (!_repositories.ContainsKey(type))
            {
                _repositories.Add(type, repository);
            }
            else
            {
                Debug.LogWarning($"[DataStorageService] Repository for {type} is already registered. Overwriting.");
                _repositories[type] = repository;
            }
        }

        public IDataRepository GetRepository(StorageType type)
        {
            if (_repositories.TryGetValue(type, out var repository))
            {
                return repository;
            }
            Debug.LogError($"[DataStorageService] No repository registered for {type}");
            return null;
        }

        public void Save<T>(string key, T data, StorageType type)
        {
            var repo = GetRepository(type);
            if (repo != null)
            {
                repo.Save(key, data);
            }
        }

        public T Load<T>(string key, StorageType type, T defaultData = default)
        {
            var repo = GetRepository(type);
            if (repo != null)
            {
                return repo.Load(key, defaultData);
            }
            return defaultData;
        }

        public bool HasKey(string key, StorageType type)
        {
            var repo = GetRepository(type);
            if (repo != null)
            {
                return repo.HasKey(key);
            }
            return false;
        }

        public void Delete(string key, StorageType type)
        {
            var repo = GetRepository(type);
            if (repo != null)
            {
                repo.Delete(key);
            }
        }
    }
}
