using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Dylanng.Core;
using Dylanng.Core.Base;
using Dylanng.Dylanng_EventBus;
using Dylanng.Events.SystemEvents;

namespace Dylanng.Managers
{
    public class SceneLoadManager : ManagerBase
    {
        public override void Initialize()
        {
            ServiceLocator.Register<SceneLoadManager>(this, ServiceScope.Global);
            GameLogger.Log("SceneLoadManager Initialized");
        }

        public void LoadSceneAsync(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            // TODO: Bạn có thể gọi UIManager mở Loading Screen tại đây

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }
            
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            
            EventBus.Publish<SceneLoadedEvent>(new SceneLoadedEvent { SceneName = sceneName });
        }
    }
}