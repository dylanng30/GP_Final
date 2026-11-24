using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Combat.Abilities;
using Game.Combat.Abilities.Effects;

namespace Dylanng.Editor
{
    [CustomPropertyDrawer(typeof(IAbilityEffect), true)]
    public class AbilityEffectDrawer : PropertyDrawer
    {
        private List<Type> _implementations;
        private string[] _implementationNames;
        private string[] _implementationFullNames;

        private void Init()
        {
            if (_implementations != null) return;

            // Lấy tất cả các class C# thuần triển khai IAbilityEffect và có thể khởi tạo được
            _implementations = TypeCache.GetTypesDerivedFrom<IAbilityEffect>()
                .Where(type => !type.IsInterface && !type.IsAbstract)
                .ToList();

            // Thêm tùy chọn "None" vào đầu
            _implementationNames = new string[] { "None" }.Concat(_implementations.Select(t => t.Name)).ToArray();
            _implementationFullNames = new string[] { "" }.Concat(_implementations.Select(t => t.AssemblyQualifiedName)).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init();

            EditorGUI.BeginProperty(position, label, property);

            // Xác định dòng đầu tiên để hiển thị Label và Dropdown chọn Type
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            // Tìm index hiện tại của kiểu dữ liệu đã được gán
            int currentIndex = 0; // Mặc định là "None"
            string managedRefValue = property.managedReferenceFullTypename;

            if (!string.IsNullOrEmpty(managedRefValue))
            {
                // Định dạng của managedReferenceFullTypename: "AssemblyName FullTypeName"
                string[] parts = managedRefValue.Split(' ');
                if (parts.Length > 1)
                {
                    string fullTypeName = parts[1];
                    Type currentType = _implementations.FirstOrDefault(t => t.FullName == fullTypeName);
                    if (currentType != null)
                    {
                        currentIndex = _implementations.IndexOf(currentType) + 1; // +1 do có "None" ở index 0
                    }
                }
            }

            // Vẽ tiêu đề dạng Foldout (cho phép thu gọn/mở rộng các trường con)
            float dropdownWidth = 150f;
            Rect dropdownRect = new Rect(position.xMax - dropdownWidth, position.y, dropdownWidth, EditorGUIUtility.singleLineHeight);
            Rect foldoutRect = new Rect(position.x, position.y, position.width - dropdownWidth - 5f, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            // Vẽ Dropdown để chọn loại Effect
            EditorGUI.BeginChangeCheck();
            int selectedIndex = EditorGUI.Popup(dropdownRect, currentIndex, _implementationNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedIndex > 0)
                {
                    Type newType = _implementations[selectedIndex - 1];
                    property.managedReferenceValue = Activator.CreateInstance(newType);
                    property.isExpanded = true;
                }
                else
                {
                    property.managedReferenceValue = null;
                }
                property.serializedObject.ApplyModifiedProperties();
            }

            // Vẽ các trường dữ liệu con của class được chọn nếu foldout đang mở
            if (property.isExpanded && property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();
                
                bool enterChildren = true;
                float currentY = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;
                        
                    float height = EditorGUI.GetPropertyHeight(iterator, true);
                    Rect childRect = new Rect(position.x, currentY, position.width, height);
                    
                    EditorGUI.PropertyField(childRect, iterator, true);
                    
                    currentY += height + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init();

            float height = EditorGUIUtility.singleLineHeight;

            if (property.isExpanded && property.managedReferenceValue != null)
            {
                SerializedProperty iterator = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();
                
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    if (SerializedProperty.EqualContents(iterator, endProperty))
                        break;

                    height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                    enterChildren = false;
                }
            }

            return height;
        }
    }
}
