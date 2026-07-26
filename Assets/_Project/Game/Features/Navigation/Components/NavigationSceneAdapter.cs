using System.Collections;
using UnityEngine;
using GP_Final.Navigation.Events;
using TCO_CustomSceneController;
using Dylanng; // For SceneLoadedEvent

namespace GP_Final.Navigation.Components
{
    /// <summary>
    /// Lắng nghe yêu cầu chuyển cảnh từ hệ thống Navigation (TeleportRequestEvent)
    /// và gọi TCO_CustomSceneController.SceneController để thực thi.
    /// Sau khi xong, phát ra SceneLoadedEvent để hệ thống biết.
    /// </summary>
    public class NavigationSceneAdapter : MonoBehaviour
    {
        private void Awake()
        {
            EventBus.Subscribe<TeleportRequestEvent>(OnTeleportRequest);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TeleportRequestEvent>(OnTeleportRequest);
        }

        private void OnTeleportRequest(TeleportRequestEvent evt)
        {
            Debug.Log($"[NavigationSceneAdapter] Received TeleportRequest to {evt.TargetScene}. Starting LoadSceneRoutine...");
            StartCoroutine(LoadSceneRoutine(evt));
        }

        private IEnumerator LoadSceneRoutine(TeleportRequestEvent evt)
        {
            if (SceneController.Instance == null)
            {
                Debug.LogError("[NavigationSceneAdapter] SceneController.Instance is null! Vui lòng đảm bảo SceneController đã được khởi tạo (VD: trong Bootstrapper).");
                yield break;
            }

            SceneController.SceneTransitionPlan plan;

            if (evt.TargetSceneSO != null)
            {
                plan = SceneController.Instance.GetSceneTransitionPlan(evt.TargetSceneSO);
            }
            else
            {
                // Dự phòng nếu chưa kéo file SO vào Inspector của Portal
                plan = new SceneController.SceneTransitionPlan()
                    .Load("Main", evt.TargetScene, setActive: true)
                    .WithOverlay() 
                    .WithClearUnusedAssets();
            }

            // Chờ SceneController thực thi xong
            yield return plan.Perform();

            // Bắn sự kiện báo cho UIManager và MultiSceneTransitionHandler biết
            string loadedSceneName = evt.TargetSceneSO != null ? evt.TargetSceneSO.ActiveScene.SceneName : evt.TargetScene;
            EventBus.Publish(new SceneLoadedEvent { SceneName = loadedSceneName });
        }
    }
}
