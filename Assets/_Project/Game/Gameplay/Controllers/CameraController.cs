using System.Collections;
using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Events;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public enum CameraMode
    {
        TopDown,
        ThirdPerson
    }

    /// <summary>
    /// Camera theo hero đang điều khiển. Góc nhìn độc lập với chế độ auto.
    /// ThirdPerson có xoay bằng chuột hoặc vuốt, zoom, tránh vật cản, lock-on và rung khi trúng đòn.
    /// </summary>
    public class CameraController : PersistentSingleton<CameraController>
    {
        private const float MouseLookScale = 10f;
        private const float TouchLookScale = 0.5f;
        private const float DefaultPitch = 15f;
        private const float HeroSwitchBlendDuration = 0.25f;
        private const float CollisionSkin = 0.2f;
        private const float ShakeDecaySpeed = 6f;
        private const float HitShake = 0.05f;
        private const float CritShake = 0.14f;

        [Header("--- TOP DOWN SETTINGS ---")]
        [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 10f, -10f);
        [SerializeField] private Vector3 topDownLookOffset = new Vector3(0f, 1f, 0f);

        [Header("--- THIRD PERSON SETTINGS ---")]
        [SerializeField] private float thirdPersonDistance = 5f;
        [SerializeField] private float thirdPersonHeight = 2f;
        [SerializeField] private Vector3 thirdPersonLookOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float swipeSensitivity = 0.2f;
        [SerializeField] private float minPitch = -5f;
        [SerializeField] private float maxPitch = 45f;
        [SerializeField] private float minDistance = 2.5f;
        [SerializeField] private float maxDistance = 9f;
        [SerializeField] private float zoomSpeed = 4f;
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private float lockOnTurnSpeed = 6f;

        [Header("--- GENERAL SETTINGS ---")]
        [SerializeField] private float positionSmoothSpeed = 5f;
        [SerializeField] private float rotationSmoothSpeed = 5f;

        [Header("--- TRANSITION SETTINGS ---")]
        [SerializeField] private float transitionDuration = 0.8f;

        private Transform _target;
        private Transform _lockOnTarget;
        private InputManager _input;
        private Coroutine _transitionCoroutine;
        private bool _isTransitioning;
        private bool _isRotationInitialized;
        private float _currentYaw;
        private float _currentPitch = DefaultPitch;
        private float _shakeStrength;
        private int _collisionMask;

        public CameraMode Mode { get; private set; } = CameraMode.ThirdPerson;

        protected override void Awake()
        {
            // Tách khỏi GameObject cha (nếu có) để lệnh DontDestroyOnLoad có thể hoạt động hợp lệ
            transform.SetParent(null);
            base.Awake();

            _collisionMask = ~LayerMask.GetMask("Hero", "Monster", "UI", "Ignore Raycast", "TransparentFX");
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Subscribe<LockOnChangedEvent>(OnLockOnChanged);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);

            UpdateTarget();
            if (_target == null) return;

            if (!_isRotationInitialized)
            {
                _currentYaw = _target.eulerAngles.y;
                _isRotationInitialized = true;
            }
            ComputePose(Mode, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Unsubscribe<LockOnChangedEvent>(OnLockOnChanged);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
        }

        public void SetMode(CameraMode mode)
        {
            if (Mode == mode) return;

            if (mode == CameraMode.ThirdPerson && _target != null)
            {
                _currentYaw = _target.eulerAngles.y;
                _currentPitch = DefaultPitch;
            }

            Mode = mode;
            StartBlend(transitionDuration);
        }

        public void ToggleMode()
        {
            SetMode(Mode == CameraMode.TopDown ? CameraMode.ThirdPerson : CameraMode.TopDown);
        }

        private void OnActiveHeroChanged(ActiveHeroChangedEvent evt)
        {
            UpdateTarget();
            StartBlend(HeroSwitchBlendDuration);
        }

        private void OnLockOnChanged(LockOnChangedEvent evt)
        {
            _lockOnTarget = evt.Target;
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (_target == null || evt.Caster != _target.gameObject) return;

            _shakeStrength = Mathf.Max(_shakeStrength, evt.IsCritical ? CritShake : HitShake);
        }

        private void UpdateTarget()
        {
            var party = ServiceLocator.Get<PartyCombatManager>();
            if (party == null) return;

            HeroInstance activeHero = party.GetActiveHero();
            _target = activeHero != null ? activeHero.transform : null;
        }

        private void Update()
        {
            if (Instance != this || _target == null || _isTransitioning) return;

            if (Mode == CameraMode.ThirdPerson)
            {
                ReadLookInput();
                ReadZoomInput();
            }
        }

        private void ReadLookInput()
        {
            _input ??= ServiceLocator.Get<InputManager>();

            if (Cursor.lockState == CursorLockMode.Locked && _input != null)
            {
                ApplyLook(_input.RotationInput * (swipeSensitivity * MouseLookScale));
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.position.x <= Screen.width * 0.5f) continue;

                if (touch.phase == TouchPhase.Moved)
                {
                    ApplyLook(touch.deltaPosition * (swipeSensitivity * TouchLookScale));
                }
                return;
            }
        }

        private void ApplyLook(Vector2 delta)
        {
            if (_lockOnTarget == null) _currentYaw += delta.x;

            _currentPitch = Mathf.Clamp(_currentPitch - delta.y, minPitch, maxPitch);
        }

        private void ReadZoomInput()
        {
            if (_input == null) return;

            thirdPersonDistance = Mathf.Clamp(thirdPersonDistance - _input.ZoomInput * zoomSpeed, minDistance, maxDistance);
        }

        private void LateUpdate()
        {
            if (Instance != this || _target == null || _isTransitioning) return;

            UpdateLockOnYaw();

            ComputePose(Mode, out Vector3 position, out Quaternion rotation);

            if (Mode == CameraMode.TopDown)
            {
                float deltaTime = Time.unscaledDeltaTime;
                position = Vector3.Lerp(transform.position, position, positionSmoothSpeed * deltaTime);
                rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSmoothSpeed * deltaTime);
            }

            transform.SetPositionAndRotation(position + SampleShake(), rotation);
        }

        private void UpdateLockOnYaw()
        {
            if (_lockOnTarget == null || Mode != CameraMode.ThirdPerson) return;

            Vector3 toTarget = _lockOnTarget.position - _target.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f) return;

            float desiredYaw = Quaternion.LookRotation(toTarget).eulerAngles.y;
            _currentYaw = Mathf.LerpAngle(_currentYaw, desiredYaw, lockOnTurnSpeed * Time.unscaledDeltaTime);
        }

        private Vector3 SampleShake()
        {
            if (_shakeStrength <= 0f) return Vector3.zero;

            Vector3 offset = Random.insideUnitSphere * _shakeStrength;
            _shakeStrength = Mathf.MoveTowards(_shakeStrength, 0f, ShakeDecaySpeed * _shakeStrength * Time.unscaledDeltaTime + 0.001f);
            return offset;
        }

        private void ComputePose(CameraMode mode, out Vector3 position, out Quaternion rotation)
        {
            Vector3 lookPoint;

            if (mode == CameraMode.TopDown)
            {
                position = _target.position + topDownOffset;
                lookPoint = _target.position + topDownLookOffset;
            }
            else
            {
                lookPoint = _target.position + thirdPersonLookOffset;
                Vector3 orbit = Quaternion.Euler(_currentPitch, _currentYaw, 0f) * Vector3.back;
                position = ResolveOrbitPosition(_target.position + Vector3.up * thirdPersonHeight, orbit);
            }

            Vector3 direction = lookPoint - position;
            rotation = direction != Vector3.zero ? Quaternion.LookRotation(direction) : transform.rotation;
        }

        private Vector3 ResolveOrbitPosition(Vector3 pivot, Vector3 orbitDirection)
        {
            float distance = thirdPersonDistance;

            if (Physics.SphereCast(pivot, collisionRadius, orbitDirection, out RaycastHit hit, distance, _collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(hit.distance - CollisionSkin, 0f);
            }

            return pivot + orbitDirection * distance;
        }

        private void StartBlend(float duration)
        {
            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = StartCoroutine(BlendRoutine(duration));
        }

        private IEnumerator BlendRoutine(float duration)
        {
            _isTransitioning = true;

            float elapsed = 0f;
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;

            while (elapsed < duration && _target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                ComputePose(Mode, out Vector3 position, out Quaternion rotation);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, position, t),
                    Quaternion.Slerp(startRotation, rotation, t));

                yield return null;
            }

            _isTransitioning = false;
        }
    }
}
