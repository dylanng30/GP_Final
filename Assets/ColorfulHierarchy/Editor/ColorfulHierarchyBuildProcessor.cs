using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ColorfulHierarchy
{
    public class ColorfulHierarchyBuildProcessor : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // report is null when entering Play Mode in the Editor
            // We only want to strip components when actually building the game
            if (report == null) return;

            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                ColorfulHierarchyCategory[] categories = root.GetComponentsInChildren<ColorfulHierarchyCategory>(true);
                foreach (var category in categories)
                {
                    // Destroy the component, NOT the GameObject
                    Object.DestroyImmediate(category);
                }
            }
        }
    }
}
