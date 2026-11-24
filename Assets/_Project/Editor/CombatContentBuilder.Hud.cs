using Game.Combat;
using Game.Shaders.WorldProgressBar;
using Game.UIs.Screens.CombatHUD;
using Game.UIs.Widgets;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

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

        private const string UiManagerPrefabPath = "Assets/_Project/Game/UI_Global/CoreWidgets/UI_Manager.prefab";
        private const string LookSwipeAreaName = "LookSwipeArea";
        private const string ViewToggleButtonName = "ViewToggleButton";
        private const string ViewToggleButtonLabel = "Đổi camera";
        // Bootstrapper đặt nút Change mode ở giữa đáy màn hình, nút đổi camera nằm ngay bên trái.
        private static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);
        private static readonly Vector2 ViewToggleButtonPosition = new Vector2(-220f, 0f);

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

        /// <summary>Dựng các điều khiển dùng chung cho PC và mobile. Chạy lại không tạo trùng.</summary>
        public static void BuildPlayerControls()
        {
            BuildGlobalControls();
            BuildCombatActionButtons();

            AssetDatabase.SaveAssets();
            Debug.Log("[Content] Đã dựng vùng vuốt và các nút điều khiển.");
        }

        private static void BuildGlobalControls()
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(UiManagerPrefabPath))
            {
                GameObject root = scope.prefabContentsRoot;
                if (root.GetComponentInChildren<LookSwipeArea>(true) != null) return;

                CreateLookSwipeArea(root.GetComponentInChildren<Canvas>(true).transform);
                CreateViewToggleButton(root.GetComponentInChildren<Button>(true));
            }
        }

        private static void CreateLookSwipeArea(Transform canvas)
        {
            var area = new GameObject(LookSwipeAreaName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LookSwipeArea))
            {
                layer = canvas.gameObject.layer
            };

            var rect = (RectTransform)area.transform;
            rect.SetParent(canvas, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            area.GetComponent<Image>().color = Color.clear;
        }

        private static void CreateViewToggleButton(Button changeModeButton)
        {
            Button viewButton = Object.Instantiate(changeModeButton, changeModeButton.transform.parent);
            viewButton.name = ViewToggleButtonName;
            UnityEventTools.RemovePersistentListener(viewButton.onClick, 0);

            var rect = (RectTransform)viewButton.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = BottomCenter;
            rect.anchoredPosition = ViewToggleButtonPosition;

            viewButton.GetComponentInChildren<TMP_Text>().text = ViewToggleButtonLabel;
            viewButton.gameObject.AddComponent<CameraViewToggleButton>();
        }

        private static void BuildCombatActionButtons()
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(CombatHudPrefabPath))
            {
                var hud = new SerializedObject(scope.prefabContentsRoot.GetComponent<Screen_CombatHUD>());
                var dashButton = (Button)hud.FindProperty("dashBtn").objectReferenceValue;

                CreateActionButton(hud, dashButton, "jumpBtn", "JumpButton", "Nhảy", new Vector2(-440f, 60f));
                CreateActionButton(hud, dashButton, "lockOnBtn", "LockOnButton", "Khóa", new Vector2(-580f, 200f));
                CreateActionButton(hud, dashButton, "regroupBtn", "RegroupButton", "Tập hợp", new Vector2(-580f, 60f));

                hud.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateActionButton(
            SerializedObject hud, Button template, string fieldName, string buttonName, string label, Vector2 position)
        {
            SerializedProperty property = hud.FindProperty(fieldName);
            if (property.objectReferenceValue != null) return;

            Button button = Object.Instantiate(template, template.transform.parent);
            button.name = buttonName;
            ((RectTransform)button.transform).anchoredPosition = position;
            button.GetComponentInChildren<TMP_Text>().text = label;

            property.objectReferenceValue = button;
        }
    }
}
