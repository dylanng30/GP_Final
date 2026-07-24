using System;
using Dylanng;
using Game.UIs.Popups.Shop;
using Game.UIs.Screens.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Screens.Explore
{
    public class Screen_ExploreHUD : UIScreen
    {
        [Header("---MAIN---")]
        [SerializeField] private Button menuBtn;

        [Header("---Menu dropdown---")]
        [SerializeField] private GameObject menuDropdown;
        [SerializeField] private Button gachaShopBtn, heroCollectionBtn;
        
        
        // Cache
        private UIManager _uiManager;
        private UIManager UIManager => _uiManager ??= ServiceLocator.Get<UIManager>();
        
        public override void Initialize()
        {
            if (menuBtn) menuBtn.onClick.AddListener(OnMenuButtonClicked);
            if (gachaShopBtn) gachaShopBtn.onClick.AddListener(OnGachaShopButtonClicked);
            if (heroCollectionBtn) heroCollectionBtn.onClick.AddListener(OnHeroCollectionButtonClicked);
        }

        #region --- BUTTON LISNTERS ---

        private void OnMenuButtonClicked()
        {
            menuDropdown.SetActive(!menuDropdown.activeSelf);
        }
        private void OnGachaShopButtonClicked()
        {
            UIManager.OpenScreen<Screen_GachaShop>();
        }
        private void OnHeroCollectionButtonClicked()
        {
            UIManager.OpenScreen<Screen_HeroCollection>();
        }
        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (menuBtn) menuBtn.onClick.RemoveAllListeners();
            if (gachaShopBtn) gachaShopBtn.onClick.RemoveAllListeners();
            if (heroCollectionBtn) heroCollectionBtn.onClick.RemoveAllListeners();
        }
    }
}