using System;
using System.Collections.Generic;
using System.Linq;
using Dylanng;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using Game.Utilities;
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

        /// <summary>Trả về toàn bộ HeroAsset (bao gồm AnimationConfig) theo heroId.</summary>
        public HeroAsset GetHeroAsset(string heroId)
            => _assetDic.TryGetValue(heroId, out var asset) ? asset : null;

        /// <summary>Id các hero có model để đưa vào trận, sắp theo id.</summary>
        public IReadOnlyList<string> GetPlayableHeroIds()
            => _assetDic.Values.Where(asset => asset.HeroPrefab != null).Select(asset => asset.HeroId).OrderBy(id => id).ToList();
        #endregion
        
        #region ---Hero Data---

        public BaseHeroData GetHeroData(string heroId)
        {
            if (_remoteHeroDataDic != null && _remoteHeroDataDic.TryGetValue(heroId, out var heroData))
            {
                return heroData;
            }
            
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

                // <--- ĐỘI HÌNH TEST: hero trong danh sách này vào đội (000000 Fire, 000001 Wood, 000002 Earth, 000003 Mental, 000004 Water).
                // Thứ tự slot theo thứ tự trong dữ liệu hero, slot đầu là hero được điều khiển khi vào game.
                string[] testPartyHeroIds = { "hero_000000", "hero_000003", "hero_000004" };

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
                    bool isMember = System.Array.IndexOf(testPartyHeroIds, baseHero.HeroId) >= 0;
                    HeroDataInstance heroInstance = new HeroDataInstance()
                    {
                        HeroId = baseHero.HeroId,
                        IsUnlocked = true,
                        IsPartyMemeber = isMember,
                        HeroLevel = 1,
                        HeroStarLevel = baseHero.HeroStarLevel
                    };
                    SaveData.Heros.Add(heroInstance);
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
            var storageService = ServiceLocator.Get<IDataStorageService>();
            if (SaveData != null && storageService != null)
            {
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