using System.Collections;
using Dylanng;
using Game.Events;
using Game.Managers.Party;
using Game.Gameplay.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Gameplay.Controllers
{
    public enum CameraMode
    {
        TopDown,
        ThirdPerson
    }

    public class CameraController : MonoBehaviour
    {
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

        [Header("--- GENERAL SETTINGS ---")]
        [SerializeField] private float positionSmoothSpeed = 5f;
        [SerializeField] private float rotationSmoothSpeed = 5f;
        
        [Header("--- TRANSITION SETTINGS ---")]
        [SerializeField] private float transitionDuration = 0.8f;
        [SerializeField] private float transitionTimeScale = 0.1f;

        private Transform _target;
        private CameraMode _currentMode = CameraMode.TopDown;
        private bool _isTransitioning = false;

        // Third person rotation state
        private float _currentYaw = 0f;
        private float _currentPitch = 15f;
        
        private Coroutine _transitionCoroutine;

        private void OnEnable()
        {
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Subscribe<ToggleFollowModeEvent>(OnToggleFollowMode);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
            EventBus.Unsubscribe<ToggleFollowModeEvent>(OnToggleFollowMode);
        }
        
        private void Start()
        {
            // Snap to initial mode
            _currentMode = CameraMode.TopDown;
            if (_target != null)
            {
                _currentYaw = _target.eulerAngles.y;
            }
        }

        private void OnActiveHeroChanged(ActiveHeroChangedEvent evt)
        {
            UpdateTarget();
            if (_target != null && _currentMode == CameraMode.ThirdPerson)
            {
                _currentYaw = _target.eulerAngles.y;
            }
        }

        private void OnToggleFollowMode(ToggleFollowModeEvent evt)
        {
            CameraMode targetMode = evt.IsFollowModeEnabled ? CameraMode.TopDown : CameraMode.ThirdPerson;
            if (_currentMode != targetMode)
            {
                if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = StartCoroutine(TransitionRoutine(targetMode));
            }
        }

        private void UpdateTarget()
        {
            var partyCombatManager = ServiceLocator.Get<PartyCombatManager>();
            if (partyCombatManager != null)
            {
                HeroInstance activeHero = partyCombatManager.GetActiveHero();
                _target = activeHero != null ? activeHero.transform : null;
            }
        }

        private void Update()
        {
            HandleSwipeInput();
        }

        private void HandleSwipeInput()
        {
            if (_currentMode != CameraMode.ThirdPerson || _isTransitioning) return;

            // Handle mobile touches
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    // Allow rotation if touch is on the right half of the screen
                    if (touch.position.x > Screen.width / 2f)
                    {
                        if (touch.phase == TouchPhase.Moved)
                        {
                            // Multiply by a factor for touch delta
                            _currentYaw += touch.deltaPosition.x * swipeSensitivity * 0.5f;
                            _currentPitch -= touch.deltaPosition.y * swipeSensitivity * 0.5f;
                            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
                        }
                        // Only process the first valid touch on the right half for camera
                        return; 
                    }
                }
            }
            // Fallback for Editor/PC mouse testing
            else if (Input.GetMouseButton(0))
            {
                // Allow rotation if mouse is on the right half of the screen
                if (Input.mousePosition.x > Screen.width / 2f)
                {
                    float mouseX = Input.GetAxis("Mouse X");
                    float mouseY = Input.GetAxis("Mouse Y");

                    _currentYaw += mouseX * swipeSensitivity * 10f;
                    _currentPitch -= mouseY * swipeSensitivity * 10f;
                    _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null || _isTransitioning) return;

            Vector3 desiredPosition;
            Vector3 lookTarget;

            if (_currentMode == CameraMode.TopDown)
            {
                desiredPosition = _target.position + topDownOffset;
                lookTarget = _target.position + topDownLookOffset;

                // Smooth Position
                transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.unscaledDeltaTime);

                // Smooth Rotation
                Vector3 directionToTarget = lookTarget - transform.position;
                if (directionToTarget != Vector3.zero)
                {
                    Quaternion desiredRotation = Quaternion.LookRotation(directionToTarget);
                    transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.unscaledDeltaTime);
                }
            }
            else // ThirdPerson
            {
                // Calculate position based on yaw/pitch orbit around target
                Quaternion orbitRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                Vector3 orbitDirection = orbitRotation * Vector3.back;
                
                desiredPosition = _target.position + Vector3.up * thirdPersonHeight + orbitDirection * thirdPersonDistance;
                lookTarget = _target.position + thirdPersonLookOffset;

                // Snap Position & Rotation immediately
                transform.position = desiredPosition;
                
                Vector3 directionToTarget = lookTarget - transform.position;
                if (directionToTarget != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(directionToTarget);
                }
            }
        }

        private IEnumerator TransitionRoutine(CameraMode targetMode)
        {
            _isTransitioning = true;
            _currentMode = targetMode;

            // Slow down time
            float originalTimeScale = Time.timeScale;
            Time.timeScale = transitionTimeScale;

            // Pre-calculate target parameters if needed
            if (targetMode == CameraMode.ThirdPerson && _target != null)
            {
                // Align camera behind the character initially
                _currentYaw = _target.eulerAngles.y;
                _currentPitch = 15f; 
            }

            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime; // Use unscaled to run at normal speed while game is slow
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                
                // Smooth step for nicer ease-in ease-out
                float curveT = Mathf.SmoothStep(0, 1, t);

                if (_target != null)
                {
                    Vector3 desiredPosition;
                    Vector3 lookTarget;

                    if (targetMode == CameraMode.TopDown)
                    {
                        desiredPosition = _target.position + topDownOffset;
                        lookTarget = _target.position + topDownLookOffset;
                    }
                    else
                    {
                        Quaternion orbitRotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                        Vector3 orbitDirection = orbitRotation * Vector3.back;
                        desiredPosition = _target.position + Vector3.up * thirdPersonHeight + orbitDirection * thirdPersonDistance;
                        lookTarget = _target.position + thirdPersonLookOffset;
                    }

                    transform.position = Vector3.Lerp(startPos, desiredPosition, curveT);
                    
                    Vector3 dir = lookTarget - transform.position;
                    if (dir != Vector3.zero)
                    {
                        Quaternion desiredRot = Quaternion.LookRotation(dir);
                        transform.rotation = Quaternion.Slerp(startRot, desiredRot, curveT);
                    }
                }

                yield return null;
            }

            // Restore time
            Time.timeScale = 1f; // Or originalTimeScale if it was not 1
            _isTransitioning = false;
        }
    }
}
