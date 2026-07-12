using Dylanng.Core.Base;

namespace Dylanng.Core.Data
{
    public interface IDataStorageService : IService
    {
        void RegisterRepository(StorageType type, IDataRepository repository);
        IDataRepository GetRepository(StorageType type);
        
        void Save<T>(string key, T data, StorageType type);
        T Load<T>(string key, StorageType type, T defaultData = default);
        bool HasKey(string key, StorageType type);
        void Delete(string key, StorageType type);
    }
}
