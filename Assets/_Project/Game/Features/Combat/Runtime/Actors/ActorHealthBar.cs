using Dylanng;
using Game.Combat.Domain;
using Game.Gameplay.Entities;
using Game.Shaders.WorldProgressBar;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Thanh máu trên đầu actor, vẽ bằng quad và shader nên không cần Canvas.
    /// Đặt prefab làm con của bất kỳ actor nào có ITargetable, thanh luôn quay theo camera.
    /// </summary>
    [RequireComponent(typeof(WorldProgressBar_ShaderController))]
    public sealed class ActorHealthBar : MonoBehaviourBase, ILateUpdatable
    {
        private WorldProgressBar_ShaderController _bar;
        private HealthPool _health;

        protected override void Awake()
        {
            base.Awake();
            _bar = GetComponent<WorldProgressBar_ShaderController>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ServiceLocator.Get<ITickSystem>()?.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ServiceLocator.Get<ITickSystem>()?.Unregister(this);
        }

        /// <summary>Lấy HealthPool ở Start vì actor khởi tạo máu trong Awake của chính nó.</summary>
        protected override void Start()
        {
            base.Start();

            ITargetable owner = GetComponentInParent<ITargetable>();
            if (owner == null) return;

            _health = owner.Health;
            _health.Changed += Refresh;
            Refresh();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_health != null) _health.Changed -= Refresh;
        }

        public void OnLateUpdate(float deltaTime)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null) TransformCache.rotation = mainCamera.transform.rotation;
        }

        private void Refresh()
        {
            _bar.UpdateProgress(_health.Ratio);
            _bar.SetVisible(!_health.IsDead);
        }
    }
}
