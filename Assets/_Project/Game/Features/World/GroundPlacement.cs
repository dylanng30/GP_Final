using UnityEngine;

namespace Game.World
{
    /// <summary>
    /// Tìm chỗ đứng cho nhân vật quanh một marker: trên mặt có collider (đất, sàn, bệ) và thân không chạm nhà, cây, đá.
    /// Dò từ ngay trên marker xuống nên không đứng lên mái nhà hay tán cây phía trên.
    /// </summary>
    public static class GroundPlacement
    {
        private const string GroundLayer = "Ground";
        private static readonly string[] ActorLayers = { "Hero", "Monster" };

        // Vòng tìm quanh marker (m) và số điểm trên mỗi vòng.
        private static readonly float[] SearchRings = { 0f, 1.5f, 3f, 4.5f, 6f, 8f };
        private const int SamplesPerRing = 12;
        private const float ProbeAbove = 1.5f;
        private const float ProbeDepth = 6f;
        private const float Skin = 0.05f;
        private const float Clearance = 0.1f;

        /// <summary>Trả về false và chính <paramref name="point"/> nếu không có chỗ trống trong bán kính tìm.</summary>
        public static bool TryFindStandingSpot(Vector3 point, float radius, float height, out Vector3 spot)
        {
            int surfaceMask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask(ActorLayers);
            // Mặt đất dốc có thể chạm đáy capsule nên không tính là vật cản.
            int blockingMask = surfaceMask & ~LayerMask.GetMask(GroundLayer);

            foreach (float ring in SearchRings)
            {
                int samples = ring <= 0f ? 1 : SamplesPerRing;
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * Mathf.PI * 2f / samples;
                    Vector3 candidate = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                    Vector3 origin = candidate + Vector3.up * ProbeAbove;
                    if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, ProbeDepth, surfaceMask, QueryTriggerInteraction.Ignore)) continue;

                    Vector3 bottom = hit.point + Vector3.up * (radius + Skin);
                    Vector3 top = hit.point + Vector3.up * Mathf.Max(radius + Skin, height - radius);
                    if (Physics.CheckCapsule(bottom, top, radius, blockingMask, QueryTriggerInteraction.Ignore)) continue;

                    spot = hit.point + Vector3.up * Clearance;
                    return true;
                }
            }

            spot = point;
            return false;
        }
    }
}
