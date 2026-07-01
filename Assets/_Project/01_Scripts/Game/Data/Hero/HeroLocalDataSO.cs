using System;
using System.Collections.Generic;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Data.Hero
{
    [CreateAssetMenu(fileName = "HeroLocalDataSO",menuName = "SO/HeroLocalDataSO")]
    public class HeroLocalDataSO : ScriptableObject
    {
        [field: SerializeField] public List<HeroAsset> HeroAssets { get; private set; }
        [field: SerializeField] public List<BaseHeroData> HeroData { get; private set; }
    }

    [Serializable]
    public class HeroAsset
    {
        public string HeroId;
        public HeroInstance HeroPrefab;
        public Sprite HeroAvatarSprite;
    }
    
    [Serializable]
    public class BaseHeroData
    {
        [Header("---GENERAL INFORMATION---")]
        public string HeroId;
        public string HeroName;
        public int HeroStarLevel;
        public HeroRarity BaseRarity;
        public ElementalFaction Faction;
        public HeroRole Role;
        
        [Space(10)]
        [Header("---BASE COMBAT STATS")]
        public float BaseHp = 1000f;
        public float BaseAtk = 100f;
        public float BaseDef = 50f;
        public float BaseSpeed = 100f;
        public float BaseCritRate = 0.05f;
        public float BaseCritDamage = 1.5f;
        public float MaxEnergy = 100f;
        
        [Header("---GROWTH MODIFIERS---")]
        public float HpGrowthPerLevel = 50f;
        public float AtkGrowthPerLevel = 10f;
        public float DefGrowthPerLevel = 5f;
        public float StarUpMultiplier = 1.15f;
    }
    
    
}

