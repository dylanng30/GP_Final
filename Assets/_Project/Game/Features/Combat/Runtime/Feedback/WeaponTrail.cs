using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Vệt sáng bám theo lưỡi vũ khí. Mỗi khung hình lấy vị trí chuôi và mũi của từng lưỡi sau khi animation chạy,
    /// nối các mẫu thành một dải mesh làm mượt bằng Catmull-Rom. Dải chỉ sáng khi lưỡi di chuyển nhanh,
    /// nên tự khớp với nhát chém trong animation mà không cần chỉnh tay từng clip.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponTrail : MonoBehaviour
    {
        private const int MaxSamples = 48;
        private const int Subdivisions = 4;

        private class Blade
        {
            public Transform Transform;
            public Vector3 LocalBase;
            public Vector3 LocalTip;
            public readonly List<Sample> Samples = new List<Sample>();
            public Mesh Mesh;
            public GameObject View;
        }

        private struct Sample
        {
            public Vector3 Base;
            public Vector3 Tip;
            public float Time;
            public float Strength;
        }

        private readonly List<Blade> _blades = new List<Blade>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        private Material _material;
        private Color _color = Color.white;
        private float _lifetime = 0.18f;
        private float _fullSpeed = 6f;
        private float _emitUntil;

        public bool HasBlades => _blades.Count > 0;

        /// <summary>Tìm các lưỡi vũ khí theo tên renderer. Trả về false nếu nhân vật không có vũ khí phù hợp.</summary>
        public bool Initialize(Material material, IList<string> weaponKeywords, Transform viewParent)
        {
            _material = material;
            foreach (MeshRenderer candidate in GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!MatchesKeyword(candidate.name, weaponKeywords) || !candidate.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null) continue;

                ResolveBladeAxis(filter.sharedMesh.bounds, out Vector3 localBase, out Vector3 localTip);
                var blade = new Blade
                {
                    Transform = candidate.transform,
                    LocalBase = localBase,
                    LocalTip = localTip,
                    Mesh = new Mesh { name = "WeaponTrail" }
                };
                blade.Mesh.MarkDynamic();
                blade.View = CreateView(blade.Mesh, viewParent);
                _blades.Add(blade);
            }
            return _blades.Count > 0;
        }

        public void Emit(float duration, Color color, float lifetime, float fullSpeed)
        {
            _color = color;
            _lifetime = Mathf.Max(0.02f, lifetime);
            _fullSpeed = Mathf.Max(0.1f, fullSpeed);
            _emitUntil = Mathf.Max(_emitUntil, Time.time + duration);
        }

        private void LateUpdate()
        {
            bool emitting = Time.time < _emitUntil;
            foreach (Blade blade in _blades)
            {
                if (blade.Transform == null) continue;

                if (emitting) AddSample(blade);
                PruneSamples(blade);
                Rebuild(blade);
            }
        }

        private void AddSample(Blade blade)
        {
            Vector3 basePoint = blade.Transform.TransformPoint(blade.LocalBase);
            Vector3 tipPoint = blade.Transform.TransformPoint(blade.LocalTip);

            float strength = 0f;
            if (blade.Samples.Count > 0)
            {
                Sample last = blade.Samples[blade.Samples.Count - 1];
                float dt = Mathf.Max(0.0001f, Time.time - last.Time);
                // Hitstop làm dt gần 0: giữ nguyên độ sáng cũ thay vì tính ra tốc độ ảo.
                if (Time.time - last.Time < 0.0001f) return;
                strength = Mathf.Clamp01((tipPoint - last.Tip).magnitude / dt / _fullSpeed);
            }

            blade.Samples.Add(new Sample { Base = basePoint, Tip = tipPoint, Time = Time.time, Strength = strength });
            if (blade.Samples.Count > MaxSamples) blade.Samples.RemoveAt(0);
        }

        private void PruneSamples(Blade blade)
        {
            float oldest = Time.time - _lifetime;
            int remove = 0;
            while (remove < blade.Samples.Count && blade.Samples[remove].Time < oldest) remove++;
            if (remove > 0) blade.Samples.RemoveRange(0, remove);
        }

        private void Rebuild(Blade blade)
        {
            blade.Mesh.Clear();
            List<Sample> samples = blade.Samples;
            if (samples.Count < 2) return;

            _vertices.Clear();
            _colors.Clear();
            _uvs.Clear();
            _triangles.Clear();

            for (int i = 0; i < samples.Count - 1; i++)
            {
                Sample p0 = samples[Mathf.Max(0, i - 1)];
                Sample p1 = samples[i];
                Sample p2 = samples[i + 1];
                Sample p3 = samples[Mathf.Min(samples.Count - 1, i + 2)];

                int steps = i == samples.Count - 2 ? Subdivisions + 1 : Subdivisions;
                for (int s = 0; s < steps; s++)
                {
                    float t = (float)s / Subdivisions;
                    Vector3 basePoint = CatmullRom(p0.Base, p1.Base, p2.Base, p3.Base, t);
                    Vector3 tipPoint = CatmullRom(p0.Tip, p1.Tip, p2.Tip, p3.Tip, t);
                    float time = Mathf.Lerp(p1.Time, p2.Time, t);
                    float strength = Mathf.Lerp(p1.Strength, p2.Strength, t);

                    float age = Mathf.Clamp01((Time.time - time) / _lifetime);
                    float alpha = (1f - age) * (1f - age) * strength;

                    _vertices.Add(basePoint);
                    _vertices.Add(tipPoint);
                    _colors.Add(new Color(_color.r, _color.g, _color.b, alpha * 0.25f));
                    _colors.Add(new Color(_color.r, _color.g, _color.b, alpha));
                    _uvs.Add(new Vector2(age, 0.5f));
                    _uvs.Add(new Vector2(age, 0.5f));
                }
            }

            for (int v = 0; v + 3 < _vertices.Count; v += 2)
            {
                _triangles.Add(v);
                _triangles.Add(v + 1);
                _triangles.Add(v + 3);
                _triangles.Add(v);
                _triangles.Add(v + 3);
                _triangles.Add(v + 2);
            }

            blade.Mesh.SetVertices(_vertices);
            blade.Mesh.SetColors(_colors);
            blade.Mesh.SetUVs(0, _uvs);
            blade.Mesh.SetTriangles(_triangles, 0);
            blade.Mesh.RecalculateBounds();
        }

        private GameObject CreateView(Mesh mesh, Transform parent)
        {
            // Mesh dựng trong không gian thế giới nên view đặt ở gốc, không gắn theo nhân vật.
            var view = new GameObject("WeaponTrail_" + name);
            view.transform.SetParent(parent, false);
            view.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = view.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return view;
        }

        /// <summary>Trục dài nhất của mesh là thân lưỡi; đầu xa pivot hơn là mũi, chuôi lấy cách pivot 15% chiều dài.</summary>
        private static void ResolveBladeAxis(Bounds bounds, out Vector3 localBase, out Vector3 localTip)
        {
            Vector3 size = bounds.size;
            int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;

            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 dir = Vector3.zero;
            dir[axis] = 1f;

            bool tipAtMax = Mathf.Abs(max[axis]) >= Mathf.Abs(min[axis]);
            float tipValue = tipAtMax ? max[axis] : min[axis];
            float baseValue = Mathf.Lerp(0f, tipValue, 0.15f);

            localTip = bounds.center;
            localTip[axis] = tipValue;
            localBase = bounds.center;
            localBase[axis] = baseValue;
        }

        private static bool MatchesKeyword(string objectName, IList<string> keywords)
        {
            if (keywords == null) return false;
            foreach (string keyword in keywords)
            {
                if (!string.IsNullOrEmpty(keyword) && objectName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private void OnDestroy()
        {
            foreach (Blade blade in _blades)
            {
                if (blade.View != null) Destroy(blade.View);
                if (blade.Mesh != null) Destroy(blade.Mesh);
            }
        }
    }
}
