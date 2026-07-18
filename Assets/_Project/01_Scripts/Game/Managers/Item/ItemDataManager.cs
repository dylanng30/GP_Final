using System.Collections.Generic;
using Dylanng.Core;
using Dylanng.Core.Base;
using Game.Data.Hero;
using Game.Data.Item;
using UnityEngine;

namespace Game.Managers.Item
{
    public class ItemDataManager : ManagerBase
    {
        [SerializeField] private ItemLocalDataSO localData;
        [SerializeField] private ItemRemoteData remoteData;
        
        // Cache
        private Dictionary<string, ItemAsset> _assetDic;
        private Dictionary<string, BaseItemData> _localItemDataDic;
        private Dictionary<string, BaseItemData> _remoteItemDataDic;
        
        public override void Initialize()
        {
            // Load item assets
            _assetDic = new Dictionary<string, ItemAsset>();
            foreach (var asset in localData.ItemAssets)
            {
                _assetDic[asset.ItemId] = asset;
            }
            
            // Load item data
            _localItemDataDic = new Dictionary<string, BaseItemData>();
            foreach (var equipmentItem in localData.LocalItemData.EquipmentItems)
            {
                _localItemDataDic[equipmentItem.ItemId] = equipmentItem;
            }

            foreach (var luckyBoxItem in localData.LocalItemData.LuckyBoxItems)
            {
                _localItemDataDic[luckyBoxItem.ItemId] = luckyBoxItem;
            }

            ServiceLocator.Register<ItemDataManager>(this);
        }

        #region ---Assets---
        public GameObject GetHeroModel(string heroId)
            => _assetDic.TryGetValue(heroId, out var asset) ? asset.ItemPrefab : null;
        
        public Sprite GetHeroSprite(string heroId)
            => _assetDic.TryGetValue(heroId, out var asset) ? asset.ItemSprite : null;
        #endregion

        #region ---Item Data---
        public BaseItemData GetItemData(string itemId)
        {
            if (_remoteItemDataDic != null && _remoteItemDataDic.TryGetValue(itemId, out var remoteItemData))
            {
                Debug.Log($"Get remote item data for {itemId}");
                return remoteItemData;
            }
            
            Debug.Log($"Get local item data for {itemId}");
            return _localItemDataDic.TryGetValue(itemId, out var localItemData) ? localItemData : null;
        }
        #endregion
    }
}