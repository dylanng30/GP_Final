using System.Linq;
using Dylanng;
using Game.Events;
using Game.Gameplay.Controllers;
using Game.Managers.Party;
using Game.Managers.Hero;
using Game.Utilities;
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
        [SerializeField] private SkillCastButton ultimateBtn;
        [SerializeField] private Button dashBtn;
        [SerializeField] private Button jumpBtn;
        [SerializeField] private Button lockOnBtn;
        [SerializeField] private Button regroupBtn;

        private int _selectedWidgetId = 0;
        
        private PartyCombatManager _partyCombatManager;
        private PartyCombatManager PartyCombatManager => _partyCombatManager ??= ServiceLocator.Get<PartyCombatManager>();

        public override void Initialize()
        {
            base.Initialize();
            gameObject.AddComponent<CombatInfoPanel>();

            // Setup Toggle

            if (followActiveHeroToggle != null)
            {
                followActiveHeroToggle.SetIsOn(PartyCombatManager != null && PartyCombatManager.IsFollowModeEnabled);
                followActiveHeroToggle.SetAction(OnFollowActiveHeroToggleClicked);
            }
            
            // Setup Action Buttons
            if (dashBtn != null) dashBtn.onClick.AddListener(OnDashClicked);
            if (jumpBtn != null) jumpBtn.onClick.AddListener(OnJumpClicked);
            if (lockOnBtn != null) lockOnBtn.onClick.AddListener(OnLockOnClicked);
            if (regroupBtn != null) regroupBtn.onClick.AddListener(OnRegroupClicked);

            UpdateActionButtonsVisibility(PartyCombatManager != null && PartyCombatManager.IsFollowModeEnabled);
            
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
            EventBus.Subscribe<PartyHeroesSpawnedEvent>(OnPartyHeroesSpawned);
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
                        UpdateWidgetHealth(characterAvatarWidgets[i], heroId);
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

        private void UpdateWidgetHealth(HeroAvatarWidget widget, string heroId)
        {
            var hero = PartyCombatManager?.ActiveHeroes.FirstOrDefault(candidate => candidate != null && candidate.HeroId == heroId);
            if (hero != null && hero.Health.Max > 0f) widget.UpdateHealth(hero.Health.Current, hero.Health.Max);
            else widget.UpdateHealth(1f, 1f);
        }

        public override void Hide()
        {
            base.Hide();
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Unsubscribe<EntityDamagedEvent>(OnEntityDamaged);
            EventBus.Unsubscribe<PartyHeroesSpawnedEvent>(OnPartyHeroesSpawned);

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
                if (ultimateBtn != null) ultimateBtn.gameObject.SetActive(!isAutoMode);
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

        private void OnJumpClicked()
        {
            PartyCombatManager?.GetActiveHero()?.Jump();
        }

        private void OnLockOnClicked()
        {
            var hero = PartyCombatManager?.GetActiveHero();
            if (hero != null && hero.TryGetComponent<PlayerTargetingSystem>(out var targeting)) targeting.ToggleHardLock();
        }

        private void OnRegroupClicked()
        {
            PartyCombatManager?.RegroupFollowers();
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

        /// <summary>Đổi hero trong đội thì cập nhật chân dung và nút skill.</summary>
        private void OnPartyHeroesSpawned(PartyHeroesSpawnedEvent evt)
        {
            RefreshWidgets();
            UpdateSkillButtonsForActiveHero();
        }

        private void UpdateSkillButtonsForActiveHero()
        {
            var activeHero = PartyCombatManager?.GetActiveHero();
            if (skill1Btn != null) skill1Btn.SetupSkill(activeHero);
            if (skill2Btn != null) skill2Btn.SetupSkill(activeHero);
            if (ultimateBtn != null) ultimateBtn.SetupSkill(activeHero);
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

