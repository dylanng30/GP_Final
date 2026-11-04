using UnityEngine;
using Game.Navigation.Events;
using Dylanng;
using Game.Gameplay.Entities;

namespace Game.Navigation.Components
{
    [RequireComponent(typeof(Collider))]
    public class Portal : MonoBehaviour
    {
        [Header("Portal Settings")]
        public string portalId;
        public string targetScene;
        public TCO_CustomSceneController.SceneNavigationSO targetSceneSO;
        public string targetPortalId;

        [Header("Spawn Settings")]
        public Transform SpawnPoint;

        [Header("Safety")]
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
