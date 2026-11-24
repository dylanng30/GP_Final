using System.Collections.Generic;
using Game.Combat.Stats;
using Game.Combat;
using Game.Gameplay.AI.Core;
using Game.Gameplay.AI.Gambit;
using Game.Gameplay.AI.GOAP;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.AI.Controllers
{
    [RequireComponent(typeof(HeroInstance))]
    public class EnemyBotController : MonoBehaviour, IAIContext
    {
        [Header("--- AI CONFIGURATION ---")]
        [SerializeField] private List<GambitRuleSO> gambitRules;
        [SerializeField] private List<GOAPGoalSO> goapGoals;
        [SerializeField] private List<GOAPActionBase> availableActions;

        [Header("--- TARGETING & LEASH ---")]
        [SerializeField] private float detectionRadius = 12.0f;
        [SerializeField] private float spellRange = 8.0f;
        [SerializeField] private float maxDistanceFromSpawn = 15.0f;
        [SerializeField] private LayerMask targetLayerMask;

        private HeroInstance _heroInstance;
        private IEntityController _entityController;
        private AIAnimationBridge _animBridge;
        private StatController _statController;
        private ITargetable _currentTarget;
        private AIWorldState _worldState = new AIWorldState(0);
        private readonly AIBrain _aiBrain = new AIBrain();
        private Vector3 _spawnPosition;

        private readonly Dictionary<int, float> _skillCooldownTimers = new Dictionary<int, float>();
        private readonly List<ITargetable> _scanResults = new List<ITargetable>();
        private float _scanTimer;
        private const float SCAN_INTERVAL = 0.25f;

        public IEntityController EntityController => _entityController;
        public HeroInstance EntityInstance => _heroInstance;
        public ITargetable CurrentTarget => _currentTarget;
        public LayerMask TargetLayerMask => targetLayerMask;
        
        public Vector3 Position => transform.position;
        public Vector3 TargetPosition => _currentTarget != null ? _currentTarget.Transform.position : transform.position;
        public Vector3 SpawnPosition => _spawnPosition;
        
        public float DistanceToTarget => _currentTarget != null ? Vector3.Distance(transform.position, _currentTarget.Transform.position) : float.MaxValue;
        public float DistanceToSpawn => Vector3.Distance(transform.position, _spawnPosition);
        public float MaxDistanceFromSpawn => maxDistanceFromSpawn;
        public float HealthPercentage => _heroInstance != null ? _heroInstance.Health.Ratio : 1f;

        public AIWorldState WorldState
        {
            get => _worldState;
            set => _worldState = value;
        }

        protected virtual void Awake()
        {
            _spawnPosition = transform.position;
            _heroInstance = GetComponent<HeroInstance>();
            _entityController = GetComponent<IEntityController>();
            _animBridge = GetComponent<AIAnimationBridge>();
            _statController = GetComponent<StatController>() ?? GetComponentInChildren<StatController>();
        }

        protected virtual void OnEnable()
        {
            _aiBrain.Initialize(this, gambitRules, goapGoals, availableActions);
        }

        protected virtual void OnDisable()
        {
            _aiBrain.UnregisterFromTickSystem();
        }

        protected virtual void Update()
        {
            if (_aiBrain != null)
            {
                if (!_aiBrain.IsRegisteredToTickSystem)
                {
                    _aiBrain.RegisterToTickSystem();
                    if (!_aiBrain.IsRegisteredToTickSystem)
                    {
                        _aiBrain.OnUpdate(Time.deltaTime);
                    }
                }
            }
        }

        public bool TrySetTarget(ITargetable newTarget)
        {
            if (newTarget == null || newTarget.IsDead) return false;
            _currentTarget = newTarget;
            _worldState.SetState(AIWorldStateKey.TargetExists, true);
            bool inMelee = DistanceToTarget <= 2.0f;
            _worldState.SetState(AIWorldStateKey.TargetInMeleeRange, inMelee);
            _worldState.SetState(AIWorldStateKey.TargetInSpellRange, DistanceToTarget <= spellRange);
            return true;
        }

        /// <summary>Bỏ mục tiêu hiện tại, ví dụ khi quái chết hoặc hồi sinh.</summary>
        public void ClearTarget()
        {
            _currentTarget = null;
            _worldState.SetState(AIWorldStateKey.TargetExists, false);
            _worldState.SetState(AIWorldStateKey.TargetInMeleeRange, false);
            _worldState.SetState(AIWorldStateKey.TargetInSpellRange, false);
        }

        public void ScanAndValidateTarget(float deltaTime)
        {
            _scanTimer -= deltaTime;
            _worldState.SetState(AIWorldStateKey.IsAtSpawn, DistanceToSpawn <= 0.5f);
            _worldState.SetState(AIWorldStateKey.IsStaggered, !_heroInstance.Status.CanAct);

            if (_currentTarget != null)
            {
                bool isOutsideLeash = maxDistanceFromSpawn > 0f && 
                    (Vector3.Distance(_spawnPosition, _currentTarget.Transform.position) > maxDistanceFromSpawn ||
                     Vector3.Distance(_spawnPosition, transform.position) > maxDistanceFromSpawn);

                if (_currentTarget.IsDead || 
                    _currentTarget.Transform == null || 
                    Vector3.Distance(transform.position, _currentTarget.Transform.position) > detectionRadius * 1.5f || 
                    isOutsideLeash)
                {
                    ClearTarget();
                }
                else
                {
                    _worldState.SetState(AIWorldStateKey.TargetExists, true);
                    bool inMelee = DistanceToTarget <= 2.0f;
                    _worldState.SetState(AIWorldStateKey.TargetInMeleeRange, inMelee);
                    _worldState.SetState(AIWorldStateKey.TargetInSpellRange, DistanceToTarget <= spellRange);
                }
            }

            if (_currentTarget == null)
            {
                if (_scanTimer <= 0f)
                {
                    _scanTimer = SCAN_INTERVAL;
                    ITargetable nearest = FindNearestTarget(targetLayerMask.value != 0 ? targetLayerMask : (LayerMask)(-1));

                    if (nearest != null)
                    {
                        TrySetTarget(nearest);
                    }
                }
            }
        }

        private ITargetable FindNearestTarget(LayerMask mask)
        {
            TargetQuery.Collect(transform.position, detectionRadius, mask, transform, _scanResults);

            ITargetable nearest = null;
            float minSqrDist = float.MaxValue;

            foreach (ITargetable candidate in _scanResults)
            {
                Vector3 candidatePosition = candidate.Transform.position;
                bool isOutsideLeash = maxDistanceFromSpawn > 0f
                                      && Vector3.Distance(_spawnPosition, candidatePosition) > maxDistanceFromSpawn;
                if (isOutsideLeash) continue;

                float sqrDist = (candidatePosition - transform.position).sqrMagnitude;
                if (sqrDist >= minSqrDist) continue;

                minSqrDist = sqrDist;
                nearest = candidate;
            }
            return nearest;
        }

        public bool CanCastBossSkill(int skillIndex)
        {
            if (_skillCooldownTimers.TryGetValue(skillIndex, out float expiryTime))
            {
                return Time.time >= expiryTime;
            }
            return true;
        }

        public void StartBossSkillCooldown(int skillIndex, float cooldownDuration)
        {
            _skillCooldownTimers[skillIndex] = Time.time + cooldownDuration;
        }
    }
}
