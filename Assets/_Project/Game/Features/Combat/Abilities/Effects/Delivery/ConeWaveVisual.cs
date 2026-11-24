using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>
    /// Vẽ sóng hình quạt bằng mesh dựng trong code, gồm ba vòng cung: mép sóng sáng rõ,
    /// thân sóng nhạt hơn và đuôi mờ dần để thấy vùng sóng đã quét.
    /// Dùng màu đỉnh nên material cần shader đọc vertex color, ví dụ Sprites/Default.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class ConeWaveVisual : MonoBehaviour
    {
        private const int RingCount = 3;

        [SerializeField, Min(2)] private int arcSegments = 20;
        [SerializeField] private float edgeWidth = 0.7f;
        [SerializeField] private float trailLength = 3f;
        [SerializeField] private float heightOffset = 0.15f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private Color edgeColor = new Color(1f, 0.97f, 0.85f, 1f);
        [SerializeField] private Color bodyColor = new Color(0.8f, 0.6f, 1f, 0.55f);
        [SerializeField] private Color tailColor = new Color(0.8f, 0.6f, 1f, 0f);

        private Mesh _mesh;
        private Vector3[] _vertices;
        private Color[] _colors;
        private Vector3 _direction;
        private float _range;
        private float _halfAngle;
        private float _speed;
        private float _travelled;
        private float _fadeTimer;

        private void Awake()
        {
            _mesh = new Mesh { name = "ConeWave" };
            GetComponent<MeshFilter>().sharedMesh = _mesh;

            int vertexCount = (arcSegments + 1) * RingCount;
            _vertices = new Vector3[vertexCount];
            _colors = new Color[vertexCount];
            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.triangles = BuildTriangles();
        }

        private void OnDestroy()
        {
            Destroy(_mesh);
        }

        public void Play(Vector3 origin, Vector3 direction, float range, float arcAngle, float speed)
        {
            transform.SetPositionAndRotation(origin + Vector3.up * heightOffset, Quaternion.identity);
            _direction = direction;
            _range = range;
            _halfAngle = arcAngle * 0.5f;
            _speed = speed;
            _travelled = 0f;
            _fadeTimer = fadeDuration;
        }

        private void Update()
        {
            if (_travelled < _range)
            {
                _travelled = Mathf.Min(_range, _travelled + _speed * Time.deltaTime);
                Rebuild(1f);
                return;
            }

            _fadeTimer -= Time.deltaTime;
            Rebuild(Mathf.Clamp01(_fadeTimer / fadeDuration));
            if (_fadeTimer <= 0f) Destroy(gameObject);
        }

        private void Rebuild(float alpha)
        {
            float outer = _travelled;
            float edgeStart = Mathf.Max(0f, outer - edgeWidth);
            float tail = Mathf.Max(0f, outer - trailLength);

            float[] radii = { tail, edgeStart, outer };
            Color[] ringColors = { Fade(tailColor, alpha), Fade(bodyColor, alpha), Fade(edgeColor, alpha) };

            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = Mathf.Lerp(-_halfAngle, _halfAngle, (float)i / arcSegments);
                Vector3 ray = Quaternion.AngleAxis(angle, Vector3.up) * _direction;

                for (int ring = 0; ring < RingCount; ring++)
                {
                    int index = i * RingCount + ring;
                    _vertices[index] = ray * radii[ring];
                    _colors[index] = ringColors[ring];
                }
            }

            _mesh.vertices = _vertices;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();
        }

        private static Color Fade(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        /// <summary>Nối từng cặp vòng cung liền nhau thành dải tam giác.</summary>
        private int[] BuildTriangles()
        {
            int bandCount = RingCount - 1;
            var triangles = new int[arcSegments * bandCount * 6];
            int t = 0;

            for (int i = 0; i < arcSegments; i++)
            {
                for (int ring = 0; ring < bandCount; ring++)
                {
                    int innerA = i * RingCount + ring;
                    int outerA = innerA + 1;
                    int innerB = innerA + RingCount;
                    int outerB = innerB + 1;

                    triangles[t++] = innerA;
                    triangles[t++] = outerA;
                    triangles[t++] = outerB;
                    triangles[t++] = innerA;
                    triangles[t++] = outerB;
                    triangles[t++] = innerB;
                }
            }
            return triangles;
        }
    }
}
