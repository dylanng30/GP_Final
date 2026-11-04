using System.Collections.Generic;
using Dylanng;
using Game.Combat;
using Game.Events;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    /// <summary>
    /// Chọn mục tiêu cho hero đang điều khiển.
    /// Soft-lock chấm điểm theo khoảng cách và hướng camera, hard-lock giữ mục tiêu tới khi mất.
    /// </summary>
    public class PlayerTargetingSystem : MonoBehaviour
    {
        [Header("--- Targeting Config ---")]
        [SerializeField] private float scanRadius = 10f;
        [SerializeField] private float loseTargetRadius = 13f;
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private float scanInterval = 0.3f;

        [Tooltip("Trọng số của góc lệch so với hướng camera khi chọn mục tiêu")]
        [SerializeField] private float angleScoreWeight = 2f;

        private readonly List<ITargetable> _candidates = new List<ITargetable>();

        private ITargetable _currentTarget;
        private float _scanTimer;

        public ITargetable CurrentTarget => _currentTarget;
        public LayerMask EnemyLayerMask => enemyLayerMask;
        public bool IsHardLocked { get; private set; }

        private void Update()
        {
            ValidateCurrentTarget();

            if (IsHardLocked) return;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;

            _scanTimer = scanInterval;
            ITargetable best = FindBestTarget();
            if (best != _currentTarget) SetTarget(best);
        }

        /// <summary>Bật hoặc tắt hard-lock vào mục tiêu tốt nhất hiện tại.</summary>
        public void ToggleHardLock()
        {
            if (IsHardLocked)
            {
                ReleaseHardLock();
                return;
            }

            ITargetable best = _currentTarget ?? FindBestTarget();
            if (best == null) return;

            SetTarget(best);
            IsHardLocked = true;
            EventBus.Publish(new LockOnChangedEvent(best.Transform));
        }

        public void SetTarget(ITargetable newTarget)
        {
            _currentTarget?.OnTargeted(false);
            _currentTarget = newTarget;
            _currentTarget?.OnTargeted(true);
        }

        public void ClearTarget()
        {
            ReleaseHardLock();
            SetTarget(null);
        }

        private void ReleaseHardLock()
        {
            if (!IsHardLocked) return;

            IsHardLocked = false;
            EventBus.Publish(new LockOnChangedEvent(null));
        }

        private void ValidateCurrentTarget()
        {
            if (_currentTarget == null) return;

            bool isLost = _currentTarget.IsDead || _currentTarget.Transform == null
                          || Vector3.Distance(transform.position, _currentTarget.Transform.position) > loseTargetRadius;

            if (isLost) ClearTarget();
        }

        private ITargetable FindBestTarget()
        {
            TargetQuery.Collect(transform.position, scanRadius, enemyLayerMask, transform, _candidates);

            Vector3 viewForward = ResolveViewForward();
            ITargetable best = null;
            float bestScore = float.MaxValue;

            foreach (ITargetable candidate in _candidates)
            {
                float score = ScoreCandidate(candidate, viewForward);
                if (score >= bestScore) continue;

                bestScore = score;
                best = candidate;
            }
            return best;
        }

        private float ScoreCandidate(ITargetable candidate, Vector3 viewForward)
        {
            Vector3 toCandidate = candidate.Transform.position - transform.position;
            float normalizedDistance = toCandidate.magnitude / scanRadius;

            toCandidate.y = 0f;
            float normalizedAngle = Vector3.Angle(viewForward, toCandidate) / 180f;

            return normalizedDistance + normalizedAngle * angleScoreWeight;
        }

        private Vector3 ResolveViewForward()
        {
            Transform view = CameraController.Instance != null ? CameraController.Instance.transform : transform;
            Vector3 forward = view.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.001f ? forward.normalized : transform.forward;
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        private void OnDestroy()
        {
            ClearTarget();
        }
    }
}
