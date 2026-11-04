using System;
using System.Collections.Generic;
using Dylanng;
using Game.Combat.Domain;
using Game.Events;
using Game.Gameplay.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Combat
{
    /// <summary>Nối StatusSet với actor: chỉ số, sát thương theo nhịp và VFX.</summary>
    public sealed class StatusController
    {
        private readonly ITargetable _owner;
        private readonly Dictionary<StatusInstance, GameObject> _vfxInstances = new Dictionary<StatusInstance, GameObject>();

        public StatusSet Set { get; } = new StatusSet();
        public ControlFlags Flags => Set.Flags;
        public bool CanMove => (Set.Flags & ControlFlags.CannotMove) == 0;
        public bool CanAct => (Set.Flags & ControlFlags.CannotAct) == 0;

        public event Action ControlChanged
        {
            add => Set.ControlChanged += value;
            remove => Set.ControlChanged -= value;
        }

        public StatusController(ITargetable owner)
        {
            _owner = owner;

            Set.Applied += OnApplied;
            Set.Removed += OnRemoved;
            Set.StacksChanged += OnStacksChanged;
            Set.Ticked += OnTicked;
        }

        public void Tick(float deltaTime) => Set.Tick(deltaTime);

        public object AddLock(ControlFlags flags) => Set.AddLock(flags);

        public void RemoveLock(object token) => Set.RemoveLock(token);

        private void OnApplied(StatusInstance instance)
        {
            ApplyStatBuffs(instance);
            ApplyScale(instance.Definition.ScaleMultiplier);
            SpawnVfx(instance);
        }

        private void OnRemoved(StatusInstance instance)
        {
            _owner.Stats?.RemoveAllModifiersFromSource(instance);
            ApplyScale(1f / instance.Definition.ScaleMultiplier);

            if (_vfxInstances.TryGetValue(instance, out GameObject vfx))
            {
                _vfxInstances.Remove(instance);
                if (vfx != null) Object.Destroy(vfx);
            }

            EventBus.Publish(new StatusRemovedEvent(_owner.Transform.gameObject, instance.Definition));
        }

        private void OnStacksChanged(StatusInstance instance)
        {
            _owner.Stats?.RemoveAllModifiersFromSource(instance);
            ApplyStatBuffs(instance);
        }

        private void OnTicked(StatusInstance instance)
        {
            ServiceLocator.Get<CombatResolver>()?.Deal(
                instance.Source as GameObject,
                _owner.Transform.gameObject,
                instance.TickPower * instance.Stacks,
                instance.Definition.Element,
                _owner.Transform.position,
                canCrit: false);
        }

        private void ApplyStatBuffs(StatusInstance instance)
        {
            if (_owner.Stats == null) return;

            foreach (StatBuff buff in instance.Definition.StatBuffs)
            {
                var modifier = new Dylanng.Gameplay.RPG.Stats.StatModifier(
                    buff.Value * instance.Stacks, buff.ModifierType, instance);
                _owner.Stats.AddModifier(buff.StatType, modifier);
            }
        }

        private void ApplyScale(float multiplier)
        {
            if (!Mathf.Approximately(multiplier, 1f)) _owner.Transform.localScale *= multiplier;
        }

        private void SpawnVfx(StatusInstance instance)
        {
            StatusDefinitionSO definition = instance.Definition;
            if (definition.LoopVfxPrefab == null) return;

            GameObject vfx = VfxSpawner.Spawn(definition.LoopVfxPrefab, _owner.Transform.position, _owner.Transform.rotation, _owner.Transform);
            vfx.transform.localPosition = definition.VfxOffset;
            _vfxInstances[instance] = vfx;
        }
    }
}
