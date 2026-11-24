using Game.Gameplay.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UIs.Widgets
{
    /// <summary>Nút đổi giữa góc nhìn thứ 3 và top-down, nằm ở tầng Tops nên hiện trên mọi màn hình.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class CameraViewToggleButton : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(ToggleView);
        }

        private static void ToggleView()
        {
            CameraController cameraController = CameraController.Instance;
            if (cameraController != null) cameraController.ToggleMode();
        }
    }
}
