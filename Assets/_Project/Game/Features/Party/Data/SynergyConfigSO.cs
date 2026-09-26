using System.Collections.Generic;
using UnityEngine;
using Game.Data.Hero;
using Dylanng.Gameplay.RPG.Stats;

namespace Game.Managers.Party
{
    [CreateAssetMenu(fileName = "SynergyConfig", menuName = "SO/Party/SynergyConfig")]
    public class SynergyConfigSO : ScriptableObject
    {
        [Header("Điều kiện kích hoạt")]
        public GameElement RequiredElement;
        public int RequiredCount; // Ví dụ: Cần 3 tướng cùng hệ

        [Header("Chỉ số được buff")]
        public List<SynergyBuff> Buffs;
    }

    [System.Serializable]
    public class SynergyBuff
    {
        public StatType StatType;
        public float Value;
        public StatModType ModifierType;
    }
}
