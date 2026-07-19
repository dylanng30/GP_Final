using Dylanng.Core;
using Dylanng.Core.UI;
using Dylanng.Dylanng_EventBus;
using Game.Events;
using Game.Managers.Party;
using Game.Managers.Hero;
using GAME.Utilities;
using Game.UIs.Widgets;
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
        [SerializeField] private VerticalLayoutGroup heroAvatarWidgetGrid;
        [SerializeField] private HeroAvatarWidget[] characterAvatarWidgets;

        [Space(10)]
        [Header("--- MANUAL ACTION BUTTONS ---")]
        [SerializeField] private GameObject actionButtonsContainer;
        [SerializeField] private AttackCastButton attackBtn;
        [SerializeField] private SkillCastButton skill1Btn;
        [SerializeField] private SkillCastButton skill2Btn;
        [SerializeField] private Button dashBtn;

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
            
            // Setup Action Buttons
            if (dashBtn != null) dashBtn.onClick.AddListener(OnDashClicked);

            UpdateActionButtonsVisibility(true); // Default to Auto mode (Toggle is ON)
            
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
            UpdateSkillButtonsForActiveHero();
        }

        public override void Show()
        {
            base.Show();
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged);
            RefreshWidgets();
            UpdateSkillButtonsForActiveHero();
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

            // Tự động reset về trạng thái Auto Follow (TopDown) khi chuyển sang Explore hoặc ẩn HUD
            if (followActiveHeroToggle != null)
            {
                followActiveHeroToggle.SetIsOn(true);
            }
            OnFollowActiveHeroToggleClicked(true);
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
            UpdateActionButtonsVisibility(isOn);
        }

        private void UpdateActionButtonsVisibility(bool isAutoMode)
        {
            if (actionButtonsContainer != null)
            {
                actionButtonsContainer.SetActive(!isAutoMode);
            }
            else
            {
                if (attackBtn != null) attackBtn.gameObject.SetActive(!isAutoMode);
                if (skill1Btn != null) skill1Btn.gameObject.SetActive(!isAutoMode);
                if (skill2Btn != null) skill2Btn.gameObject.SetActive(!isAutoMode);
                if (dashBtn != null) dashBtn.gameObject.SetActive(!isAutoMode);
            }
        }



        private void OnSkillClicked(int skillIndex)
        {
            PartyCombatManager?.GetActiveHero()?.UseSkill(skillIndex);
        }

        private void OnDashClicked()
        {
            PartyCombatManager?.GetActiveHero()?.Dash();
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

            UpdateSkillButtonsForActiveHero();
        }

        private void UpdateSkillButtonsForActiveHero()
        {
            var activeHero = PartyCombatManager?.GetActiveHero();
            if (skill1Btn != null) skill1Btn.SetupSkill(activeHero);
            if (skill2Btn != null) skill2Btn.SetupSkill(activeHero);
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

