using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>Phóng to dần từ nhỏ tới kích thước gốc mỗi lần bật, dùng cho quả cầu tụ lực.</summary>
    public class GrowOnEnable : MonoBehaviour
    {
        [SerializeField] private float duration = 0.8f;
        [SerializeField, Range(0f, 1f)] private float startScale = 0.15f;
        [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 _baseScale;
        private bool _hasBase;
        private float _elapsed;

        private void OnEnable()
        {
            if (!_hasBase)
            {
                _baseScale = transform.localScale;
                _hasBase = true;
            }
            _elapsed = 0f;
            transform.localScale = _baseScale * startScale;
        }

        private void Update()
        {
            if (_elapsed >= duration) return;

            _elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(_elapsed / duration));
            transform.localScale = _baseScale * Mathf.Lerp(startScale, 1f, t);
        }
    }
}
