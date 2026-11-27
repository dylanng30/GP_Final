using System.Collections.Generic;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Domain
{
    /// <summary>Định nghĩa một trạng thái. Thêm trạng thái mới chỉ cần tạo asset.</summary>
    [CreateAssetMenu(fileName = "NewStatus", menuName = "Dylanng/RPG/Combat/Status")]
    public class StatusDefinitionSO : ScriptableObject
    {
        [Header("--- INFO ---")]
        public string DisplayName;
        public Sprite Icon;
        public GameElement Element;

        [Header("--- DURATION ---")]
        public float Duration = 3f;
        public StatusStackPolicy StackPolicy = StatusStackPolicy.Refresh;
        [Min(1)] public int MaxStacks = 1;

        [Header("--- EFFECTS ---")]
        public ControlFlags ControlFlags;
        public List<StatBuff> StatBuffs = new List<StatBuff>();

        [Tooltip("Hệ số sát thương khi người mang trạng thái gây sát thương theo hệ chỉ định")]
        public List<ElementDamageModifier> OutgoingDamageModifiers = new List<ElementDamageModifier>();

        [Header("--- DAMAGE OVER TIME ---")]
        [Tooltip("0 nghĩa là không gây sát thương theo thời gian")]
        public float TickInterval;
        public float TickDamage;
        [Tooltip("Tỉ lệ ATK của người gây trạng thái cộng vào sát thương mỗi nhịp")]
        public float TickAttackRatio;

        [Header("--- PRESENTATION ---")]
        [Tooltip("Hệ số phóng to người mang trạng thái, ví dụ dạng khổng lồ")]
        public float ScaleMultiplier = 1f;
        public GameObject LoopVfxPrefab;
        public Vector3 VfxOffset = Vector3.up;
        [Tooltip("Hệ số kích thước của VFX lặp, ví dụ dạng khổng lồ cần aura to hơn")]
        public float LoopVfxScale = 1f;
        [Tooltip("Thời gian phóng to/thu nhỏ theo ScaleMultiplier. 0 là đổi ngay")]
        public float ScaleTweenDuration = 0.35f;

        [Tooltip("VFX một lần khi trạng thái bắt đầu, đặt dưới chân")]
        public GameObject ApplyVfxPrefab;
        public float ApplyVfxScale = 1f;
        [Tooltip("VFX một lần khi trạng thái kết thúc")]
        public GameObject RemoveVfxPrefab;
        public float RemoveVfxScale = 1f;
        public string ApplySoundKey = "";
        public string RemoveSoundKey = "";

        [Tooltip("Viền phát sáng trên người mang trạng thái. Alpha = 0 là tắt")]
        public Color RimColor = new Color(1f, 0.8f, 0.3f, 0f);
        [Tooltip("Rung camera mỗi bước chân khi hero đang điều khiển mang trạng thái. 0 là tắt")]
        public float FootstepShake = 0f;
        public float FootstepInterval = 0.38f;

        public bool IsControl => ControlFlags != ControlFlags.None;
    }
}
