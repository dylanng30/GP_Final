using Dylanng.Gameplay.RPG.Stats;
using Game.Combat;
using Game.Combat.Domain;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    public interface ITargetable
    {
        Transform Transform { get; }
        StatController Stats { get; }
        GameElement Element { get; }
        HealthPool Health { get; }
        StatusController Status { get; }
        bool IsDead { get; }
        bool IsInvulnerable { get; }
        void OnTargeted(bool isTargeted);
    }
}
