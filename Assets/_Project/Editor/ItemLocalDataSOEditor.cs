using UnityEditor;
using UnityEngine;
using Game.Data.Item;

[CustomEditor(typeof(ItemLocalDataSO))]
public class ItemLocalDataSOEditor : Editor
{
    private string searchId = "";
    private SerializedProperty itemAssetsProp;
    private SerializedProperty localItemDataProp;
    
    private SerializedProperty equipmentItemsProp;
    private SerializedProperty luckyBoxItemsProp;

    private void OnEnable()
    {
        itemAssetsProp = serializedObject.FindProperty("<ItemAssets>k__BackingField");
        if (itemAssetsProp == null) itemAssetsProp = serializedObject.FindProperty("ItemAssets");

        localItemDataProp = serializedObject.FindProperty("<LocalItemData>k__BackingField");
        if (localItemDataProp == null) localItemDataProp = serializedObject.FindProperty("LocalItemData");

        if (localItemDataProp != null)
        {
            equipmentItemsProp = localItemDataProp.FindPropertyRelative("<EquipmentItems>k__BackingField");
            if (equipmentItemsProp == null) equipmentItemsProp = localItemDataProp.FindPropertyRelative("EquipmentItems");

            luckyBoxItemsProp = localItemDataProp.FindPropertyRelative("<LuckyBoxItems>k__BackingField");
            if (luckyBoxItemsProp == null) luckyBoxItemsProp = localItemDataProp.FindPropertyRelative("LuckyBoxItems");
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        GUIStyle searchBoxStyle = new GUIStyle(EditorStyles.helpBox);
        EditorGUILayout.BeginVertical(searchBoxStyle);
        searchId = EditorGUILayout.TextField("Find with ItemId:", searchId);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();

        if (string.IsNullOrEmpty(searchId))
        {
            if (itemAssetsProp != null) EditorGUILayout.PropertyField(itemAssetsProp, new GUIContent("Item Assets"), true);
            if (localItemDataProp != null) EditorGUILayout.PropertyField(localItemDataProp, new GUIContent("Local Item Data"), true);
        }
        else
        {
            DrawSearchResults(itemAssetsProp, "Item Assets");
            EditorGUILayout.Space();
            DrawSearchResults(equipmentItemsProp, "Equipment Items");
            EditorGUILayout.Space();
            DrawSearchResults(luckyBoxItemsProp, "Lucky Box Items");
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
            
            SerializedProperty idProp = elementProp.FindPropertyRelative("ItemId");
            
            if (idProp == null)
            {
                idProp = elementProp.FindPropertyRelative("<ItemId>k__BackingField");
            }

            if (idProp != null && idProp.propertyType == SerializedPropertyType.String)
            {
                string idValue = idProp.stringValue;

                if (idValue.ToLower().Contains(searchId.ToLower()))
                {
                    EditorGUILayout.PropertyField(elementProp, new GUIContent($"Index: {i} | ItemId: {idValue}"), true);
                    found = true;
                }
            }
        }

        if (!found)
        {
            EditorGUILayout.HelpBox($"Not found ItemId in {labelName}.", MessageType.Info);
        }
        
        EditorGUI.indentLevel--;
    }
}