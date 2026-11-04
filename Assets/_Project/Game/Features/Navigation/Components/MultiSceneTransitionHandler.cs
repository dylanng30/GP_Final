using UnityEngine;
using Dylanng;
using Game.Navigation.Events;

namespace Game.Navigation.Components
{
    [RequireComponent(typeof(MultiSceneNavigator))]
    public class MultiSceneTransitionHandler : MonoBehaviour
    {
        private MultiSceneNavigator navigator;
        
        private string pendingPortalId;

        private void Awake()
        {
            navigator = GetComponent<MultiSceneNavigator>();
            
            EventBus.Subscribe<TeleportRequestEvent>(OnTeleportRequest);
            EventBus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            DontDestroyOnLoad(this.gameObject);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<TeleportRequestEvent>(OnTeleportRequest);
            EventBus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
        }

        private void OnTeleportRequest(TeleportRequestEvent evt)
        {
            // Record where we want to appear in the next scene
            pendingPortalId = evt.TargetPortalId;
        }

        private void OnSceneLoaded(SceneLoadedEvent evt)
        {
            if (!string.IsNullOrEmpty(pendingPortalId))
            {
                // Give the scene a tiny frame to initialize its PortalManager
                // (or ensure PortalManager's Awake runs before this event is processed)
                if (PortalManager.Instance != null)
                {
                    Vector3 spawnPos = PortalManager.Instance.GetPortalPosition(pendingPortalId);
                    if (spawnPos != Vector3.zero)
                    {
                        // 1. Move character to the portal
                        transform.position = spawnPos;
                        
                        // 2. Tell the navigator we arrived at the portal so it can resume pathfinding
                        navigator.OnSceneLoadedAndPlacedAtPortal(pendingPortalId);
                    }
                    else
                    {
                        Debug.LogError($"[TransitionHandler] Portal '{pendingPortalId}' not found in the new scene!");
                    }
                }
                else
                {
                    Debug.LogError("[TransitionHandler] PortalManager not found in the new scene!");
                }
                
                // Clear it so it doesn't fire again randomly
                pendingPortalId = null;
            }
        }
    }
}
