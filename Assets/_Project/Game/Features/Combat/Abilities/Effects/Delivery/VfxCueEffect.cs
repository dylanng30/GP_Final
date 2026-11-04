using System;
using Game.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    /// <summary>Hiển thị một VFX tại vị trí chỉ định, tự hủy sau Lifetime.</summary>
    [Serializable]
    public class VfxCueEffect : IAbilityEffect
    {
        public GameObject Prefab;
        public EffectOrigin Origin = EffectOrigin.Point;
        public Vector3 Offset;
        public float Scale = 1f;
        public float Lifetime = 2f;
        [Tooltip("Gắn VFX vào người thi triển để đi theo")]
        public bool FollowCaster = false;

        public void Execute(in AbilityContext context)
        {
            if (Prefab == null || context.Caster == null) return;

            Vector3 position = ResolvePosition(context) + Offset;
            Quaternion rotation = Quaternion.LookRotation(context.Direction);
            Transform parent = FollowCaster ? context.Caster.transform : null;

            GameObject vfx = VfxSpawner.Spawn(Prefab, position, rotation, parent);
            vfx.transform.localScale *= Scale;
            Object.Destroy(vfx, Lifetime);
        }

        private Vector3 ResolvePosition(in AbilityContext context)
        {
            switch (Origin)
            {
                case EffectOrigin.Caster: return context.Caster.transform.position;
                case EffectOrigin.Target when context.Target != null: return context.Target.transform.position;
                default: return context.Point;
            }
        }
    }
}
