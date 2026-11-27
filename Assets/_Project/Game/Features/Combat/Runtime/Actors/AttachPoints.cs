using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Tìm transform con theo tên và lưu cache để không phải duyệt lại cây xương.
    /// Chỉ tìm trong object đang bật: hero đổi model (biến hình) thì điểm gắn lấy theo model đang hiện.
    /// </summary>
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

            if (_cache.TryGetValue(pointName, out Transform cached) && cached != null && cached.gameObject.activeInHierarchy)
            {
                return cached;
            }

            Transform found = FindDeep(_root, pointName, activeOnly: true);
            if (found != null) _cache[pointName] = found;
            else _cache.Remove(pointName);
            return found;
        }

        public static Transform FindDeep(Transform parent, string pointName) => FindDeep(parent, pointName, activeOnly: false);

        public static Transform FindDeep(Transform parent, string pointName, bool activeOnly)
        {
            if (activeOnly && !parent.gameObject.activeSelf) return null;
            if (parent.name == pointName) return parent;

            foreach (Transform child in parent)
            {
                Transform result = FindDeep(child, pointName, activeOnly);
                if (result != null) return result;
            }
            return null;
        }
    }
}
