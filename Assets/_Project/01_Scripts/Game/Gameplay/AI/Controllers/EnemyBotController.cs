using System.Collections.Generic;
using Dylanng.Gameplay.RPG.Stats;
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
        public float HealthPercentage
        {
            get
            {
                if (_statController == null) return 1f;
                float maxHp = _statController.GetStatValue(StatType.MaxHP);
                if (maxHp <= 0) return 1f;
                float curHp = maxHp;
                return curHp / maxHp;
            }
        }

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

        public void ScanAndValidateTarget(float deltaTime)
        {
            _scanTimer -= deltaTime;
            _worldState.SetState(AIWorldStateKey.IsAtSpawn, DistanceToSpawn <= 0.5f);

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
                    _currentTarget = null;
                    _worldState.SetState(AIWorldStateKey.TargetExists, false);
                    _worldState.SetState(AIWorldStateKey.TargetInMeleeRange, false);
                    _worldState.SetState(AIWorldStateKey.TargetInSpellRange, false);
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
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, mask);
            ITargetable nearest = null;
            float minSqrDist = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                ITargetable targetable = colliders[i].GetComponent<ITargetable>() ?? colliders[i].GetComponentInParent<ITargetable>();
                if (targetable != null)
                {
                    if (targetable.IsDead || targetable.Transform == transform || targetable.Transform == _heroInstance?.Transform) continue;
                    
                    if (maxDistanceFromSpawn > 0f && Vector3.Distance(_spawnPosition, targetable.Transform.position) > maxDistanceFromSpawn) continue;

                    float sqrDist = (targetable.Transform.position - transform.position).sqrMagnitude;
                    if (sqrDist < minSqrDist && sqrDist <= detectionRadius * detectionRadius)
                    {
                        minSqrDist = sqrDist;
                        nearest = targetable;
                    }
                }
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
