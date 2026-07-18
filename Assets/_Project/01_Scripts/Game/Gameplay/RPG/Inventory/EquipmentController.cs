using System;
using System.Collections.Generic;
using Dylanng.Core.Base;
using Dylanng.Gameplay.RPG.Stats;
using Game.Data.Item;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Inventory
{
    [RequireComponent(typeof(StatController))]
    public class EquipmentController : ComponentBase
    {
        public class EquippedItemInstance
        {
            public EquipmentItemData Data;
            public int Level;
            public List<StatModifier> AppliedModifiers = new List<StatModifier>();
        }

        private StatController _statController;
        private readonly Dictionary<EquipmentSlot, EquippedItemInstance> _equippedItems = new Dictionary<EquipmentSlot, EquippedItemInstance>();

        public event Action<EquipmentSlot, EquippedItemInstance> OnEquipmentChanged;

        protected override void Awake()
        {
            base.Awake();
            _statController = GetComponent<StatController>();
            if (_statController == null) _statController = gameObject.AddComponent<StatController>();
        }

        public bool EquipItem(EquipmentItemData itemData, int itemLevel)
        {
            if (itemData == null || itemData.EquipSlot == EquipmentSlot.None) return false;
            if (_statController == null) _statController = GetComponent<StatController>();

            // Remove existing item in this slot if present
            UnequipSlot(itemData.EquipSlot);

            var equippedInstance = new EquippedItemInstance
            {
                Data = itemData,
                Level = itemLevel
            };

            ApplyModifierIfPositive(equippedInstance, StatType.MaxHP, itemData.BonusHp, itemLevel);
            ApplyModifierIfPositive(equippedInstance, StatType.ATK, itemData.BonusAtk, itemLevel);
            ApplyModifierIfPositive(equippedInstance, StatType.DEF, itemData.BonusDef, itemLevel);
            ApplyModifierIfPositive(equippedInstance, StatType.Speed, itemData.BonusSpeed, itemLevel);
            ApplyModifierIfPositive(equippedInstance, StatType.CritRate, itemData.BonusCritRate, itemLevel);
            ApplyModifierIfPositive(equippedInstance, StatType.CritDamage, itemData.BonusCritDamage, itemLevel);

            _equippedItems[itemData.EquipSlot] = equippedInstance;
            OnEquipmentChanged?.Invoke(itemData.EquipSlot, equippedInstance);
            return true;
        }

        public void UnequipSlot(EquipmentSlot slot)
        {
            if (_equippedItems.TryGetValue(slot, out var existingItem))
            {
                if (_statController != null)
                {
                    _statController.RemoveAllModifiersFromSource(existingItem);
                }
                _equippedItems.Remove(slot);
                OnEquipmentChanged?.Invoke(slot, null);
            }
        }

        public EquippedItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            _equippedItems.TryGetValue(slot, out var item);
            return item;
        }

        public IReadOnlyDictionary<EquipmentSlot, EquippedItemInstance> GetAllEquippedItems() => _equippedItems;

        private void ApplyModifierIfPositive(EquippedItemInstance itemInstance, StatType statType, float baseBonus, int level)
        {
            if (baseBonus <= 0) return;

            float scaledBonus = EquipmentStatCalculator.GetScaledBonus(baseBonus, level);
            var mod = new StatModifier(scaledBonus, StatModType.Flat, (int)StatModType.Flat, itemInstance);
            
            itemInstance.AppliedModifiers.Add(mod);
            if (_statController != null)
            {
                _statController.AddModifier(statType, mod);
            }
        }
    }
}
