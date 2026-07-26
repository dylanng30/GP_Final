using UnityEngine;
using GP_Final.Navigation.Events;
using Dylanng;
using Game.Gameplay.Entities;

namespace GP_Final.Navigation.Components
{
    [RequireComponent(typeof(Collider))]
    public class Portal : MonoBehaviour
    {
        [Header("Portal Settings")]
        public string portalId;
        public string targetScene; // Dự phòng nếu chưa gán SO
        public TCO_CustomSceneController.SceneNavigationSO targetSceneSO; // Kéo thả file ScriptableObject vào đây!
        public string targetPortalId;

        [Header("Spawn Settings")]
        [Tooltip("Điểm nhân vật sẽ đứng khi dịch chuyển TỚI portal này. Nếu để trống sẽ dùng vị trí của Portal.")]
        public Transform SpawnPoint;

        [Header("Safety")]
        [Tooltip("Thời gian chờ trước khi Portal có thể được kích hoạt lại (chống kẹt)")]
        public float TeleportCooldown = 1f;
        private float lastTeleportTime = -999f;

        private void OnEnable()
        {
            if (PortalManager.Instance != null)
            {
                PortalManager.Instance.RegisterPortal(this);
            }
        }

        private void OnDisable()
        {
            if (PortalManager.Instance != null)
            {
                PortalManager.Instance.UnregisterPortal(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - lastTeleportTime < TeleportCooldown) return;

            Debug.Log($"[Portal {portalId}] Something entered: {other.name}, Tag: {other.tag}");
            
            if (other.TryGetComponent(out HeroInstance hero)) 
            {
                lastTeleportTime = Time.time;
                string sceneName = targetSceneSO != null ? targetSceneSO.ActiveScene.SceneName : targetScene;
                Debug.Log($"[Portal {portalId}] Hero {hero.HeroId} entered! Requesting teleport to {sceneName} (Portal: {targetPortalId})");
                EventBus.Publish(new TeleportRequestEvent(sceneName, targetPortalId, targetSceneSO));
            }
        }
    }
}
