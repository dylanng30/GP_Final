using Dylanng;
using Game.Combat.Abilities.Events;
using Game.Animation.Hero;
using Game.Combat;
using UnityEngine;

namespace Game.Combat.Abilities.States
{
    /// <summary>Thi triển một skill: chạy animation, kích hoạt effect tại animation event hoặc khi hết thời gian cast.</summary>
    public class AbilityCastState : StateBase
    {
        private readonly MonoBehaviourBase _caster;
        private readonly AbilitySO _ability;
        private readonly AbilityContext _context;

        private float _timer;
        private bool _hasTriggeredEffects;
        private HeroAnimatorController _slowedHero;
        private Animator _slowedAnimator;

        public bool IsFinished { get; private set; }
        public bool LocksMovement => _ability.LocksMovement;

        public AbilityCastState(MonoBehaviourBase caster, AbilitySO ability, in AbilityContext context)
        {
            _caster = caster;
            _ability = ability;
            _context = context;
        }

        public override void Enter()
        {
            base.Enter();
            PlayAnimation();
            ApplyAnimationSpeed();
            EventBus.Publish(new AbilityCastStartedEvent(_caster.gameObject, _ability));
            PublishTelegraph();
        }

        private void PublishTelegraph()
        {
            if (_ability.IndicatorType != SkillIndicatorType.AreaTarget || _ability.IndicatorRadius <= 0f) return;
            if (_ability.TotalCastDuration <= 0f) return;

            Vector3 point = _context.Point;
            if (point == Vector3.zero)
            {
                point = _context.Target != null ? _context.Target.transform.position : _caster.transform.position;
            }

            EventBus.Publish(new AbilityTelegraphEvent(
                _caster.gameObject, _ability, point, _ability.IndicatorRadius, _ability.TotalCastDuration));
        }

        public override void Update()
        {
            base.Update();
            if (IsFinished) return;

            _timer += Time.deltaTime;
            if (_timer < _ability.TotalCastDuration) return;

            TriggerEffects();
            IsFinished = true;
            RestoreAnimationSpeed();
        }

        public override void Exit()
        {
            base.Exit();
            RestoreAnimationSpeed();
        }

        public override void OnAnimationTransitionEvent()
        {
            base.OnAnimationTransitionEvent();
            if (!IsFinished) TriggerEffects();
        }

        /// <summary>Hủy cast. Effect đã kích hoạt thì không hoàn tác.</summary>
        public void Cancel()
        {
            IsFinished = true;
            RestoreAnimationSpeed();
        }

        /// <summary>Chạy chậm animation trong pha tụ lực để animation event rơi đúng lúc skill bắn ra.</summary>
        private void ApplyAnimationSpeed()
        {
            if (Mathf.Approximately(_ability.AnimationSpeed, 1f) || _ability.AnimationSpeed <= 0f) return;

            // Hero: chỉ chậm state skill qua parameter ActionSpeed, Locomotion và đánh thường không bị ảnh hưởng.
            _slowedHero = _caster.GetComponentInChildren<HeroAnimatorController>();
            if (_slowedHero != null)
            {
                _slowedHero.SetActionSpeed(_ability.AnimationSpeed);
                return;
            }

            _slowedAnimator = _caster.GetComponentInChildren<Animator>();
            if (_slowedAnimator != null) _slowedAnimator.speed = _ability.AnimationSpeed;
        }

        private void RestoreAnimationSpeed()
        {
            if (_slowedHero != null)
            {
                _slowedHero.SetActionSpeed(1f);
                _slowedHero = null;
            }

            if (_slowedAnimator == null) return;

            _slowedAnimator.speed = 1f;
            _slowedAnimator = null;
        }

        private void PlayAnimation()
        {
            int animationHash = _ability.GetAnimationHash();
            if (animationHash == 0) return;

            // Animator nằm trên model con (Model/<mesh>), không nằm trên root của caster.
            var animatorController = _caster.GetComponentInChildren<HeroAnimatorController>();
            if (animatorController != null)
            {
                animatorController.PlaySkillByHash(animationHash);
                return;
            }

            var animator = _caster.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetTrigger(animationHash);
        }

        private void TriggerEffects()
        {
            if (_hasTriggeredEffects || _caster == null) return;

            _hasTriggeredEffects = true;
            // Phát bắn: phần giật súng còn lại chạy tốc độ thường cho có lực.
            RestoreAnimationSpeed();
            _ability.ExecuteEffects(_context);
            EventBus.Publish(new AbilityExecutedEvent(_caster.gameObject, _ability, _context.Point));
        }
    }
}
