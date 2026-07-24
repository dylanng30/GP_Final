using System;
using System.Collections.Generic;
using Dylanng;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using GAME.Utilities;
using UnityEngine;

namespace Game.Managers.Hero
{
    public class HeroDataManager : ManagerBase
    {
        [SerializeField] private HeroLocalDataSO localData;
        [SerializeField] private HeroRemoteData remoteData;

        public HeroSaveData SaveData { get; private set; }

        // Cache
        private Dictionary<string, HeroAsset> _assetDic;
        private Dictionary<string, BaseHeroData> _localHeroDataDic;
        private Dictionary<string, BaseHeroData> _remoteHeroDataDic;
        
        public bool IsRemoteDataLoaded => remoteData != null && remoteData.HeroData != null && remoteData.HeroData.Count > 0;

        public override void Initialize()
        {
            _assetDic = new Dictionary<string, HeroAsset>();
            foreach (var asset in localData.HeroAssets)
            {
                _assetDic[asset.HeroId] = asset;
            }
            
            _localHeroDataDic = new Dictionary<string, BaseHeroData>();
            foreach (var baseHeroData in localData.HeroData)
            {
                _localHeroDataDic[baseHeroData.HeroId] = baseHeroData;
            }

            if (!IsRemoteDataLoaded)
            {
                GameLogger.LogError("[HeroDataManager] Remote hero data failed to load or is empty!");
                return;
            }

            var storageService = ServiceLocator.Get<IDataStorageService>();
            if (storageService == null)
            {
                storageService = new DataStorageService();
                storageService.Initialize();
                ServiceLocator.Register<IDataStorageService>(storageService);
            }

            _remoteHeroDataDic = new Dictionary<string, BaseHeroData>();
            foreach (var baseHeroData in remoteData.HeroData)
            {
                _remoteHeroDataDic[baseHeroData.HeroId] = baseHeroData;
            }
            
            // load local data (TEMP) => Remote
            LoadLocalData();
            
            ServiceLocator.Register<HeroDataManager>(this);
        }

        #region ---Assets---
        public HeroInstance GetHeroModel(string heroId)
            => _assetDic.TryGetValue(heroId, out var asset) ? asset.HeroPrefab : null;
        
        public Sprite GetHeroSprite(string heroId)
            => _assetDic.TryGetValue(heroId, out var asset) ? asset.HeroAvatarSprite : null;
        #endregion
        
        #region ---Hero Data---

        public BaseHeroData GetHeroData(string heroId)
        {
            if (_remoteHeroDataDic != null && _remoteHeroDataDic.TryGetValue(heroId, out var heroData))
            {
                Debug.Log($"Get remote hero data for {heroId}");
                return heroData;
            }
            
            Debug.Log($"Get local hero data for {heroId}");
            return _localHeroDataDic.TryGetValue(heroId, out var hero) ? hero : null;
        }
        #endregion

        private void LoadLocalData()
        {
            var storageService = ServiceLocator.Get<IDataStorageService>();
            
            if (storageService.HasKey(Constants.HERO_KEY, StorageType.PlayerPrefs))
            {
                SaveData = storageService.Load<HeroSaveData>(Constants.HERO_KEY, StorageType.PlayerPrefs);
            }
            else
            {
                SaveData = new HeroSaveData();
            }

            if (SaveData.Heros == null)
            {
                SaveData.Heros = new List<HeroDataInstance>();
            }

            bool hasChanged = false;
            if (remoteData != null && remoteData.HeroData != null)
            {
                // Clear the current heroes list during testing so that changing testPartySize is reflected immediately on restart
                SaveData.Heros.Clear();

                int testPartySize = 1; // <--- THAY ĐỔI SỐ LƯỢNG HERO MUỐN TEST TẠI ĐÂY (Ví dụ: 2, 3, 4)
                int addedCount = 0;

                foreach (var baseHero in remoteData.HeroData)
                {
                    /*
                    // =================================================================================
                    // --- BỎ COMMENT ĐOẠN NÀY VÀ XOÁ ĐOẠN TEST PHÍA DƯỚI ĐỂ QUAY VỀ LOGIC GAME THỰC TẾ (CHỈ ADD DEFAULT HERO) ---
                    if (!SaveData.Heros.Exists(x => x.HeroId == baseHero.HeroId))
                    {
                        bool isDefault = baseHero.HeroId == Constants.HERO_ID_DEFAULT;
                        HeroDataInstance heroInstance = new HeroDataInstance()
                        {
                            HeroId = baseHero.HeroId,
                            IsUnlocked = isDefault,
                            IsPartyMemeber = isDefault,
                            HeroLevel = 1,
                            HeroStarLevel = baseHero.HeroStarLevel
                        };
                        SaveData.Heros.Add(heroInstance);
                        hasChanged = true;
                    }
                    // =================================================================================
                    */

                    // --- LOGIC PHỤC VỤ TEST: Cho phép điền số lượng hero bất kỳ vào Party để kiểm tra đội hình ---
                    bool isMember = addedCount < testPartySize;
                    HeroDataInstance heroInstance = new HeroDataInstance()
                    {
                        HeroId = baseHero.HeroId,
                        IsUnlocked = true,
                        IsPartyMemeber = isMember,
                        HeroLevel = 1,
                        HeroStarLevel = baseHero.HeroStarLevel
                    };
                    SaveData.Heros.Add(heroInstance);
                    addedCount++;
                    hasChanged = true;
                }
            }

            if (hasChanged || !storageService.HasKey(Constants.HERO_KEY, StorageType.PlayerPrefs))
            {
                SaveLocalData();
            }
        }

        private void SaveLocalData()
        {
            if (SaveData != null)
            {
                var storageService = ServiceLocator.Get<IDataStorageService>();
                storageService.Save(Constants.HERO_KEY, SaveData, StorageType.PlayerPrefs);
            }
        }

        protected override void OnDestroy()
        {
            SaveLocalData();
            
            base.OnDestroy();
        }
    }
    
    [Serializable]
    public class HeroRemoteData
    {
        public List<BaseHeroData> HeroData;
    }
}