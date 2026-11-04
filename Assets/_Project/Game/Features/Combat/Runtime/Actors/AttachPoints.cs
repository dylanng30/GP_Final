using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Tìm transform con theo tên và lưu cache để không phải duyệt lại cây xương.</summary>
    public sealed class AttachPoints
    {
        private readonly Transform _root;
        private readonly Dictionary<string, Transform> _cache = new Dictionary<string, Transform>();

        public AttachPoints(Transform root)
        {
            _root = root;
        }

        public Transform Get(string pointName)
        {
            if (string.IsNullOrEmpty(pointName)) return null;

            if (_cache.TryGetValue(pointName, out Transform cached) && cached != null) return cached;

            Transform found = FindDeep(_root, pointName);
            if (found != null) _cache[pointName] = found;
            return found;
        }

        public static Transform FindDeep(Transform parent, string pointName)
        {
            if (parent.name == pointName) return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindDeep(child, pointName);
                if (result != null) return result;
            }
            return null;
        }
    }
}
