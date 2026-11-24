using Dylanng;
using System;
using System.Collections.Generic;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Abilities.BasicAttack
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
        public GameElement Element = GameElement.Physical;

        [Tooltip("Thời gian giãn cách giữa các đòn đánh thường (giây)")]
        public float AttackCooldown = 1.0f;

        [Tooltip("Lượng Nộ nhận được mỗi đòn đánh thường trúng mục tiêu")]
        public float EnergyPerHit = 15f;

        [Tooltip("Offset vị trí xuất hiện viên đạn tính từ tâm Hero")]
        public Vector3 SpawnOffset = new Vector3(0f, 1.2f, 0.8f);

        [Tooltip("Tên của Transform con (như FirePoint, WeaponTip) để spawn đạn. Nếu trống, sẽ dùng SpawnOffset.")]
        public string SpawnPointName = "";

        [Tooltip("LayerMask mục tiêu (Nếu để Nothing sẽ tự động lấy từ Hero/Bot bắn ra)")]
        public LayerMask TargetLayerMask;

        [Header("--- Ranged Presentation ---")]
        [Tooltip("Hiệu ứng lửa đầu nòng, gắn theo điểm bắn lúc đạn rời nòng")]
        public GameObject MuzzleFlashPrefab;

        public float MuzzleFlashScale = 1f;

        [Tooltip("Key âm thanh trong CombatSoundBank phát lúc bắn. Để trống nếu prefab đạn đã có âm thanh riêng")]
        public string ShootSoundKey = "";

        [Tooltip("Độ giật camera mỗi phát bắn của hero người chơi đang điều khiển")]
        public float ShootShake = 0.04f;

        [Header("--- Melee & Combo ---")]
        [Tooltip("Đòn cận chiến quét vùng trước mặt thay vì bắn đạn")]
        public bool IsMelee = false;

        [Tooltip("Chuỗi đòn đánh. Để trống nghĩa là chỉ có một đòn")]
        public List<ComboStep> Combo = new List<ComboStep>();

        [Tooltip("Sau khoảng thời gian này không đánh tiếp, combo quay về đòn đầu")]
        public float ComboResetTime = 1.5f;

        public ComboStep GetComboStep(int index)
        {
            return index >= 0 && index < Combo.Count ? Combo[index] : ComboStep.Default;
        }
    }

    [Serializable]
    public struct ComboStep
    {
        public static readonly ComboStep Default = new ComboStep
        {
            DamageMultiplier = 1f,
            Range = 2.5f,
            ArcAngle = 120f,
            LungeDistance = 0f,
            SlashRoll = 35f
        };

        public float DamageMultiplier;
        public float Range;
        public float ArcAngle;
        public float LungeDistance;

        [Tooltip("Góc nghiêng của vệt chém quanh hướng nhìn (độ). Dương: chéo từ trên phải xuống, âm: chéo từ trên trái xuống, 0: ngang")]
        public float SlashRoll;

        [Tooltip("Đảo chiều quét của vệt chém")]
        public bool MirrorSlash;

        [Tooltip("Dùng vệt chém dày của đòn kết thúc")]
        public bool IsFinisher;
    }
}
