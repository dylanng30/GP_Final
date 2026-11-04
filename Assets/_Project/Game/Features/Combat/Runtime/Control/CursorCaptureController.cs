using Dylanng;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Khóa con trỏ ở góc nhìn thứ 3 trên PC. Giữ Left Alt để hiện con trỏ và bấm UI.
    /// </summary>
    public sealed class CursorCaptureController : MonoBehaviour
    {
        private const KeyCode ReleaseKey = KeyCode.LeftAlt;

        private void Update()
        {
            CursorLockMode desired = ShouldCapture() ? CursorLockMode.Locked : CursorLockMode.None;
            if (Cursor.lockState == desired) return;

            Cursor.lockState = desired;
            Cursor.visible = desired != CursorLockMode.Locked;
        }

        private static bool ShouldCapture()
        {
            var camera = CameraController.Instance;
            bool isThirdPerson = camera != null && camera.Mode == CameraMode.ThirdPerson;

            return isThirdPerson && Application.isFocused && !Input.GetKey(ReleaseKey) && IsUsingPcInput();
        }

        private static bool IsUsingPcInput()
        {
            InputManager input = ServiceLocator.Get<InputManager>();
            return input != null && input.UsePCInput;
        }
    }
}
