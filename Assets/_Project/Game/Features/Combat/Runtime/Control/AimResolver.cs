using Game.Combat.Abilities;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    public readonly struct AimResult
    {
        public readonly GameObject Target;
        public readonly Vector3 Point;

        public AimResult(GameObject target, Vector3 point)
        {
            Target = target;
            Point = point;
        }
    }

    /// <summary>Tìm mục tiêu và điểm ngắm cho skill từ mục tiêu đang chọn hoặc từ tâm camera.</summary>
    public static class AimResolver
    {
        private const float MaxAimDistance = 100f;
        private const string GroundLayerName = "Ground";
        private const float DefaultAimDistance = 8f;
        private static readonly Vector3 ViewportCenter = new Vector3(0.5f, 0.5f, 0f);

        public static AimResult Resolve(HeroInstance hero, ITargetable selectedTarget, AbilitySO ability)
        {
            float range = ability != null ? ability.Range : DefaultAimDistance;

            if (IsInRange(hero, selectedTarget, range))
            {
                GameObject target = selectedTarget.Transform.gameObject;
                return new AimResult(target, target.transform.position);
            }

            return new AimResult(null, ResolveGroundPoint(hero, range));
        }

        private static bool IsInRange(HeroInstance hero, ITargetable target, float range)
        {
            if (target == null || target.IsDead || target.Transform == null) return false;
            return Vector3.Distance(hero.transform.position, target.Transform.position) <= range;
        }

        private static Vector3 ResolveGroundPoint(HeroInstance hero, float range)
        {
            Vector3 origin = hero.transform.position;
            Vector3 forwardPoint = origin + hero.transform.forward * range;

            Camera camera = Camera.main;
            bool isAimFromCamera = camera != null && CameraController.Instance != null
                                   && CameraController.Instance.Mode == CameraMode.ThirdPerson;
            if (!isAimFromCamera) return forwardPoint;

            Ray ray = camera.ViewportPointToRay(ViewportCenter);
            int groundMask = LayerMask.GetMask(GroundLayerName);

            Vector3 aimPoint = Physics.Raycast(ray, out RaycastHit hit, MaxAimDistance, groundMask)
                ? hit.point
                : ray.GetPoint(range);

            Vector3 offset = aimPoint - origin;
            offset.y = 0f;
            return origin + Vector3.ClampMagnitude(offset, range);
        }
    }
}
