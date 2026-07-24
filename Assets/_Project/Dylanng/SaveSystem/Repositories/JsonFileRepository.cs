using System.IO;
using UnityEngine;

namespace Dylanng
{
    public class JsonFileRepository : IDataRepository
    {
        public void Save<T>(string key, T data)
        {
            if (data == null) return;
            string path = GetFilePath(key);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }

        public T Load<T>(string key, T defaultData = default)
        {
            if (HasKey(key))
            {
                string path = GetFilePath(key);
                string json = File.ReadAllText(path);
                return JsonUtility.FromJson<T>(json);
            }
            return defaultData;
        }

        public bool HasKey(string key)
        {
            string path = GetFilePath(key);
            return File.Exists(path);
        }

        public void Delete(string key)
        {
            string path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetFilePath(string key)
        {
            return Path.Combine(Application.persistentDataPath, $"{key}.json");
        }
    }
}
