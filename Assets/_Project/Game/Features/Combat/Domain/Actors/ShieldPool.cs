using System;
using System.Collections.Generic;

namespace Game.Combat.Domain
{
    /// <summary>Các lớp khiên của một actor. Khiên thêm sau bị trừ trước.</summary>
    public sealed class ShieldPool
    {
        private sealed class Layer
        {
            public object Source;
            public float Amount;
        }

        private readonly List<Layer> _layers = new List<Layer>();

        public float Total { get; private set; }

        public void Add(object source, float amount)
        {
            if (amount <= 0f) return;

            _layers.Add(new Layer { Source = source, Amount = amount });
            Total += amount;
        }

        public void RemoveSource(object source)
        {
            for (int i = _layers.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_layers[i].Source, source)) continue;

                Total -= _layers[i].Amount;
                _layers.RemoveAt(i);
            }
        }

        public void Clear()
        {
            _layers.Clear();
            Total = 0f;
        }

        /// <summary>Trừ khiên và trả về phần sát thương còn lại.</summary>
        public float Absorb(float damage)
        {
            float remaining = damage;
            for (int i = _layers.Count - 1; i >= 0 && remaining > 0f; i--)
            {
                Layer layer = _layers[i];
                float absorbed = Math.Min(layer.Amount, remaining);

                layer.Amount -= absorbed;
                remaining -= absorbed;
                Total -= absorbed;

                if (layer.Amount <= 0f) _layers.RemoveAt(i);
            }
            return remaining;
        }
    }
}
