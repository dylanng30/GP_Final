using System.Collections.Generic;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.BasicAttack;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Inventory;
using Dylanng.Gameplay.RPG.Stats;
using Game.Animation.Hero;
using Game.Combat;
using Game.Combat.Domain;
using Game.Data.Hero;
using Game.Data.Item;
using Game.Events;
using Game.Gameplay.AI.Controllers;
using Game.Gameplay.Controllers;
using Game.Managers.Hero;
using Game.Managers.Item;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    /// <summary>
    /// Actor trong trận của hero và quái. Lớp này chỉ ghép các thành phần:
    /// HealthPool, EnergyPool, CharacterMotor, AbilityRunner và BasicAttackRunner.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class HeroInstance : MonoBehaviourBase, IEntityController, ITargetable
    {
        private const float SpeedStatToMoveSpeed = 1f / 20f;
        private const float DefaultSkillCastDuration = 1.2f;

        [Header("--- MOVEMENT SETTINGS ---")] [SerializeField]
        private float _moveSpeed = 5f;

        [SerializeField] private float _rotationSpeed = 15f;
        [SerializeField] private float _gravity = -15f;

        [Header("--- UX ---")] [SerializeField]
        private GameObject controlledCircleSign;

        [Header("--- COMBAT & ABILITIES ---")]
        [Tooltip("Hệ của entity không có dữ liệu hero (ví dụ quái). Hero lấy từ BaseHeroData.Faction khi Setup.")]
        [SerializeField] private GameElement element = GameElement.Physical;

        [SerializeField] private float maxEnergyLimit = 200f;
        [SerializeField] private BasicAttackSO basicAttackConfig;
        [SerializeField] private List<AbilitySO> abilities = new List<AbilitySO>();

        [Header("--- JUMP CONFIG ---")] [SerializeField]
        private float jumpForce = 8f;

        [Header("--- DASH CONFIG ---")] [SerializeField]
        private float dashDistance = 5f;

        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private float dashInvincibilityDuration = 0.2f;

        [SerializeField] private CharacterController characterController;
        [SerializeField] private HeroAnimatorController animatorController;
        [SerializeField] private PlayerInputController playerController;
        [SerializeField] private HeroAIController aiController;

        private CharacterMotor _motor;
        private AttachPoints _attachPoints;
        private AbilityRunner _abilityRunner;
        private BasicAttackRunner _basicAttack;

        public string HeroId { get; private set; }
        public StatController Stats { get; private set; }
        public EquipmentController Equipments { get; private set; }
        public HealthPool Health { get; } = new HealthPool();
        public EnergyPool Energy { get; private set; }
        public StatusController Status { get; private set; }
        public Transform Transform => TransformCache;
        public GameElement Element => element;
        public bool IsDead => Health.IsDead;
        public bool IsCasting => _abilityRunner.IsCasting;
        public bool IsInvulnerable => _motor.IsInvincible || Stats.GetStatValue(StatType.DamageImmunity) > 0f;

        public Vector2 CurrentMoveInput => _motor.MoveInput;
        public bool IsGrounded => _motor.IsGrounded;
        public float CurrentEnergy => Energy.Current;
        public float MaxEnergyLimit => Energy.Max;

        protected override void Awake()
        {
            base.Awake();

            Stats = GetComponent<StatController>();
            Equipments = GetComponent<EquipmentController>();
            Energy = new EnergyPool(maxEnergyLimit);
            Status = new StatusController(this);

            _motor = new CharacterMotor(TransformCache, characterController, new MotorSettings
            {
                MoveSpeed = _moveSpeed,
                RotationSpeed = _rotationSpeed,
                Gravity = _gravity,
                JumpForce = jumpForce,
                DashDistance = dashDistance,
                DashDuration = dashDuration,
                DashInvincibilityDuration = dashInvincibilityDuration
            });
            _attachPoints = new AttachPoints(TransformCache);
            _abilityRunner = new AbilityRunner(this, abilities, Energy);
            _basicAttack = new BasicAttackRunner(this, basicAttackConfig, animatorController, _attachPoints);

            Health.Changed += OnHealthChanged;
            Health.Died += OnDied;
            Status.ControlChanged += OnControlChanged;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Stats != null) Stats.OnAnyStatChanged += OnStatChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Stats != null) Stats.OnAnyStatChanged -= OnStatChanged;
        }

        protected override void Start()
        {
            base.Start();

            if (Stats != null && Stats.MaxHP <= 0f) InitializeFallbackStats();
            if (Health.Max <= 0f) Health.Restore(Stats != null ? Stats.MaxHP : 0f);

            _basicAttack.PrewarmPool();
        }

        private void InitializeFallbackStats()
        {
            Stats.InitializeStat(StatType.MaxHP, 1000f);
            Stats.InitializeStat(StatType.ATK, 100f);
            Stats.InitializeStat(StatType.DEF, 50f);
            Stats.InitializeStat(StatType.Speed, 100f);
        }

        public void Setup(string heroId)
        {
            HeroId = heroId;

            var heroDataManager = ServiceLocator.Get<HeroDataManager>();
            if (heroDataManager != null)
            {
                ApplyHeroData(heroDataManager, heroId);
                SetupAnimation(heroDataManager, heroId);
            }

            playerController.Initialize(this);
            aiController.Initialize(this, this);

            SetAsActive(false);
            Energy.Clear();
        }

        private void ApplyHeroData(HeroDataManager heroDataManager, string heroId)
        {
            BaseHeroData baseHeroData = heroDataManager.GetHeroData(heroId);
            if (baseHeroData != null) element = baseHeroData.Faction;

            int heroLevel = 1;
            int heroStar = baseHeroData != null ? baseHeroData.HeroStarLevel : 1;
            List<ItemDataInstance> heroItems = null;

            var savedInstance = heroDataManager.SaveData?.Heros?.Find(x => x.HeroId == heroId);
            if (savedInstance != null)
            {
                heroLevel = savedInstance.HeroLevel;
                heroStar = savedInstance.HeroStarLevel;
                heroItems = savedInstance.HeroItems;
            }

            _abilityRunner.SetLevel(heroLevel);

            if (Stats == null) return;

            Stats.Initialize(heroId, heroLevel, heroStar);
            EquipItems(heroItems);

            float speedStat = Stats.Speed;
            if (speedStat > 0f) _motor.SetMoveSpeed(speedStat * SpeedStatToMoveSpeed);

            Health.Restore(Stats.MaxHP);
        }

        private void EquipItems(List<ItemDataInstance> heroItems)
        {
            var itemDataManager = ServiceLocator.Get<ItemDataManager>();
            if (heroItems == null || itemDataManager == null) return;

            foreach (ItemDataInstance item in heroItems)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;

                if (itemDataManager.GetItemData(item.ItemId) is EquipmentItemData equipment)
                {
                    Equipments.EquipItem(equipment, item.ItemLevel);
                }
            }
        }

        private void SetupAnimation(HeroDataManager heroDataManager, string heroId)
        {
            if (animatorController == null) return;

            HeroAsset heroAsset = heroDataManager.GetHeroAsset(heroId);
            if (heroAsset?.AnimationConfig != null)
            {
                animatorController.Setup(heroAsset.AnimationConfig);
            }
            else
            {
                Debug.LogWarning($"[HeroInstance] {heroId}: Không tìm thấy AnimationConfig trong HeroLocalDataSO.");
            }
        }

        public void SetAsActive(bool isActive)
        {
            playerController.enabled = isActive;
            aiController.enabled = true;
            if (controlledCircleSign != null) controlledCircleSign.SetActive(isActive);

            if (!isActive) aiController.FollowActiveHero();
        }

        private void Update()
        {
            if (characterController == null) return;

            bool canMove = Status.CanMove && !_abilityRunner.IsMovementLocked;
            _motor.Tick(Time.deltaTime, canMove);
            animatorController?.SetMovementSpeed(canMove ? _motor.MoveMagnitude : 0f);

            Status.Tick(Time.deltaTime);
            _abilityRunner.Tick();
            _basicAttack.Tick();
        }

        private void OnStatChanged(StatType type, float oldValue, float newValue)
        {
            if (type == StatType.MaxHP && Health.Max > 0f) Health.SetMax(newValue);
            if (type == StatType.Speed && newValue > 0f) _motor.SetMoveSpeed(newValue * SpeedStatToMoveSpeed);
        }

        private void OnControlChanged()
        {
            if (!Status.CanAct) _abilityRunner.Interrupt();
        }

        private void OnHealthChanged()
        {
            if (string.IsNullOrEmpty(HeroId)) return;

            EventBus.Publish(new EntityDamagedEvent(HeroId, Health.Current, Health.Max, 0f));
        }

        private void OnDied()
        {
            animatorController?.PlayDeath();
            Status.Set.Clear();
            EventBus.Publish(new ActorDiedEvent(GameObjectCache));
        }

        public void OnTargeted(bool isTargeted)
        {
            if (controlledCircleSign != null) controlledCircleSign.SetActive(isTargeted);
        }

        /// <summary>Quay mặt ngay về một điểm trên mặt phẳng ngang.</summary>
        public void FaceTowards(Vector3 point)
        {
            Vector3 direction = point - TransformCache.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                TransformCache.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }

        public void GainEnergy(float amount) => Energy.Gain(amount);

        public Transform GetBone(string boneName) => _attachPoints.Get(boneName);

        #region --- Basic Attack ---

        public bool CanAttack() => Status.CanAct && _basicAttack.CanAttack;

        public float GetAttackCooldown() => _basicAttack.Cooldown;

        public void Attack(ITargetable target)
        {
            if (Status.CanAct) _basicAttack.Attack(target ?? ResolveCurrentTarget());
        }

        private ITargetable ResolveCurrentTarget()
        {
            if (TryGetComponent<PlayerTargetingSystem>(out var targeting) && IsValidTarget(targeting.CurrentTarget))
            {
                return targeting.CurrentTarget;
            }

            if (TryGetComponent<EnemyBotController>(out var enemyBot) && IsValidTarget(enemyBot.CurrentTarget))
            {
                return enemyBot.CurrentTarget;
            }

            return aiController != null ? aiController.CurrentTarget : null;
        }

        private static bool IsValidTarget(ITargetable target) => target != null && !target.IsDead;

        #endregion

        #region --- Abilities ---

        public AbilitySO GetAbility(int slot) => _abilityRunner.Get(slot);

        public bool CanUseSkill(int slot = AbilitySlot.Skill1) => Status.CanAct && _abilityRunner.CanUse(slot);

        public float GetSkillRemainingCooldown(int slot = AbilitySlot.Skill1) => _abilityRunner.GetRemainingCooldown(slot);

        public float GetSkillCastDuration(int slot = AbilitySlot.Skill1)
        {
            AbilitySO ability = GetAbility(slot);
            return ability != null ? ability.TotalCastDuration : DefaultSkillCastDuration;
        }

        public void UseSkill(int slot, GameObject target)
        {
            if (target == null && TryGetComponent<PlayerTargetingSystem>(out var targeting)
                                && IsValidTarget(targeting.CurrentTarget))
            {
                target = targeting.CurrentTarget.Transform.gameObject;
            }

            Vector3 point = target != null ? target.transform.position : TransformCache.position + TransformCache.forward * 2f;
            UseSkill(slot, target, point);
        }

        public void UseSkill(int slot, GameObject target, Vector3 point)
        {
            if (!Status.CanAct || !_abilityRunner.TryUse(slot, target, point)) return;

            FaceTowards(target != null ? target.transform.position : point);
        }

        public LayerMask GetTargetLayerMask()
        {
            if (TryGetComponent<PlayerTargetingSystem>(out var targeting) && targeting.EnemyLayerMask.value != 0)
            {
                return targeting.EnemyLayerMask;
            }

            if (TryGetComponent<HeroAIController>(out var heroAi) && heroAi.EnemyLayerMask.value != 0)
            {
                return heroAi.EnemyLayerMask;
            }

            if (TryGetComponent<EnemyBotController>(out var enemyBot) && enemyBot.TargetLayerMask.value != 0)
            {
                return enemyBot.TargetLayerMask;
            }

            return 0;
        }

        #endregion

        #region --- IEntityController ---

        public void Move(Vector2 direction) => _motor.SetMoveInput(direction);

        public void StopMove() => _motor.SetMoveInput(Vector2.zero);

        public void Attack() => Attack(null);

        public void UseSkill(int slot) => UseSkill(slot, null);

        public void Jump()
        {
            if (Status.CanMove && _motor.TryJump()) animatorController?.PlayJump();
        }

        public void Dash()
        {
            if (!IsDead && Status.CanMove && _motor.TryDash()) animatorController?.PlayRoll();
        }

        #endregion

        #region --- Animation Event Callbacks ---

        /// <summary>Được gọi bởi HeroAnimatorController khi AnimationEvent 'Transition' kích hoạt.</summary>
        public void NotifyAnimationTransition()
        {
            _abilityRunner.OnAnimationTransition();
            _basicAttack.OnAnimationHit();
        }

        /// <summary>Được gọi bởi HeroAnimatorController khi AnimationEvent 'Enter' kích hoạt.</summary>
        public void NotifyAnimationEnter() => _abilityRunner.OnAnimationEnter();

        /// <summary>Được gọi bởi HeroAnimatorController khi AnimationEvent 'Exit' kích hoạt.</summary>
        public void NotifyAnimationExit() => _abilityRunner.OnAnimationExit();

        #endregion
    }
}
