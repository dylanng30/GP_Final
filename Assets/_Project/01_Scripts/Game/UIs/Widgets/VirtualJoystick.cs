using Dylanng.Core;
using Dylanng.Services;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UIs.Widgets
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("--- JOYSTICK COMPONENTS ---")]
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        [Header("--- JOYSTICK SETTINGS ---")]
        [SerializeField] private bool isFloating = true;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.05f;
        [SerializeField] private float handleMoveRadius = 100f;

        private Vector2 _inputVector = Vector2.zero;
        private Vector2 _defaultBackgroundPosition;

        private void Start()
        {

            if (background != null)
            {
                _defaultBackgroundPosition = background.anchoredPosition;
                if (handleMoveRadius <= 0f)
                {
                    handleMoveRadius = background.sizeDelta.x * 0.5f;
                }
            }

            if (isFloating && background != null)
            {
                background.gameObject.SetActive(false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            if (isFloating)
            {
                background.gameObject.SetActive(true);
                RectTransform parentRect = background.parent as RectTransform;
                if (parentRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(
                        parentRect,
                        eventData.position,
                        eventData.pressEventCamera,
                        out Vector3 worldPoint))
                {
                    background.position = worldPoint;
                }
            }

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            OnDrag(eventData);
            
            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager?.SetJoystickActive(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (background == null || handle == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 position))
            {
                Vector2 rawInput = position / handleMoveRadius;
                if (rawInput.magnitude > 1f)
                {
                    rawInput.Normalize();
                }

                if (rawInput.magnitude < deadZone)
                {
                    _inputVector = Vector2.zero;
                }
                else
                {
                    float rescaledMagnitude = (rawInput.magnitude - deadZone) / (1f - deadZone);
                    _inputVector = rawInput.normalized * rescaledMagnitude;
                }

                handle.anchoredPosition = rawInput * handleMoveRadius;

                var inputManager = ServiceLocator.Get<InputManager>();
                inputManager?.SetMovementInput(_inputVector);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _inputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (isFloating && background != null)
            {
                background.anchoredPosition = _defaultBackgroundPosition;
                background.gameObject.SetActive(false);
            }

            var inputManager = ServiceLocator.Get<InputManager>();
            if (inputManager != null)
            {
                inputManager.SetMovementInput(Vector2.zero);
                inputManager.SetJoystickActive(false);
            }
        }

        private void OnDisable()
        {
            _inputVector = Vector2.zero;
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            if (isFloating && background != null)
            {
                background.anchoredPosition = _defaultBackgroundPosition;
                background.gameObject.SetActive(false);
            }

            var inputManager = ServiceLocator.Get<InputManager>();
            if (inputManager != null)
            {
                inputManager.SetMovementInput(Vector2.zero);
                inputManager.SetJoystickActive(false);
            }
        }
    }
}
