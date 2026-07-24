using System;
using System.Collections.Generic;
using Game.Data.Item;

namespace Game.Data.Hero
{
    [Serializable]
    public class HeroSaveData
    {
        public List<HeroDataInstance> Heros;
    }

    [Serializable]
    public class HeroDataInstance
    {
        public string HeroId;
        public bool IsUnlocked;
        public int HeroLevel;
        public int HeroStarLevel;
        public bool IsPartyMemeber;
        
        public List<ItemDataInstance> HeroItems;
    }
}