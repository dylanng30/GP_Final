using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAME.Data.Gacha
{
    [Serializable]
    public class GachaLocalData
    {
        [Header("--- ITEM ASSETS ---")]
        public List<WeaponLocalData> Weapons;
        public List<ArmorLocalData> Armors;
        public List<AccessoryLocalData> Accessories;
        public List<ConsumableLocalData> Consumables;
        public List<MagicSkillLocalData> MagicAndSkills;
        public List<CharacterLocalData> Characters;
        public List<CosmeticLocalData> Cosmetics;
        public List<MountPetLocalData> MountsAndPets;
        public List<FurnitureLocalData> Furnitures;
        public List<MaterialUpgradeLocalData> MaterialsAndUpgrades;
        public List<MysteryItemLocalData> MysteryItems;

        [Header("--- SHOP ASSETS ---")]
        public List<ShopThemeLocalData> ShopThemes; 
    }

    #region BASE CLASSES
    [Serializable]
    public abstract class BaseItemLocalData
    {
        public string Id;
        public Sprite Icon;
        public string LocalizedNameKey;
        public string LocalizedDescKey;
    }

    [Serializable]
    public class ModelItemLocalData : BaseItemLocalData
    {
        public GameObject Prefab;
    }
    #endregion

    #region ITEM DATA CLASSES
    [Serializable] 
    public class WeaponLocalData : ModelItemLocalData 
    { 
        public ParticleSystem AttackEffect; 
        public AudioClip AttackSound; 
    }

    [Serializable]
    public class ArmorLocalData : ModelItemLocalData
    {
        
    }

    [Serializable]
    public class AccessoryLocalData : ModelItemLocalData
    {
        
    }

    [Serializable]
    public class ConsumableLocalData : BaseItemLocalData
    {
        
    } 
    
    [Serializable] 
    public class MagicSkillLocalData : BaseItemLocalData 
    { 
        public AnimationClip CastAnimation; 
        public GameObject SpellVFXPrefab; 
    }

    [Serializable] 
    public class CharacterLocalData : ModelItemLocalData 
    { 
        public Sprite GachaSplashArt;
        public AudioClip VoiceLine; 
        public Sprite AvatarIcon;
    }

    [Serializable] 
    public class CosmeticLocalData : ModelItemLocalData 
    { 
        public AnimationClip CustomAnimation; 
        public Sprite BannerArtwork; 
    }

    [Serializable]
    public class MountPetLocalData : ModelItemLocalData
    {
        
    }

    [Serializable]
    public class FurnitureLocalData : ModelItemLocalData
    {
        
    }
    
    [Serializable] public class MaterialUpgradeLocalData : BaseItemLocalData { }
    
    [Serializable] 
    public class MysteryItemLocalData : BaseItemLocalData 
    {
        public GameObject LootboxPrefab;
    }
    #endregion

    #region SHOP UI DATA
    [Serializable]
    public class ShopThemeLocalData
    {
        public string ShopId;
        public Sprite BackgroundImage, DeactiveButtonImage, ActiveButtonImage;
        public AudioClip BGM;
        
        [Tooltip("Loại UI: Grid, Carousel, Tab, InfiniteScroll")]
        public string UiLayoutType; 
    }
    #endregion
}