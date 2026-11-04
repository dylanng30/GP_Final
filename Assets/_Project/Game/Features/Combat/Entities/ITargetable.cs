using Dylanng.Gameplay.RPG.Stats;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    public interface ITargetable
    {
        Transform Transform { get; }
        StatController Stats { get; }
        GameElement Element { get; }
        bool IsDead { get; }
        void OnTargeted(bool isTargeted);
    }
}
