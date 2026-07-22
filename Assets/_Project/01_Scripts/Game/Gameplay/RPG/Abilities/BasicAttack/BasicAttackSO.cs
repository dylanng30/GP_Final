using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.BasicAttack
{
    [CreateAssetMenu(fileName = "NewBasicAttack", menuName = "Dylanng/RPG/Abilities/Basic Attack Data")]
    public class BasicAttackSO : ScriptableData
    {
        [Header("--- Projectile Config ---")]
        [Tooltip("Key prefab viên đạn trong PoolManager")]
        public string ProjectilePoolKey = "BasicAttack_Melee_Invisible";

        [Tooltip("Prefab của projectile để tự động đăng ký vào PoolManager")]
        public BasicAttackProjectile ProjectilePrefab;

        [Tooltip("Nếu true, đạn không có hình dạng (đánh cận chiến hoặc chém vô hình)")]
        public bool IsInvisibleVisual = false;

        [Tooltip("Tốc độ bay của viên đạn")]
        public float ProjectileSpeed = 25f;

        [Tooltip("Khoảng cách bay tối đa của viên đạn trước khi tự thu hồi")]
        public float MaxRange = 8f;

        [Tooltip("Thời gian tồn tại tối đa trên đời nếu không chạm mục tiêu hoặc MaxRange")]
        public float MaxLifetime = 3f;

        [Header("--- Combat Config ---")]
        [Tooltip("Hệ số sát thương so với chỉ số ATK của Hero (1.0 = 100% ATK)")]
        public float DamageMultiplier = 1.0f;

        [Tooltip("Hệ thuộc tính của đòn đánh thường")]
        public ElementType Element = ElementType.Physical;

        [Tooltip("Thời gian giãn cách giữa các đòn đánh thường (giây)")]
        public float AttackCooldown = 1.0f;

        [Tooltip("Offset vị trí xuất hiện viên đạn tính từ tâm Hero")]
        public Vector3 SpawnOffset = new Vector3(0f, 1.2f, 0.8f);

        [Tooltip("LayerMask mục tiêu (Nếu để Nothing sẽ tự động lấy từ Hero/Bot bắn ra)")]
        public LayerMask TargetLayerMask;
    }
}
