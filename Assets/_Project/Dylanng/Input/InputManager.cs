using UnityEngine;

namespace Dylanng
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

        public bool UsePCInput { get; private set; } = true;

        public override void Initialize()
        {
            ServiceLocator.Register<InputManager>(this, ServiceScope.Global);
            ServiceLocator.Get<ITickSystem>().Register(this);

            // UnityEngine.Device trả về thông tin của máy giả lập khi chạy trong Device Simulator.
            SetUsePCInput(!UnityEngine.Device.Application.isMobilePlatform);
        }

        /// <summary>Đổi kiểu input và xóa giá trị đang giữ để không còn trôi sang kiểu mới.</summary>
        public void SetUsePCInput(bool usePCInput)
        {
            UsePCInput = usePCInput;

            MovementInput = Vector2.zero;
            RotationInput = Vector2.zero;
            ZoomInput = 0f;
            JumpInput = false;
            SprintInput = false;
            AttackInput = false;
            BlockInput = false;

            EventBus.Publish(new InputModeChangedEvent(usePCInput));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Get<ITickSystem>()?.Unregister(this);
        }

        public void OnUpdate(float deltaTime)
        {
            // Mobile không cần đọc ở đây: nút, joystick và LookSwipeArea tự đưa input vào.
            if (UsePCInput) PollPCKeyboardInput();
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
            /*bool isPointerOverUI = false;
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    isPointerOverUI = true;
                }
                else if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                {
                    isPointerOverUI = true;
                }
            }*/

            // Disable Fire1 mapping to AttackInput to prevent unintended attacks on screen taps
            // AttackInput = !isPointerOverUI && Input.GetButtonDown("Fire1");
            BlockInput = Input.GetButton("Fire2");

            RotationInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            ZoomInput = Input.GetAxis("Mouse ScrollWheel");
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