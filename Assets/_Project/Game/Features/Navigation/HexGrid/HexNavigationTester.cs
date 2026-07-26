using UnityEngine;
using GP_Final.Navigation.Components;
using GP_Final.Navigation.HexGrid.Core;
using UnityEngine.SceneManagement;

namespace GP_Final.Navigation.HexGrid
{
    /// <summary>
    /// Attach this script to your Hero (or an empty GameObject with reference to the Hero) to test pathfinding.
    /// </summary>
    public class HexNavigationTester : MonoBehaviour
    {
        [Header("References")]
        public MultiSceneNavigator targetNavigator;
        
        [Header("Multi-Scene Test Setup")]
        public string testTargetScene = "Scene_C";
        public Vector3 testTargetPosition = new Vector3(10f, 0f, 15f);
        public KeyCode multiSceneTestKey = KeyCode.M;

        [Header("Local Scene Test Setup")]
        public KeyCode localMoveKey = KeyCode.Mouse1; // Right click

        private void Start()
        {
            if (targetNavigator == null)
            {
                targetNavigator = GetComponent<MultiSceneNavigator>();
            }
        }

        private void Update()
        {
            if (targetNavigator == null) return;

            // 1. Test Local Pathfinding (Click on the grid to move there)
            if (Input.GetKeyDown(localMoveKey))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                // Assuming hex grid is at y = 0
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero); 
                
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    // Navigate to the clicked point in the CURRENT scene
                    string currentScene = SceneManager.GetActiveScene().name;
                    targetNavigator.NavigateTo(currentScene, hitPoint);
                    
                    Debug.Log($"[Tester] Commanded hero to move locally to: {hitPoint}");
                }
            }

            // 2. Test Multi-Scene Pathfinding (Press a hotkey)
            if (Input.GetKeyDown(multiSceneTestKey))
            {
                Debug.Log($"[Tester] Commanded hero to move across scenes to {testTargetScene} at {testTargetPosition}");
                targetNavigator.NavigateTo(testTargetScene, testTargetPosition);
            }
        }
    }
}
