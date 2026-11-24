using System;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>Đĩa cảnh báo dưới đất, lấp đầy từ tâm ra mép trong thời gian cast rồi mờ đi.</summary>
    public class GroundTelegraph : MonoBehaviour
    {
        private const float FadeOutDuration = 0.18f;
        private const float GroundOffset = 0.04f;
        private const float GroundProbeHeight = 3f;
        private const float GroundProbeDistance = 8f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private static readonly RaycastHit[] GroundHits = new RaycastHit[8];

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _block;
        private float _elapsed;
        private float _duration;
        private Action<GroundTelegraph> _onFinished;

        public static GroundTelegraph Create(Material material, Transform parent)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "GroundTelegraph";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);

            var telegraph = quad.AddComponent<GroundTelegraph>();
            telegraph._renderer = quad.GetComponent<MeshRenderer>();
            telegraph._renderer.sharedMaterial = material;
            telegraph._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            telegraph._renderer.receiveShadows = false;
            telegraph._block = new MaterialPropertyBlock();
            quad.SetActive(false);
            return telegraph;
        }

        public void Show(Vector3 point, float radius, float duration, Color color, Action<GroundTelegraph> onFinished)
        {
            _elapsed = 0f;
            _duration = Mathf.Max(0.05f, duration);
            _onFinished = onFinished;

            transform.SetPositionAndRotation(
                new Vector3(point.x, FindGroundHeight(point) + GroundOffset, point.z),
                Quaternion.Euler(90f, 0f, 0f));
            transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

            _block.Clear();
            _block.SetColor(BaseColorId, color);
            _block.SetColor(FillColorId, Color.Lerp(color, Color.black, 0.25f));
            _block.SetFloat(ProgressId, 0f);
            _block.SetFloat(FadeId, 1f);
            _renderer.SetPropertyBlock(_block);

            gameObject.SetActive(true);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(_elapsed / _duration);
            float fade = 1f - Mathf.Clamp01((_elapsed - _duration) / FadeOutDuration);

            _block.SetFloat(ProgressId, progress);
            _block.SetFloat(FadeId, fade);
            _renderer.SetPropertyBlock(_block);

            if (fade > 0f) return;

            gameObject.SetActive(false);
            _onFinished?.Invoke(this);
        }

        /// <summary>Tìm mặt đất dưới điểm, bỏ qua collider của nhân vật.</summary>
        private static float FindGroundHeight(Vector3 point)
        {
            Vector3 origin = point + Vector3.up * GroundProbeHeight;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, GroundHits, GroundProbeDistance, ~0, QueryTriggerInteraction.Ignore);

            float best = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                if (GroundHits[i].collider.GetComponentInParent<ITargetable>() != null) continue;
                if (GroundHits[i].point.y > best) best = GroundHits[i].point.y;
            }
            return float.IsNegativeInfinity(best) ? point.y : best;
        }
    }
}
