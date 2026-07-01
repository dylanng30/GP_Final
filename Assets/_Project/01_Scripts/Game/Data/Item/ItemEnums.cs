using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Item
{
    public enum ItemType
    {
        Material, Consumable, Equipment, Quest, Currency, LuckyBox
    }

    public enum ItemRarity
    {
        Common, Uncommon, Rare, Epic, Legendary, Mythic
    }

    public enum EquipmentSlot
    {
        None, Weapon, Helmet, Armor, Boots, Accessory
    }
}

