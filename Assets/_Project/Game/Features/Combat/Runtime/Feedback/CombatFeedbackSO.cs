using System;
using System.Collections.Generic;
using Game.Combat.Domain;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Thông số cảm giác đánh: hitstop, chớp sáng, nảy model, đẩy lùi, rung camera và VFX theo hệ.
    /// Asset nằm ở Resources/CombatFeedback/CombatFeedbackConfig để CombatFeedbackManager tự nạp.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatFeedbackConfig", menuName = "Game/Combat/Combat Feedback Config")]
    public class CombatFeedbackSO : ScriptableObject
    {
        public const string ResourcePath = "CombatFeedback/CombatFeedbackConfig";

        [Serializable]
        public class ElementFeedback
        {
            public GameElement Element;
            public Color Color = Color.white;
            public GameObject ImpactPrefab;
            public GameObject CritImpactPrefab;
            public GameObject DeathPrefab;
            public GameObject SlashPrefab;
            public GameObject FinisherSlashPrefab;
            [Tooltip("VFX nổ dưới đất khi đòn kết của combo trúng mục tiêu")]
            public GameObject FinisherBurstPrefab;
            [Tooltip("Dash để lại chuỗi bóng mờ thay cho vệt sáng")]
            public bool UseAfterimageDash;
            [Tooltip("Màu bóng mờ. Alpha = 0 thì dùng Color")]
            public Color AfterimageColor = new Color(1f, 1f, 1f, 0f);
            [Tooltip("Key âm thanh khi trúng đòn hệ này")]
            public string HitSoundKey = "";
        }

        [Header("Âm thanh (key trong CombatSoundBank)")]
        public string DefaultHitSoundKey = "hit";
        public string CritSoundKey = "hit_crit";
        public string SwingSoundKey = "swing";
        public string FinisherSwingSoundKey = "swing_finisher";
        public string DeathSoundKey = "death";
        public string DashSoundKey = "dash";
        public string LandSoundKey = "land";

        [Header("Vệt sáng bám theo lưỡi vũ khí")]
        [Tooltip("Renderer có tên chứa một trong các từ này được coi là lưỡi vũ khí. Hero có lưỡi thì dùng vệt bám kiếm thay cho vệt chém cố định")]
        public List<string> WeaponKeywords = new List<string> { "Sword", "Blade", "Katana" };
        [Tooltip("Thời gian vệt còn lưu lại sau khi lưỡi đi qua (giây)")]
        public float WeaponTrailLifetime = 0.16f;
        [Tooltip("Tốc độ mũi kiếm (m/s) để vệt sáng tối đa. Lưỡi chậm hơn thì mờ hơn")]
        public float WeaponTrailFullSpeed = 4f;
        [Tooltip("Vệt phát trong bao lâu kể từ khi bắt đầu đòn đánh thường")]
        public float WeaponTrailAttackDuration = 1f;
        [Range(0f, 1f)] public float WeaponTrailTint = 0.45f;

        [Header("Đòn kết combo")]
        public float FinisherShake = 0.12f;
        public float FinisherBurstScale = 0.8f;

        [Header("Ultimate")]
        [Range(0.05f, 1f)] public float UltimateSlowMoScale = 0.2f;
        [Tooltip("Giây, thời gian thực")]
        public float UltimateSlowMoDuration = 0.35f;
        public Color ScreenFlashColor = new Color(1f, 1f, 1f, 0.75f);
        public float ScreenFlashDuration = 0.3f;

        [Header("Bóng mờ (afterimage)")]
        public Material GhostMaterial;
        public int DashAfterimageCount = 4;
        public float AfterimageLifetime = 0.35f;

        [Header("Trạng thái")]
        public Material RimMaterial;
        public float FootstepDustScale = 0.5f;

        [Header("Vệt chém cận chiến")]
        public GameObject DefaultSlashPrefab;
        public GameObject DefaultFinisherSlashPrefab;
        [Tooltip("Tỉ lệ của vệt chém theo tầm đánh của đòn (Range 2.6 x 0.4 ≈ 1.04)")]
        public float SlashScalePerRange = 0.4f;
        public float FinisherSlashScale = 1.2f;
        [Tooltip("Ngả vệt chém về phía camera (độ, âm là ngả lên) để không bị nhìn thấy cạnh mỏng")]
        public float SlashPitch = -20f;
        public float SlashForwardOffset = 0.3f;
        public float SlashLifetime = 0.6f;

        [Header("Hitstop (giây, thời gian thực)")]
        public float NormalHitStop = 0.045f;
        public float CritHitStop = 0.08f;
        public float KillHitStop = 0.13f;
        public float PlayerHurtHitStop = 0.035f;
        [Range(0f, 0.3f)] public float HitStopTimeScale = 0.03f;
        [Tooltip("Tổng thời gian tối đa của một lần hitstop, kể cả khi nhiều đòn cộng dồn.")]
        public float MaxHitStop = 0.16f;
        [Tooltip("Khoảng nghỉ sau mỗi lần hitstop để đòn đánh nhanh không bị giật liên tục.")]
        public float HitStopCooldown = 0.05f;

        [Header("Chớp sáng khi trúng đòn")]
        public Material FlashMaterial;
        public Color FlashColor = Color.white;
        public float FlashDuration = 0.07f;
        public float CritFlashDuration = 0.11f;
        public float PeriodicFlashDuration = 0.05f;

        [Header("Nảy model (squash)")]
        public float SquashAmount = 0.12f;
        public float CritSquashAmount = 0.2f;
        public float SquashDuration = 0.2f;

        [Header("Đẩy lùi (chỉ áp cho mục tiêu không do người chơi điều khiển)")]
        public float PushDistance = 0.35f;
        public float CritPushDistance = 0.7f;
        public float PushDuration = 0.12f;

        [Header("Rung camera")]
        public float PlayerHurtShake = 0.1f;
        public float KillShake = 0.2f;
        public float UltimateShake = 0.28f;

        [Header("VFX")]
        public float ImpactScale = 0.8f;
        public float ImpactLifetime = 1.2f;
        public float DeathLifetime = 2.5f;
        public GameObject DefaultImpactPrefab;
        public GameObject DefaultCritImpactPrefab;
        public GameObject DefaultDeathPrefab;
        public GameObject DashPuffPrefab;
        public GameObject LandDustPrefab;
        public float DustScale = 0.7f;
        public List<ElementFeedback> Elements = new List<ElementFeedback>();

        [Header("Vệt dash")]
        public Material TrailMaterial;
        public float TrailTime = 0.22f;
        public float TrailWidth = 0.7f;

        [Header("Vòng cảnh báo skill diện rộng")]
        public Material TelegraphMaterial;
        public Color EnemyTelegraphColor = new Color(1f, 0.22f, 0.15f, 1f);
        [Range(0f, 1f)] public float AllyTelegraphAlpha = 0.55f;

        public ElementFeedback Get(GameElement element)
        {
            foreach (ElementFeedback entry in Elements)
            {
                if (entry != null && entry.Element == element) return entry;
            }
            return null;
        }

        public Color GetAfterimageColor(GameElement element)
        {
            ElementFeedback entry = Get(element);
            if (entry != null && entry.AfterimageColor.a > 0f) return entry.AfterimageColor;
            return GetColor(element);
        }

        public Color GetColor(GameElement element)
        {
            ElementFeedback entry = Get(element);
            return entry != null ? entry.Color : DefaultColor(element);
        }

        public static Color DefaultColor(GameElement element)
        {
            switch (element)
            {
                case GameElement.Fire: return new Color(1f, 0.45f, 0.15f);
                case GameElement.Water: return new Color(0.25f, 0.6f, 1f);
                case GameElement.Wood: return new Color(0.35f, 0.9f, 0.35f);
                case GameElement.Earth: return new Color(0.9f, 0.68f, 0.3f);
                case GameElement.Metal: return new Color(0.88f, 0.9f, 1f);
                case GameElement.Light: return new Color(1f, 0.95f, 0.6f);
                case GameElement.Darkness: return new Color(0.6f, 0.3f, 0.9f);
                default: return Color.white;
            }
        }
    }
}
