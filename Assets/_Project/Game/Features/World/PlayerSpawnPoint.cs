using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Điểm xuất hiện của đội khi vào scene. Mỗi scene nên có một điểm.
    /// Scene không có điểm này thì đội đứng quanh gốc tọa độ như trước.
    /// </summary>
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Khoảng cách ngang giữa các hero khi xếp hàng")]
        [SerializeField] private float formationSpacing = 2f;

        [Tooltip("Kích thước thân hero dùng để tìm chỗ đứng không vướng vật cản")]
        [SerializeField] private float actorRadius = 0.6f;
        [SerializeField] private float actorHeight = 2.6f;

        public static PlayerSpawnPoint Current { get; private set; }

        public Quaternion Rotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        /// <summary>Vị trí của hero thứ <paramref name="index"/> trong đội <paramref name="count"/> người, đã đặt lên mặt đất.</summary>
        public Vector3 GetFormationPosition(int index, int count)
        {
            return GetFormationPosition(transform.position, Rotation, index, count, formationSpacing, actorRadius, actorHeight);
        }

        public static Vector3 GetFormationPosition(Vector3 center, Quaternion rotation, int index, int count,
            float spacing, float radius, float height)
        {
            float offset = (index - (count - 1) * 0.5f) * spacing;
            Vector3 point = center + rotation * Vector3.right * offset;
            GroundPlacement.TryFindStandingSpot(point, radius, height, out Vector3 spot);
            return spot;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.6f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
