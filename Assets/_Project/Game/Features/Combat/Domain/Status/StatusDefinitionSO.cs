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

        public bool IsControl => ControlFlags != ControlFlags.None;
    }
}
