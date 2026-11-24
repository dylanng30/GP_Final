using UnityEngine;
using UnityEngine.UI;

namespace Game.Combat.Feedback
{
    /// <summary>Chớp toàn màn hình bằng một Image phủ trên cùng, mờ dần theo thời gian thực.</summary>
    public class ScreenFlash : MonoBehaviour
    {
        private const int SortingOrder = 900;

        private Image _image;
        private Color _color;
        private float _startTime;
        private float _duration;
        private bool _isPlaying;

        public static ScreenFlash Create(Transform parent)
        {
            var root = new GameObject("ScreenFlash");
            root.transform.SetParent(parent, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;

            var imageObject = new GameObject("Flash");
            imageObject.transform.SetParent(root.transform, false);
            var rect = imageObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var flash = root.AddComponent<ScreenFlash>();
            flash._image = imageObject.AddComponent<Image>();
            flash._image.raycastTarget = false;
            flash._image.enabled = false;
            return flash;
        }

        public void Play(Color color, float duration)
        {
            _color = color;
            _duration = Mathf.Max(0.01f, duration);
            _startTime = Time.unscaledTime;
            _isPlaying = true;
            _image.enabled = true;
            _image.color = color;
        }

        private void Update()
        {
            if (!_isPlaying) return;

            float t = (Time.unscaledTime - _startTime) / _duration;
            if (t >= 1f)
            {
                _isPlaying = false;
                _image.enabled = false;
                return;
            }

            Color color = _color;
            color.a *= (1f - t) * (1f - t);
            _image.color = color;
        }
    }
}
