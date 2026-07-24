using System;
using System.Collections.Generic;

namespace Game.Data.Item
{
    [Serializable]
    public class ItemSaveData
    {
        public List<ItemDataInstance> Items;
    }

    [Serializable]
    public class ItemDataInstance
    {
        public string ItemId;
        public int ItemLevel;
    }
}