using System;
using System.Collections.Generic;
using Dylanng.Core;
using Dylanng.Core.Base;
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
            if (PlayerPrefs.HasKey(Constants.HERO_KEY))
            {
                string json = PlayerPrefs.GetString(Constants.HERO_KEY);
                SaveData = JsonUtility.FromJson<HeroSaveData>(json);
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
                foreach (var baseHero in remoteData.HeroData)
                {
                    /*if (!SaveData.Heros.Exists(x => x.HeroId == baseHero.HeroId))
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
                    }*/
                    
                    HeroDataInstance heroInstance = new HeroDataInstance()
                    {
                        HeroId = baseHero.HeroId,
                        IsUnlocked = true,
                        IsPartyMemeber = true,
                        HeroLevel = 1,
                        HeroStarLevel = baseHero.HeroStarLevel
                    };
                    SaveData.Heros.Add(heroInstance);
                    hasChanged = true;
                }
            }

            if (hasChanged || !PlayerPrefs.HasKey(Constants.HERO_KEY))
            {
                SaveLocalData();
            }
        }

        private void SaveLocalData()
        {
            if (SaveData != null)
            {
                string json = JsonUtility.ToJson(SaveData);  
                PlayerPrefs.SetString(Constants.HERO_KEY, json);
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