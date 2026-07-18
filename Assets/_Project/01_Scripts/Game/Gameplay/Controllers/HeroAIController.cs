using Dylanng.Core;
using Dylanng.Core.State;
using Dylanng.Services;
using Game.Gameplay.Controllers.States;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public class HeroAIController : MonoBehaviour
    {
        [Header("--- AI COMBAT CONFIG ---")]
        [SerializeField] private float detectionRadius = 10.0f;
        [SerializeField] private float attackRange = 2.0f;
        [SerializeField] private float loseTargetRadius = 15.0f;
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private float scanInterval = 0.5f;

        public float DetectionRadius => detectionRadius;
        public float AttackRange => attackRange;
        public float LoseTargetRadius => loseTargetRadius;
        public LayerMask EnemyLayerMask => enemyLayerMask;

        private StateMachine _stateMachine;
        private AIFollowState _followState;
        private AICombatState _combatState;

        private float _scanTimer;
        private ITargetable _currentTarget;
        public ITargetable CurrentTarget => _currentTarget;

        public bool IsInCombatState => _stateMachine != null && _stateMachine.CurrentState == _combatState;

        public void Initialize(IEntityController entityController, HeroInstance heroInstance)
        {
            _stateMachine = new StateMachine();
            
            _followState = new AIFollowState(entityController, heroInstance);
            _combatState = new AICombatState(entityController, heroInstance, this);

            _stateMachine.Initialize(_followState);
            _scanTimer = 0f;
        }

        private void Update()
        {
            var partyManager = ServiceLocator.Get<PartyCombatManager>();
            var inputManager = ServiceLocator.Get<InputManager>();
            
            bool isUserMoving = inputManager != null && inputManager.MovementInput.sqrMagnitude > 0.01f;
            bool isFollowEnabled = partyManager != null && partyManager.IsFollowModeEnabled;

            if (partyManager != null)
            {
                // Teleport logic: Teleport far away heroes back near the active hero (regardless of toggle state)
                var activeHero = partyManager.GetActiveHero();
                if (activeHero != null && activeHero.transform != transform)
                {
                    float distance = Vector3.Distance(transform.position, activeHero.transform.position);
                    if (distance > 15.0f)
                    {
                        var cc = GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        
                        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 2f;
                        randomOffset.y = 0;
                        transform.position = activeHero.transform.position + randomOffset;
                        
                        if (cc != null) cc.enabled = true;
                        
                        GetComponent<IEntityController>()?.StopMove();
                    }
                }
            }

            // Ở chế độ độc lập (isFollowEnabled == false), Active Hero không chạy AI combat hay tự động bắn
            var myHeroInstance = GetComponent<HeroInstance>();
            bool isActiveHero = partyManager != null && partyManager.GetActiveHero() == myHeroInstance;
            if (isActiveHero && !isFollowEnabled)
            {
                if (_stateMachine != null && _stateMachine.CurrentState != _followState)
                {
                    FollowActiveHero();
                }
                _stateMachine?.Update();
                return;
            }

            // 1. Khi user đang di chuyển bằng joystick trong chế độ follow -> không tấn công (quay về FollowState ngay lập tức)
            if (isUserMoving && isFollowEnabled)
            {
                if (_stateMachine != null && _stateMachine.CurrentState != _followState)
                {
                    FollowActiveHero();
                }
                _stateMachine?.Update();
                return;
            }

            // 2. Khi user dừng di chuyển joystick -> tiến hành quét mục tiêu và chiến đấu cho cả Active Hero lẫn Follower
            if (_stateMachine != null && _stateMachine.CurrentState == _followState)
            {
                _scanTimer -= Time.deltaTime;
                if (_scanTimer <= 0f)
                {
                    _scanTimer = scanInterval;
                    ScanForNearestTarget();
                }
            }

            _stateMachine?.Update();
        }

        private void ScanForNearestTarget()
        {
            Collider[] colliders = enemyLayerMask.value != 0 
                ? Physics.OverlapSphere(transform.position, detectionRadius, enemyLayerMask)
                : Physics.OverlapSphere(transform.position, detectionRadius);

            ITargetable nearestTarget = null;
            float minSqrDist = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.IsDead || targetable.Transform == transform) continue;

                    float sqrDist = (targetable.Transform.position - transform.position).sqrMagnitude;
                    if (sqrDist < minSqrDist && sqrDist <= detectionRadius * detectionRadius)
                    {
                        minSqrDist = sqrDist;
                        nearestTarget = targetable;
                    }
                }
            }

            if (nearestTarget != null)
            {
                SetTargetEnemy(nearestTarget);
            }
        }

        public void SetTargetEnemy(Transform enemy, bool broadcast = true)
        {
            if (enemy != null && enemy.TryGetComponent<ITargetable>(out var targetable))
            {
                SetTargetEnemy(targetable, broadcast);
            }
        }

        public void SetTargetEnemy(ITargetable target, bool broadcast = true)
        {
            var partyManager = ServiceLocator.Get<PartyCombatManager>();
            var myHeroInstance = GetComponent<HeroInstance>();
            bool isActiveHero = partyManager != null && partyManager.GetActiveHero() == myHeroInstance;
            bool isFollowEnabled = partyManager != null && partyManager.IsFollowModeEnabled;

            if (isActiveHero && !isFollowEnabled)
            {
                return;
            }

            if (target != null && !target.IsDead)
            {
                if (_currentTarget == target && _stateMachine.CurrentState == _combatState) return;

                _currentTarget = target;
                target.OnTargeted(true);
                _combatState?.SetTarget(target);
                _stateMachine.TransitionTo(_combatState);

                if (broadcast)
                {
                    partyManager?.BroadcastTargetToParty(target, myHeroInstance);
                }
            }
        }

        public void FollowActiveHero()
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnTargeted(false);
                _currentTarget = null;
            }
            _stateMachine.TransitionTo(_followState);
        }
    }
}
