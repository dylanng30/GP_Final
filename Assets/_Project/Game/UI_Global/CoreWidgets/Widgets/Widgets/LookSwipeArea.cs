using System.Collections.Generic;
using System.Linq;
using Dylanng;
using Game.Gameplay.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UIs.Widgets
{
    /// <summary>
    /// Vùng trong suốt nằm dưới mọi màn hình: một ngón xoay camera, hai ngón zoom.
    /// Đi qua EventSystem nên nút và joystick nằm trên không bị vuốt nhầm, và chuột trong Editor dùng được như ngón tay.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class LookSwipeArea : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IDragHandler
    {
        private const int PinchPointerCount = 2;

        private readonly Dictionary<int, Vector2> _pointers = new Dictionary<int, Vector2>();
        private Graphic _graphic;

        /// <summary>Đăng ký ở Awake vì UI khởi tạo trước InputManager trong lúc boot.</summary>
        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _graphic.raycastTarget = false;
            EventBus.Subscribe<InputModeChangedEvent>(OnInputModeChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<InputModeChangedEvent>(OnInputModeChanged);
        }

        private void OnDisable()
        {
            _pointers.Clear();
        }

        private void OnInputModeChanged(InputModeChangedEvent evt)
        {
            _graphic.raycastTarget = !evt.UsePCInput;
            _pointers.Clear();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointers[eventData.pointerId] = eventData.position;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pointers.Remove(eventData.pointerId);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pointers.TryGetValue(eventData.pointerId, out Vector2 previousPosition)) return;

            CameraController cameraController = CameraController.Instance;
            if (cameraController != null)
            {
                if (_pointers.Count >= PinchPointerCount) ApplyPinch(cameraController, eventData, previousPosition);
                else cameraController.AddSwipeLook(eventData.delta);
            }

            _pointers[eventData.pointerId] = eventData.position;
        }

        private void ApplyPinch(CameraController cameraController, PointerEventData eventData, Vector2 previousPosition)
        {
            Vector2 otherPosition = _pointers.First(pointer => pointer.Key != eventData.pointerId).Value;

            float previousDistance = Vector2.Distance(previousPosition, otherPosition);
            float currentDistance = Vector2.Distance(eventData.position, otherPosition);

            cameraController.AddPinchZoom(currentDistance - previousDistance);
        }
    }
}
