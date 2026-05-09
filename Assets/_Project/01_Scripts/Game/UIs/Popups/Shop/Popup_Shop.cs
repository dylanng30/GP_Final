using System;
using System.Collections.Generic;
using Dylanng.Core.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Popups.Shop
{
    public class Popup_Shop : UIPopup
    {
        [SerializeField] private Button prevShopBtn, nextShopBtn;
        [SerializeField] private RollShopButton singleRollBtn, multiRollBtn;
        
        [SerializeField] private GameObject[] shopThemes;
        [SerializeField] private SwitchShopButton[] switchShopButtons;

        private SwitchShopButton _currentSwitchShopBtn;
        private GameObject _currentShopTheme;
        
        private int _currentShopIndex;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            _currentShopIndex = 0;

            foreach (var switchBtn in switchShopButtons)
            {
                switchBtn.SwitchButton.onClick.RemoveAllListeners();
                switchBtn.SwitchButton.onClick.AddListener(() => OnSwitchShopButtonClicked(switchBtn));
            }
            
            prevShopBtn.onClick.RemoveAllListeners();
            prevShopBtn.onClick.AddListener(() => SelectShopByDirection(-1));
            
            
            nextShopBtn.onClick.RemoveAllListeners();
            nextShopBtn.onClick.AddListener(() => SelectShopByDirection(1));
            
            for (int i = 0; i < shopThemes.Length; i++)
            {
                // Set up current theme & switch shop button
                bool isSelected = i == _currentShopIndex;
                
                if (isSelected)
                {
                    _currentShopTheme = shopThemes[i];
                    _currentSwitchShopBtn = switchShopButtons[i];
                }
                
                shopThemes[i].SetActive(isSelected);
                switchShopButtons[i].ThemeImage.color = isSelected ? Color.green : Color.red;
            }
            
            // Set up roll buttons
            singleRollBtn.RollButtonText.text = $"Roll X{singleRollBtn.RollCount}";
            multiRollBtn.RollButtonText.text = $"Roll X{multiRollBtn.RollCount}";
            
            RefreshUI();
        }

        private void OnSwitchShopButtonClicked(SwitchShopButton currentSwitchShopBtn)
        {
            if (_currentSwitchShopBtn != null)
            {
                _currentSwitchShopBtn.ThemeImage.color = Color.red;
            }
            
            _currentSwitchShopBtn = currentSwitchShopBtn;
            _currentSwitchShopBtn.ThemeImage.color = Color.green;
            
            //Switch
        }

        private void SelectShopByDirection(int shopDirection)
        {
            _currentShopIndex += shopDirection;
            RefreshUI();
        }

        private void RefreshUI()
        {
            prevShopBtn.gameObject.SetActive(_currentShopIndex > 0);
            nextShopBtn.gameObject.SetActive(_currentShopIndex < (shopThemes.Length - 1));
            
            for (int i = 0; i < shopThemes.Length; i++)
            {
                bool isSelected = i == _currentShopIndex;
                
                if (isSelected)
                {
                    _currentShopTheme = shopThemes[i];
                    _currentSwitchShopBtn = switchShopButtons[i];
                }
                
                shopThemes[i].SetActive(isSelected);
                switchShopButtons[i].ThemeImage.color = isSelected ? Color.green : Color.red;
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
        //public Sprite[] ThemeSprites;
    }
}