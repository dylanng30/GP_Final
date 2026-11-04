using System.Collections.Generic;
using UnityEngine;

namespace Game.Navigation.Components
{
    public class PortalManager : MonoBehaviour
    {
        public static PortalManager Instance { get; private set; }
        
        private Dictionary<string, Portal> portalsInScene = new Dictionary<string, Portal>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // Tách ra khỏi cha (nếu có) vì DontDestroyOnLoad chỉ hoạt động trên Root Object
                transform.SetParent(null); 
                DontDestroyOnLoad(gameObject); // Bất tử qua các Scene
                RegisterAllPortals();
            }
            else if (Instance != this)
            {
                Destroy(gameObject); // Tự sát nếu đã có 1 Manager khác đang tồn tại
            }
        }

        private void RegisterAllPortals()
        {
            Portal[] portals = FindObjectsOfType<Portal>(true); // Tham số true để tìm cả các portal bị ẩn
            foreach (var portal in portals)
            {
                RegisterPortal(portal);
            }
        }

        public void RegisterPortal(Portal portal)
        {
            if (string.IsNullOrEmpty(portal.portalId)) return;

            if (portalsInScene.ContainsKey(portal.portalId))
            {
                Debug.LogWarning($"PortalManager: Cảnh báo trùng lặp Portal ID '{portal.portalId}'. Portal mới sẽ ghi đè portal cũ.");
            }
            portalsInScene[portal.portalId] = portal; // Luôn ghi đè để ưu tiên Portal mới nhất (vừa OnEnable)
        }

        public void UnregisterPortal(Portal portal)
        {
            if (string.IsNullOrEmpty(portal.portalId)) return;

            if (portalsInScene.TryGetValue(portal.portalId, out Portal existingPortal))
            {
                // Chỉ gỡ bỏ nếu portal đang đăng ký chính xác là portal gọi hàm Unregister
                // Giúp tránh lỗi race condition khi Scene A (tắt sau) gỡ nhầm Portal của Scene B (bật trước)
                if (existingPortal == portal) 
                {
                    portalsInScene.Remove(portal.portalId);
                }
            }
        }

        public Vector3 GetPortalPosition(string portalId)
        {
            if (portalsInScene.TryGetValue(portalId, out Portal portal))
            {
                if (portal.SpawnPoint != null)
                {
                    return portal.SpawnPoint.position;
                }
                return portal.transform.position;
            }
            Debug.LogError($"PortalManager: Portal {portalId} not found in this scene!");
            return Vector3.zero;
        }
    }
}
