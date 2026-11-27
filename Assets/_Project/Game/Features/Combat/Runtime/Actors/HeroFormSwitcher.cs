using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Animation.Hero;
using Game.Combat.Domain;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Đổi model của hero theo trạng thái, ví dụ dạng Cự Nhân của Kim. Mỗi dạng là một model con có
    /// Animator và HeroAnimatorController riêng. Trạng thái bắt đầu thì bật model của dạng đó, hết thì về model gốc.
    /// Thêm dạng mới chỉ cần khai báo trong danh sách, không phải sửa code.
    /// </summary>
    [RequireComponent(typeof(HeroInstance))]
    public sealed class HeroFormSwitcher : MonoBehaviour
    {
        [Serializable]
        public sealed class Form
        {
            [Tooltip("Trạng thái kích hoạt dạng này")]
            public StatusDefinitionSO Status;
            [Tooltip("Model con của hero, để tắt sẵn. Cần Animator và HeroAnimatorController")]
            public GameObject Model;

            [Header("Thân va chạm (0 = giữ nguyên)")]
            public float CharacterHeight;
            public float CharacterRadius;

            [Header("VFX đổi model, đặt ở giữa thân")]
            public GameObject EnterVfx;
            public float EnterVfxScale = 1f;
            public GameObject ExitVfx;
            public float ExitVfxScale = 1f;
        }

        [Tooltip("Model mặc định của hero")]
        [SerializeField] private GameObject baseModel;
        [SerializeField] private List<Form> forms = new List<Form>();

        [Header("--- Hiệu ứng bật khi đổi model ---")]
        [Tooltip("Object được co giãn khi đổi model. Để trống thì dùng cha của model gốc")]
        [SerializeField] private Transform popTarget;
        [Tooltip("Dạng mới hiện ra từ kích thước này rồi bật về 1")]
        [SerializeField] private float enterPopFrom = 0.6f;
        [Tooltip("Lúc về model gốc, model hiện ra từ kích thước này rồi về 1")]
        [SerializeField] private float exitPopFrom = 1.3f;
        [SerializeField] private float popDuration = 0.45f;
        [SerializeField] private float vfxLifetime = 3f;

        private HeroInstance _hero;
        private CharacterController _controller;
        private float _baseHeight;
        private float _baseRadius;
        private Vector3 _baseCenter;
        private Vector3 _popBaseScale;
        private Form _current;

        public bool IsTransformed => _current != null;

        private void Awake()
        {
            _hero = GetComponent<HeroInstance>();
            _controller = GetComponent<CharacterController>();
            if (popTarget == null && baseModel != null) popTarget = baseModel.transform.parent;
            if (popTarget != null) _popBaseScale = popTarget.localScale;

            if (_controller != null)
            {
                _baseHeight = _controller.height;
                _baseRadius = _controller.radius;
                _baseCenter = _controller.center;
            }

            foreach (Form form in forms)
            {
                if (form.Model != null) form.Model.SetActive(false);
            }
        }

        private void Start()
        {
            _hero.Status.Set.Applied += OnStatusApplied;
            _hero.Status.Set.Removed += OnStatusRemoved;
        }

        private void OnDestroy()
        {
            if (popTarget != null) popTarget.DOKill();
            if (_hero == null || _hero.Status == null) return;

            _hero.Status.Set.Applied -= OnStatusApplied;
            _hero.Status.Set.Removed -= OnStatusRemoved;
        }

        private void OnStatusApplied(StatusInstance instance)
        {
            Form form = forms.Find(f => f.Status == instance.Definition && f.Model != null);
            if (form == null || form == _current) return;

            SwitchTo(form);
        }

        private void OnStatusRemoved(StatusInstance instance)
        {
            if (_current == null || _current.Status != instance.Definition) return;

            SwitchTo(null);
        }

        private void SwitchTo(Form next)
        {
            Form previous = _current;
            GameObject fromModel = previous != null ? previous.Model : baseModel;
            GameObject toModel = next != null ? next.Model : baseModel;
            if (toModel == null) return;

            // Bật model mới trước để Animator đọc được parameter, rồi mới chuyển lệnh animation sang nó.
            toModel.SetActive(true);
            var animator = toModel.GetComponent<HeroAnimatorController>();
            if (animator != null) _hero.SwapAnimatorController(animator);
            if (fromModel != null && fromModel != toModel) fromModel.SetActive(false);

            _current = next;
            ApplyCharacterSize(next);

            bool entering = next != null;
            Form vfxSource = entering ? next : previous;
            if (vfxSource != null)
            {
                SpawnVfx(entering ? vfxSource.EnterVfx : vfxSource.ExitVfx, entering ? vfxSource.EnterVfxScale : vfxSource.ExitVfxScale);
            }
            Pop(entering ? enterPopFrom : exitPopFrom);
        }

        private void ApplyCharacterSize(Form form)
        {
            if (_controller == null) return;

            float height = form != null && form.CharacterHeight > 0f ? form.CharacterHeight : _baseHeight;
            float radius = form != null && form.CharacterRadius > 0f ? form.CharacterRadius : _baseRadius;

            // Giữ đáy capsule ở chân: tâm dời theo nửa phần chênh chiều cao.
            float feet = _baseCenter.y - _baseHeight * 0.5f;
            _controller.radius = Mathf.Min(radius, height * 0.5f);
            _controller.height = height;
            _controller.center = new Vector3(_baseCenter.x, feet + height * 0.5f, _baseCenter.z);
        }

        private void SpawnVfx(GameObject prefab, float scale)
        {
            if (prefab == null) return;

            Vector3 center = transform.position + Vector3.up * (_controller != null ? _controller.center.y : 1f);
            GameObject vfx = VfxSpawner.Spawn(prefab, center, Quaternion.identity);
            vfx.transform.localScale *= scale;
            Destroy(vfx, vfxLifetime);
        }

        private void Pop(float from)
        {
            if (popTarget == null || popDuration <= 0f || Mathf.Approximately(from, 1f)) return;

            popTarget.DOKill();
            popTarget.localScale = _popBaseScale * from;
            popTarget.DOScale(_popBaseScale, popDuration).SetEase(from < 1f ? Ease.OutBack : Ease.OutCubic);
        }
    }
}
