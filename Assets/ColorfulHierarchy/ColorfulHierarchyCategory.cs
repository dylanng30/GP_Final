using UnityEngine;

namespace ColorfulHierarchy
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Colorful Hierarchy/Category")]
    public class ColorfulHierarchyCategory : MonoBehaviour
    {
        [Header("Text Settings")]
        public Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        public FontStyle fontStyle = FontStyle.Bold;
        public TextAnchor textAlignment = TextAnchor.MiddleLeft;
        
        [Header("Background Settings")]
        public bool enableBackground = true;
        public Color backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        
        [Header("Border Settings")]
        public bool leftBorder = true;
        public Color leftBorderColor = new Color(0.2f, 0.6f, 1f, 1f);
        public int leftBorderWidth = 4;
        
        [Header("Icon Settings")]
        public Texture2D icon;
        
        [Header("Style Extras")]
        public bool textDropShadow = true;

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.RepaintHierarchyWindow();
#endif
        }
    }
}
