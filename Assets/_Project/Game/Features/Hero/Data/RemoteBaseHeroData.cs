using System;

namespace Game.Data.Hero
{
    [Serializable]
    public class RemoteBaseHeroData
    {
        public string HeroId;
        
        public float BaseHp = 1000f;
        public float BaseAtk = 100f;
        public float BaseDef = 50f;
        public float BaseSpeed = 100f;
        public float BaseCritRate = 0.05f;
        public float BaseCritDamage = 1.5f;
        public float MaxEnergy = 100f;
        
        public float HpGrowthPerLevel = 50f;
        public float AtkGrowthPerLevel = 10f;
        public float DefGrowthPerLevel = 5f;
        public float StarUpMultiplier = 1.15f;
    }
}
