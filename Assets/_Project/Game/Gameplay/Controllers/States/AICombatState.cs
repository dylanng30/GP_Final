using Dylanng;
using Game.Combat;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers.States
{
    /// <summary>Đuổi mục tiêu, giữ tầm đánh, ưu tiên ultimate rồi đến skill, cuối cùng là đánh thường.</summary>
    public class AICombatState : StateBase
    {
        private const float TurnSpeed = 15f;

        // Ultimate đứng đầu vì chỉ dùng được khi đủ Nộ.
        private static readonly int[] SkillPriority = { AbilitySlot.Ultimate, AbilitySlot.Skill2, AbilitySlot.Skill1 };

        private readonly IEntityController _entityController;
        private readonly HeroInstance _heroInstance;
        private readonly HeroAIController _aiController;
        private ITargetable _target;
        private float _actionCooldownTimer;

        public AICombatState(IEntityController entityController, HeroInstance heroInstance, HeroAIController aiController)
        {
            _entityController = entityController;
            _heroInstance = heroInstance;
            _aiController = aiController;
        }

        public void SetTarget(ITargetable target)
        {
            _target = target;
        }

        public override void Enter()
        {
            base.Enter();
            _actionCooldownTimer = 0f;
            if (_target == null && _aiController.CurrentTarget != null)
            {
                _target = _aiController.CurrentTarget;
            }
        }

        public override void Exit()
        {
            base.Exit();
            _entityController.StopMove();
            if (_target != null)
            {
                _target.OnTargeted(false);
                _target = null;
            }
        }

        public override void Update()
        {
            base.Update();

            if (ShouldDisengage())
            {
                _aiController.FollowActiveHero();
                return;
            }

            Vector3 heroPosition = _heroInstance.transform.position;
            Vector3 targetPosition = _target.Transform.position;

            if (Vector3.Distance(heroPosition, targetPosition) > _aiController.AttackRange)
            {
                _entityController.MoveInWorldSpace((targetPosition - heroPosition).normalized);
                return;
            }

            _entityController.StopMove();
            FaceTarget(targetPosition - heroPosition);
            TickActions();
        }

        private bool ShouldDisengage()
        {
            if (_target == null || _target.IsDead || _target.Transform == null) return true;

            float distance = Vector3.Distance(_heroInstance.transform.position, _target.Transform.position);
            return distance > _aiController.LoseTargetRadius;
        }

        private void FaceTarget(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f) return;

            Transform hero = _heroInstance.transform;
            hero.rotation = Quaternion.Slerp(hero.rotation, Quaternion.LookRotation(direction), Time.deltaTime * TurnSpeed);
        }

        private void TickActions()
        {
            _actionCooldownTimer -= Time.deltaTime;
            if (_actionCooldownTimer > 0f) return;

            foreach (int slot in SkillPriority)
            {
                if (!_heroInstance.CanUseSkill(slot)) continue;

                _heroInstance.UseSkill(slot, _target.Transform.gameObject);
                _actionCooldownTimer = _heroInstance.GetSkillCastDuration(slot);
                return;
            }

            _heroInstance.Attack(_target);
            _actionCooldownTimer = _heroInstance.GetAttackCooldown();
        }
    }
}
