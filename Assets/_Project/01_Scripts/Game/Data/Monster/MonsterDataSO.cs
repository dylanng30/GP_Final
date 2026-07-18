using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Monster
{
    [Serializable]
    public class BaseMonsterData
    {
        [Header("---GENERAL INFORMATION---")]
        public string MonsterId;
        public string MonsterName;
        public int BaseLevel = 1;
        
        [Space(10)]
        [Header("---BASE COMBAT STATS---")]
        public float BaseHp = 500f;
        public float BaseAtk = 50f;
        public float BaseDef = 20f;
        public float BaseSpeed = 90f;
        public float BaseCritRate = 0.05f;
        public float BaseCritDamage = 1.5f;
        
        [Header("---GROWTH MODIFIERS---")]
        public float HpGrowthPerLevel = 60f;
        public float AtkGrowthPerLevel = 12f;
        public float DefGrowthPerLevel = 6f;
    }

    [CreateAssetMenu(fileName = "MonsterDataSO", menuName = "SO/MonsterDataSO")]
    public class MonsterDataSO : ScriptableObject
    {
        public List<BaseMonsterData> MonsterData;
    }
}
