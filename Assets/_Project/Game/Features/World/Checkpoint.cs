using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Điểm hồi sinh. Hero đang điều khiển đi vào bán kính thì điểm này thành nơi đội quay lại
    /// khi rơi khỏi map hoặc khi cả đội ngã. <see cref="WorldRespawnService"/> kiểm tra khoảng cách mỗi frame.
    /// </summary>
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private float activationRadius = 4f;

        [Tooltip("Hướng đội đứng khi hồi sinh. Để trống thì dùng hướng của chính checkpoint")]
        [SerializeField] private Transform facing;

        public float ActivationRadius => activationRadius;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => Quaternion.Euler(0f, (facing != null ? facing : transform).eulerAngles.y, 0f);

        public bool Contains(Vector3 point)
        {
            Vector3 delta = point - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude <= activationRadius * activationRadius;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, activationRadius);
        }
    }
}
