using Game.Data.Hero;
namespace Game.Data.Hero
{
    public enum GameElement 
    { 
        Physical = 0,
        Metal = 1, // Kim
        Water = 2, // Thủy
        Wood = 3,  // Mộc
        Fire = 4,  // Hỏa
        Earth = 5, // Thổ
        Light = 6,
        Darkness = 7
    }

    public enum HeroRole 
    { 
        Protector, Supporter, Striker, Mage, Assassin
    }

    public enum HeroRarity 
    { 
        Common, Uncommon, Rare, Epic, Legendary
    }
}