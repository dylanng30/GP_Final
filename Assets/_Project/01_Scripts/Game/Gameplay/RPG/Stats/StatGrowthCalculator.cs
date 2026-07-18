using UnityEngine;
using Game.Data.Hero;
using Game.Data.Monster;

namespace Dylanng.Gameplay.RPG.Stats
{
    public static class StatGrowthCalculator
    {
        public static float CalculateStat(float baseValue, float growthPerLevel, int level, int starLevel, float starMultiplier)
        {
            float levelScaled = baseValue + (growthPerLevel * Mathf.Max(0, level - 1));
            float starScaled = levelScaled * Mathf.Pow(starMultiplier, Mathf.Max(0, starLevel - 1));
            return (float)System.Math.Round(starScaled, 2);
        }

        public static void SetupHeroStats(StatController controller, RemoteBaseHeroData heroData, int level, int starLevel)
        {
            if (controller == null || heroData == null) return;

            float maxHp = CalculateStat(heroData.BaseHp, heroData.HpGrowthPerLevel, level, starLevel, heroData.StarUpMultiplier);
            float atk = CalculateStat(heroData.BaseAtk, heroData.AtkGrowthPerLevel, level, starLevel, heroData.StarUpMultiplier);
            float def = CalculateStat(heroData.BaseDef, heroData.DefGrowthPerLevel, level, starLevel, heroData.StarUpMultiplier);
            float speed = heroData.BaseSpeed;
            float critRate = heroData.BaseCritRate;
            float critDamage = heroData.BaseCritDamage;
            float maxEnergy = heroData.MaxEnergy;

            controller.InitializeStat(StatType.MaxHP, maxHp);
            controller.InitializeStat(StatType.ATK, atk);
            controller.InitializeStat(StatType.DEF, def);
            controller.InitializeStat(StatType.Speed, speed);
            controller.InitializeStat(StatType.CritRate, critRate);
            controller.InitializeStat(StatType.CritDamage, critDamage);
            controller.InitializeStat(StatType.MaxEnergy, maxEnergy);
        }

        public static void SetupMonsterStats(StatController controller, BaseMonsterData monsterData, int level)
        {
            if (controller == null || monsterData == null) return;

            float maxHp = CalculateStat(monsterData.BaseHp, monsterData.HpGrowthPerLevel, level, 1, 1f);
            float atk = CalculateStat(monsterData.BaseAtk, monsterData.AtkGrowthPerLevel, level, 1, 1f);
            float def = CalculateStat(monsterData.BaseDef, monsterData.DefGrowthPerLevel, level, 1, 1f);
            float speed = monsterData.BaseSpeed;
            float critRate = monsterData.BaseCritRate;
            float critDamage = monsterData.BaseCritDamage;

            controller.InitializeStat(StatType.MaxHP, maxHp);
            controller.InitializeStat(StatType.ATK, atk);
            controller.InitializeStat(StatType.DEF, def);
            controller.InitializeStat(StatType.Speed, speed);
            controller.InitializeStat(StatType.CritRate, critRate);
            controller.InitializeStat(StatType.CritDamage, critDamage);
        }
    }
}
