#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Dylanng;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Công cụ test, không có trong bản phát hành: F9 đổi giữa input PC và mobile, kèm dòng nhắc thao tác ở cuối màn hình.
    /// </summary>
    public sealed class InputModeDebugOverlay : MonoBehaviour
    {
        private const KeyCode SwitchKey = KeyCode.F9;
        private const int FontSize = 16;
        private const float LabelHeight = 30f;

        private const string PcFreeCursorHint = "PC  |  Bấm vùng trống: khóa chuột  |  V: đổi camera  |  Tab: đổi hero  |  F9: sang Mobile";
        private const string PcCapturedHint = "PC  |  Alt hoặc Esc: hiện chuột  |  V: đổi camera  |  Tab: đổi hero  |  F9: sang Mobile";
        private const string PcTopDownHint = "PC  |  Bấm chuột trái: đánh  |  V: đổi camera  |  Tab: đổi hero  |  F9: sang Mobile";
        private const string MobileHint = "Mobile  |  Giữ chuột và kéo để giả lập vuốt, joystick để di chuyển  |  F9: sang PC";

        private InputManager _input;
        private GUIStyle _style;

        private InputManager Manager => _input != null ? _input : _input = ServiceLocator.Get<InputManager>();

        private void Update()
        {
            if (Manager != null && Input.GetKeyDown(SwitchKey)) Manager.SetUsePCInput(!Manager.UsePCInput);
        }

        private void OnGUI()
        {
            if (Manager == null) return;

            _style ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = FontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            var area = new Rect(0f, Screen.height - LabelHeight, Screen.width, LabelHeight);
            GUI.Box(area, GetHint(), _style);
        }

        private string GetHint()
        {
            if (!Manager.UsePCInput) return MobileHint;

            CameraController camera = CameraController.Instance;
            if (camera != null && camera.Mode == CameraMode.TopDown) return PcTopDownHint;

            return Cursor.lockState == CursorLockMode.Locked ? PcCapturedHint : PcFreeCursorHint;
        }
    }
}
#endif
