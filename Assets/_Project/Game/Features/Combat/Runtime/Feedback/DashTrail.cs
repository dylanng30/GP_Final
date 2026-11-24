using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>Vệt sáng theo màu hệ, chỉ phát trong lúc dash. Tự tạo TrailRenderer con ở ngang thân.</summary>
    [DisallowMultipleComponent]
    public class DashTrail : MonoBehaviour
    {
        private const float StopPadding = 0.05f;

        private TrailRenderer _trail;
        private float _stopTime;

        public void Play(Material material, Color color, float duration, float trailTime, float width)
        {
            if (material == null) return;
            if (_trail == null) _trail = CreateTrail(material);

            _trail.time = trailTime;
            _trail.widthMultiplier = width;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.35f), 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = gradient;

            _trail.Clear();
            _trail.emitting = true;
            _stopTime = Time.time + duration + StopPadding;
        }

        private void Update()
        {
            if (_trail != null && _trail.emitting && Time.time >= _stopTime) _trail.emitting = false;
        }

        private TrailRenderer CreateTrail(Material material)
        {
            var trailObject = new GameObject("DashTrail");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = new Vector3(0f, ResolveChestHeight(), 0f);

            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = material;
            trail.emitting = false;
            trail.minVertexDistance = 0.08f;
            trail.alignment = LineAlignment.View;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            return trail;
        }

        private float ResolveChestHeight()
        {
            var controller = GetComponent<CharacterController>();
            return controller != null ? controller.center.y : 0.9f;
        }
    }
}
