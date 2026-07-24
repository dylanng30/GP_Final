using System;
using System.Collections;
using Dylanng;
using GAME.Data.Gacha;
using Game.UIs.Screens.Explore;
using GAME.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Game.UIs.Popups.Shop
{
    public class Screen_GachaShop : UIScreen
    {
        [SerializeField] private Button closeBtn;
        [SerializeField] private Button prevShopBtn, nextShopBtn;
        [SerializeField] private RollShopButton singleRollBtn, multiRollBtn;
        
        [SerializeField] private GachaShopItem[] gachaShopItems;

        private PauseSystem _pauseSystem;
        
        private int _lastShopIndex, _currentShopIndex;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();
            
            // Load remote data
            StartCoroutine(LoadDataCoroutine());
            //
            
            _lastShopIndex = -1;
            _currentShopIndex = 0;
            
            if (closeBtn) closeBtn.onClick.AddListener(OnCloseButtonClicked);
            
            //Set up listener for previous shop button & next shop button
            prevShopBtn.onClick.AddListener(() => SelectShopByDirection(-1));
            nextShopBtn.onClick.AddListener(() => SelectShopByDirection(1));
            //

            // Set up initial UI visibility
            for (int i = 0; i < gachaShopItems.Length; i++)
            {
                int index = i;
                
                // Set up current theme & switch shop button
                bool isSelected = index == _currentShopIndex;
                gachaShopItems[index].Theme.gameObject.SetActive(isSelected);
                gachaShopItems[index].SwitchShopButton.ThemeImage.color = isSelected ? Color.green : Color.red;
                
                // Add listener to switch shop button
                gachaShopItems[index].SwitchShopButton.SwitchButton.onClick.RemoveAllListeners();
                gachaShopItems[index].SwitchShopButton.SwitchButton.onClick.AddListener(() => OnSwitchShopButtonClicked(index));
            }
            //
            
            // Set up roll buttons
            singleRollBtn.RollButtonText.text = $"Roll X{singleRollBtn.RollCount}";
            singleRollBtn.RollButton.onClick.RemoveAllListeners();
            singleRollBtn.RollButton.onClick.AddListener(() => Roll(singleRollBtn.RollCount));
            
            multiRollBtn.RollButtonText.text = $"Roll X{multiRollBtn.RollCount}";
            multiRollBtn.RollButton.onClick.RemoveAllListeners();
            multiRollBtn.RollButton.onClick.AddListener(() => Roll(multiRollBtn.RollCount));
            //
            
            RefreshUI();
        }

        public override void Show()
        {
            // Pause game
            _pauseSystem =_pauseSystem != null ? _pauseSystem : ServiceLocator.Get<PauseSystem>();
            _pauseSystem.SetPause(true);
            
            base.Show();
        }

        public override void Hide()
        {
            // Resume game
            _pauseSystem.SetPause(false);
            
            base.Hide();
        }

        #region --- BUTTON EVENTS ---
        private void OnSwitchShopButtonClicked(int newIndex)
        {
            // Save last shop index
            _lastShopIndex = _currentShopIndex;
            _currentShopIndex = newIndex;
            
            RefreshUI();
        }

        private void OnCloseButtonClicked()
        {
            ServiceLocator.Get<UIManager>()?.OpenScreen<Screen_ExploreHUD>();
        }
        #endregion
        

        private void SelectShopByDirection(int shopDirection)
        {
            _lastShopIndex = _currentShopIndex;
            _currentShopIndex += shopDirection;
            RefreshUI();
        }

        public void RefreshUI()
        {
            // Disables
            if (_lastShopIndex != -1)
            {
                gachaShopItems[_lastShopIndex].SwitchShopButton.ThemeImage.color = Color.red;
                gachaShopItems[_lastShopIndex].Theme.gameObject.SetActive(false);
            } 
            
            // Enable
            gachaShopItems[_currentShopIndex].SwitchShopButton.ThemeImage.color = Color.green;
            gachaShopItems[_currentShopIndex].Theme.gameObject.SetActive(true);
            
            // Enable or disable previous shop button & next shop button
            prevShopBtn.gameObject.SetActive(_currentShopIndex > 0);
            nextShopBtn.gameObject.SetActive(_currentShopIndex < (gachaShopItems.Length - 1));
            
        }

        private void Roll(int rollCount)
        {
            var curGachaShopItem = gachaShopItems[_currentShopIndex];
            var inventory = curGachaShopItem.InstanceRemoteData.Inventory;

            for (int i = 0; i < rollCount; i++)
            {
                if (curGachaShopItem.Weights.CanGetRandom())
                {
                    int resultIndex = curGachaShopItem.Weights.GetRandom();
                    var item = inventory[resultIndex];
                    Debug.Log(item.ItemId);
                    // Add item
                } 
            }
        }

        private IEnumerator LoadDataCoroutine()
        {
            yield return new WaitUntil(() => GachaDataManager.Instance != null && GachaDataManager.Instance.FetchSucess);
            
            for (int i = 0; i < gachaShopItems.Length; i++)
            {
                var insRemoteData = GachaDataManager.Instance.ActiveShops[i];
                gachaShopItems[i].SetRemoteData(insRemoteData);
            }
        }
    }
    
    [Serializable]
    public class GachaShopItem
    {
        public Image Theme;
        public SwitchShopButton SwitchShopButton;
        
        // Cache
        public ShopInstanceRemoteData InstanceRemoteData { get; private set; }
        public WeightedCollection<int> Weights { get; private set; }

        public void SetRemoteData(ShopInstanceRemoteData instanceRemoteData)
        {
            InstanceRemoteData = instanceRemoteData;
            
            Weights = new WeightedCollection<int>();
            int itemCount = InstanceRemoteData.Inventory.Count;

            for (int i = 0; i < itemCount; i++)
            {
                int index = i;
                Weights.Add(index, InstanceRemoteData.Inventory[index].DropRate);
            }
        }
    }

    [Serializable]
    public class RollShopButton
    {
        public Button RollButton;
        public Image ConsumedResourceImage;
        public TextMeshProUGUI RollButtonText;
        public int RollCount;
    }

    [Serializable]
    public class SwitchShopButton
    {
        public Image ThemeImage;
        public Button SwitchButton;
    }
}