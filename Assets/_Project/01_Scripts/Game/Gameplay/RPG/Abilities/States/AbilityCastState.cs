using System;
using Dylanng.Core.Base;
using Dylanng.Core.State;
using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Abilities.Events;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.States
{
    public class AbilityCastState : StateBase
    {
        private readonly MonoBehaviourBase _caster;
        private readonly AbilitySO _abilitySO;
        private readonly GameObject _target;
        private readonly Vector3 _targetPoint;
        private readonly Action _onCompleted;

        private float _timer;
        private bool _hasTriggeredEffect;
        private bool _isFinished;

        public AbilityCastState(MonoBehaviourBase caster, AbilitySO abilitySO, GameObject target = null, Vector3 targetPoint = default, Action onCompleted = null)
        {
            _caster = caster;
            _abilitySO = abilitySO;
            _target = target;
            _targetPoint = targetPoint;
            _onCompleted = onCompleted;
        }

        public override void Enter()
        {
            base.Enter();
            _timer = 0f;
            _hasTriggeredEffect = false;
            _isFinished = false;

            if (_caster == null || _abilitySO == null)
            {
                _isFinished = true;
                _onCompleted?.Invoke();
                return;
            }

            if (_abilitySO.AnimationTriggerHash != 0 && _caster.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(_abilitySO.AnimationTriggerHash);
            }

            EventBus.Publish(new AbilityCastStartedEvent(_caster.gameObject, _abilitySO));
            Debug.Log($"[AbilityCastState] {_caster.name} bắt đầu thi triển {_abilitySO.AbilityName} (Thời gian: {_abilitySO.TotalCastDuration}s)");
        }

        public override void Update()
        {
            base.Update();
            if (_isFinished) return;

            _timer += Time.deltaTime;

            if (_timer >= _abilitySO.TotalCastDuration)
            {
                if (!_hasTriggeredEffect)
                {
                    TriggerAbilityEffects();
                }

                _isFinished = true;
                _onCompleted?.Invoke();
            }
        }

        public override void OnAnimationTransitionEvent()
        {
            base.OnAnimationTransitionEvent();
            if (!_hasTriggeredEffect && !_isFinished)
            {
                TriggerAbilityEffects();
            }
        }

        private void TriggerAbilityEffects()
        {
            _hasTriggeredEffect = true;
            if (_abilitySO != null && _caster != null)
            {
                _abilitySO.ExecuteEffects(_caster.gameObject, _target, _targetPoint);
                EventBus.Publish(new AbilityExecutedEvent(_caster.gameObject, _abilitySO, _targetPoint));
            }
        }

        public override void Exit()
        {
            base.Exit();
            Debug.Log($"[AbilityCastState] {_caster?.name} kết thúc thi triển {_abilitySO?.AbilityName}.");
        }
    }
}
