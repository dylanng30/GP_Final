using Game.UIs.Screens.CombatHUD;
using Game.UIs.Widgets;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Combat
{
    public static partial class CombatContentBuilder
    {
        private const string CombatHudPrefabPath = "Assets/_Project/Game/Features/Combat/UIs/Screen_CombatHUD.prefab";
        private const string UltimateButtonName = "UltimateCastButton";
        private const int UltimateSlotIndex = 2;
        private static readonly Vector2 UltimateButtonOffset = new Vector2(-170f, 90f);

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
    }
}
