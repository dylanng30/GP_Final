using System.Collections.Generic;
using Dylanng;
using Game.Combat;
using Game.Combat.Domain;
using Game.Gameplay.Controllers.States;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public class HeroAIController : MonoBehaviour
    {
        private const float RegroupDistance = 15f;
        private const float RegroupSpreadRadius = 2f;
        private const float OvercomesTargetWeight = 0.7f;
        private const float OvercomedByTargetWeight = 1.3f;

        private readonly List<ITargetable> _scanResults = new List<ITargetable>();

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

        private HeroInstance _heroInstance;
        private StateMachine _stateMachine;
        private AIFollowState _followState;
        private AICombatState _combatState;

        private float _scanTimer;
        private ITargetable _currentTarget;
        public ITargetable CurrentTarget => _currentTarget;

        public bool IsInCombatState => _stateMachine != null && _stateMachine.CurrentState == _combatState;

        public void Initialize(IEntityController entityController, HeroInstance heroInstance)
        {
            _heroInstance = heroInstance;
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

            HeroInstance leader = partyManager != null ? partyManager.GetActiveHero() : null;
            if (leader != null && leader.transform != transform
                               && Vector3.Distance(transform.position, leader.transform.position) > RegroupDistance)
            {
                RegroupTo(leader.transform.position);
            }

            // Ở chế độ độc lập (isFollowEnabled == false), Active Hero không chạy AI combat hay tự động bắn
            HeroInstance myHeroInstance = _heroInstance;
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
            TargetQuery.Collect(transform.position, detectionRadius, enemyLayerMask, transform, _scanResults);

            ITargetable bestTarget = null;
            float bestScore = float.MaxValue;

            foreach (ITargetable candidate in _scanResults)
            {
                float score = ScoreTarget(candidate);
                if (score >= bestScore) continue;

                bestScore = score;
                bestTarget = candidate;
            }

            if (bestTarget != null)
            {
                SetTargetEnemy(bestTarget);
            }
        }

        /// <summary>Điểm càng thấp càng ưu tiên: gần hơn và khắc được hệ của mục tiêu.</summary>
        private float ScoreTarget(ITargetable candidate)
        {
            float distance = Vector3.Distance(transform.position, candidate.Transform.position);

            var resolver = ServiceLocator.Get<CombatResolver>();
            HeroInstance hero = _heroInstance;
            if (resolver == null || hero == null) return distance;

            switch (resolver.GetRelation(hero.Element, candidate.Element))
            {
                case ElementRelation.Overcomes: return distance * OvercomesTargetWeight;
                case ElementRelation.OvercomedBy: return distance * OvercomedByTargetWeight;
                default: return distance;
            }
        }

        /// <summary>Dịch chuyển về gần một điểm, dùng khi tụt lại quá xa hoặc khi người chơi ra lệnh tập hợp.</summary>
        public void RegroupTo(Vector3 point)
        {
            var characterController = GetComponent<CharacterController>();
            if (characterController != null) characterController.enabled = false;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * RegroupSpreadRadius;
            transform.position = point + new Vector3(offset.x, 0f, offset.y);

            if (characterController != null) characterController.enabled = true;

            GetComponent<IEntityController>()?.StopMove();
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
            HeroInstance myHeroInstance = _heroInstance;
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
