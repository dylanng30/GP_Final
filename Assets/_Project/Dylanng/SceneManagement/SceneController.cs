using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TCO_CustomSceneController
{
    public class SceneController : MonoBehaviour
    {
        #region --- PERSISTENT SINGLETON ---
        public static SceneController Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
                return;
            }
            
            Destroy(gameObject);
        }
        #endregion
        
        [SerializeField] private BaseLoadingOverlay loadingOverlay;
        
        private Dictionary<string, string> loadedSceneBySlot = new Dictionary<string, string>();
        
        private bool _isBusy = false;
        
        // API
        
        public SceneTransitionPlan GetSceneTransitionPlan(SceneNavigationSO navigationData)
        {
            var plan = new SceneTransitionPlan();
            
            if (navigationData.UnloadSceneSlots != null)
            {
                foreach (string unloadSceneSlot in navigationData.UnloadSceneSlots)
                {
                    plan.Unload(unloadSceneSlot);
                }
            }
            
            plan.Load(navigationData.ActiveScene.SlotKey, navigationData.ActiveScene.SceneName, setActive: true);
            
            if (navigationData.AdditiveScenes != null)
            {
                foreach (var additiveScene in navigationData.AdditiveScenes)
                {
                    plan.Load(additiveScene.SlotKey, additiveScene.SceneName, setActive: false); 
                }
            }
            
            if (navigationData.HasOverlay)
            {
                plan.WithOverlay();
            }

            if (navigationData.ClearUnusedAssets)
            {
                plan.WithClearUnusedAssets();
            }

            return plan;
        }
        
        // Implementations
        private Coroutine ExecutePlan(SceneTransitionPlan plan)
        {
            if (_isBusy)
            {
                Debug.LogWarning("Scene change already in progress");
                return null;
            }
            
            _isBusy = true;
            return StartCoroutine(ChangeSceneCoroutine(plan));
        }

        private IEnumerator ChangeSceneCoroutine(SceneTransitionPlan plan)
        {
            if (plan.Overlay)
            {
                yield return loadingOverlay.StartLoad();
                yield return new WaitForSeconds(0.5f);
            }

            foreach (var slotKey in plan.SceneToUnload)
            {
                yield return UnloadSceneCoroutine(slotKey);
            }

            if (plan.ClearUnusedAssets) yield return CleanupUnusedAssetsCoroutine();

            foreach (var kvp in plan.ScenesToLoad)
            {
                string slotKey = kvp.Key;
                string sceneName = kvp.Value;
                bool isTargetActive = (plan.ActiveSceneName == sceneName);

                if (loadedSceneBySlot.TryGetValue(slotKey, out string currentSceneInSlot))
                {
                    if (currentSceneInSlot == sceneName)
                    {
                        // Scene is already loaded in this slot! Just update its visibility and activation.
                        Scene existingScene = SceneManager.GetSceneByName(sceneName);
                        if (isTargetActive)
                        {
                            if (existingScene.IsValid() && existingScene.isLoaded)
                            {
                                SceneManager.SetActiveScene(existingScene);
                                DynamicGI.UpdateEnvironment(); // Cập nhật lại ánh sáng môi trường
                            }
                        }
                        SetSceneVisibility(existingScene, isTargetActive);
                        continue; // Bỏ qua việc load lại vì đã load rồi
                    }
                    else
                    {
                        yield return UnloadSceneCoroutine(slotKey);
                    }
                }

                yield return LoadAdditiveCoroutine(slotKey, sceneName, isTargetActive);
            }

            if (plan.Overlay)
            {
                yield return loadingOverlay.EndLoad();
            }
            
            _isBusy = false;
        }

        private IEnumerator LoadAdditiveCoroutine(string slotKey, string sceneName, bool setActive)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) yield break;
            
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (loadOp == null) yield break;
            
            loadOp.allowSceneActivation = false;
            while (loadOp.progress < 0.9f)
            {
                if (loadingOverlay is ProgressLoadingOverlay progressOverlay)
                {
                    progressOverlay.UpdateProgress(loadOp.progress / 0.9f); 
                }
                yield return null;
            }
            
            loadOp.allowSceneActivation = true;
            while (!loadOp.isDone)
            {
                yield return null;
            }

            Scene newScene = SceneManager.GetSceneByName(sceneName);

            if (setActive)
            {
                if (newScene.IsValid() && newScene.isLoaded)
                {
                    SceneManager.SetActiveScene(newScene);
                    DynamicGI.UpdateEnvironment(); // Cập nhật lại ánh sáng môi trường
                }
                SetSceneVisibility(newScene, true);
            }
            else
            {
                // Nếu load ngầm (Additive preloading), lập tức giấu toàn bộ các Object của Scene đi
                SetSceneVisibility(newScene, false);
            }
            
            loadedSceneBySlot[slotKey] = sceneName;
        }

        private void SetSceneVisibility(Scene scene, bool isVisible)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                rootObj.SetActive(isVisible);
            }
        }

        private IEnumerator UnloadSceneCoroutine(string slotKey)
        {
            if (!loadedSceneBySlot.TryGetValue(slotKey, out string sceneName)) yield break;
            if (string.IsNullOrEmpty(sceneName)) yield break;
            
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(sceneName);
            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                {
                    yield return null;
                }
            }
            loadedSceneBySlot.Remove(slotKey);
        }

        private IEnumerator CleanupUnusedAssetsCoroutine()
        {
            AsyncOperation cleanupOp = Resources.UnloadUnusedAssets();
            while (!cleanupOp.isDone)
            {
                yield return null;
            }
        }

        // Transition
        public class SceneTransitionPlan
        {
            public Dictionary<string, string> ScenesToLoad = new Dictionary<string, string>();
            public List<string> SceneToUnload { get; } = new List<string>();
            public string ActiveSceneName { get; private set; }
            public bool ClearUnusedAssets { get; private set; }
            public bool Overlay { get; private set; } = false;

            public SceneTransitionPlan Load(string slotKey, string sceneName, bool setActive = false)
            {
                if (string.IsNullOrWhiteSpace(slotKey) || string.IsNullOrWhiteSpace(sceneName)) return this;
                
                ScenesToLoad[slotKey] = sceneName;
                if (setActive) ActiveSceneName = sceneName;
                return this;
            }

            public SceneTransitionPlan Unload(string slotKey)
            {
                if (string.IsNullOrWhiteSpace(slotKey)) return this;
                
                SceneToUnload.Add(slotKey);
                return this;
            }
            
            public SceneTransitionPlan WithOverlay()
            {
                Overlay = true;
                return this;
            }

            public SceneTransitionPlan WithClearUnusedAssets()
            {
                ClearUnusedAssets = true;
                return this;
            }

            public Coroutine Perform()
            {
                return SceneController.Instance.ExecutePlan(this);
            }
        }
    }
}