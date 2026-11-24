using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>Đẩy một VFX bay thẳng với vận tốc cố định rồi tự hủy. Dùng cho vệt chém bay của sóng kiếm.</summary>
    public sealed class LinearMover : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _travelTime;
        private float _elapsed;
        private float _lingerTime;

        public void Launch(Vector3 velocity, float travelTime, float lingerTime)
        {
            _velocity = velocity;
            _travelTime = Mathf.Max(0.01f, travelTime);
            _lingerTime = Mathf.Max(0f, lingerTime);
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed <= _travelTime)
            {
                transform.position += _velocity * Time.deltaTime;
                return;
            }

            // Dừng lại và để particle tắt dần trước khi hủy.
            if (_elapsed >= _travelTime + _lingerTime) Destroy(gameObject);
        }
    }
}
