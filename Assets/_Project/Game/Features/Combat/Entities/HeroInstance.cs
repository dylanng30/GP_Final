using Game.Data.Hero;
using Dylanng;
using Dylanng.Gameplay.RPG.Stats;
using Dylanng.Gameplay.RPG.Inventory;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.BasicAttack;
using Dylanng.Gameplay.RPG.Abilities.Commands;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Data.Item;
using Game.Gameplay.Controllers;
using Game.Animation.Hero;
using System.Collections;
using System.Collections.Generic;
using Game.Gameplay.AI.Controllers;
using Game.Managers.Hero;
using Game.Managers.Item;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    [RequireComponent(typeof(CharacterController))]
    public class HeroInstance : MonoBehaviourBase, IEntityController, ITargetable
    {
        [Header("--- MOVEMENT SETTINGS ---")] [SerializeField]
        private float _moveSpeed = 5f;

        [SerializeField] private float _rotationSpeed = 15f;
        [SerializeField] private float _gravity = -15f;

        [Header("--- UX ---")] [SerializeField]
        private GameObject controlledCircleSign;

        [Header("--- COMBAT & ABILITIES ---")] [SerializeField]
        private float maxEnergyLimit = 200f; // Giới hạn tối đa không được vượt quá 200 Nộ

        [SerializeField] private BasicAttackSO basicAttackConfig;
        [SerializeField] private List<AbilitySO> abilities = new List<AbilitySO>();

        [Header("--- JUMP CONFIG ---")] [SerializeField]
        private float jumpForce = 8f;

        [Header("--- DASH CONFIG ---")] [SerializeField]
        private float dashDistance = 5f;

        [SerializeField] private float dashDuration = 0.25f;
        [SerializeField] private float dashInvincibilityDuration = 0.2f;

        [SerializeField] private CharacterController characterController;
        private Transform _mainCameraTransform;
        private StateMachine _skillStateMachine;
        [SerializeField] private HeroAnimatorController animatorController;

        private Vector2 _currentMoveInput;
        private float _velocityY;
        private float _lastAttackTime = -999f;
        private Dictionary<int, float> _skillLastUseTimes = new Dictionary<int, float>();

        // Jump / Dash state
        private bool _isJumping;
        private bool _isDashing;
        private bool _isDashInvincible;

        // Pending Basic Attack state
        private bool _isPendingBasicAttack;
        private ITargetable _pendingAttackTarget;
        private float _pendingAttackTime;
        private float _pendingAttackFinalDamage;

        // Cache xương/transform để không phải tìm kiếm đệ quy nhiều lần
        private Dictionary<string, Transform> _cachedBones = new Dictionary<string, Transform>();

        // ─── Exposed state (dùng bởi HeroAnimatorController) ─────────────────────
        public Vector2 CurrentMoveInput => _currentMoveInput;
        public bool IsGrounded => characterController != null && characterController.isGrounded;

        public bool CanAttack()
        {
            float cooldown = basicAttackConfig != null ? basicAttackConfig.AttackCooldown : 1.0f;
            return Time.time - _lastAttackTime >= cooldown;
        }

        public float GetAttackCooldown()
        {
            return basicAttackConfig != null ? basicAttackConfig.AttackCooldown : 1.0f;
        }

        public string HeroId { get; private set; }
        public StatController Stats { get; private set; }
        public EquipmentController Equipments { get; private set; }
        public Transform Transform => TransformCache;
        public bool IsDead { get; private set; }
        public float CurrentEnergy { get; private set; }
        public float MaxEnergyLimit => maxEnergyLimit;
        public GameElement HeroElement { get; private set; }

        [SerializeField] private PlayerInputController playerController;
        [SerializeField] private HeroAIController aiController;

        protected override void Awake()
        {
            base.Awake();
            _skillStateMachine = new StateMachine();

            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EventBus.Subscribe<DamageDealtEvent>(OnDamageReceived);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageReceived);
        }

        protected override void Start()
        {
            base.Start();

            if (Stats == null) Stats = GetComponent<StatController>();
            if (Stats != null && Stats.GetStatValue(StatType.MaxHP) <= 0)
            {
                Stats.InitializeStat(StatType.MaxHP, 1000f);
                Stats.InitializeStat(StatType.ATK, 100f);
                Stats.InitializeStat(StatType.DEF, 50f);
                Stats.InitializeStat(StatType.Speed, 100f);
            }

            // Tự động tạo pool cho đòn đánh thường nếu có cấu hình và prefab
            if (basicAttackConfig != null && basicAttackConfig.ProjectilePrefab != null)
            {
                var poolManager = ServiceLocator.Get<PoolManager>();
                if (poolManager != null)
                {
                    poolManager.CreatePool(basicAttackConfig.ProjectilePoolKey, basicAttackConfig.ProjectilePrefab, 10);
                }
            }
        }

        public void Setup(string heroId)
        {
            HeroId = heroId;
            Stats = GetComponent<StatController>();
            Equipments = GetComponent<EquipmentController>();

            var heroDataManager = ServiceLocator.Get<HeroDataManager>();
            var itemDataManager = ServiceLocator.Get<ItemDataManager>();

            if (heroDataManager != null)
            {
                var baseHeroData = heroDataManager.GetHeroData(heroId);
                int heroLevel = 1;
                int heroStar = baseHeroData != null ? baseHeroData.HeroStarLevel : 1;
                List<ItemDataInstance> heroItems = null;

                if (heroDataManager.SaveData != null && heroDataManager.SaveData.Heros != null)
                {
                    var savedInstance = heroDataManager.SaveData.Heros.Find(x => x.HeroId == heroId);
                    if (savedInstance != null)
                    {
                        heroLevel = savedInstance.HeroLevel;
                        heroStar = savedInstance.HeroStarLevel;
                        heroItems = savedInstance.HeroItems;
                    }
                }

                if (Stats != null)
                {
                    Stats.Initialize(heroId, heroLevel, heroStar);
                }

                if (heroItems != null && itemDataManager != null)
                {
                    foreach (var itemInstance in heroItems)
                    {
                        if (itemInstance == null || string.IsNullOrEmpty(itemInstance.ItemId)) continue;
                        if (itemDataManager.GetItemData(itemInstance.ItemId) is EquipmentItemData equipData)
                        {
                            Equipments.EquipItem(equipData, itemInstance.ItemLevel);
                        }
                    }
                }

                if (Stats != null)
                {
                    float calcSpeed = Stats.GetStatValue(StatType.Speed);
                    if (calcSpeed > 0)
                    {
                        _moveSpeed = calcSpeed / 20f;
                    }
                }
            }

            playerController.Initialize(this);
            aiController.Initialize(this, this);

            // Setup HeroAnimatorController với AnimationConfig tương ứng hero này
            if (animatorController != null)
            {
                var hdm = ServiceLocator.Get<HeroDataManager>();
                if (hdm != null)
                {
                    var heroAsset = hdm.GetHeroAsset(heroId);
                    if (heroAsset?.AnimationConfig != null)
                    {
                        animatorController.Setup(heroAsset.AnimationConfig);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[HeroInstance] {heroId}: Không tìm thấy AnimationConfig trong HeroLocalDataSO. Animation sẽ không hoạt động.");
                    }
                }
            }

            // Default to AI
            SetAsActive(false);
            CurrentEnergy = 0f;
        }

        public void SetAsActive(bool isActive)
        {
            if (isActive)
            {
                playerController.enabled = true;
                aiController.enabled = true;
                if (controlledCircleSign != null) controlledCircleSign.SetActive(true);
            }
            else
            {
                if (controlledCircleSign != null) controlledCircleSign.SetActive(false);
                playerController.enabled = false;
                aiController.enabled = true;
                aiController.FollowActiveHero();
            }
        }

        private void Update()
        {
            if (characterController == null) return;

            ApplyMovementAndGravity();
            _skillStateMachine?.Update();

            if (_isPendingBasicAttack && Time.time - _pendingAttackTime > 1.0f)
            {
                ExecutePendingBasicAttack();
            }
        }

        private void OnDamageReceived(DamageDealtEvent evt)
        {
            if (evt.Target != gameObject || Stats == null) return;

            // Nếu đang Dash invincible → miễn sát thương
            if (_isDashInvincible)
            {
                Debug.Log($"[HeroInstance] {HeroId} miễn sát thương do đang Dash!");
                return;
            }

            // Check Immunity
            if (Stats.GetStatValue(StatType.DamageImmunity) > 0)
            {
                Debug.Log($"[HeroInstance] {HeroId} miễn nhiễm {evt.Damage} sát thương!");
                return;
            }

            float remainingDamage = evt.Damage;

            float multiplier = Game.Gameplay.Combat.ElementalDamageCalculator.GetMultiplier(evt.Element, HeroElement);
            remainingDamage *= multiplier;

            // Check Shield
            float shield = Stats.GetStatValue(StatType.Shield);
            if (shield > 0)
            {
                if (shield >= remainingDamage)
                {
                    float newShield = shield - remainingDamage;
                    Debug.Log($"[HeroInstance] {HeroId} dùng khiên chặn {remainingDamage} sát thương!");
                    return;
                }
                else
                {
                    remainingDamage -= shield;
                    Debug.Log($"[HeroInstance] {HeroId} vỡ khiên, nhận {remainingDamage} sát thương!");
                }
            }

            // Apply Damage to HP
            float currentHp = Stats.GetStatValue(StatType.MaxHP);
            float newHp = Mathf.Max(0f, currentHp - remainingDamage);
            Stats.InitializeStat(StatType.MaxHP, newHp);

            Debug.Log($"[HeroInstance] {HeroId} nhận {remainingDamage} sát thương. HP còn lại: {newHp}");

            if (newHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            IsDead = true;
            animatorController?.PlayDeath();
            Debug.Log($"[HeroInstance] {HeroId} đã chết!");
            // Implement death logic here
        }

        public void OnTargeted(bool isTargeted)
        {
            if (controlledCircleSign != null)
            {
                controlledCircleSign.SetActive(isTargeted);
            }
        }

        private void ApplyMovementAndGravity()
        {
            Vector3 moveDirection = Vector3.zero;

            if (_currentMoveInput.sqrMagnitude > 0.01f)
            {
                // Always ensure we have the current active CameraController's Camera
                if (CameraController.Instance != null)
                {
                    _mainCameraTransform = CameraController.Instance.transform;
                }
                else if (Camera.main != null)
                {
                    _mainCameraTransform = Camera.main.transform;
                }
                else
                {
                    _mainCameraTransform = null;
                }

                if (_mainCameraTransform != null)
                {
                    Vector3 cameraForward = _mainCameraTransform.forward;
                    Vector3 cameraRight = _mainCameraTransform.right;

                    cameraForward.y = 0f;
                    cameraRight.y = 0f;

                    if (cameraForward.sqrMagnitude > 0.001f)
                        cameraForward.Normalize();
                    else
                        cameraForward = Vector3.forward;

                    if (cameraRight.sqrMagnitude > 0.001f)
                        cameraRight.Normalize();
                    else
                        cameraRight = Vector3.right;

                    moveDirection = cameraForward * _currentMoveInput.y + cameraRight * _currentMoveInput.x;
                }
                else
                {
                    moveDirection = new Vector3(_currentMoveInput.x, 0f, _currentMoveInput.y);
                }

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                        _rotationSpeed * Time.deltaTime);
                }
            }

            if (characterController.isGrounded && _velocityY < 0)
            {
                _velocityY = -2f;
                _isJumping = false; // Landing: reset jump state
            }

            _velocityY += _gravity * Time.deltaTime;

            // Drive animation movement speed (0 = Idle, ~0.5 = Walk, 1 = Run)
            float moveMagnitude = _isDashing ? 0f : _currentMoveInput.magnitude;
            animatorController?.SetMovementSpeed(moveMagnitude);

            // Khi đang Dash, DashCoroutine tự xử lý di chuyển — bỏ qua movement thông thường
            if (_isDashing) return;

            Vector3 finalVelocity = moveDirection * _moveSpeed;
            finalVelocity.y = _velocityY;

            characterController.Move(finalVelocity * Time.deltaTime);
        }

        public void GainEnergy(float amount)
        {
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, maxEnergyLimit);
            Debug.Log($"[HeroInstance] {HeroId} hồi +{amount} Nộ. Hiện tại: {CurrentEnergy}/{maxEnergyLimit}");
        }

        public AbilitySO GetAbility(int index)
        {
            if (abilities != null && index >= 0 && index < abilities.Count)
            {
                return abilities[index];
            }

            return null;
        }

        public bool CanUseSkill(int skillIndex = 0)
        {
            AbilitySO skill = GetAbility(skillIndex);
            if (skill == null) return false;

            // Kiểm tra Cooldown
            if (_skillLastUseTimes.TryGetValue(skillIndex, out float lastUseTime))
            {
                if (Time.time - lastUseTime < skill.Cooldown)
                {
                    return false;
                }
            }

            // Kiểm tra Nộ
            return CurrentEnergy >= skill.EnergyCost;
        }

        public float GetSkillRemainingCooldown(int skillIndex = 0)
        {
            AbilitySO skill = GetAbility(skillIndex);
            if (skill == null) return 0f;

            if (_skillLastUseTimes.TryGetValue(skillIndex, out float lastUseTime))
            {
                float elapsed = Time.time - lastUseTime;
                return Mathf.Max(0f, skill.Cooldown - elapsed);
            }

            return 0f;
        }

        public float GetSkillCastDuration(int skillIndex = 0)
        {
            AbilitySO skill = GetAbility(skillIndex);
            return skill != null ? skill.TotalCastDuration : 1.2f;
        }

        public void Attack(ITargetable target = null)
        {
            if (target == null)
            {
                if (TryGetComponent<PlayerTargetingSystem>(out var pts) && pts.CurrentTarget != null &&
                    !pts.CurrentTarget.IsDead)
                {
                    target = pts.CurrentTarget;
                }
                else if (TryGetComponent<EnemyBotController>(out var enemyCtrl) && enemyCtrl.CurrentTarget != null &&
                         !enemyCtrl.CurrentTarget.IsDead)
                {
                    target = enemyCtrl.CurrentTarget;
                }
                else
                {
                    target = aiController != null ? aiController.CurrentTarget : null;
                }
            }

            if (target != null && (target.IsDead || target.Transform == null))
            {
                return;
            }

            if (!CanAttack()) return;
            _lastAttackTime = Time.time;

            // Rotate towards target
            if (target != null && target.Transform != null)
            {
                Vector3 lookDirection = target.Transform.position;
                lookDirection.y = transform.position.y;
                Vector3 dir = (lookDirection - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // Delegate animation qua HeroAnimatorController (không gọi Animator trực tiếp)
            animatorController?.PlayAttack();

            float atkDamage = Stats != null ? Stats.ATK : 50f;
            float damageMultiplier = basicAttackConfig != null ? basicAttackConfig.DamageMultiplier : 1.0f;
            float finalDamage = atkDamage * damageMultiplier;

            _isPendingBasicAttack = true;
            _pendingAttackTarget = target;
            _pendingAttackFinalDamage = finalDamage;
            _pendingAttackTime = Time.time;

            if (animatorController == null)
            {
                ExecutePendingBasicAttack();
            }
        }

        private void ExecutePendingBasicAttack()
        {
            if (!_isPendingBasicAttack) return;
            _isPendingBasicAttack = false;

            ITargetable target = _pendingAttackTarget;
            float finalDamage = _pendingAttackFinalDamage;

            Vector3 spawnPos = transform.TransformPoint(basicAttackConfig != null ? basicAttackConfig.SpawnOffset : new Vector3(0f, 1.2f, 0.8f));
            Quaternion spawnRot = transform.rotation;

            if (basicAttackConfig != null && !string.IsNullOrEmpty(basicAttackConfig.SpawnPointName))
            {
                Transform spawnPoint = GetBone(basicAttackConfig.SpawnPointName);
                if (spawnPoint != null)
                {
                    spawnPos = spawnPoint.position;
                    spawnRot = spawnPoint.rotation;
                }
            }

            if (target != null && target.Transform != null)
            {
                Vector3 dir = (target.Transform.position + Vector3.up * 1f - spawnPos).normalized;
                if (dir != Vector3.zero)
                {
                    spawnRot = Quaternion.LookRotation(dir);
                }
            }

            string poolKey = basicAttackConfig != null
                ? basicAttackConfig.ProjectilePoolKey
                : "BasicAttack_Melee_Invisible";

            var poolManager = ServiceLocator.Get<PoolManager>();
            BasicAttackProjectile projectileObj = null;

            if (poolManager != null)
            {
                projectileObj = poolManager.Spawn<BasicAttackProjectile>(poolKey, spawnPos, spawnRot);
            }

            if (projectileObj != null)
            {
                projectileObj.Setup(this, target, finalDamage, basicAttackConfig);
            }
            else
            {
                if (target != null && target.Transform != null)
                {
                    bool isCrit = UnityEngine.Random.value <= 0.2f;
                    EventBus.Publish(new DamageDealtEvent(gameObject, target.Transform.gameObject, finalDamage,
                        basicAttackConfig != null ? basicAttackConfig.Element : GameElement.Physical,
                        target.Transform.position, isCrit));
                }
            }
        }

        public Transform GetBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName)) return null;

            if (_cachedBones.TryGetValue(boneName, out Transform bone))
            {
                if (bone != null) return bone;
            }

            bone = FindChildRecursive(transform, boneName);
            if (bone != null)
            {
                _cachedBones[boneName] = bone;
            }
            return bone;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (target == null)
            {
                if (TryGetComponent<PlayerTargetingSystem>(out var pts) && pts.CurrentTarget != null &&
                    !pts.CurrentTarget.IsDead)
                {
                    target = pts.CurrentTarget.Transform.gameObject;
                }
            }

            AbilitySO skill = GetAbility(skillIndex);
            if (skill == null)
            {
                Debug.LogWarning(
                    $"[HeroInstance] {HeroId} không có kỹ năng ở slot {skillIndex} (chưa gán vào danh sách Abilities)!");
                return;
            }

            if (!CanUseSkill(skillIndex))
            {
                Debug.LogWarning(
                    $"[HeroInstance] {HeroId} không thể dùng {skill.AbilityName} (Chưa đủ Nộ hoặc đang Cooldown: {GetSkillRemainingCooldown(skillIndex):F1}s)");
                return;
            }

            // Rotate towards target
            if (target != null)
            {
                Vector3 lookDirection = target.transform.position;
                lookDirection.y = transform.position.y;
                Vector3 dir = (lookDirection - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // Tiêu hao nộ và ghi nhận thời gian hồi chiêu
            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - skill.EnergyCost);
            _skillLastUseTimes[skillIndex] = Time.time;

            Debug.Log(
                $"[HeroInstance] {HeroId} tiêu hao {skill.EnergyCost} Nộ thi triển {skill.AbilityName}. Nộ còn lại: {CurrentEnergy}/{maxEnergyLimit}");

            var castCommand = new CastAbilityCommand(
                caster: this,
                abilitySO: skill,
                stateMachine: _skillStateMachine,
                target: target,
                targetPoint: target != null ? target.transform.position : transform.position + transform.forward * 2f
            );

            castCommand.Execute();
        }

        public void UseSkill(int skillIndex, GameObject target, Vector3 targetPoint)
        {
            AbilitySO skill = GetAbility(skillIndex);
            if (skill == null)
            {
                Debug.LogWarning($"[HeroInstance] {HeroId} không có kỹ năng ở slot {skillIndex}!");
                return;
            }

            if (!CanUseSkill(skillIndex))
            {
                Debug.LogWarning(
                    $"[HeroInstance] {HeroId} không thể dùng {skill.AbilityName} (Chưa đủ Nộ hoặc đang Cooldown: {GetSkillRemainingCooldown(skillIndex):F1}s)");
                return;
            }

            // Rotate towards targetPoint or target
            Vector3 lookDirection = target != null ? target.transform.position : targetPoint;
            lookDirection.y = transform.position.y;
            Vector3 dir = (lookDirection - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }

            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - skill.EnergyCost);
            _skillLastUseTimes[skillIndex] = Time.time;

            Debug.Log(
                $"[HeroInstance] {HeroId} tiêu hao {skill.EnergyCost} Nộ thi triển {skill.AbilityName}. Nộ còn lại: {CurrentEnergy}/{maxEnergyLimit}");

            var castCommand = new CastAbilityCommand(
                caster: this,
                abilitySO: skill,
                stateMachine: _skillStateMachine,
                target: target,
                targetPoint: targetPoint
            );

            castCommand.Execute();
        }

        public LayerMask GetTargetLayerMask()
        {
            if (TryGetComponent<PlayerTargetingSystem>(out var pts) && pts.EnemyLayerMask.value != 0)
            {
                return pts.EnemyLayerMask;
            }

            if (TryGetComponent<HeroAIController>(out var aiCtrl) && aiCtrl.EnemyLayerMask.value != 0)
            {
                return aiCtrl.EnemyLayerMask;
            }

            if (TryGetComponent<EnemyBotController>(out var botCtrl) && botCtrl.TargetLayerMask.value != 0)
            {
                return botCtrl.TargetLayerMask;
            }

            return (LayerMask)0;
        }

        #region --- IEntityController ---

        public void Move(Vector2 direction)
        {
            _currentMoveInput = direction;
        }

        public void StopMove()
        {
            _currentMoveInput = Vector2.zero;
        }

        public void Attack()
        {
            Attack(null);
        }

        public void UseSkill(int skillIndex)
        {
            UseSkill(skillIndex, null);
        }

        public void Jump()
        {
            if (characterController == null || !characterController.isGrounded || _isJumping || _isDashing) return;

            _isJumping = true;
            _velocityY = jumpForce;
            animatorController?.PlayJump();
        }

        public void Dash()
        {
            if (_isDashing || IsDead) return;

            // Hướng dash: theo input nếu có, nếu không thì theo forward
            Vector3 dashDir = Vector3.zero;
            if (_currentMoveInput.sqrMagnitude > 0.01f && _mainCameraTransform != null)
            {
                Vector3 camFwd = _mainCameraTransform.forward;
                camFwd.y = 0f;
                camFwd.Normalize();
                Vector3 camRight = _mainCameraTransform.right;
                camRight.y = 0f;
                camRight.Normalize();
                dashDir = (camFwd * _currentMoveInput.y + camRight * _currentMoveInput.x).normalized;
            }
            else
            {
                dashDir = transform.forward;
            }

            animatorController?.PlayRoll();
            StartCoroutine(DashCoroutine(dashDir));
        }

        private IEnumerator DashCoroutine(Vector3 dashDir)
        {
            _isDashing = true;
            _isDashInvincible = true;

            // Tạm thời vô hiệu input thường
            var savedInput = _currentMoveInput;
            _currentMoveInput = Vector2.zero;

            Vector3 startPos = transform.position;
            Vector3 previousPos = startPos;
            float elapsed = 0f;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashDuration;
                // Ease-out curve: bắt đầu nhanh, dần chậm
                float curveT = 1f - (1f - t) * (1f - t);
                Vector3 dest = startPos + dashDir * (dashDistance * curveT);
                Vector3 delta = dest - previousPos;
                characterController?.Move(delta);
                previousPos = transform.position;
                yield return null;
            }

            // Hết thời gian invincibility còn lại sau khi kết thúc dash movement
            float invincRemaining = dashInvincibilityDuration - dashDuration;
            if (invincRemaining > 0f)
                yield return new WaitForSeconds(invincRemaining);

            _isDashInvincible = false;
            _isDashing = false;
            _currentMoveInput = savedInput;
        }

        #endregion

        #region --- Animation Event Callbacks ---

        /// <summary>Được gọi bởi HeroAnimatorController khi AnimationEvent 'Transition' kích hoạt.</summary>
        public void NotifyAnimationTransition()
        {
            _skillStateMachine?.OnAnimationTransitionEvent();
            if (_isPendingBasicAttack)
            {
                ExecutePendingBasicAttack();
            }
        }

        /// <summary>Được gọi bởi HeroAnimatorController khi AnimationEvent 'Enter' kích hoạt.</summary>
        public void NotifyAnimationEnter() => _skillStateMachine?.OnAnimationEnterEvent();

        /// <summary>Được gọi bởi HeroAnimatorController khi AnimationEvent 'Exit' kích hoạt.</summary>
        public void NotifyAnimationExit() => _skillStateMachine?.OnAnimationExitEvent();

        #endregion
    }
}