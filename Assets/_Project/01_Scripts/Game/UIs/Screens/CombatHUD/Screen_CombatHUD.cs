using Dylanng.Core;
using Dylanng.Core.UI;
using Game.Events;
using Game.Managers.Party;
using Game.Managers.Hero;
using GAME.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UIs.Screens.CombatHUD
{
    public class Screen_CombatHUD : UIScreen
    {
        [Header("--- FOLLOW TOGGLE ---")]
        [SerializeField] private CustomToggle followActiveHeroToggle;
        
        [FormerlySerializedAs("heroAvaterWidgetGrid")]
        [Space(10)]
        [Header("--- HERO AVATAR WIDGETS SETTINGS ---")]
        [SerializeField] private HorizontalLayoutGroup heroAvatarWidgetGrid;
        [SerializeField] private HeroAvatarWidget[] characterAvatarWidgets;

        private int _selectedWidgetId = 0;
        
        private PartyCombatManager _partyCombatManager;
        private PartyCombatManager PartyCombatManager => _partyCombatManager ??= ServiceLocator.Get<PartyCombatManager>();

        public override void Initialize()
        {
            base.Initialize();

            // Setup Toggle

            if (followActiveHeroToggle != null)
            {
                followActiveHeroToggle.SetIsOn(true);
                followActiveHeroToggle.SetAction(OnFollowActiveHeroToggleClicked);
            }
            
            
            // Setup HeroAvatarWidgets
            for (int i = 0; i < characterAvatarWidgets.Length; i++)
            {
                int widgetId = i;
                characterAvatarWidgets[i].Initialize();
                characterAvatarWidgets[i].Setup(widgetId);
                characterAvatarWidgets[i].SetupAction(OnAvatarClicked);
                characterAvatarWidgets[i].SetSelected(widgetId == _selectedWidgetId);
            }

            RefreshWidgets();
        }

        public override void Show()
        {
            base.Show();
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
            RefreshWidgets();
        }

        public void RefreshWidgets()
        {
            var partyManager = ServiceLocator.Get<PartyManager>();
            var heroDataManager = ServiceLocator.Get<HeroDataManager>();

            for (int i = 0; i < characterAvatarWidgets.Length; i++)
            {
                if (characterAvatarWidgets[i] == null) continue;

                if (partyManager != null && i < partyManager.HeroIds.Length)
                {
                    string heroId = partyManager.HeroIds[i];
                    if (!string.IsNullOrEmpty(heroId))
                    {
                        characterAvatarWidgets[i].gameObject.SetActive(true);
                        characterAvatarWidgets[i].SetHeroId(heroId);
                        Sprite sprite = heroDataManager != null ? heroDataManager.GetHeroSprite(heroId) : null;
                        characterAvatarWidgets[i].SetSprite(sprite);
                        characterAvatarWidgets[i].UpdateHealth(1f, 1f);
                    }
                    else
                    {
                        characterAvatarWidgets[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    characterAvatarWidgets[i].gameObject.SetActive(false);
                }
            }
        }

        public override void Hide()
        {
            base.Hide();
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            
            //TODO: Set character...
        }

        private void OnAvatarClicked(int widgetId)
        {
            PartyCombatManager?.SwitchActiveHero(widgetId);
        }

        private void OnFollowActiveHeroToggleClicked(bool isOn)
        {
            EventBus.Publish(new ToggleFollowModeEvent(isOn));
        }

        #region --- GAMEPLAY EVENT ---
        private void OnActiveHeroChanged(ActiveHeroChangedEvent evt)
        {
            if (_selectedWidgetId >= 0 && _selectedWidgetId < characterAvatarWidgets.Length)
            {
                heroAvatarWidgetGrid.enabled = false;
                characterAvatarWidgets[_selectedWidgetId].SetSelected(false);
            }

            _selectedWidgetId = evt.NewActiveIndex;

            if (_selectedWidgetId >= 0 && _selectedWidgetId < characterAvatarWidgets.Length)
            {
                characterAvatarWidgets[_selectedWidgetId].SetSelected(true);
                heroAvatarWidgetGrid.enabled = true;
            }
        }

        private void OnEntityDamaged(EntityDamagedEvent evt)
        {
            if (characterAvatarWidgets == null) return;
            foreach (var widget in characterAvatarWidgets)
            {
                if (widget != null && widget.HeroId == evt.EntityId)
                {
                    widget.UpdateHealth(evt.CurrentHealth, evt.MaxHealth);
                }
            }
        }
        #endregion

        
    }
}

