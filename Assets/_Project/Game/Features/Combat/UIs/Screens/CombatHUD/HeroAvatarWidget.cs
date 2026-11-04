using System;
using Dylanng;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Screens
{
    public class HeroAvatarWidget : UIBase
    {
        [Header("--- UI REFERENCES ---")]
        [SerializeField] private RectTransform mainRect;
        [SerializeField] private Button avatarBtn;
        [SerializeField] private Image avatarImg, healthImg, ultimateChargeImg;
        
        [Header("---UX Settings---")]
        [SerializeField] private float selectedScaleMultiplier = 1.1f;
        
        public string HeroId { get; private set; }
        private int _widgetId = -1;
        private Action<int> _onClickCallback;

        // Cache
        private Vector2 _originalSize;
        
        public override void Initialize()
        {
            // Save origin size widget -> easy to back origin size
            _originalSize = mainRect.sizeDelta;

            if (avatarBtn) avatarBtn.onClick.AddListener(OnAvatarButtonClicked);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (avatarBtn) avatarBtn.onClick.RemoveAllListeners();
            
            _onClickCallback = null;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (data is int widgetId)
            {
                _widgetId = widgetId;
            }
        }

        public void SetHeroId(string heroId)
        {
            HeroId = heroId;
        }

        public void SetupAction(Action<int> callback)
        {
            _onClickCallback = callback;
        }

        public void SetSelected(bool isSelected)
        {
            mainRect.sizeDelta = isSelected ? _originalSize * selectedScaleMultiplier : _originalSize;
        }

        public void SetSprite(Sprite sprite)
        {
            if (avatarImg != null)
            {
                avatarImg.sprite = sprite;
            }
        }

        private void OnAvatarButtonClicked()
        {
            _onClickCallback?.Invoke(_widgetId);
        }

        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthImg != null)
            {
                healthImg.fillAmount = currentHealth / maxHealth;
            }
        }
    }
}