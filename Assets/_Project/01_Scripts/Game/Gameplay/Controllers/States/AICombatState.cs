using Dylanng.Core;
using Dylanng.Core.State;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers.States
{
    public class AICombatState : StateBase
    {
        private readonly IEntityController _entityController;
        private readonly HeroInstance _heroInstance;
        private readonly HeroAIController _aiController;
        private ITargetable _target;
        private float _attackCooldownTimer;

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
            _attackCooldownTimer = 0f;
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

            // 1. Kiểm tra tính hợp lệ của mục tiêu
            if (_target == null || _target.IsDead || _target.Transform == null)
            {
                _aiController.FollowActiveHero();
                return;
            }

            Vector3 heroPos = _heroInstance.transform.position;
            Vector3 targetPos = _target.Transform.position;
            float distance = Vector3.Distance(heroPos, targetPos);

            // 2. Nếu mục tiêu đi ra quá xa khỏi tầm bỏ theo dõi (LoseTargetRadius)
            if (distance > _aiController.LoseTargetRadius)
            {
                Debug.Log($"[AICombatState] Mục tiêu đi quá xa ({distance:F1}m > {_aiController.LoseTargetRadius}m). Quay lại Follow.");
                _aiController.FollowActiveHero();
                return;
            }

            // 3. Xử lý di chuyển và khoảng cách tầm đánh
            if (distance > _aiController.AttackRange)
            {
                Vector3 direction = (targetPos - heroPos).normalized;
                
                _entityController.MoveInWorldSpace(direction);
            }
            else
            {
                // Đã vào tầm đánh -> Dừng di chuyển và xoay người hướng về mục tiêu
                _entityController.StopMove();

                Vector3 lookDir = (targetPos - heroPos);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    _heroInstance.transform.rotation = Quaternion.Slerp(_heroInstance.transform.rotation, targetRot, Time.deltaTime * 15f);
                }

                // 4. Xử lý chu kỳ tấn công và tung kỹ năng
                _attackCooldownTimer -= Time.deltaTime;
                if (_attackCooldownTimer <= 0f)
                {
                    if (_heroInstance.CanUseSkill())
                    {
                        Debug.Log($"[AICombatState] {_heroInstance.HeroId} đủ Nộ! Tự động thi triển kỹ năng lên {_target.Transform.name}");
                        _heroInstance.UseSkill(0, _target.Transform.gameObject);
                        _attackCooldownTimer = _heroInstance.GetSkillCastDuration();
                    }
                    else
                    {
                        _heroInstance.Attack(_target);
                        _heroInstance.GainEnergy(15f); // Tích +15 Energy mỗi đòn đánh thường trúng
                        _attackCooldownTimer = _heroInstance.GetAttackCooldown(); // Cooldown giữa các đòn đánh thường lấy từ cấu hình Hero
                    }
                }
            }
        }
    }
}
