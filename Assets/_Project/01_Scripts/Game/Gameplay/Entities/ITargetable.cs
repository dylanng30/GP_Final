using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    public interface ITargetable
    {
        Transform Transform { get; }
        StatController Stats { get; }
        bool IsDead { get; }
        void OnTargeted(bool isTargeted);
    }
}
