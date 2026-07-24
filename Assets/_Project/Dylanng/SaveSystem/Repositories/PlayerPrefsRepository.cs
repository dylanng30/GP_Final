using UnityEngine;

namespace Dylanng
{
    public class PlayerPrefsRepository : IDataRepository
    {
        public void Save<T>(string key, T data)
        {
            if (data == null) return;
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }

        public T Load<T>(string key, T defaultData = default)
        {
            if (HasKey(key))
            {
                string json = PlayerPrefs.GetString(key);
                return JsonUtility.FromJson<T>(json);
            }
            return defaultData;
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public void Delete(string key)
        {
            if (HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
            }
        }
    }
}
