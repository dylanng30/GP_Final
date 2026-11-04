using System.Collections.Generic;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Truy vấn mục tiêu dùng chung, không cấp phát bộ nhớ mỗi lần gọi.</summary>
    public static class TargetQuery
    {
        private const int BufferSize = 64;
        private const int AllLayers = -1;

        private static readonly Collider[] Buffer = new Collider[BufferSize];
        private static readonly List<ITargetable> NearestBuffer = new List<ITargetable>(BufferSize);

        /// <summary>Lấy các mục tiêu còn sống trong vùng tròn, bỏ qua self. Mask 0 nghĩa là mọi layer.</summary>
        public static void Collect(Vector3 center, float radius, LayerMask mask, Transform self, List<ITargetable> results)
        {
            results.Clear();

            int layers = mask.value != 0 ? mask.value : AllLayers;
            int count = Physics.OverlapSphereNonAlloc(center, radius, Buffer, layers);

            for (int i = 0; i < count; i++)
            {
                var targetable = Buffer[i].GetComponentInParent<ITargetable>();
                if (targetable == null || targetable.IsDead || targetable.Transform == self) continue;
                if (!results.Contains(targetable)) results.Add(targetable);
            }
        }

        public static ITargetable FindNearest(Vector3 center, float radius, LayerMask mask, Transform self)
        {
            Collect(center, radius, mask, self, NearestBuffer);

            ITargetable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            foreach (ITargetable candidate in NearestBuffer)
            {
                float sqrDistance = (candidate.Transform.position - center).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }
            return nearest;
        }

        /// <summary>Layer của kẻ địch đối với actor. Trả về 0 nếu actor không khai báo.</summary>
        public static LayerMask GetEnemyMask(GameObject actor)
        {
            return actor != null && actor.TryGetComponent<HeroInstance>(out var hero) ? hero.GetTargetLayerMask() : default;
        }

        /// <summary>Layer của đồng minh, tức các actor cùng layer với người thi triển.</summary>
        public static LayerMask GetAllyMask(GameObject actor)
        {
            return actor != null ? (LayerMask)(1 << actor.layer) : default;
        }
    }
}
