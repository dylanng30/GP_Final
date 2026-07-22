using System;
using System.Text;

namespace Game.Gameplay.AI.Core
{
    public struct AIWorldState : IEquatable<AIWorldState>
    {
        private ulong _stateMask;

        public ulong StateMask => _stateMask;

        public AIWorldState(ulong initialMask)
        {
            _stateMask = initialMask;
        }

        public bool HasState(AIWorldStateKey key)
        {
            if ((byte)key >= 64) return false;
            ulong mask = 1UL << (byte)key;
            return (_stateMask & mask) != 0;
        }

        public void SetState(AIWorldStateKey key, bool value)
        {
            if ((byte)key >= 64) return;
            ulong mask = 1UL << (byte)key;
            if (value)
            {
                _stateMask |= mask;
            }
            else
            {
                _stateMask &= ~mask;
            }
        }

        public bool ContainsAll(AIWorldState requiredConditions)
        {
            return (_stateMask & requiredConditions._stateMask) == requiredConditions._stateMask;
        }

        public bool Satisfies(AIWorldState requiredMask, AIWorldState requiredValues)
        {
            return (_stateMask & requiredMask._stateMask) == (requiredValues._stateMask & requiredMask._stateMask);
        }

        public void ApplyEffects(AIWorldState effectsMask, AIWorldState effectsValues)
        {
            _stateMask &= ~effectsMask._stateMask;
            _stateMask |= (effectsMask._stateMask & effectsValues._stateMask);
        }

        public void Clear()
        {
            _stateMask = 0;
        }

        public bool Equals(AIWorldState other)
        {
            return _stateMask == other._stateMask;
        }

        public override bool Equals(object obj)
        {
            return obj is AIWorldState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _stateMask.GetHashCode();
        }

        public static bool operator ==(AIWorldState left, AIWorldState right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AIWorldState left, AIWorldState right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AIWorldState[ ");
            foreach (AIWorldStateKey key in Enum.GetValues(typeof(AIWorldStateKey)))
            {
                if (HasState(key))
                {
                    sb.Append($"{key} ");
                }
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}
