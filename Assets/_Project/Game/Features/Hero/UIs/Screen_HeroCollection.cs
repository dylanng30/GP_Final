using System;
using System.Collections.Generic;
using Dylanng;
using Game.Data.Hero;
using Game.Managers.Hero;
using Game.Managers.Party;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Screens.Hero
{
    public class Screen_HeroCollection : UIScreen
    {
        [SerializeField] private List<HeroOverviewSlotView> combatOverviewSlots;
        [SerializeField] private List<HeroOverviewSlotView> ownedOverviewSlots;
        [SerializeField] private GridLayoutGroup ownedGridLayoutGroup;
        [SerializeField] private HorizontalLayoutGroup combatHorizontalLayoutGroup;
        [SerializeField] private Button changeHeroesBtn, confirmBtn;
        [SerializeField] private GameObject backdrop;

        private HeroCollectionMode _currentMode;
        private string firstSelectedHeroId, secondSelectedHeroId;
        
        private HeroDataManager _heroDataManager;
        private HeroSaveData _heroSaveData;

        private Dictionary<string, HeroOverviewSlotView> _heroSlotDic;
        
        public override void Initialize()
        {
            base.Initialize();
            gameObject.AddComponent<Game.UIs.Party.SynergyPreviewLabel>();
            
            SwitchMode(HeroCollectionMode.Normal);
            
            changeHeroesBtn.onClick.AddListener(() => SwitchMode(HeroCollectionMode.ChangeHeroes));
            confirmBtn.onClick.AddListener(() => SwitchMode(HeroCollectionMode.Normal));
            
            RefreshCollection();
        }

        public override void Show()
        {
            base.Show();
            RefreshCollection();
        }

        public void RefreshCollection()
        {
            _heroDataManager = _heroDataManager ?? ServiceLocator.Get<HeroDataManager>();
            _heroSaveData = _heroSaveData ?? (_heroDataManager != null ? _heroDataManager.SaveData : null);

            if (_heroSaveData == null || _heroSaveData.Heros == null) return;

            _heroSlotDic = new Dictionary<string, HeroOverviewSlotView>();

            if (combatOverviewSlots != null)
            {
                foreach (var slot in combatOverviewSlots)
                {
                    if (slot != null) slot.SetHero(string.Empty, null, null, null, null);
                }
            }

            if (ownedOverviewSlots != null)
            {
                foreach (var slot in ownedOverviewSlots)
                {
                    if (slot != null) slot.SetHero(string.Empty, null, null, null, null);
                }
            }

            int combatIndex = -1;
            int ownedIndex = -1;

            for (int i = 0; i < _heroSaveData.Heros.Count; i++)
            {
                var hero = _heroSaveData.Heros[i];
                var heroId = hero.HeroId;
                Sprite sprite = _heroDataManager != null ? _heroDataManager.GetHeroSprite(heroId) : null;

                if (hero.IsPartyMemeber)
                {
                    combatIndex++;
                    if (combatOverviewSlots != null && combatIndex < combatOverviewSlots.Count && combatOverviewSlots[combatIndex] != null)
                    {
                        combatOverviewSlots[combatIndex].SetHero(heroId, sprite, null, null, null);
                        combatOverviewSlots[combatIndex].OnHeroButtonClicked -= SelectHero;
                        combatOverviewSlots[combatIndex].OnHeroButtonClicked += SelectHero;
                        _heroSlotDic[heroId] = combatOverviewSlots[combatIndex];
                    }
                }
                else
                {
                    ownedIndex++;
                    if (ownedOverviewSlots != null && ownedIndex < ownedOverviewSlots.Count && ownedOverviewSlots[ownedIndex] != null)
                    {
                        ownedOverviewSlots[ownedIndex].SetHero(heroId, sprite, null, null, null);
                        ownedOverviewSlots[ownedIndex].OnHeroButtonClicked -= SelectHero;
                        ownedOverviewSlots[ownedIndex].OnHeroButtonClicked += SelectHero;
                        _heroSlotDic[heroId] = ownedOverviewSlots[ownedIndex];
                    }
                }
            }
        }

        private void SelectHero(string heroId)
        {
            switch (_currentMode)
            {
                case HeroCollectionMode.Normal:
                    ShowHeroInformation(heroId);
                    break;
                case HeroCollectionMode.ChangeHeroes:
                    HandleHeroSwappingLogic(heroId);
                    break;
            }
        }

        private void ShowHeroInformation(string heroId)
        {
            //
        }
        
        private void HandleHeroSwappingLogic(string heroId)
        {
            if (string.IsNullOrEmpty(firstSelectedHeroId))
            {
                firstSelectedHeroId = heroId;
                var firstSlot = _heroSlotDic[firstSelectedHeroId];
                firstSlot.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            }
            else if (firstSelectedHeroId == heroId)
            {
                var firstSlot = _heroSlotDic[firstSelectedHeroId];
                firstSlot.transform.localScale = Vector3.one;
                firstSelectedHeroId = string.Empty;
            }
            else if (string.IsNullOrEmpty(secondSelectedHeroId))
            {
                secondSelectedHeroId = heroId;
                var firstSlot = _heroSlotDic[firstSelectedHeroId];
                var secondSlot = _heroSlotDic[secondSelectedHeroId];

                SwapHeroOverviewSlot(firstSlot, secondSlot);
                
                firstSlot.transform.localScale = Vector3.one;
                firstSelectedHeroId = string.Empty;
                secondSelectedHeroId = string.Empty;
            }
        }

        private void SwapHeroOverviewSlot(HeroOverviewSlotView firstSlot, HeroOverviewSlotView secondSlot)
        {
            combatHorizontalLayoutGroup.enabled = false;
            ownedGridLayoutGroup.enabled = false;
            
            int firstIndex = firstSlot.transform.GetSiblingIndex();
            int secondIndex = secondSlot.transform.GetSiblingIndex();
            
            Vector2 firstPosition = firstSlot.Rect.anchoredPosition;
            Vector2 secondPosition = secondSlot.Rect.anchoredPosition;
            
            Transform firstParent = firstSlot.transform.parent;
            Transform secondParent = secondSlot.transform.parent;
            
            firstSlot.transform.SetParent(secondParent);
            secondSlot.transform.SetParent(firstParent);
            
            firstSlot.transform.SetSiblingIndex(secondIndex);
            secondSlot.transform.SetSiblingIndex(firstIndex);
            
            firstSlot.Rect.anchoredPosition = secondPosition;
            secondSlot.Rect.anchoredPosition = firstPosition;
            
            combatHorizontalLayoutGroup.enabled = true;
            ownedGridLayoutGroup.enabled = true;
        }

        private void SwitchMode(HeroCollectionMode mode)
        {
            _currentMode = mode;
            backdrop.SetActive(mode == HeroCollectionMode.ChangeHeroes);
            changeHeroesBtn.gameObject.SetActive(mode == HeroCollectionMode.Normal);
            
            if (!string.IsNullOrEmpty(firstSelectedHeroId) && _heroSlotDic.ContainsKey(firstSelectedHeroId))
            {
                _heroSlotDic[firstSelectedHeroId].transform.localScale = Vector3.one;
            }
            
            firstSelectedHeroId = String.Empty;
            secondSelectedHeroId = String.Empty;
        }
        protected override void OnDestroy()
        {
            changeHeroesBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.RemoveAllListeners();
            
            
            
            base.OnDestroy();
        }
    }
    
    public enum HeroCollectionMode
    {
        Normal, ChangeHeroes
    }
}

