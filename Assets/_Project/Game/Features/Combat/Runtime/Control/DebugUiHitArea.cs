using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Vùng màn hình của các bảng debug vẽ bằng IMGUI. EventSystem không biết IMGUI,
    /// nên bấm vào đó sẽ bị coi là bấm vào vùng trống và khóa con trỏ hoặc ra đòn nếu không chặn ở đây.
    /// </summary>
    public static class DebugUiHitArea
    {
        private static readonly List<Rect> Areas = new List<Rect>();
        private static int _frame = -1;

        /// <summary>Gọi trong OnGUI mỗi frame. Rect theo toạ độ GUI: gốc ở góc trên trái.</summary>
        public static void Register(Rect guiRect)
        {
            if (_frame != Time.frameCount)
            {
                _frame = Time.frameCount;
                Areas.Clear();
            }
            Areas.Add(guiRect);
        }

        /// <summary>Điểm theo toạ độ Input.mousePosition: gốc ở góc dưới trái.</summary>
        public static bool Contains(Vector2 screenPoint)
        {
            // Rect đăng ký từ frame trước vẫn dùng được vì OnGUI chạy sau Update.
            if (Time.frameCount - _frame > 1) return false;

            var guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            foreach (Rect area in Areas)
            {
                if (area.Contains(guiPoint)) return true;
            }
            return false;
        }
    }
}
