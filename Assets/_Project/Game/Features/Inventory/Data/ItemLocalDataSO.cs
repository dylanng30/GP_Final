using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Item
{
    [CreateAssetMenu(fileName = "ItemLocalDataSO",menuName = "SO/ItemLocalDataSO")]
    public class ItemLocalDataSO : ScriptableObject
    {
        [field: SerializeField] public List<ItemAsset> ItemAssets { get; private set; }
        [field: SerializeField] public LocalItemData LocalItemData { get; private set; }
    }

    [Serializable]
    public class ItemAsset
    {
        public string ItemId;
        public GameObject ItemPrefab;
        public Sprite ItemSprite;
    }

    [Serializable]
    public class LocalItemData
    {
        [field: SerializeField] public List<EquipmentItemData> EquipmentItems { get; private set; }
        [field: SerializeField] public List<LuckyBoxItemData> LuckyBoxItems { get; private set; }
    }

    [Serializable]
    public abstract class BaseItemData
    {
        [Header("---GENERAL INFORMATION---")]
        public string ItemId;
        public string ItemName;
        public string Description;
        public ItemType Type;
        public ItemRarity Rarity;
        
        [Space(10)]
        [Header("---INVENTORY & ECONOMY---")]
        public bool IsStackable = true;
        public int MaxStackCount = 99;
        public int BuyPrice = 0;
        public int SellPrice = 0;
        public bool IsDroppable = true;
    }
    
    [Serializable]
    public class EquipmentItemData : BaseItemData
    {
        [Header("---EQUIPMENT STATS---")]
        public int MaxLevel = 80;
        public EquipmentSlot EquipSlot = EquipmentSlot.None;
        public float BonusHp = 0f;
        public float BonusAtk = 0f;
        public float BonusDef = 0f;
        public float BonusSpeed = 0f;
        public float BonusCritRate = 0f;
        public float BonusCritDamage = 0f;
    }

    [Serializable]
    public class LuckyBoxItemData : BaseItemData
    {
        [Space(10)]
        [Header("---GIFT POOL---")]
        public List<string> GiftIds;
    }
}

