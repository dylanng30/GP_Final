using System.Linq;
using Game.Managers;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using TCO_CustomSceneController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Combat
{
    /// <summary>
    /// Tạo và chạy scene thử combat.
    /// Menu: Tools > Combat > Build Sandbox Scene | Play Sandbox
    /// Scene được dựng lại từ Scene_A để luôn đồng bộ các local manager, camera và UI.
    /// </summary>
    public static class CombatSandboxTools
    {
        private const string SourceScenePath = "Assets/_Project/01_Scenes/Scene_A.unity";
        private const string BootstrapperScenePath = "Assets/_Project/01_Scenes/Bootstrapper.unity";
        private const string SandboxFolder = "Assets/_Project/01_Scenes/Sandbox";
        private const string SandboxSceneName = "Combat_Sandbox";
        private const string SandboxScenePath = SandboxFolder + "/" + SandboxSceneName + ".unity";

        private const string NavigationFolder = "Assets/_Project/Game/Features/Navigation/Data/SceneNavigations";
        private const string SourceNavigationPath = NavigationFolder + "/BootToSceneA_SO.asset";
        private const string SandboxNavigationPath = NavigationFolder + "/BootToCombatSandbox_SO.asset";

        private const string DummyPrefabPath = "Assets/_Project/02_Prefabs/Hero/DummyBot.prefab";
        private const string PortalObjectName = "Portal_A_Out";
        private const string GroundLayerName = "Ground";

        private static readonly GameElement[] DummyElements =
        {
            GameElement.Metal, GameElement.Wood, GameElement.Earth, GameElement.Water, GameElement.Fire
        };

        private const float DummySpacing = 6f;
        private const float DummyRowZ = 12f;
        private const float GroundY = -1f;

        [MenuItem("Tools/Combat/Build Sandbox Scene")]
        public static void BuildSandboxScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[CombatSandboxTools] Hãy thoát Play Mode trước khi dựng Sandbox.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxScenePath) != null;
            if (sceneExists && !Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Combat Sandbox",
                    "Scene Sandbox đã tồn tại. Dựng lại sẽ xóa mọi chỉnh sửa tay trong scene này.",
                    "Dựng lại", "Hủy"))
            {
                return;
            }

            CreateSandboxNavigation();
            CreateSandboxScene(sceneExists);
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            Debug.Log($"[CombatSandboxTools] Đã dựng xong {SandboxScenePath}. Chạy bằng Tools/Combat/Play Sandbox.");
        }

        [MenuItem("Tools/Combat/Play Sandbox")]
        public static void PlaySandbox()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var bootstrapper = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapperScenePath);
            bool isSandboxReady = AssetDatabase.LoadAssetAtPath<SceneAsset>(SandboxScenePath) != null
                                  && AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(SandboxNavigationPath) != null;

            if (bootstrapper == null || !isSandboxReady)
            {
                Debug.LogError("[CombatSandboxTools] Chưa có Sandbox. Hãy chạy Tools/Combat/Build Sandbox Scene trước.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            RegisterSceneInBuildSettings();
            SessionState.SetString(GameManager.BootNavigationOverrideKey, SandboxNavigationPath);
            EditorSceneManager.playModeStartScene = bootstrapper;
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeCleanup()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            // Trả lại luồng khởi động mặc định để nút Play thường không bị ảnh hưởng.
            SessionState.EraseString(GameManager.BootNavigationOverrideKey);
            EditorSceneManager.playModeStartScene = null;
        }

        private static void CreateSandboxNavigation()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(SandboxNavigationPath) == null)
            {
                AssetDatabase.CopyAsset(SourceNavigationPath, SandboxNavigationPath);
            }

            var navigation = AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(SandboxNavigationPath);
            navigation.ActiveScene = new SceneSlotData
            {
                SlotKey = navigation.ActiveScene.SlotKey,
                SceneName = SandboxSceneName
            };
            EditorUtility.SetDirty(navigation);
        }

        private static void CreateSandboxScene(bool replaceExisting)
        {
            if (!AssetDatabase.IsValidFolder(SandboxFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/01_Scenes", "Sandbox");
            }

            if (replaceExisting)
            {
                AssetDatabase.DeleteAsset(SandboxScenePath);
            }

            AssetDatabase.CopyAsset(SourceScenePath, SandboxScenePath);
            Scene scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);

            RemovePortal();

            var root = new GameObject("---COMBAT SANDBOX---");
            SpawnDummies(root.transform);
            SpawnObstacles(root.transform);

            EditorSceneManager.SaveScene(scene);
        }

        private static void RemovePortal()
        {
            // Sandbox không chuyển scene: portal cũ sẽ đưa người chơi sang Scene_B.
            GameObject portal = GameObject.Find(PortalObjectName);
            if (portal != null)
            {
                Object.DestroyImmediate(portal);
            }
        }

        private static void SpawnDummies(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[CombatSandboxTools] Không tìm thấy prefab {DummyPrefabPath}.");
                return;
            }

            float startX = -DummySpacing * (DummyElements.Length - 1) * 0.5f;
            for (int i = 0; i < DummyElements.Length; i++)
            {
                var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                dummy.name = $"Dummy_{DummyElements[i]}";
                dummy.transform.position = new Vector3(startX + i * DummySpacing, GroundY, DummyRowZ);
                SetDummyElement(dummy, DummyElements[i]);
            }
        }

        private static void SetDummyElement(GameObject dummy, GameElement element)
        {
            var serializedDummy = new SerializedObject(dummy.GetComponent<DummyBotInstance>());
            serializedDummy.FindProperty("element").intValue = (int)element;
            serializedDummy.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SpawnObstacles(Transform parent)
        {
            // Tường và dốc dùng để thử va chạm camera và di chuyển ở Phase 5.
            CreateBlock("Wall_CameraTest", parent, new Vector3(-16f, 1f, 0f), new Vector3(1f, 6f, 16f), Vector3.zero);
            CreateBlock("Ramp_MotorTest", parent, new Vector3(16f, 0f, 0f), new Vector3(8f, 1f, 12f), new Vector3(0f, 0f, 15f));
        }

        private static void CreateBlock(string objectName, Transform parent, Vector3 position, Vector3 scale, Vector3 euler)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.layer = LayerMask.NameToLayer(GroundLayerName);
            block.transform.SetParent(parent);
            block.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            block.transform.localScale = scale;
        }

        private static void RegisterSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(scene => scene.path == SandboxScenePath)) return;

            EditorBuildSettings.scenes = scenes
                .Append(new EditorBuildSettingsScene(SandboxScenePath, true))
                .ToArray();
        }
    }
}
