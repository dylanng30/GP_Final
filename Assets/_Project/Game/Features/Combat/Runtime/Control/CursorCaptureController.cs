using Dylanng;
using Game.Gameplay.Controllers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Combat
{
    /// <summary>
    /// Khóa con trỏ ở góc nhìn thứ 3 trên PC để chuột xoay camera.
    /// Vào game con trỏ vẫn hiện. Bấm vùng trống để khóa, Left Alt để bật tắt, Esc để thả ra bấm UI.
    /// </summary>
    public sealed class CursorCaptureController : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.LeftAlt;
        private const KeyCode ReleaseKey = KeyCode.Escape;

        public bool WantsCapture { get; private set; }

        public static bool IsPointerOverUi =>
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            || DebugUiHitArea.Contains(Input.mousePosition);

        public void ToggleCapture() => WantsCapture = !WantsCapture;

        public void ReleaseCapture() => WantsCapture = false;

        /// <summary>Khóa khi nhả chuột trên vùng trống, để cú bấm này không thành đòn đánh.</summary>
        public void CaptureFromWorldClick()
        {
            if (!IsPointerOverUi) WantsCapture = true;
        }

        private void Update()
        {
            ReadCaptureRequest();

            bool isCaptured = ShouldCapture();
            Cursor.lockState = isCaptured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isCaptured;
        }

        private void ReadCaptureRequest()
        {
            if (!IsPcThirdPerson()) return;

            if (Input.GetKeyDown(ReleaseKey)) ReleaseCapture();
            else if (Input.GetKeyDown(ToggleKey)) ToggleCapture();
            else if (!WantsCapture && Input.GetMouseButtonUp(0)) CaptureFromWorldClick();
        }

        private bool ShouldCapture()
        {
            return WantsCapture && Application.isFocused && IsPcThirdPerson();
        }

        private static bool IsPcThirdPerson()
        {
            CameraController camera = CameraController.Instance;
            InputManager input = ServiceLocator.Get<InputManager>();

            return camera != null && camera.Mode == CameraMode.ThirdPerson && input != null && input.UsePCInput;
        }
    }
}
