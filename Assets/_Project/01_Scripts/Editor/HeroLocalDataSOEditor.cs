using Game.Data.Hero;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeroLocalDataSO))]
public class HeroLocalDataSOEditor : Editor
{
    private string searchId = "";
    private SerializedProperty heroAssetsProp;
    private SerializedProperty localHeroDataProp;

    private void OnEnable()
    {
        // Quét List khai báo dạng Auto-property
        heroAssetsProp = serializedObject.FindProperty("<HeroAssets>k__BackingField");
        localHeroDataProp = serializedObject.FindProperty("<HeroData>k__BackingField");
        
        // Quét List khai báo dạng field thường (dự phòng)
        if (heroAssetsProp == null) heroAssetsProp = serializedObject.FindProperty("HeroAssets");
        if (localHeroDataProp == null) localHeroDataProp = serializedObject.FindProperty("HeroData");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Vẽ thanh tìm kiếm
        EditorGUILayout.Space();
        GUIStyle searchBoxStyle = new GUIStyle(EditorStyles.helpBox);
        EditorGUILayout.BeginVertical(searchBoxStyle);
        searchId = EditorGUILayout.TextField("Find with HeroId:", searchId);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        // Logic hiển thị
        if (string.IsNullOrEmpty(searchId))
        {
            // Hiển thị bình thường nếu không tìm kiếm
            if (heroAssetsProp != null) EditorGUILayout.PropertyField(heroAssetsProp, new GUIContent("Hero Assets"), true);
            if (localHeroDataProp != null) EditorGUILayout.PropertyField(localHeroDataProp, new GUIContent(" Hero Data"), true);
        }
        else
        {
            // Hiển thị kết quả tìm kiếm
            DrawSearchResults(heroAssetsProp, "Hero Assets");
            EditorGUILayout.Space();
            DrawSearchResults(localHeroDataProp, "Hero Data");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSearchResults(SerializedProperty listProperty, string labelName)
    {
        if (listProperty == null || !listProperty.isArray) return;

        EditorGUILayout.LabelField($"{labelName}", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        bool found = false;
        
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty elementProp = listProperty.GetArrayElementAtIndex(i);
            
            // GIẢI PHÁP DỨT ĐIỂM TÌM HEROID
            // 1. Thử tìm với field bình thường
            SerializedProperty idProp = elementProp.FindPropertyRelative("HeroId");
            
            // 2. Thử tìm với dạng Auto-property nếu cách 1 không thấy
            if (idProp == null)
            {
                idProp = elementProp.FindPropertyRelative("<HeroId>k__BackingField");
            }

            // Nếu tìm thấy và nó là kiểu string
            if (idProp != null && idProp.propertyType == SerializedPropertyType.String)
            {
                string idValue = idProp.stringValue;

                // Kiểm tra chuỗi chứa nội dung tìm kiếm (không phân biệt hoa thường)
                if (idValue.ToLower().Contains(searchId.ToLower()))
                {
                    EditorGUILayout.PropertyField(elementProp, new GUIContent($"Index {i} | HeroId: {idValue}"), true);
                    found = true;
                }
            }
        }

        if (!found)
        {
            EditorGUILayout.HelpBox($"Not found HeroId in {labelName}.", MessageType.Info);
        }
        
        EditorGUI.indentLevel--;
    }
}