using System;
using Dylanng.Core.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Screens.Hero
{
    public class HeroOverviewSlotView : UIBase
    {
        public RectTransform Rect;
        [SerializeField] private Button heroButton;
        [SerializeField] private Image heroAvatarImage, heroFrameImage, roleImage, elementImage;
        
        private string _currentHeroId;
        public Action<string> OnHeroButtonClicked;

        protected override void Awake()
        {
            base.Awake();
            heroButton.onClick.AddListener(Select);    
        }

        public void SetHero(string heroId, Sprite avatarIcon, Sprite frameIcon, Sprite roleIcon, Sprite elementIcon)
        {
            _currentHeroId = heroId;
            bool hasHero = !string.IsNullOrEmpty(_currentHeroId);
            gameObject.SetActive(hasHero);

            if (hasHero)
            {
                heroAvatarImage.sprite = avatarIcon;
                heroFrameImage.sprite = frameIcon;
                roleImage.sprite = roleIcon;     
                elementImage.sprite = elementIcon; 
            }
        }

        private void Select()
        {
            Debug.Log($"Select {_currentHeroId}");
            OnHeroButtonClicked?.Invoke(_currentHeroId);
        }

        protected override void OnDestroy()
        {
            heroButton.onClick.RemoveAllListeners();
            
            OnHeroButtonClicked = null;
            
            base.OnDestroy();
        }
    }
}