using Dylanng.Core.Base;
using Dylanng.Core;
using Dylanng.Core.Pooling;
using Dylanng.Core.State;
using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Stats;
using Dylanng.Gameplay.RPG.Inventory;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.BasicAttack;
using Dylanng.Gameplay.RPG.Abilities.Commands;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Data.Item;
using Game.Gameplay.Controllers;
using System.Collections.Generic;
using Game.Managers.Hero;
using Game.Managers.Item;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    [RequireComponent(typeof(CharacterController))]
    public class HeroInstance : MonoBehaviourBase, IEntityController
    {
        [Header("--- MOVEMENT SETTINGS ---")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f;
        [SerializeField] private float _gravity = -15f;
        
        [Header("--- UX ---")]
        [SerializeField] private GameObject controlledCircleSign;

        [Header("--- COMBAT & ABILITIES ---")]
        [SerializeField] private float maxEnergyLimit = 200f; // Giới hạn tối đa không được vượt quá 200 Nộ
        [SerializeField] private BasicAttackSO basicAttackConfig;
        [SerializeField] private List<AbilitySO> abilities = new List<AbilitySO>();

        private CharacterController _characterController;
        private Transform _mainCameraTransform;
        private StateMachine _skillStateMachine;

        private Vector2 _currentMoveInput;
        private float _velocityY;
        private float _lastAttackTime = -999f;
        private Dictionary<int, float> _skillLastUseTimes = new Dictionary<int, float>();

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
        public float CurrentEnergy { get; private set; }
        public float MaxEnergyLimit => maxEnergyLimit;

        private PlayerInputController _playerController;
        private HeroAIController _aiController;

        protected override void Awake()
        {
            base.Awake();
            _playerController = GetComponent<PlayerInputController>();
            _aiController = GetComponent<HeroAIController>();
            _characterController = GetComponent<CharacterController>();
            _skillStateMachine = new StateMachine();

            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
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

            _playerController.Initialize(this);
            _aiController.Initialize(this, this);

            // Default to AI
            SetAsActive(false);
            CurrentEnergy = 0f;
        }

        public void SetAsActive(bool isActive)
        {
            if (isActive)
            {
                _playerController.enabled = true;
                _aiController.enabled = true;
                if (controlledCircleSign != null) controlledCircleSign.SetActive(true);
            }
            else
            {
                if (controlledCircleSign != null) controlledCircleSign.SetActive(false);
                _playerController.enabled = false;
                _aiController.enabled = true;
                _aiController.FollowActiveHero();
            }
        }

        private void Update()
        {
            if (_characterController == null) return;

            ApplyMovementAndGravity();
            _skillStateMachine?.Update();
        }

        private void ApplyMovementAndGravity()
        {
            Vector3 moveDirection = Vector3.zero;
            
            if (_currentMoveInput.sqrMagnitude > 0.01f)
            {
                if (_mainCameraTransform != null)
                {
                    Vector3 cameraForward = _mainCameraTransform.forward;
                    Vector3 cameraRight = _mainCameraTransform.right;

                    cameraForward.y = 0f;
                    cameraRight.y = 0f;

                    cameraForward.Normalize();
                    cameraRight.Normalize();

                    moveDirection = cameraForward * _currentMoveInput.y + cameraRight * _currentMoveInput.x;
                }
                else
                {
                    moveDirection = new Vector3(_currentMoveInput.x, 0f, _currentMoveInput.y);
                }

                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }

            if (_characterController.isGrounded && _velocityY < 0)
            {
                _velocityY = -2f;
            }

            _velocityY += _gravity * Time.deltaTime;

            Vector3 finalVelocity = moveDirection * _moveSpeed;
            finalVelocity.y = _velocityY;

            _characterController.Move(finalVelocity * Time.deltaTime);
        }

        #region --- COMBAT & ABILITIES API ---
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
                if (TryGetComponent<PlayerTargetingSystem>(out var pts) && pts.CurrentTarget != null && !pts.CurrentTarget.IsDead)
                {
                    target = pts.CurrentTarget;
                }
                else
                {
                    target = _aiController != null ? _aiController.CurrentTarget : null;
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

            if (TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger("Attack");
            }

            float atkDamage = Stats != null ? Stats.ATK : 50f;
            float damageMultiplier = basicAttackConfig != null ? basicAttackConfig.DamageMultiplier : 1.0f;
            float finalDamage = atkDamage * damageMultiplier;

            Vector3 spawnOffset = basicAttackConfig != null ? basicAttackConfig.SpawnOffset : new Vector3(0f, 1.2f, 0.8f);
            Vector3 spawnPos = transform.TransformPoint(spawnOffset);

            Quaternion spawnRot = transform.rotation;
            if (target != null && target.Transform != null)
            {
                Vector3 dir = (target.Transform.position + Vector3.up * 1f - spawnPos).normalized;
                if (dir != Vector3.zero)
                {
                    spawnRot = Quaternion.LookRotation(dir);
                }
            }

            string poolKey = basicAttackConfig != null ? basicAttackConfig.ProjectilePoolKey : "BasicAttack_Melee_Invisible";

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
                    EventBus.Publish(new DamageDealtEvent(gameObject, target.Transform.gameObject, finalDamage, basicAttackConfig != null ? basicAttackConfig.Element : ElementType.Physical, target.Transform.position, isCrit));
                }
            }
        }

        public void UseSkill(int skillIndex, GameObject target = null)
        {
            if (target == null)
            {
                if (TryGetComponent<PlayerTargetingSystem>(out var pts) && pts.CurrentTarget != null && !pts.CurrentTarget.IsDead)
                {
                    target = pts.CurrentTarget.Transform.gameObject;
                }
            }

            AbilitySO skill = GetAbility(skillIndex);
            if (skill == null)
            {
                Debug.LogWarning($"[HeroInstance] {HeroId} không có kỹ năng ở slot {skillIndex} (chưa gán vào danh sách Abilities)!");
                return;
            }

            if (!CanUseSkill(skillIndex))
            {
                Debug.LogWarning($"[HeroInstance] {HeroId} không thể dùng {skill.AbilityName} (Chưa đủ Nộ hoặc đang Cooldown: {GetSkillRemainingCooldown(skillIndex):F1}s)");
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

            Debug.Log($"[HeroInstance] {HeroId} tiêu hao {skill.EnergyCost} Nộ thi triển {skill.AbilityName}. Nộ còn lại: {CurrentEnergy}/{maxEnergyLimit}");

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
                Debug.LogWarning($"[HeroInstance] {HeroId} không thể dùng {skill.AbilityName} (Chưa đủ Nộ hoặc đang Cooldown: {GetSkillRemainingCooldown(skillIndex):F1}s)");
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

            Debug.Log($"[HeroInstance] {HeroId} tiêu hao {skill.EnergyCost} Nộ thi triển {skill.AbilityName}. Nộ còn lại: {CurrentEnergy}/{maxEnergyLimit}");

            var castCommand = new CastAbilityCommand(
                caster: this,
                abilitySO: skill,
                stateMachine: _skillStateMachine,
                target: target,
                targetPoint: targetPoint
            );

            castCommand.Execute();
        }
        #endregion

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
        }

        public void Dash()
        {
        }
        #endregion
    }
}
