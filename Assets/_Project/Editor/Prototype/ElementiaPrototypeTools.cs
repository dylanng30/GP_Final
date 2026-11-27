using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Data.Hero;
using Game.Gameplay.AI.Controllers;
using Game.Managers;
using Game.Monster;
using Game.World;
using TCO_CustomSceneController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor.Prototype
{
    /// <summary>
    /// Gắn lớp gameplay vào map Elementia để chơi thử, không dựng lại map.
    /// Menu: Tools > Elementia > Setup Prototype | Play Prototype
    /// Chạy lại bao nhiêu lần cũng được: mọi thứ tool tạo nằm dưới "---PROTOTYPE---" và được dựng lại từ đầu.
    /// </summary>
    public static class ElementiaPrototypeTools
    {
        private const string MapScenePath = "Assets/_Project/01_Scenes/Map/Elementia_Map.unity";
        private const string MapSceneName = "Elementia_Map";
        private const string SourceScenePath = "Assets/_Project/01_Scenes/Scene_A.unity";
        private const string BootstrapperScenePath = "Assets/_Project/01_Scenes/Bootstrapper.unity";

        private const string NavigationFolder = "Assets/_Project/Game/Features/Navigation/Data/SceneNavigations";
        private const string SourceNavigationPath = NavigationFolder + "/BootToSceneA_SO.asset";
        private const string MapNavigationPath = NavigationFolder + "/BootToElementiaMap_SO.asset";

        private const string MonsterPrefabFormat = "Assets/_Project/02_Prefabs/Monster/Monster_Crab_{0}.prefab";

        private const string PrototypeRootName = "---PROTOTYPE---";
        private const string MapRootName = "ELEMENTIA_MAP";
        private const string TerrainGroupName = "Terrain";
        private const string PlayerSpawnName = "PlayerSpawn";
        private const string ScaleReferencePrefix = "ScaleRef_Character";
        private const string CameraName = "Main Camera";
        private const string EventSystemName = "EventSystem";
        private const string FloatingTextName = "FloatingTextManager";
        private const string GroundLayerName = "Ground";

        // Collider có tên chứa các từ này là mặt đi lại được (đường, sàn, cầu) ngoài nhóm Terrain.
        private static readonly string[] WalkableNameHints =
        {
            "Paving", "Avenue", "Forecourt", "Floor", "Bridge", "Road", "Path", "Steps", "Stairs", "Plaza"
        };

        private const float KillYMarginBelowTerrain = 5f;
        private const float CameraFarClip = 1500f;
        private const float GroundProbeHeight = 6f;
        private const float GroundClearance = 0.1f;

        // Vòng dò chỗ trống quanh marker (m) và số điểm trên mỗi vòng.
        private static readonly float[] FreeSpotRings = { 0f, 1.5f, 3f, 4.5f, 6f, 8f, 10f, 13f };
        private const int FreeSpotSamplesPerRing = 12;
        private const float FreeSpotSkin = 0.05f;

        private enum Tier { Normal, Elite, Boss }

        /// <summary>Hệ số nhân lên giá trị của prefab quái. Quái thường giữ nguyên prefab.</summary>
        private struct TierSettings
        {
            public float Scale;
            public float HpMultiplier;
            public float AttackMultiplier;
            public float DetectionMultiplier;
            public float LeashMultiplier;
            /// <summary>Âm: giữ giá trị của prefab. 0: không hồi sinh.</summary>
            public float RespawnDelay;
        }

        private const float KeepPrefabValue = -1f;

        private static readonly Dictionary<Tier, TierSettings> Tiers = new Dictionary<Tier, TierSettings>
        {
            { Tier.Normal, new TierSettings { Scale = 1f, HpMultiplier = 1f, AttackMultiplier = 1f, DetectionMultiplier = 1f, LeashMultiplier = 1f, RespawnDelay = KeepPrefabValue } },
            { Tier.Elite, new TierSettings { Scale = 1.5f, HpMultiplier = 3f, AttackMultiplier = 1.4f, DetectionMultiplier = 1.3f, LeashMultiplier = 1.4f, RespawnDelay = 30f } },
            { Tier.Boss, new TierSettings { Scale = 2.5f, HpMultiplier = 10f, AttackMultiplier = 1.8f, DetectionMultiplier = 1.8f, LeashMultiplier = 2f, RespawnDelay = 0f } },
        };

        // Hệ quái theo tên marker, so theo thứ tự nên mẫu cụ thể đứng trước ("VineRuins" trước "Ruins").
        private static readonly (string Hint, GameElement Element)[] ElementByMarker =
        {
            ("Wilds", GameElement.Earth),
            ("JungleW_ForestEdge", GameElement.Wood),
            ("JungleW_MarshHeart", GameElement.Water),
            ("JungleE_Trail", GameElement.Wood),
            ("JungleE_VineRuins", GameElement.Metal),
            ("AshField", GameElement.Fire),
            ("Ruins", GameElement.Earth),
            ("CrystalHollow", GameElement.Metal),
            ("MirrorLake", GameElement.Water),
            ("CanopyHollow", GameElement.Wood),
            ("BossSpawn_Stage1", GameElement.Wood),
            ("BossSpawn", GameElement.Fire),
        };

        [MenuItem("Tools/Elementia/Setup Prototype")]
        public static void SetupMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[ElementiaPrototype] Hãy thoát Play Mode trước khi setup.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Debug.Log(Setup());
        }

        [MenuItem("Tools/Elementia/Play Prototype")]
        public static void PlayPrototype()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var bootstrapper = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootstrapperScenePath);
            if (bootstrapper == null || AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(MapNavigationPath) == null)
            {
                Debug.LogError("[ElementiaPrototype] Chưa setup. Hãy chạy Tools/Elementia/Setup Prototype trước.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            RegisterSceneInBuildSettings();
            SessionState.SetString(GameManager.BootNavigationOverrideKey, MapNavigationPath);
            EditorSceneManager.playModeStartScene = bootstrapper;
            EditorApplication.isPlaying = true;
            // CombatSandboxTools xóa override và playModeStartScene khi thoát Play Mode.
        }

        /// <summary>Chạy bằng batchmode: -executeMethod Game.Editor.Prototype.ElementiaPrototypeTools.RunBatch</summary>
        public static void RunBatch()
        {
            int exitCode = 0;
            try
            {
                string report = Setup();
                Debug.Log(report);
                if (report.Contains("[LỖI]")) exitCode = 1;
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        public static string Setup()
        {
            var report = new StringBuilder("[ElementiaPrototype] Setup ").AppendLine(MapScenePath);

            CreateNavigation(report);
            RegisterSceneInBuildSettings();

            Scene map = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
            GameObject mapRoot = FindRoot(map, MapRootName);
            if (mapRoot == null)
            {
                report.AppendLine("[LỖI] Không tìm thấy " + MapRootName + " trong scene.");
                return report.ToString();
            }

            DestroyRoot(map, PrototypeRootName);
            var prototypeRoot = new GameObject(PrototypeRootName);
            var managers = new GameObject("Managers");
            managers.transform.SetParent(prototypeRoot.transform, false);

            RemoveScaleReference(mapRoot, report);
            // Cỏ đồng đội thêm tay nằm ngoài ELEMENTIA_MAP, nên quét mọi root của scene.
            int windFixed = map.GetRootGameObjects().Sum(Elementia.EditorTools.ElementiaMapBuilder.ExcludeWindShadersFromBatching);
            report.AppendLine($"Bỏ static batching cho {windFixed} renderer cỏ cây dùng shader gió.");
            int groundCount = AssignGroundLayer(mapRoot, out float killY);
            report.AppendLine($"Layer Ground: {groundCount} collider. Kill Y = {killY:F1}");

            Transform spawn = FindDeep(mapRoot.transform, PlayerSpawnName);
            if (spawn == null) report.AppendLine("[LỖI] Không tìm thấy " + PlayerSpawnName + ".");
            else GetOrAdd<PlayerSpawnPoint>(spawn.gameObject);

            CopySceneServices(map, managers.transform, spawn, report);

            int checkpointCount = SetupCheckpoints(mapRoot);
            var respawn = new GameObject("WorldRespawnService").AddComponent<WorldRespawnService>();
            respawn.transform.SetParent(managers.transform, false);
            SetFloat(respawn, "killY", killY);
            report.AppendLine($"Checkpoint: {checkpointCount}");

            Physics.SyncTransforms();
            SpawnEnemies(mapRoot, prototypeRoot.transform, report);

            EditorSceneManager.MarkSceneDirty(map);
            EditorSceneManager.SaveScene(map);
            AssetDatabase.SaveAssets();
            report.Append("Xong. Chơi thử bằng Tools/Elementia/Play Prototype.");
            return report.ToString();
        }

        // ---------------- Scene services ----------------

        /// <summary>Lấy camera, EventSystem và FloatingTextManager từ Scene_A để giữ đúng cấu hình đã chỉnh.</summary>
        private static void CopySceneServices(Scene map, Transform managers, Transform spawn, StringBuilder report)
        {
            Camera oldCamera = FindRoot(map, CameraName)?.GetComponent<Camera>();
            float farClip = oldCamera != null ? Mathf.Max(oldCamera.farClipPlane, CameraFarClip) : CameraFarClip;
            DestroyRoot(map, CameraName);
            DestroyRoot(map, EventSystemName);

            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(map);

                GameObject camera = CloneFrom(source, map, CameraName, null, report);
                if (camera != null)
                {
                    camera.tag = "MainCamera";
                    var cam = camera.GetComponent<Camera>();
                    if (cam != null) cam.farClipPlane = farClip;
                    if (spawn != null)
                    {
                        camera.transform.position = spawn.position - spawn.forward * 6f + Vector3.up * 3f;
                        camera.transform.LookAt(spawn.position + Vector3.up * 1.5f);
                    }
                }

                CloneFrom(source, map, EventSystemName, null, report);
                CloneFrom(source, map, FloatingTextName, managers, report);
            }
            finally
            {
                EditorSceneManager.CloseScene(source, true);
            }
        }

        private static GameObject CloneFrom(Scene source, Scene map, string objectName, Transform parent, StringBuilder report)
        {
            Transform original = source.GetRootGameObjects()
                .Select(root => FindDeep(root.transform, objectName))
                .FirstOrDefault(found => found != null);
            if (original == null)
            {
                report.AppendLine($"[LỖI] Scene_A không có {objectName}.");
                return null;
            }

            GameObject clone = Object.Instantiate(original.gameObject);
            clone.name = objectName;
            if (parent != null) clone.transform.SetParent(parent, true);
            else if (clone.scene != map) SceneManager.MoveGameObjectToScene(clone, map);
            report.AppendLine("Đã chép " + objectName + " từ Scene_A.");
            return clone;
        }

        // ---------------- Map data ----------------

        private static void RemoveScaleReference(GameObject mapRoot, StringBuilder report)
        {
            var references = mapRoot.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.StartsWith(ScaleReferencePrefix))
                .Select(t => t.gameObject)
                .ToList();
            foreach (GameObject reference in references) Object.DestroyImmediate(reference);
            if (references.Count > 0) report.AppendLine("Đã xóa model tham chiếu tỉ lệ ở điểm spawn.");
        }

        /// <summary>Đưa mặt đi lại được sang layer Ground cho ngắm và chỉ báo skill. Trả về Kill Y dưới đáy địa hình.</summary>
        private static int AssignGroundLayer(GameObject mapRoot, out float killY)
        {
            int groundLayer = LayerMask.NameToLayer(GroundLayerName);
            Transform terrain = mapRoot.transform.Find(TerrainGroupName);
            float lowest = float.MaxValue;
            int count = 0;

            foreach (MeshCollider collider in mapRoot.GetComponentsInChildren<MeshCollider>(true))
            {
                bool inTerrain = terrain != null && collider.transform.IsChildOf(terrain);
                bool walkable = inTerrain || WalkableNameHints.Any(hint => collider.name.Contains(hint));
                if (!walkable) continue;

                collider.gameObject.layer = groundLayer;
                count++;
                if (inTerrain && collider.sharedMesh != null) lowest = Mathf.Min(lowest, collider.bounds.min.y);
            }

            killY = (lowest < float.MaxValue ? lowest : 0f) - KillYMarginBelowTerrain;
            return count;
        }

        private static int SetupCheckpoints(GameObject mapRoot)
        {
            // Marker nằm rải trong nhóm của từng khu (Gameplay_Connecting, Gameplay_Volcano...), nên tìm cả map.
            int count = 0;
            foreach (Transform t in mapRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Checkpoint_")) continue;
                GetOrAdd<Checkpoint>(t.gameObject);
                count++;
            }
            return count;
        }

        // ---------------- Enemies ----------------

        private static void SpawnEnemies(GameObject mapRoot, Transform prototypeRoot, StringBuilder report)
        {
            var enemies = new GameObject("Enemies").transform;
            enemies.SetParent(prototypeRoot, false);
            var counts = new Dictionary<Tier, int> { { Tier.Normal, 0 }, { Tier.Elite, 0 }, { Tier.Boss, 0 } };

            foreach (Transform marker in mapRoot.GetComponentsInChildren<Transform>(true).ToList())
            {
                Tier tier;
                if (marker.name.StartsWith("EnemySpawn_")) tier = Tier.Normal;
                else if (marker.name.StartsWith("CombatArea_") && marker.name.EndsWith("_Elite")) tier = Tier.Elite;
                else if (marker.name.StartsWith("BossSpawn")) tier = Tier.Boss;
                else continue;

                if (!TryGetElement(marker.name, out GameElement element))
                {
                    report.AppendLine($"[LỖI] Chưa gán hệ cho marker {marker.name}.");
                    continue;
                }
                if (SpawnMonster(marker, element, tier, enemies, report)) counts[tier]++;
            }

            report.AppendLine($"Quái: {counts[Tier.Normal]} thường, {counts[Tier.Elite]} tinh anh, {counts[Tier.Boss]} boss.");
        }

        private static bool TryGetElement(string markerName, out GameElement element)
        {
            foreach (var (hint, value) in ElementByMarker)
            {
                if (!markerName.Contains(hint)) continue;
                element = value;
                return true;
            }
            element = default;
            return false;
        }

        private static bool SpawnMonster(Transform marker, GameElement element, Tier tier, Transform parent, StringBuilder report)
        {
            string prefabPath = string.Format(MonsterPrefabFormat, element);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                report.AppendLine("[LỖI] Không tìm thấy " + prefabPath);
                return false;
            }

            TierSettings settings = Tiers[tier];
            var monster = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            monster.name = $"{tier}_{element}_{marker.name}";
            var controller = prefab.GetComponent<CharacterController>();
            float radius = (controller != null ? controller.radius : 0.5f) * settings.Scale;
            float height = (controller != null ? controller.height : 1f) * settings.Scale;
            if (!TryFindFreeGroundSpot(marker.position, radius, height, out Vector3 position)
                && !GroundPlacement.TryFindStandingSpot(marker.position, radius, height, out position))
            {
                report.AppendLine($"[Cảnh báo] {monster.name}: không tìm được chỗ trống trên mặt đất, đặt tại marker.");
            }
            monster.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, marker.eulerAngles.y, 0f));
            if (tier == Tier.Normal) return true;

            monster.transform.localScale = prefab.transform.localScale * settings.Scale;

            var stats = new SerializedObject(monster.GetComponent<MonsterInstance>());
            Multiply(stats, "maxHp", settings.HpMultiplier);
            Multiply(stats, "attack", settings.AttackMultiplier);
            Multiply(stats, "flipLift", settings.Scale);
            Multiply(stats, "sinkDepth", settings.Scale);
            if (settings.RespawnDelay >= 0f) stats.FindProperty("respawnDelay").floatValue = settings.RespawnDelay;
            stats.ApplyModifiedPropertiesWithoutUndo();

            var ai = new SerializedObject(monster.GetComponent<EnemyBotController>());
            Multiply(ai, "detectionRadius", settings.DetectionMultiplier);
            Multiply(ai, "maxDistanceFromSpawn", settings.LeashMultiplier);
            ai.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        /// <summary>
        /// Tìm điểm trên layer Ground gần marker mà thân quái không chạm cây, đá hay nhà.
        /// Chỉ dò layer Ground nên tia đi xuyên tán cây và đá trang trí.
        /// </summary>
        private static bool TryFindFreeGroundSpot(Vector3 marker, float radius, float height, out Vector3 position)
        {
            int groundMask = LayerMask.GetMask(GroundLayerName);
            int blockingMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask(GroundLayerName, "Hero", "Monster");

            foreach (float ring in FreeSpotRings)
            {
                int samples = ring <= 0f ? 1 : FreeSpotSamplesPerRing;
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * Mathf.PI * 2f / samples;
                    Vector3 candidate = marker + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                    Vector3 origin = candidate + Vector3.up * GroundProbeHeight;
                    if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore)) continue;

                    Vector3 bottom = hit.point + Vector3.up * (radius + FreeSpotSkin);
                    Vector3 top = hit.point + Vector3.up * Mathf.Max(radius + FreeSpotSkin, height - radius);
                    if (Physics.CheckCapsule(bottom, top, radius, blockingMask, QueryTriggerInteraction.Ignore)) continue;

                    position = hit.point + Vector3.up * GroundClearance;
                    return true;
                }
            }

            position = marker;
            return false;
        }

        private static void Multiply(SerializedObject target, string property, float multiplier)
        {
            target.FindProperty(property).floatValue *= multiplier;
        }

        // ---------------- Project settings ----------------

        private static void CreateNavigation(StringBuilder report)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(MapNavigationPath) == null)
            {
                AssetDatabase.CopyAsset(SourceNavigationPath, MapNavigationPath);
                report.AppendLine("Đã tạo " + MapNavigationPath);
            }

            var navigation = AssetDatabase.LoadAssetAtPath<SceneNavigationSO>(MapNavigationPath);
            navigation.ActiveScene = new SceneSlotData
            {
                SlotKey = navigation.ActiveScene.SlotKey,
                SceneName = MapSceneName
            };
            EditorUtility.SetDirty(navigation);
        }

        private static void RegisterSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(scene => scene.path == MapScenePath)) return;

            EditorBuildSettings.scenes = scenes.Append(new EditorBuildSettingsScene(MapScenePath, true)).ToArray();
        }

        // ---------------- Helpers ----------------

        private static GameObject FindRoot(Scene scene, string objectName)
        {
            return scene.GetRootGameObjects().FirstOrDefault(go => go.name == objectName);
        }

        private static void DestroyRoot(Scene scene, string objectName)
        {
            foreach (GameObject go in scene.GetRootGameObjects().Where(go => go.name == objectName).ToList())
            {
                Object.DestroyImmediate(go);
            }
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root.name == objectName) return root;
            foreach (Transform child in root)
            {
                Transform found = FindDeep(child, objectName);
                if (found != null) return found;
            }
            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            return go.TryGetComponent(out T existing) ? existing : go.AddComponent<T>();
        }

        private static void SetFloat(Object target, string property, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
