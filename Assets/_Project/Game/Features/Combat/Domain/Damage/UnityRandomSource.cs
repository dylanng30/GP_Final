using UnityEngine;

namespace Game.Combat.Domain
{
    public sealed class UnityRandomSource : IRandomSource
    {
        public float NextFloat() => Random.value;
    }
}
