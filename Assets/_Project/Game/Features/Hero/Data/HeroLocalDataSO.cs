using System;
using System.Collections.Generic;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Data.Hero
{
    [CreateAssetMenu(fileName = "HeroLocalDataSO", menuName = "SO/HeroLocalDataSO")]
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
        [Tooltip("Config animation riêng cho hero này (tên Trigger, parameter Speed, v.v.)")]
        public Game.Animation.Hero.HeroAnimationConfig AnimationConfig;
    }

    [Serializable]
    public class BaseHeroData
    {
        [Header("---GENERAL INFORMATION---")]
        public string HeroId;
        public string HeroName;
        public int HeroStarLevel;
        public HeroRarity BaseRarity;
        public GameElement Faction;
        public HeroRole Role;
    }
}
