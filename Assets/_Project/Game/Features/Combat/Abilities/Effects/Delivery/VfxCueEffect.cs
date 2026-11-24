using Dylanng;
using System;
using Game.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Combat.Abilities.Effects
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
        [Tooltip("Hạ VFX xuống mặt đất bên dưới vị trí (vòng sóng xung kích, vết cháy)")]
        public bool SnapToGround = false;

        private const float GroundProbeUp = 2f;
        private const float GroundProbeDistance = 8f;

        public void Execute(in AbilityContext context)
        {
            if (Prefab == null || context.Caster == null) return;

            Vector3 position = ResolvePosition(context);
            if (SnapToGround) position = FindGround(position);
            position += Offset;
            Quaternion rotation = Quaternion.LookRotation(context.Direction);
            Transform parent = FollowCaster ? context.Caster.transform : null;

            GameObject vfx = VfxSpawner.Spawn(Prefab, position, rotation, parent);
            vfx.transform.localScale *= Scale;
            Object.Destroy(vfx, Lifetime);
        }

        /// <summary>Điểm mặt đất gần nhất bên dưới, bỏ qua collider của nhân vật.</summary>
        private static Vector3 FindGround(Vector3 position)
        {
            RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * GroundProbeUp, Vector3.down, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            Vector3 ground = position;
            foreach (RaycastHit hit in hits)
            {
                if (hit.distance >= best || hit.collider.GetComponentInParent<Game.Gameplay.Entities.ITargetable>() != null) continue;
                if (hit.collider is CharacterController) continue;

                best = hit.distance;
                ground = hit.point;
            }
            return ground;
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
