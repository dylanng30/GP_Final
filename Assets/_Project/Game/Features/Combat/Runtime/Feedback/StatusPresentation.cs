using System;
using System.Collections.Generic;
using Game.Combat.Domain;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Phần hiển thị của trạng thái trên một actor: viền phát sáng (thêm một material phủ lên mesh)
    /// và bước chân nặng khi ở dạng khổng lồ. CombatFeedbackManager gắn component này khi cần.
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusPresentation : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private const float MoveThreshold = 0.1f;

        private readonly List<StatusDefinitionSO> _active = new List<StatusDefinitionSO>();
        private readonly List<Renderer> _rimRenderers = new List<Renderer>();
        private Material _rimTemplate;
        private Material _rimInstance;
        private HeroInstance _hero;
        private float _footstepTimer;

        public Func<bool> IsControlledByPlayer;
        public Action<Vector3, float> OnFootstep;

        public void Initialize(Material rimTemplate)
        {
            _rimTemplate = rimTemplate;
            _hero = GetComponent<HeroInstance>();
        }

        /// <summary>Trả về false nếu trạng thái đã có (chỉ làm mới thời gian), để không phát lại VFX bắt đầu.</summary>
        public bool Add(StatusDefinitionSO status)
        {
            if (_active.Contains(status)) return false;

            _active.Add(status);
            RefreshRim();
            return true;
        }

        public bool Remove(StatusDefinitionSO status)
        {
            if (!_active.Remove(status)) return false;

            RefreshRim();
            return true;
        }

        private void Update()
        {
            StatusDefinitionSO heavy = null;
            foreach (StatusDefinitionSO status in _active)
            {
                if (status.FootstepShake > 0f && (heavy == null || status.FootstepShake > heavy.FootstepShake)) heavy = status;
            }

            if (heavy == null || _hero == null || !_hero.IsGrounded || _hero.CurrentMoveInput.sqrMagnitude < MoveThreshold * MoveThreshold)
            {
                _footstepTimer = 0f;
                return;
            }
            if (IsControlledByPlayer != null && !IsControlledByPlayer()) return;

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer > 0f) return;

            _footstepTimer = heavy.FootstepInterval;
            OnFootstep?.Invoke(transform.position, heavy.FootstepShake);
        }

        private void RefreshRim()
        {
            Color rim = new Color(0f, 0f, 0f, 0f);
            foreach (StatusDefinitionSO status in _active)
            {
                if (status.RimColor.a > rim.a) rim = status.RimColor;
            }

            RemoveRim();
            if (rim.a <= 0f || _rimTemplate == null) return;

            if (_rimInstance == null) _rimInstance = new Material(_rimTemplate) { name = "StatusRim" };
            _rimInstance.SetColor(BaseColorId, rim);

            foreach (Renderer candidate in GetComponentsInChildren<Renderer>(false))
            {
                if (!HitReaction.IsCharacterRenderer(candidate)) continue;

                Material[] materials = candidate.sharedMaterials;
                var withRim = new Material[materials.Length + 1];
                materials.CopyTo(withRim, 0);
                withRim[materials.Length] = _rimInstance;
                candidate.sharedMaterials = withRim;
                _rimRenderers.Add(candidate);
            }
        }

        private void RemoveRim()
        {
            foreach (Renderer candidate in _rimRenderers)
            {
                if (candidate == null) continue;

                var kept = new List<Material>(candidate.sharedMaterials);
                kept.RemoveAll(material => material == _rimInstance);
                candidate.sharedMaterials = kept.ToArray();
            }
            _rimRenderers.Clear();
        }

        private void OnDestroy()
        {
            RemoveRim();
            if (_rimInstance != null) Destroy(_rimInstance);
        }
    }
}
