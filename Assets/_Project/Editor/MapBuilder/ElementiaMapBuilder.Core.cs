// Elementia map builder - core helpers (palette UVs, mesh building, noise, spawning).
// Menu: Tools/Elementia/Build Map  -> regenerates Assets/_Project/01_Scenes/Map/Elementia_Map.unity
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        // ---------- Paths ----------
        const string ScenePath = "Assets/_Project/01_Scenes/Map/Elementia_Map.unity";
        const string GenDir = "Assets/_Project/02_Art/3D/Environment/Generated";
        const string KitDir = "Assets/_Project/02_Art/3D/Environment";
        const string PalettePath = KitDir + "/KIT_01/PaletteGrid_KIT01.png";
        const string HolotnaPrefabs = "Assets/Holotna/Mountain/Prefabs/";

        // Character reference height (Hero prefabs ~2.6-2.9m). Kit pieces are scaled up to match.
        const float KitScale = 1.25f;

        static System.Random rng;
        static float Rand(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static int RandI(int a, int b) { return rng.Next(a, b); }

        // ---------- Palette (PaletteGrid_KIT01: 8 cols x 4 rows, vertical gradient per cell) ----------
        public struct Cell { public int c, r; public float t0, t1; public Cell(int c, int r, float t0, float t1) { this.c = c; this.r = r; this.t0 = t0; this.t1 = t1; } }
        static Vector2 UV(Cell cell, float t)
        {
            float tt = Mathf.Lerp(cell.t0, cell.t1, Mathf.Clamp01(t));
            float u = (cell.c + 0.5f) / 8f;
            float v = 1f - (cell.r + 0.08f + 0.84f * tt) / 4f;
            return new Vector2(u, v);
        }
        static readonly Cell C_Grass = new Cell(6, 3, 0.55f, 0.9f);
        static readonly Cell C_GrassDark = new Cell(5, 2, 0.1f, 0.4f);
        static readonly Cell C_Path = new Cell(7, 3, 0.15f, 0.55f);
        static readonly Cell C_PathDark = new Cell(1, 0, 0.5f, 0.85f);
        static readonly Cell C_Plaza = new Cell(6, 0, 0.25f, 0.45f);
        static readonly Cell C_PlazaDark = new Cell(7, 0, 0.35f, 0.6f);
        static readonly Cell C_Cliff = new Cell(2, 0, 0.0f, 1.0f);
        static readonly Cell C_CliffDark = new Cell(2, 0, 0.55f, 1.0f);
        static readonly Cell C_Crust = new Cell(3, 0, 0.0f, 0.2f);
        static readonly Cell C_Volcanic = new Cell(7, 0, 0.7f, 0.97f);
        static readonly Cell C_Ash = new Cell(2, 0, 0.8f, 1.0f);
        static readonly Cell C_ConeSide = new Cell(7, 0, 0.75f, 1.0f);
        static readonly Cell C_Lava = new Cell(2, 3, 0.45f, 0.7f);
        static readonly Cell C_Water = new Cell(1, 3, 0.25f, 0.75f);
        static readonly Cell C_Wall = new Cell(1, 1, 0.0f, 0.2f);
        static readonly Cell C_Roof = new Cell(5, 1, 0.15f, 0.45f);
        static readonly Cell C_RoofRidge = new Cell(5, 1, 0.7f, 0.9f);
        static readonly Cell C_Wood = new Cell(7, 1, 0.2f, 0.6f);
        static readonly Cell C_WoodDark = new Cell(2, 0, 0.6f, 0.9f);
        static readonly Cell C_Red = new Cell(2, 2, 0.2f, 0.5f);
        static readonly Cell C_Gold = new Cell(6, 1, 0.1f, 0.4f);
        static readonly Cell C_Field = new Cell(5, 0, 0.1f, 0.35f);
        static readonly Cell C_FieldGreen = new Cell(2, 1, 0.6f, 0.95f);
        static readonly Cell C_Stone = new Cell(6, 0, 0.2f, 0.6f);
        static readonly Cell C_Window = new Cell(5, 1, 0.8f, 1.0f);

        // ---------- Mesh builder (flat shaded, palette UVs) ----------
        public class MB
        {
            public List<Vector3> v = new List<Vector3>();
            public List<Vector2> uv = new List<Vector2>();
            public List<int> t = new List<int>();
            public List<int> tLava = new List<int>();   // second submesh rendered with the lava material
            public bool lava;                            // while true, new triangles go to the lava submesh
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); uv.Add(ua); uv.Add(ub); uv.Add(uc);
                var dst = lava ? tLava : t; dst.Add(i); dst.Add(i + 1); dst.Add(i + 2);
            }
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 u) { Tri(a, b, c, u, u, u); }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 u) { Tri(a, b, c, u); Tri(a, c, d, u); }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud) { Tri(a, b, c, ua, ub, uc); Tri(a, c, d, ua, uc, ud); }
            // Axis-aligned (local) box, rotated by yaw around center-bottom
            public void Box(Vector3 center, Vector3 size, Cell cell, float yawDeg = 0f, float tTop = 0.2f, float tBot = 0.8f)
            {
                Quaternion q = Quaternion.Euler(0, yawDeg, 0);
                Vector3 h = size * 0.5f;
                Vector3[] p = new Vector3[8];
                for (int i = 0; i < 8; i++)
                {
                    Vector3 o = new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                    p[i] = center + q * o;
                }
                Vector2 top = UV(cell, tTop), bot = UV(cell, tBot);
                Quad(p[2], p[6], p[7], p[3], top);            // top
                Quad(p[0], p[1], p[5], p[4], bot);            // bottom
                Quad(p[0], p[2], p[3], p[1], bot, top, top, bot); // -z
                Quad(p[5], p[7], p[6], p[4], bot, top, top, bot); // +z
                Quad(p[4], p[6], p[2], p[0], bot, top, top, bot); // -x
                Quad(p[1], p[3], p[7], p[5], bot, top, top, bot); // +x
            }
            // Cylinder / cone frustum
            public void Cyl(Vector3 baseCenter, float rBot, float rTop, float height, int seg, Cell side, Cell cap, bool capTop = true)
            {
                Vector3 topC = baseCenter + Vector3.up * height;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    Vector3 b0 = baseCenter + d0 * rBot, b1 = baseCenter + d1 * rBot, t0 = topC + d0 * rTop, t1 = topC + d1 * rTop;
                    Quad(b0, t0, t1, b1, UV(side, 0.9f), UV(side, 0.1f), UV(side, 0.1f), UV(side, 0.9f));
                    if (capTop && rTop > 0.001f) Tri(topC, t1, t0, UV(cap, 0.3f));
                }
            }
            // copy another builder's triangles, transformed (batches many small props into one mesh)
            public void Append(MB o, Vector3 pos, Quaternion q, float s)
            {
                int b = v.Count;
                foreach (var p in o.v) v.Add(pos + q * (p * s));
                uv.AddRange(o.uv);
                foreach (int i in o.t) t.Add(b + i);
                foreach (int i in o.tLava) tLava.Add(b + i);
            }
            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name };
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(v); m.SetUVs(0, uv);
                if (tLava.Count > 0) { m.subMeshCount = 2; m.SetTriangles(t, 0); m.SetTriangles(tLava, 1); }
                else m.SetTriangles(t, 0);
                m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
                return m;
            }
        }

        // ---------- Materials ----------
        static Material matEnv, matLava, matWater, matCrystal, matSwamp;
        static Material MakeMat(string name, Texture tex, float smooth, Color emission, bool emissive)
        {
            string p = GenDir + "/" + name + ".mat";
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex);
            m.SetFloat("_Smoothness", smooth); m.SetFloat("_Metallic", 0f);
            m.SetFloat("_EnvironmentReflections", smooth > 0.3f ? 1f : 0f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            m.enableInstancing = true;
            AssetDatabase.CreateAsset(m, p);
            return m;
        }

        static GameObject MeshGO(string name, Mesh mesh, Material mat, Transform parent, bool collider = true)
        {
            AssetDatabase.CreateAsset(mesh, GenDir + "/Meshes/" + Sanitize(name) + "_" + (meshCounter++) + ".asset");
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (mesh.subMeshCount > 1) mr.sharedMaterials = new[] { mat, matLava }; else mr.sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }
        static int meshCounter;
        static string Sanitize(string s) { foreach (char c in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(c, '_'); return s.Replace(' ', '_'); }

        // ---------- Asset spawning ----------
        static readonly Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        static GameObject LoadPrefab(string path)
        {
            GameObject g;
            if (!prefabCache.TryGetValue(path, out g)) { g = AssetDatabase.LoadAssetAtPath<GameObject>(path); prefabCache[path] = g; }
            return g;
        }
        static GameObject SpawnPrefab(string path, Transform parent, Vector3 pos, float yaw, float scale)
        {
            var p = LoadPrefab(path);
            if (p == null) { Debug.LogWarning("[ElementiaMap] Missing prefab " + path); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(p, parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * scale;
            return go;
        }
        static GameObject Holotna(string name, Transform parent, Vector3 pos, float yaw, float scale) { return SpawnPrefab(HolotnaPrefabs + name + ".prefab", parent, pos, yaw, scale); }

        // Kit piece = one child of a kit FBX, re-pivoted to bottom-center.
        static readonly Dictionary<string, Transform> kitPieces = new Dictionary<string, Transform>();
        static void IndexKit(string fbxPath)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (root == null) { Debug.LogWarning("[ElementiaMap] Missing kit " + fbxPath); return; }
            foreach (Transform c in root.transform) kitPieces[c.name] = c;
        }
        static GameObject Kit(string piece, Transform parent, Vector3 pos, float yaw, float scale, bool collider = true)
        {
            Transform src;
            if (!kitPieces.TryGetValue(piece, out src)) { Debug.LogWarning("[ElementiaMap] Missing kit piece " + piece); return null; }
            var holder = new GameObject(piece);
            holder.transform.SetParent(parent, false);
            var srcMF = src.GetComponent<MeshFilter>();
            if (srcMF == null) { Object.DestroyImmediate(holder); return null; }
            var child = new GameObject("mesh");
            child.transform.SetParent(holder.transform, false);
            child.transform.localRotation = src.localRotation;
            child.transform.localScale = src.localScale;
            child.AddComponent<MeshFilter>().sharedMesh = srcMF.sharedMesh;
            child.AddComponent<MeshRenderer>().sharedMaterials = src.GetComponent<MeshRenderer>().sharedMaterials;
            // re-pivot: bottom center of bounds at holder origin
            Bounds mb = srcMF.sharedMesh.bounds;
            Vector3[] corners = new Vector3[8]; Bounds b = new Bounds(); bool init = false;
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 w = child.transform.localRotation * Vector3.Scale(c, child.transform.localScale);
                if (!init) { b = new Bounds(w, Vector3.zero); init = true; } else b.Encapsulate(w);
            }
            child.transform.localPosition = -new Vector3(b.center.x, b.min.y, b.center.z);
            if (collider) child.AddComponent<MeshCollider>().sharedMesh = srcMF.sharedMesh;
            holder.transform.position = pos;
            holder.transform.rotation = Quaternion.Euler(0, yaw, 0);
            holder.transform.localScale = Vector3.one * scale;
            return holder;
        }

        static GameObject Group(string name, Transform parent)
        {
            var g = new GameObject(name);
            if (parent != null) g.transform.SetParent(parent, false);
            return g;
        }

        // ---------- Noise ----------
        static float Perlin(float x, float z, float freq, float seed) { return Mathf.PerlinNoise(x * freq + seed, z * freq + seed * 1.7f); }
        static float Fbm(float x, float z, float freq, float seed)
        {
            float s = 0, a = 1, n = 0;
            for (int i = 0; i < 3; i++) { s += (Perlin(x, z, freq, seed + i * 13.1f) - 0.5f) * a; n += a; a *= 0.5f; freq *= 2.1f; }
            return s / n * 2f;
        }
        static float Smooth(float e0, float e1, float x) { float t = Mathf.Clamp01((x - e0) / (e1 - e0)); return t * t * (3 - 2 * t); }

        // ---------- Polyline helpers ----------
        public class Path
        {
            public string name; public List<Vector2> pts; public float width; public Cell cell;
            public Path(string n, float w, Cell c, params Vector2[] p) { name = n; width = w; cell = c; pts = Smoothed(new List<Vector2>(p), 1.0f); }
        }
        static List<Vector2> Smoothed(List<Vector2> ctrl, float step)
        {
            var o = new List<Vector2>();
            if (ctrl.Count < 2) return ctrl;
            for (int i = 0; i < ctrl.Count - 1; i++)
            {
                Vector2 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, ctrl.Count - 1)];
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                    o.Add(0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3));
                }
            }
            o.Add(ctrl[ctrl.Count - 1]);
            return o;
        }
        static float DistToPolyline(Vector2 p, List<Vector2> pts, out Vector2 closest)
        {
            float best = float.MaxValue; closest = pts[0];
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-5f));
                Vector2 c = a + ab * t; float d = (p - c).sqrMagnitude;
                if (d < best) { best = d; closest = c; }
            }
            return Mathf.Sqrt(best);
        }
        static bool SegIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 hit)
        {
            hit = Vector2.zero;
            Vector2 r = b - a, s = d - c; float den = r.x * s.y - r.y * s.x;
            if (Mathf.Abs(den) < 1e-6f) return false;
            float t = ((c.x - a.x) * s.y - (c.y - a.y) * s.x) / den, u = ((c.x - a.x) * r.y - (c.y - a.y) * r.x) / den;
            if (t < 0 || t > 1 || u < 0 || u > 1) return false;
            hit = a + r * t; return true;
        }
        static Vector3 V3(Vector2 p, float y) { return new Vector3(p.x, y, p.y); }
        static Vector2 XZ(Vector3 p) { return new Vector2(p.x, p.z); }
    }
}
