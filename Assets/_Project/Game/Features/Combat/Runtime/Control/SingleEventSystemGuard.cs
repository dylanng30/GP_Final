using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game.Combat
{
    /// <summary>Mỗi scene có EventSystem riêng, lớp này giữ lại đúng một cái sau khi load scene.</summary>
    public sealed class SingleEventSystemGuard : MonoBehaviour
    {
        private const string PersistentSceneName = "DontDestroyOnLoad";

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EventSystem[] systems = FindObjectsByType<EventSystem>();
            if (systems.Length <= 1) return;

            EventSystem keeper = SelectKeeper(systems, scene);

            foreach (EventSystem system in systems)
            {
                if (system != keeper) Destroy(system.gameObject);
            }
        }

        /// <summary>Ưu tiên EventSystem tồn tại xuyên scene, sau đó là của scene vừa load vì scene cũ sắp bị gỡ.</summary>
        private static EventSystem SelectKeeper(EventSystem[] systems, Scene loadedScene)
        {
            EventSystem keeper = systems[0];
            foreach (EventSystem system in systems)
            {
                Scene systemScene = system.gameObject.scene;
                if (systemScene.name == PersistentSceneName) return system;
                if (systemScene == loadedScene) keeper = system;
            }
            return keeper;
        }
    }
}
