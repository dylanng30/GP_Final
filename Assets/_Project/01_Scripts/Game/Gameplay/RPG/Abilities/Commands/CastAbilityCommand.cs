using System;
using Dylanng.Core.Base;
using Dylanng.Core.State;
using Dylanng.Gameplay.RPG.Abilities.States;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Commands
{
    public class CastAbilityCommand : CommandBase
    {
        private readonly MonoBehaviourBase _caster;
        private readonly AbilitySO _abilitySO;
        private readonly StateMachine _stateMachine;
        private readonly GameObject _target;
        private readonly Vector3 _targetPoint;
        private readonly Action _onCastCompleted;

        public CastAbilityCommand(
            MonoBehaviourBase caster, 
            AbilitySO abilitySO, 
            StateMachine stateMachine, 
            GameObject target = null, 
            Vector3 targetPoint = default,
            Action onCastCompleted = null)
        {
            _caster = caster;
            _abilitySO = abilitySO;
            _stateMachine = stateMachine;
            _target = target;
            _targetPoint = targetPoint;
            _onCastCompleted = onCastCompleted;
        }

        public override void Execute()
        {
            if (_caster == null || _abilitySO == null || _stateMachine == null)
            {
                Debug.LogWarning("[CastAbilityCommand] Caster, AbilitySO hoặc StateMachine bị null. Không thể thi triển kỹ năng.");
                return;
            }

            var castState = new AbilityCastState(_caster, _abilitySO, _target, _targetPoint, _onCastCompleted);
            _stateMachine.TransitionTo(castState);
        }

        public override void Undo()
        {
            Debug.Log($"[CastAbilityCommand] Hủy thi triển kỹ năng: {_abilitySO?.AbilityName}");
        }
    }
}
