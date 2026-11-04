using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Animation.Hero;
using Game.Combat;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.States
{
    /// <summary>Thi triển một skill: chạy animation, kích hoạt effect tại animation event hoặc khi hết thời gian cast.</summary>
    public class AbilityCastState : StateBase
    {
        private readonly MonoBehaviourBase _caster;
        private readonly AbilitySO _ability;
        private readonly AbilityContext _context;

        private float _timer;
        private bool _hasTriggeredEffects;

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
            EventBus.Publish(new AbilityCastStartedEvent(_caster.gameObject, _ability));
        }

        public override void Update()
        {
            base.Update();
            if (IsFinished) return;

            _timer += Time.deltaTime;
            if (_timer < _ability.TotalCastDuration) return;

            TriggerEffects();
            IsFinished = true;
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
        }

        private void PlayAnimation()
        {
            int animationHash = _ability.GetAnimationHash();
            if (animationHash == 0) return;

            if (_caster.TryGetComponent<HeroAnimatorController>(out var animatorController))
            {
                animatorController.PlaySkillByHash(animationHash);
            }
            else if (_caster.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(animationHash);
            }
        }

        private void TriggerEffects()
        {
            if (_hasTriggeredEffects || _caster == null) return;

            _hasTriggeredEffects = true;
            _ability.ExecuteEffects(_context);
            EventBus.Publish(new AbilityExecutedEvent(_caster.gameObject, _ability, _context.Point));
        }
    }
}
