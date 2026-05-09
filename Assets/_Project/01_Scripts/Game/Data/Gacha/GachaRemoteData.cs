using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAME.Data.Gacha
{
    [Serializable]
    public class GachaRemoteData
    {
        [Header("--- ITEM STATS ---")]
        public List<WeaponRemoteData> Weapons;
        public List<ArmorRemoteData> Armors;
        public List<AccessoryRemoteData> Accessories;
        public List<ConsumableRemoteData> Consumables;
        public List<MagicSkillRemoteData> MagicAndSkills;
        public List<CharacterRemoteData> Characters;
        public List<CosmeticRemoteData> Cosmetics;
        public List<MountPetRemoteData> MountsAndPets;
        public List<FurnitureRemoteData> Furnitures;
        public List<MaterialUpgradeRemoteData> MaterialsAndUpgrades;

        [Header("--- ACTIVE SHOPS ---")]
        public List<ShopInstanceRemoteData> ActiveShops;
    }

    #region BASE CLASSES
    [Serializable]
    public abstract class BaseItemRemoteData
    {
        public string Id;
        public int Rarity;
        public int BasePrice;
        public string DefaultCurrency;
    }
    #endregion

    #region ITEM STATS CLASSES
    [Serializable] 
    public class WeaponRemoteData : BaseItemRemoteData 
    { 
        public float BaseDamage; 
        public float FireRate; 
        public int AmmoCapacity;
    }

    [Serializable] 
    public class ArmorRemoteData : BaseItemRemoteData 
    { 
        public float Defense; 
        public float HPBonus; 
    }

    [Serializable] 
    public class AccessoryRemoteData : BaseItemRemoteData 
    { 
        public float StatMultiplier; 
        public string BuffType; 
    }

    [Serializable] 
    public class ConsumableRemoteData : BaseItemRemoteData 
    { 
        public float HealAmount; 
        public float BuffDuration; 
        public bool IsReviveItem;
    }

    [Serializable] 
    public class MagicSkillRemoteData : BaseItemRemoteData 
    { 
        public float ManaCost; 
        public float Cooldown; 
        public int TalentPointsRequired;
    }

    [Serializable] 
    public class CharacterRemoteData : BaseItemRemoteData 
    { 
        public float BaseHP; 
        public float BaseATK; 
        public string Faction;
    }

    [Serializable] 
    public class CosmeticRemoteData : BaseItemRemoteData 
    { 
        public bool IsLimitedEdition; 
    }

    [Serializable] 
    public class MountPetRemoteData : BaseItemRemoteData 
    { 
        public float MoveSpeedBonus; 
        public float CarryCapacity; 
    }

    [Serializable] 
    public class FurnitureRemoteData : BaseItemRemoteData 
    { 
        public int ComfortScore; 
        public int BuildingCost;
    }

    [Serializable] 
    public class MaterialUpgradeRemoteData : BaseItemRemoteData 
    { 
        public float SuccessRate;
    }
    #endregion

    #region SHOP MECHANICS & INSTANCE CLASSES
    [Serializable]
    public class ShopInstanceRemoteData
    {
        public string ShopId; 
        public string ShopCategory;
        
        public bool IsOpen;
        public long StartTimeEpoch;
        public long EndTimeEpoch;
        public long NextRefreshTimeEpoch;

        public ShopUnlockCondition UnlockCondition;
        public ShopDynamicEconomy DynamicEconomy;
        
        public List<ShopItemAvailability> Inventory;
    }

    [Serializable]
    public class ShopItemAvailability
    {
        public string ItemId;
        public int Price; 
        public string CurrencyType;
        
        public int StockRemaining;
        public int MaxPurchasesPerPlayer;
        
        public int DiscountPercentage;
        public float DropRate;
        public int PityCounterRequired;
    }

    [Serializable]
    public class ShopUnlockCondition
    {
        public int RequiredLevel;
        public int RequiredFame;
        public string RequiredFaction;
        public int RequiredKarma;
        public bool RequiresStealth;
    }

    [Serializable]
    public class ShopDynamicEconomy
    {
        public bool IsEnabled;
        public float CurrentSupplyDemandMultiplier;
        public int BonusPriceBasedOnQuantity;
    }
    #endregion
}