using Dylanng;
using UnityEngine;

namespace Game.Inventory
{
    public abstract class ItemSO : ScriptableData
    {
        public string ID;
        public string ItemName;
        public Sprite Icon;
        [TextArea] public string Description;
        public int MaxStack = 99;

        public abstract void Use(GameObject user);
    }
}
