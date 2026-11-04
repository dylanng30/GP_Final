using Dylanng;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    [CreateAssetMenu(fileName = "NewConsumable", menuName = "RPG/Inventory/Consumable Item")]
    public class ConsumableItemSO : ItemSO
    {
        // For simplicity in Unity Inspector without extra plugins, 
        // we can use a concrete list or SerializeReference if the user has 2019.3+
        // Here we'll implement a simple effect directly or via a list of scriptable effects.
        
        public List<ScriptableEffect> Effects;

        public override void Use(GameObject user)
        {
            if (Effects == null) return;
            foreach (var effect in Effects)
            {
                effect.ExecuteEffect(user);
            }
            Debug.Log($"Used consumable: {ItemName}");
        }
    }

    public abstract class ScriptableEffect : ScriptableObject, IItemEffect
    {
        public abstract void ExecuteEffect(GameObject target);
    }
}
