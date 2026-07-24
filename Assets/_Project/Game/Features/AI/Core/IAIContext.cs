using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.AI.Core
{
    public interface IAIContext
    {
        IEntityController EntityController { get; }
        HeroInstance EntityInstance { get; }
        ITargetable CurrentTarget { get; }
        
        Vector3 Position { get; }
        Vector3 TargetPosition { get; }
        Vector3 SpawnPosition { get; }
        
        float DistanceToTarget { get; }
        float DistanceToSpawn { get; }
        float MaxDistanceFromSpawn { get; }
        float HealthPercentage { get; }
        
        AIWorldState WorldState { get; set; }
        
        void ScanAndValidateTarget(float deltaTime);
        bool TrySetTarget(ITargetable newTarget);
        bool CanCastBossSkill(int skillIndex);
        void StartBossSkillCooldown(int skillIndex, float cooldownDuration);
    }
}
