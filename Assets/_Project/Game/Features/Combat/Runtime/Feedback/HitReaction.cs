using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Phản ứng hình ảnh của một actor khi trúng đòn: thay tạm material bằng màu chớp và nảy model.
    /// CombatFeedbackManager tự gắn component này lên mục tiêu ở lần trúng đòn đầu tiên.
    /// </summary>
    [DisallowMultipleComponent]
    public class HitReaction : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Material[]> _originalMaterials = new List<Material[]>();
        private readonly List<Material[]> _flashMaterials = new List<Material[]>();
        private readonly List<MaterialPropertyBlock> _savedBlocks = new List<MaterialPropertyBlock>();
        private readonly List<bool> _hadBlock = new List<bool>();
        private MaterialPropertyBlock _flashBlock;

        private bool _isFlashing;
        private float _flashEndTime;

        private Transform _visual;
        private Vector3 _visualBaseScale;
        private bool _isSquashing;
        private float _squashElapsed;
        private float _squashDuration;
        private float _squashAmount;

        public void Flash(Material flashMaterial, Color color, float duration)
        {
            if (flashMaterial == null || duration <= 0f) return;

            if (!_isFlashing)
            {
                CollectRenderers(flashMaterial);
                if (_renderers.Count == 0) return;

                for (int i = 0; i < _renderers.Count; i++)
                {
                    Renderer target = _renderers[i];
                    _hadBlock[i] = target.HasPropertyBlock();
                    if (_hadBlock[i]) target.GetPropertyBlock(_savedBlocks[i]);
                    target.sharedMaterials = _flashMaterials[i];
                }
                _isFlashing = true;
            }

            if (_flashBlock == null) _flashBlock = new MaterialPropertyBlock();
            _flashBlock.Clear();
            _flashBlock.SetColor(BaseColorId, color);
            foreach (Renderer target in _renderers)
            {
                if (target != null) target.SetPropertyBlock(_flashBlock);
            }

            _flashEndTime = Mathf.Max(_flashEndTime, Time.unscaledTime + duration);
        }

        public void Squash(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f) return;

            if (_visual == null)
            {
                _visual = ResolveVisual();
                if (_visual == null) return;
                _visualBaseScale = _visual.localScale;
            }

            _isSquashing = true;
            _squashElapsed = 0f;
            _squashDuration = duration;
            _squashAmount = amount;
            ApplySquash(0f);
        }

        private void LateUpdate()
        {
            if (_isFlashing && Time.unscaledTime >= _flashEndTime) RestoreMaterials();

            if (!_isSquashing) return;

            // Thời gian theo game: model giữ nguyên tư thế bị nén trong lúc hitstop.
            _squashElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_squashElapsed / _squashDuration);
            ApplySquash(t);
            if (t >= 1f) StopSquash();
        }

        private void ApplySquash(float t)
        {
            if (_visual == null)
            {
                _isSquashing = false;
                return;
            }

            // Nén mạnh ngay lúc trúng, bật nảy nhẹ qua phía ngược lại rồi về dáng cũ.
            float decay = (1f - t) * (1f - t);
            float wave = Mathf.Cos(t * Mathf.PI * 2.5f);
            float s = _squashAmount * decay * wave;
            _visual.localScale = new Vector3(
                _visualBaseScale.x * (1f + s),
                _visualBaseScale.y * (1f - s),
                _visualBaseScale.z * (1f + s));
        }

        private void StopSquash()
        {
            _isSquashing = false;
            if (_visual != null) _visual.localScale = _visualBaseScale;
        }

        private void RestoreMaterials()
        {
            _isFlashing = false;
            for (int i = 0; i < _renderers.Count; i++)
            {
                Renderer target = _renderers[i];
                if (target == null) continue;

                target.sharedMaterials = _originalMaterials[i];
                target.SetPropertyBlock(_hadBlock[i] ? _savedBlocks[i] : null);
            }
        }

        private void OnDisable()
        {
            if (_isFlashing) RestoreMaterials();
            if (_isSquashing) StopSquash();
        }

        private void CollectRenderers(Material flashMaterial)
        {
            _renderers.Clear();
            _originalMaterials.Clear();
            _flashMaterials.Clear();
            _hadBlock.Clear();

            foreach (Renderer candidate in GetComponentsInChildren<Renderer>(false))
            {
                if (!IsCharacterRenderer(candidate)) continue;

                Material[] original = candidate.sharedMaterials;
                var flash = new Material[original.Length];
                for (int i = 0; i < flash.Length; i++) flash[i] = flashMaterial;

                _renderers.Add(candidate);
                _originalMaterials.Add(original);
                _flashMaterials.Add(flash);
                _hadBlock.Add(false);
                if (_savedBlocks.Count < _renderers.Count) _savedBlocks.Add(new MaterialPropertyBlock());
            }
        }

        /// <summary>Chỉ lấy mesh đặc của nhân vật, bỏ qua VFX, vòng chọn mục tiêu, thanh máu và chữ.</summary>
        internal static bool IsCharacterRenderer(Renderer candidate)
        {
            if (!candidate.enabled) return false;
            if (!(candidate is SkinnedMeshRenderer) && !(candidate is MeshRenderer)) return false;
            if (candidate.GetComponentInParent<Canvas>() != null) return false;
            if (candidate.GetComponentInParent<ActorHealthBar>() != null) return false;

            Material[] materials = candidate.sharedMaterials;
            if (materials.Length == 0) return false;

            foreach (Material material in materials)
            {
                // Material phủ của hệ thống phản hồi (viền trạng thái) không làm mất tư cách mesh nhân vật.
                if (material != null && material.shader.name.StartsWith("Game/Feedback/")) continue;
                if (material == null || material.renderQueue >= 2500) return false;
                if (material.shader.name.Contains("ProgressBar")) return false;
            }
            return true;
        }

        /// <summary>Model có Animator thì nảy model đó, không thì nảy mesh cao nhất (thân, không phải đế).</summary>
        private Transform ResolveVisual()
        {
            var animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.transform != transform) return animator.transform;

            Transform tallest = null;
            float tallestHeight = 0f;
            foreach (Renderer candidate in GetComponentsInChildren<Renderer>(false))
            {
                if (!IsCharacterRenderer(candidate) || candidate.transform == transform) continue;

                float height = candidate.bounds.size.y;
                if (height <= tallestHeight) continue;

                tallestHeight = height;
                tallest = candidate.transform;
            }
            return tallest;
        }
    }
}
