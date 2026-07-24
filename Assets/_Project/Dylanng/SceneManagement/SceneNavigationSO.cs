using UnityEngine;

namespace TCO_CustomSceneController
{
    [System.Serializable]
    public struct SceneSlotData
    {
        public string SlotKey;
        public string SceneName;
    }

    [CreateAssetMenu(fileName = "NewSceneNavigationSO", menuName = "TCO/SceneNavigationSO")]
    public class SceneNavigationSO : ScriptableObject
    {
        public SceneSlotData ActiveScene;
        public SceneSlotData[] AdditiveScenes;
        public string[] UnloadSceneSlots; 
        public bool ClearUnusedAssets;
        public bool HasOverlay;
    }
}