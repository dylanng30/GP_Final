using System.Collections.Generic;
using UnityEngine;
using GP_Final.Navigation.Core;
using GP_Final.Navigation.Data;
using UnityEngine.SceneManagement;

namespace GP_Final.Navigation.Components
{
    public class MultiSceneNavigator : MonoBehaviour
    {
        [SerializeField] private WorldMapGraphSO worldGraph;
        
        private ILocalPathfinder localPathfinder;
        private Queue<string> globalPathQueue;
        
        // This keeps track of the final destination so we can walk there after all portals are done.
        private Vector3 finalTargetPos;
        private string finalTargetScene;

        private void Awake()
        {
            // You can change this to GetComponent<AStarGridLocalPathfinder>() later
            localPathfinder = GetComponent<ILocalPathfinder>(); 
            if (localPathfinder == null)
            {
                Debug.LogError("MultiSceneNavigator requires an ILocalPathfinder component!");
            }
            else
            {
                localPathfinder.OnDestinationReached += HandleDestinationReached;
            }
        }

        private void OnDestroy()
        {
            if (localPathfinder != null)
            {
                localPathfinder.OnDestinationReached -= HandleDestinationReached;
            }
        }

        public void NavigateTo(string targetScene, Vector3 targetPos)
        {
            finalTargetScene = targetScene;
            finalTargetPos = targetPos;
            
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == targetScene)
            {
                // Same scene, just use local pathfinder
                globalPathQueue?.Clear();
                localPathfinder.SetDestination(targetPos);
            }
            else
            {
                // Different scene, calculate global path
                List<string> path = GlobalPathfinder.FindGlobalPath(worldGraph, currentScene, transform.position, targetScene, targetPos);
                globalPathQueue = new Queue<string>(path);
                
                GoToNextPortal();
            }
        }

        private void GoToNextPortal()
        {
            if (globalPathQueue != null && globalPathQueue.Count > 0)
            {
                string nextPortalId = globalPathQueue.Peek();
                // Check if this portal is in the current scene
                if (PortalManager.Instance != null)
                {
                    Vector3 portalPos = PortalManager.Instance.GetPortalPosition(nextPortalId);
                    if (portalPos != Vector3.zero)
                    {
                         localPathfinder.SetDestination(portalPos);
                         return;
                    }
                }
            }
        }

        private void HandleDestinationReached()
        {
            if (globalPathQueue != null && globalPathQueue.Count > 0)
            {
                // We reached a portal (assuming we were walking to one)
                // However, the actual scene transition should happen via the Portal's OnTriggerEnter.
                // The scene transition will reload this character or spawn a new one, 
                // so the logic for 'continuing' the path needs to be handled after scene load.
            }
            else
            {
                // Reached final destination
                Debug.Log("MultiSceneNavigator: Reached final destination!");
            }
        }

        // Call this when the scene is loaded and character is placed at the new portal
        public void OnSceneLoadedAndPlacedAtPortal(string currentPortalId)
        {
            if (globalPathQueue != null && globalPathQueue.Count > 0)
            {
                // The current portal we just entered should be at the front of the queue, or the one we just exited.
                // Depending on how global path is constructed, you might need to dequeue the portal you just came out of.
                if (globalPathQueue.Peek() == currentPortalId)
                {
                    globalPathQueue.Dequeue();
                }

                if (globalPathQueue.Count > 0)
                {
                    GoToNextPortal();
                }
                else
                {
                    // No more portals, walk to final destination if we are in the right scene
                    string currentScene = SceneManager.GetActiveScene().name;
                    if (currentScene == finalTargetScene)
                    {
                         localPathfinder.SetDestination(finalTargetPos);
                    }
                }
            }
        }
    }
}
