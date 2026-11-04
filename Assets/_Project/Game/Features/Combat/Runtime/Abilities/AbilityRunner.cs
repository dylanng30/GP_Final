using System.Collections.Generic;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng.Gameplay.RPG.Abilities.States;
using Game.Combat.Domain;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Thứ tự slot trong danh sách skill của hero.</summary>
    public static class AbilitySlot
    {
        public const int Skill1 = 0;
        public const int Skill2 = 1;
        public const int Ultimate = 2;
        public const int Count = 3;
    }

    /// <summary>Quản lý slot skill: điều kiện dùng, cooldown, tiêu hao Nộ và trạng thái đang cast.</summary>
    public sealed class AbilityRunner
    {
        private readonly MonoBehaviourBase _owner;
        private readonly IReadOnlyList<AbilitySO> _abilities;
        private readonly EnergyPool _energy;
        private readonly StateMachine _stateMachine = new StateMachine();
        private readonly Dictionary<int, float> _readyTimes = new Dictionary<int, float>();

        private AbilityCastState _activeCast;
        private int _level = 1;

        public bool IsCasting => _activeCast != null && !_activeCast.IsFinished;
        public bool IsMovementLocked => IsCasting && _activeCast.LocksMovement;

        public AbilityRunner(MonoBehaviourBase owner, IReadOnlyList<AbilitySO> abilities, EnergyPool energy)
        {
            _owner = owner;
            _abilities = abilities;
            _energy = energy;
        }

        public void SetLevel(int level)
        {
            _level = level;
        }

        public AbilitySO Get(int slot)
        {
            return _abilities != null && slot >= 0 && slot < _abilities.Count ? _abilities[slot] : null;
        }

        public bool IsUnlocked(int slot)
        {
            AbilitySO ability = Get(slot);
            return ability != null && _level >= ability.UnlockLevel;
        }

        public float GetRemainingCooldown(int slot)
        {
            return _readyTimes.TryGetValue(slot, out float readyTime) ? Mathf.Max(0f, readyTime - Time.time) : 0f;
        }

        public bool CanUse(int slot)
        {
            if (!IsUnlocked(slot) || IsCasting || GetRemainingCooldown(slot) > 0f) return false;
            return _energy.CanSpend(Get(slot).EnergyCost);
        }

        public bool TryUse(int slot, GameObject target, Vector3 point)
        {
            if (!CanUse(slot)) return false;

            AbilitySO ability = Get(slot);
            _energy.TrySpend(ability.EnergyCost);
            _readyTimes[slot] = Time.time + ability.Cooldown;

            var context = new AbilityContext(_owner.gameObject, target, point, ability.Element, ability);
            _activeCast = new AbilityCastState(_owner, ability, context);
            _stateMachine.TransitionTo(_activeCast);
            return true;
        }

        /// <summary>Ngắt skill đang cast, ví dụ khi bị khống chế.</summary>
        public void Interrupt()
        {
            _activeCast?.Cancel();
        }

        public void Tick()
        {
            _stateMachine.Update();
        }

        public void OnAnimationTransition()
        {
            _stateMachine.OnAnimationTransitionEvent();
        }

        public void OnAnimationEnter()
        {
            _stateMachine.OnAnimationEnterEvent();
        }

        public void OnAnimationExit()
        {
            _stateMachine.OnAnimationExitEvent();
        }
    }
}
