using Game.Combat;
using Game.Shaders.WorldProgressBar;
using Game.UIs.Screens.CombatHUD;
using Game.UIs.Widgets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.Combat
{
    public static partial class CombatContentBuilder
    {
        private const string CombatHudPrefabPath = "Assets/_Project/Game/Features/Combat/UIs/Screen_CombatHUD.prefab";
        private const string UltimateButtonName = "UltimateCastButton";
        private const int UltimateSlotIndex = 2;
        private static readonly Vector2 UltimateButtonOffset = new Vector2(-170f, 90f);

        private const string HealthBarName = "ActorHealthBar";
        private const string HealthBarPrefabPath = "Assets/_Project/02_Prefabs/Combat/ActorHealthBar.prefab";
        private const string HealthBarMaterialPath = "Assets/_Project/03_Shaders/WorldProgressBar/WorldProgressBar_Material.mat";
        private const string DummyBotPrefabPath = "Assets/_Project/02_Prefabs/Hero/DummyBot.prefab";
        private static readonly Vector3 HealthBarScale = new Vector3(1.2f, 0.12f, 1f);
        private static readonly Vector3 DummyHealthBarPosition = new Vector3(0f, 2.45f, 0f);
        private static readonly Color HealthBarFillColor = new Color(0.9f, 0.15f, 0.15f);
        private static readonly Color HealthBarBackgroundColor = new Color(0.1f, 0.1f, 0.1f);

        /// <summary>Nhân bản nút skill 2 thành nút Ultimate trên HUD nếu prefab chưa có.</summary>
        private static void BuildHud()
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(CombatHudPrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;
                var hud = new SerializedObject(root.GetComponent<Screen_CombatHUD>());
                SerializedProperty ultimateProperty = hud.FindProperty("ultimateBtn");

                if (ultimateProperty.objectReferenceValue != null) return;

                var skill2 = (SkillCastButton)hud.FindProperty("skill2Btn").objectReferenceValue;
                SkillCastButton ultimate = Object.Instantiate(skill2, skill2.transform.parent);
                ultimate.name = UltimateButtonName;

                var ultimateRect = (RectTransform)ultimate.transform;
                ultimateRect.anchoredPosition = ((RectTransform)skill2.transform).anchoredPosition + UltimateButtonOffset;

                var serializedButton = new SerializedObject(ultimate);
                serializedButton.FindProperty("skillIndex").intValue = UltimateSlotIndex;
                serializedButton.ApplyModifiedPropertiesWithoutUndo();

                ultimateProperty.objectReferenceValue = ultimate;
                hud.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>Dựng prefab thanh máu từ shader WorldProgressBar rồi gắn vào DummyBot nếu chưa có.</summary>
        public static void BuildHealthBar()
        {
            var healthBar = new GameObject(HealthBarName);
            healthBar.transform.localScale = HealthBarScale;
            healthBar.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var meshRenderer = healthBar.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(HealthBarMaterialPath);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            var barController = healthBar.AddComponent<WorldProgressBar_ShaderController>();
            var serializedBar = new SerializedObject(barController);
            serializedBar.FindProperty("sRenderer").objectReferenceValue = meshRenderer;
            serializedBar.FindProperty("progressColor").colorValue = HealthBarFillColor;
            serializedBar.FindProperty("backgroundColor").colorValue = HealthBarBackgroundColor;
            serializedBar.ApplyModifiedPropertiesWithoutUndo();

            healthBar.AddComponent<ActorHealthBar>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(healthBar, HealthBarPrefabPath);
            Object.DestroyImmediate(healthBar);

            using (var scope = new PrefabUtility.EditPrefabContentsScope(DummyBotPrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root.GetComponentInChildren<ActorHealthBar>(true) != null) return;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                instance.transform.localPosition = DummyHealthBarPosition;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Content] Đã dựng thanh máu và gắn vào DummyBot.");
        }
    }
}
