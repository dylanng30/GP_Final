using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>Tia sét gấp khúc giữa hai điểm, đổi hình liên tục rồi mờ dần và tự hủy.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LightningArc : MonoBehaviour
    {
        [SerializeField, Min(2)] private int segments = 10;
        [SerializeField] private float jitter = 0.35f;
        [SerializeField] private float reshapeInterval = 0.04f;

        private LineRenderer _line;
        private Vector3 _from;
        private Vector3 _to;
        private float _lifetime;
        private float _age;
        private float _reshapeTimer;
        private float _startWidth;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = segments + 1;
            _startWidth = _line.widthMultiplier;
        }

        public void Play(Vector3 from, Vector3 to, float lifetime)
        {
            _from = from;
            _to = to;
            _lifetime = Mathf.Max(0.01f, lifetime);
            _age = 0f;

            Reshape();
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            _line.widthMultiplier = _startWidth * (1f - Mathf.Clamp01(_age / _lifetime));

            _reshapeTimer -= Time.deltaTime;
            if (_reshapeTimer <= 0f) Reshape();
        }

        private void Reshape()
        {
            _reshapeTimer = reshapeInterval;

            Vector3 direction = _to - _from;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            Vector3 up = Vector3.Cross(side, direction).normalized;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 point = Vector3.Lerp(_from, _to, t);

                // Hai đầu nằm đúng điểm, phần giữa lệch nhiều nhất.
                float strength = jitter * Mathf.Sin(t * Mathf.PI);
                point += side * Random.Range(-strength, strength) + up * Random.Range(-strength, strength);

                _line.SetPosition(i, point);
            }
        }
    }
}
