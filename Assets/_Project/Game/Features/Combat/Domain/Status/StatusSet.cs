using System;
using System.Collections.Generic;
using Game.Data.Hero;

namespace Game.Combat.Domain
{
    public sealed class StatusInstance
    {
        public StatusDefinitionSO Definition { get; }
        public object Source { get; internal set; }
        public float Remaining { get; internal set; }
        public int Stacks { get; internal set; } = 1;
        public float TickPower { get; internal set; }
        internal float TickTimer;

        internal StatusInstance(StatusDefinitionSO definition)
        {
            Definition = definition;
        }
    }

    /// <summary>Các trạng thái đang có trên một actor, cùng các khóa khống chế tạm thời.</summary>
    public sealed class StatusSet
    {
        public event Action<StatusInstance> Applied;
        public event Action<StatusInstance> Removed;
        public event Action<StatusInstance> StacksChanged;
        public event Action<StatusInstance> Ticked;
        public event Action ControlChanged;

        private readonly List<StatusInstance> _active = new List<StatusInstance>();
        private readonly Dictionary<object, ControlFlags> _locks = new Dictionary<object, ControlFlags>();

        public IReadOnlyList<StatusInstance> Active => _active;
        public ControlFlags Flags { get; private set; }

        public bool Has(StatusDefinitionSO definition) => Find(definition) != null;

        public StatusInstance Apply(StatusDefinitionSO definition, float duration, float tickPower, object source)
        {
            StatusInstance existing = Find(definition);
            if (existing == null) return AddNew(definition, duration, tickPower, source);

            switch (definition.StackPolicy)
            {
                case StatusStackPolicy.KeepExisting:
                    return existing;
                case StatusStackPolicy.Stack when existing.Stacks < definition.MaxStacks:
                    existing.Stacks++;
                    StacksChanged?.Invoke(existing);
                    break;
            }

            existing.Remaining = Math.Max(existing.Remaining, duration);
            existing.TickPower = tickPower;
            existing.Source = source;
            return existing;
        }

        public bool Remove(StatusDefinitionSO definition)
        {
            StatusInstance instance = Find(definition);
            if (instance == null) return false;

            RemoveInstance(instance);
            return true;
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                RemoveInstance(_active[i]);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                StatusInstance instance = _active[i];
                instance.Remaining -= deltaTime;

                TickDamage(instance, deltaTime);

                if (instance.Remaining <= 0f) RemoveInstance(instance);
            }
        }

        public object AddLock(ControlFlags flags)
        {
            object token = new object();
            _locks[token] = flags;
            RefreshFlags();
            return token;
        }

        public void RemoveLock(object token)
        {
            if (token != null && _locks.Remove(token)) RefreshFlags();
        }

        /// <summary>Hệ số sát thương gây ra theo hệ, gộp từ mọi trạng thái đang có.</summary>
        public float GetOutgoingMultiplier(GameElement element)
        {
            float multiplier = 1f;
            for (int i = 0; i < _active.Count; i++)
            {
                List<ElementDamageModifier> modifiers = _active[i].Definition.OutgoingDamageModifiers;
                for (int j = 0; j < modifiers.Count; j++)
                {
                    if (modifiers[j].Element == element) multiplier *= modifiers[j].Multiplier;
                }
            }
            return multiplier;
        }

        private StatusInstance AddNew(StatusDefinitionSO definition, float duration, float tickPower, object source)
        {
            var instance = new StatusInstance(definition)
            {
                Remaining = duration,
                TickPower = tickPower,
                Source = source,
                TickTimer = definition.TickInterval
            };

            _active.Add(instance);
            Applied?.Invoke(instance);
            RefreshFlags();
            return instance;
        }

        private void RemoveInstance(StatusInstance instance)
        {
            _active.Remove(instance);
            Removed?.Invoke(instance);
            RefreshFlags();
        }

        private void TickDamage(StatusInstance instance, float deltaTime)
        {
            if (instance.Definition.TickInterval <= 0f) return;

            instance.TickTimer -= deltaTime;
            if (instance.TickTimer > 0f) return;

            instance.TickTimer += instance.Definition.TickInterval;
            Ticked?.Invoke(instance);
        }

        private StatusInstance Find(StatusDefinitionSO definition)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].Definition == definition) return _active[i];
            }
            return null;
        }

        private void RefreshFlags()
        {
            ControlFlags flags = ControlFlags.None;
            for (int i = 0; i < _active.Count; i++)
            {
                flags |= _active[i].Definition.ControlFlags;
            }
            foreach (ControlFlags lockFlags in _locks.Values)
            {
                flags |= lockFlags;
            }

            if (flags == Flags) return;

            Flags = flags;
            ControlChanged?.Invoke();
        }
    }
}
