using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dylanng
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
            // TODO: B?n c� th? g?i UIManager m? Loading Screen t?i d�y

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