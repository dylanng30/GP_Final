using System;
using System.Collections.Generic;
using Game.Combat;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    public enum EffectOrigin
    {
        Caster,
        Point,
        Target
    }

    public enum TargetTeam
    {
        Enemies,
        Allies
    }

    /// <summary>Quét vùng tròn hoặc hình quạt rồi chạy HitEffects lên từng mục tiêu.</summary>
    [Serializable]
    public class AreaEffect : IAbilityEffect
    {
        private const float FullCircle = 360f;

        public EffectOrigin Origin = EffectOrigin.Point;
        public TargetTeam Team = TargetTeam.Enemies;
        public float Radius = 3f;
        [Range(1f, 360f)] public float ArcAngle = FullCircle;
        [Tooltip("Tính cả người thi triển khi Team là Allies")]
        public bool IncludeCaster = true;

        [SerializeReference] public List<IAbilityEffect> HitEffects = new List<IAbilityEffect>();

        public void Execute(in AbilityContext context)
        {
            GameObject caster = context.Caster;
            if (caster == null) return;

            var targets = new List<ITargetable>();
            Vector3 center = ResolveCenter(context);
            LayerMask mask = Team == TargetTeam.Enemies ? TargetQuery.GetEnemyMask(caster) : TargetQuery.GetAllyMask(caster);

            TargetQuery.Collect(center, Radius, mask, caster.transform, targets);

            if (Team == TargetTeam.Allies && IncludeCaster && caster.TryGetComponent<ITargetable>(out var self))
            {
                targets.Add(self);
            }

            foreach (ITargetable target in targets)
            {
                if (IsInsideArc(context, center, target.Transform.position)) ApplyHitEffects(context, target);
            }
        }

        private Vector3 ResolveCenter(in AbilityContext context)
        {
            switch (Origin)
            {
                case EffectOrigin.Caster: return context.Caster.transform.position;
                case EffectOrigin.Target when context.Target != null: return context.Target.transform.position;
                default: return context.Point;
            }
        }

        private bool IsInsideArc(in AbilityContext context, Vector3 center, Vector3 point)
        {
            if (ArcAngle >= FullCircle) return true;

            Vector3 toPoint = point - center;
            toPoint.y = 0f;
            return Vector3.Angle(context.Direction, toPoint) <= ArcAngle * 0.5f;
        }

        private void ApplyHitEffects(in AbilityContext context, ITargetable target)
        {
            AbilityContext hit = context.WithHit(target.Transform.gameObject, target.Transform.position);
            foreach (IAbilityEffect effect in HitEffects)
            {
                effect?.Execute(hit);
            }
        }
    }
}
