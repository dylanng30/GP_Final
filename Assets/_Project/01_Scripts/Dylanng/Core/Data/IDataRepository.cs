namespace Dylanng.Core.Data
{
    public interface IDataRepository
    {
        void Save<T>(string key, T data);
        T Load<T>(string key, T defaultData = default);
        bool HasKey(string key);
        void Delete(string key);
    }
}
