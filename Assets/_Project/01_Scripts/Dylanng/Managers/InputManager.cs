using UnityEngine;
using Dylanng.Core.Base;
using Dylanng.Core;
using Dylanng.Core.Systems.TickSystem;

namespace Dylanng.Services
{
    public class InputManager : ManagerBase, IUpdatable
    {
        public Vector2 MovementInput { get; private set; }
        public Vector2 RotationInput { get; private set; }
        public float ZoomInput { get; private set; }

        public bool JumpInput { get; private set; }
        public bool SprintInput { get; private set; }
        public bool AttackInput { get; private set; }
        public bool ComboInput { get; private set; }
        public bool BlockInput { get; private set; }
        public bool RunInput { get; private set; }
        public bool IsJoystickActive { get; private set; }

        public bool UsePCInput { get; set; } = true;

        public override void Initialize()
        {
            ServiceLocator.Register<InputManager>(this, ServiceScope.Global);
            ServiceLocator.Get<ITickSystem>().Register(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Get<ITickSystem>().Unregister(this);
        }

        public void OnUpdate(float deltaTime)
        {
            if (UsePCInput)
            {
                PollPCKeyboardInput();
            }
            else
            {
                PollMobileGestures();
            }
        }

        private void PollPCKeyboardInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            Vector2 keyboardMove = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            if (!IsJoystickActive || keyboardMove.sqrMagnitude > 0.01f)
            {
                MovementInput = keyboardMove;
            }

            JumpInput = Input.GetButton("Jump");
            SprintInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Kiểm tra click xuyên qua UI (UI click-through) khi nhấn Joystick hoặc các bảng điều khiển UI
            bool isPointerOverUI = false;
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    isPointerOverUI = true;
                }
                else if (Input.touchCount > 0 && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    isPointerOverUI = true;
                }
            }

            AttackInput = !isPointerOverUI && Input.GetButtonDown("Fire1");
            BlockInput = Input.GetButton("Fire2");

            RotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            ZoomInput = Input.GetAxis("Mouse ScrollWheel");
        }

        private void PollMobileGestures()
        {
            // RotationInput ở Mobile được xử lý và truyền vào từ RotateInput.cs (UI) nên không cần đọc ở đây.

            // Xử lý Pinch-to-zoom (Vuốt 2 ngón) cho Mobile
            ZoomInput = 0f;
            if (Input.touchCount >= 2)
            {
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

                float prevMagnitude = (touch1PrevPos - touch2PrevPos).magnitude;
                float currentMagnitude = (touch1.position - touch2.position).magnitude;

                ZoomInput = currentMagnitude - prevMagnitude;
            }
        }

        //UI Mobile
        public void SetMovementInput(Vector2 input) => MovementInput = input;
        public void SetJoystickActive(bool isActive) => IsJoystickActive = isActive;
        public void SetRotationInput(Vector2 input) => RotationInput = input;
        public void SetZoomInput(float input) => ZoomInput = input;

        public void SetJumpInput(bool isPressed) => JumpInput = isPressed;
        public void SetSprintInput(bool isPressed) => SprintInput = isPressed;
        public void SetAttackInput(bool isPressed) => AttackInput = isPressed;
        public void SetBlockInput(bool isPressed) => BlockInput = isPressed;
        public void SetComboInput(bool isPressed) => ComboInput = isPressed;
        public void SetRunInput(bool isPressed) => RunInput = isPressed;
    }
}