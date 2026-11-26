// Elementia map builder - volcanoes with integrated lava flows, lava channels / pools / moat with raised banks,
// basalt formations and the Boss Arena ("Altar of Embers").
using System.Collections.Generic;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        static void TriUp(MB mb, Vector3 a, Vector3 b, Vector3 c, Vector2 u)
        {
            if (Vector3.Cross(b - a, c - a).y < 0) mb.Tri(a, c, b, u); else mb.Tri(a, b, c, u);
        }
        static void QuadDS(MB mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 u) { mb.Quad(a, b, c, d, u); mb.Quad(a, d, c, b, u); }

        // ---------------- Volcano cone: lava flows are raised ribs that belong to the cone mesh ----------------
        static GameObject BuildCone(string name, Vector2 c, float r, float h, bool active, Transform parent, float seed, int streaks = 1, float smokeScale = 1.6f, float[] streakAngles = null, bool randomYaw = true)
        {
            var mb = new MB();
            int seg = Mathf.Clamp(Mathf.RoundToInt(2f * Mathf.PI * r / 2.4f), 14, 52);
            float[] rf = { 1.0f, 0.84f, 0.64f, 0.44f, 0.3f, 0.22f };
            float[] hf = { -0.5f, 0.2f, 0.52f, 0.83f, 1.0f, 0.84f };
            int L = rf.Length;
            var p = new Vector3[L, seg];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                for (int l = 0; l < L; l++)
                {
                    float jr = 1f + 0.12f * (Perlin(a * 3f, l * 2.3f, 1f, seed) - 0.5f);
                    float jh = (l > 0 && l < 4) ? (Perlin(a * 4f, l * 1.3f, 1f, seed + 5f) - 0.5f) * Mathf.Min(0.06f * h, 1.2f) : 0f;
                    float rr = r * rf[l] * jr;
                    p[l, i] = new Vector3(Mathf.Cos(a) * rr, hf[l] * h + jh, Mathf.Sin(a) * rr);
                }
            }
            // which segment columns carry lava
            var flow = new HashSet<int>(); var rim = new HashSet<int>();
            if (active)
            {
                for (int s = 0; s < streaks; s++)
                {
                    float ang = streakAngles != null && s < streakAngles.Length ? streakAngles[s] : Rand(0, 360);
                    int idx = ((Mathf.RoundToInt(ang / 360f * seg) % seg) + seg) % seg;
                    flow.Add(idx);
                }
                foreach (int f in flow) { rim.Add((f + 1) % seg); rim.Add((f - 1 + seg) % seg); }
                foreach (int f in flow) rim.Remove(f);
            }
            float rib = Mathf.Clamp(r * 0.03f, 0.2f, 0.7f);
            for (int i = 0; i < seg; i++)
            {
                int i1 = (i + 1) % seg;
                for (int l = 0; l < L - 1; l++)
                {
                    float t0 = 1f - l / 4f, t1 = 1f - (l + 1) / 4f;
                    Vector3 a = p[l, i], b = p[l + 1, i], cc = p[l + 1, i1], d = p[l, i1];
                    if (l == 4) { mb.Quad(a, b, cc, d, UV(C_Crust, 0.3f)); continue; } // crater lip (inner wall)
                    if (flow.Contains(i) && l < 4)
                    {
                        // dark crust bed over the whole segment, then a raised, meandering lava tongue inside it
                        mb.Quad(a, b, cc, d, UV(C_Crust, 0.2f));
                        Vector3 n = Vector3.Cross(b - a, d - a).normalized;
                        if (n.y < 0) n = -n;
                        Vector3 o = n * rib;
                        float mBot = 0.5f + 0.16f * Mathf.Sin(l * 1.9f + i), mTop = 0.5f + 0.16f * Mathf.Sin((l + 1) * 1.9f + i);
                        float wBot = Mathf.Lerp(0.42f, 0.22f, l / 4f), wTop = Mathf.Lerp(0.42f, 0.22f, (l + 1) / 4f);
                        Vector3 a2 = Vector3.Lerp(a, d, mBot - wBot) + o, d2 = Vector3.Lerp(a, d, mBot + wBot) + o;
                        Vector3 b2 = Vector3.Lerp(b, cc, mTop - wTop) + o, c2 = Vector3.Lerp(b, cc, mTop + wTop) + o;
                        Vector3 a1 = Vector3.Lerp(a, d, mBot - wBot), d1 = Vector3.Lerp(a, d, mBot + wBot), b1 = Vector3.Lerp(b, cc, mTop - wTop), c1 = Vector3.Lerp(b, cc, mTop + wTop);
                        mb.lava = true;
                        float tb = Mathf.Lerp(0.3f, 0.6f, l / 3f), tt = Mathf.Lerp(0.3f, 0.6f, (l + 1) / 3f);
                        mb.Quad(a2, b2, c2, d2, UV(C_Lava, tb), UV(C_Lava, tt), UV(C_Lava, tt), UV(C_Lava, tb));
                        mb.lava = false;
                        QuadDS(mb, a1, b1, b2, a2, UV(C_Crust, 0.3f));
                        QuadDS(mb, d1, c1, c2, d2, UV(C_Crust, 0.3f));
                        continue;
                    }
                    Cell cell = rim.Contains(i) ? C_Crust : C_ConeSide;
                    mb.Quad(a, b, cc, d, UV(cell, t0), UV(cell, t1), UV(cell, t1), UV(cell, t0));
                }
                // crater floor
                Vector3 cf = new Vector3(0, (active ? 0.86f : 0.78f) * h, 0);
                if (active) { mb.lava = true; mb.Tri(p[L - 1, i], cf, p[L - 1, i1], UV(C_Lava, 0.55f)); mb.lava = false; }
                else mb.Tri(p[L - 1, i], cf, p[L - 1, i1], UV(C_Crust, 0.5f));
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = new Vector3(c.x, H(c.x, c.y), c.y);
            go.transform.rotation = Quaternion.Euler(0, randomYaw ? Rand(0, 360) : 0f, 0);
            if (active)
            {
                var smoke = SpawnPrefab(FX + "Smoke/Dark/SmokeDarkSoft.prefab", go.transform, go.transform.position + Vector3.up * (0.95f * h), 0, smokeScale);
                if (smoke != null) smoke.name = "FX_Smoke";
            }
            return go;
        }

        // ---------------- Rock massif: a cluster of faceted, leaning spires of mixed size (stylized low-poly) ----------------
        static GameObject BuildCrag(string name, Vector2 c, float R, float H0, bool hot, Transform parent, float seed)
        {
            var mb = new MB();
            int spires = 3 + Mathf.RoundToInt(R / 3.5f);
            for (int s = 0; s < spires; s++)
            {
                Vector2 o; float r, h;
                if (s == 0) { o = new Vector2(Rand(-0.12f, 0.12f), Rand(-0.12f, 0.12f)) * R; r = R * Rand(0.5f, 0.62f); h = H0; }
                else
                {
                    float a = (s * 137.5f + Rand(-25f, 25f)) * Mathf.Deg2Rad, dist = R * Rand(0.35f, 0.72f);
                    o = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * dist;
                    r = R * Rand(0.24f, 0.42f); h = H0 * Rand(0.3f, 0.72f) * (1f - 0.35f * dist / R);
                }
                int seg = RandI(5, 9);
                float[] rf = { 1.0f, 0.66f, 0.34f }, hf = { -0.12f, 0.38f, 0.74f };
                var ring = new Vector3[3, seg];
                Vector3 lean = new Vector3(Rand(-0.14f, 0.14f), 0, Rand(-0.14f, 0.14f)) * r;
                for (int l = 0; l < 3; l++)
                    for (int i = 0; i < seg; i++)
                    {
                        float a = (i + Perlin(i * 1.7f, l * 2.9f, 1f, seed + s) * 0.35f) * Mathf.PI * 2f / seg;
                        float rr = r * rf[l] * (0.8f + 0.4f * Perlin(i * 2.3f, l * 1.1f, 1f, seed + s * 3.1f));
                        float yy = h * hf[l] + (l == 0 ? 0f : (Perlin(i * 3.1f, l * 0.7f, 1f, seed + s * 1.7f) - 0.5f) * h * 0.12f);
                        ring[l, i] = new Vector3(o.x + Mathf.Cos(a) * rr, yy, o.y + Mathf.Sin(a) * rr) + lean * (hf[l] + 0.12f);
                    }
                Vector3 apex = new Vector3(o.x, h, o.y) + lean * 1.1f + new Vector3(Rand(-0.08f, 0.08f), 0, Rand(-0.08f, 0.08f)) * r;
                int lavaFace = hot && s == 0 ? RandI(0, seg) : -1;
                for (int i = 0; i < seg; i++)
                {
                    int i1 = (i + 1) % seg;
                    if (i == lavaFace)
                    {   // glowing fissure down the lower flank
                        mb.Quad(ring[0, i], ring[1, i], ring[1, i1], ring[0, i1], UV(C_Crust, 0.3f));
                        Vector3 m0 = Vector3.Lerp(ring[0, i], ring[0, i1], 0.5f), m1 = Vector3.Lerp(ring[1, i], ring[1, i1], 0.5f);
                        Vector3 nrm = Vector3.Cross(ring[1, i] - ring[0, i], ring[0, i1] - ring[0, i]).normalized;
                        if (Vector3.Dot(nrm, new Vector3(m0.x - o.x, 0, m0.z - o.y)) < 0) nrm = -nrm;
                        Vector3 w0 = (ring[0, i1] - ring[0, i]) * 0.18f, w1 = (ring[1, i1] - ring[1, i]) * 0.1f, off = nrm * 0.12f;
                        mb.lava = true;
                        mb.Quad(m0 - w0 + off, m1 - w1 + off, m1 + w1 + off, m0 + w0 + off, UV(C_Lava, 0.35f), UV(C_Lava, 0.6f), UV(C_Lava, 0.6f), UV(C_Lava, 0.35f));
                        mb.lava = false;
                    }
                    else mb.Quad(ring[0, i], ring[1, i], ring[1, i1], ring[0, i1], UV(C_ConeSide, 0.85f), UV(C_ConeSide, 0.45f), UV(C_ConeSide, 0.45f), UV(C_ConeSide, 0.85f));
                    mb.Quad(ring[1, i], ring[2, i], ring[2, i1], ring[1, i1], UV(C_ConeSide, 0.45f), UV(C_ConeSide, 0.1f), UV(C_ConeSide, 0.1f), UV(C_ConeSide, 0.45f));
                    mb.Tri(ring[2, i], apex, ring[2, i1], UV(C_Crust, 0.15f));
                }
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = new Vector3(c.x, H(c.x, c.y) - 0.3f, c.y);
            go.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            if (hot)
            {
                var sm = SpawnPrefab(FX + "Smoke/Dark/SmokeDarkSoft.prefab", go.transform, go.transform.position + Vector3.up * H0 * 0.6f, 0, 1.2f);
                if (sm != null) sm.name = "FX_FissureSmoke";
            }
            return go;
        }

        // ---------------- Lava channel: raised crust banks + lava bed (bright core, dark edges) ----------------
        static GameObject BuildLavaChannel(string name, List<Vector2> pts, float width, Transform parent)
        {
            var mb = new MB();
            float hw = width * 0.5f;
            float[] off = { -(hw + 1.7f), -(hw + 0.55f), -hw, -hw * 0.45f, 0f, hw * 0.45f, hw, hw + 0.55f, hw + 1.7f };
            float[] hy = { 0.0f, 0.62f, 0.32f, 0.27f, 0.25f, 0.27f, 0.32f, 0.62f, 0.0f };
            float[] lt = { 0, 0, 0.2f, 0.42f, 0.58f, 0.42f, 0.2f, 0, 0 };
            int C = off.Length;
            Vector3[] prev = null;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 fwd = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                if (i > 0 && i < pts.Count - 1) fwd = (pts[i + 1] - pts[i - 1]).normalized;
                Vector2 n = new Vector2(-fwd.y, fwd.x);
                float taper = Mathf.Clamp01(Mathf.Min(i, pts.Count - 1 - i) / 3f) * 0.4f + 0.6f;
                float wob = 1f + 0.15f * (Perlin(pts[i].x, pts[i].y, 0.3f, 4f) - 0.5f);
                var row = new Vector3[C];
                for (int j = 0; j < C; j++)
                {
                    float o = off[j] * taper * (Mathf.Abs(off[j]) > hw + 0.1f ? wob : 1f);
                    Vector2 q = pts[i] + n * o;
                    row[j] = new Vector3(q.x, H(q.x, q.y) + hy[j], q.y);
                }
                if (prev != null)
                    for (int j = 0; j < C - 1; j++)
                    {
                        bool isLava = j >= 2 && j <= 5;
                        if (isLava)
                        {
                            mb.lava = true;
                            Vector2 ua = UV(C_Lava, lt[j]), ub = UV(C_Lava, lt[j + 1]);
                            if (Vector3.Cross(row[j] - prev[j], prev[j + 1] - prev[j]).y >= 0) mb.Quad(prev[j], row[j], row[j + 1], prev[j + 1], ua, ua, ub, ub);
                            else mb.Quad(prev[j], prev[j + 1], row[j + 1], row[j], ua, ub, ub, ua);
                            mb.lava = false;
                        }
                        else QuadUp(mb, prev[j], row[j], row[j + 1], prev[j + 1], UV(C_Crust, j == 0 || j == C - 2 ? 0.6f : 0.25f));
                    }
                prev = row;
            }
            return MeshGO(name, mb.ToMesh(name), matEnv, parent, false);
        }

        static GameObject BuildLavaPool(string name, Vector2 c, float r, Transform parent, float seed)
        {
            var mb = new MB();
            float[] rr = { 0f, 0.55f, 1.0f, 1.0f, 1.0f };
            float[] ra = { 0f, 0f, 0f, 0.6f, 1.8f };
            float[] hy = { 0.24f, 0.27f, 0.32f, 0.64f, 0.0f };
            float[] lt = { 0.6f, 0.45f, 0.2f, 0, 0 };
            int seg = Mathf.Clamp(Mathf.RoundToInt(2f * Mathf.PI * r / 1.5f), 18, 60);
            var ring = new Vector3[rr.Length, seg];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                float nr = r * (0.85f + 0.3f * Mathf.PerlinNoise(Mathf.Cos(a) * 1.4f + seed, Mathf.Sin(a) * 1.4f + seed));
                for (int k = 0; k < rr.Length; k++)
                {
                    float rad = nr * rr[k] + ra[k];
                    Vector2 q = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                    ring[k, i] = new Vector3(q.x, HLow(c.x, c.y) + hy[k], q.y);
                }
            }
            for (int i = 0; i < seg; i++)
            {
                int i1 = (i + 1) % seg;
                mb.lava = true;
                TriUp(mb, ring[0, i], ring[1, i], ring[1, i1], UV(C_Lava, lt[0]));
                QuadUp(mb, ring[1, i], ring[2, i], ring[2, i1], ring[1, i1], UV(C_Lava, lt[1]));
                mb.lava = false;
                QuadUp(mb, ring[2, i], ring[3, i], ring[3, i1], ring[2, i1], UV(C_Crust, 0.25f));
                QuadUp(mb, ring[3, i], ring[4, i], ring[4, i1], ring[3, i1], UV(C_Crust, 0.6f));
            }
            return MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
        }

        // ring of lava (moat) at a fixed base height, with an opening
        static GameObject BuildLavaRing(string name, Vector2 c, float r0, float r1, float baseY, float gapAngDeg, float gapWidthDeg, Transform parent)
        {
            var mb = new MB();
            float mid = (r0 + r1) * 0.5f;
            float[] rad = { r0 - 1.4f, r0 - 0.5f, r0, mid, r1, r1 + 0.5f, r1 + 1.8f };
            float[] hy = { 0.0f, 0.6f, 0.3f, 0.24f, 0.3f, 0.6f, 0.0f };
            float[] lt = { 0, 0, 0.2f, 0.58f, 0.2f, 0, 0 };
            int seg = 96;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * 360f / seg, a1 = (i + 1) * 360f / seg;
                if (Mathf.Abs(Mathf.DeltaAngle((a0 + a1) * 0.5f, gapAngDeg)) < gapWidthDeg * 0.5f) continue;
                Vector3 d0 = new Vector3(Mathf.Cos(a0 * Mathf.Deg2Rad), 0, Mathf.Sin(a0 * Mathf.Deg2Rad)), d1 = new Vector3(Mathf.Cos(a1 * Mathf.Deg2Rad), 0, Mathf.Sin(a1 * Mathf.Deg2Rad));
                Vector3 cc = new Vector3(c.x, baseY, c.y);
                for (int k = 0; k < rad.Length - 1; k++)
                {
                    Vector3 a = cc + d0 * rad[k] + Vector3.up * hy[k], b = cc + d0 * rad[k + 1] + Vector3.up * hy[k + 1], e = cc + d1 * rad[k + 1] + Vector3.up * hy[k + 1], f = cc + d1 * rad[k] + Vector3.up * hy[k];
                    bool isLava = k == 2 || k == 3;
                    if (isLava) { mb.lava = true; mb.Quad(a, f, e, b, UV(C_Lava, lt[k]), UV(C_Lava, lt[k]), UV(C_Lava, lt[k + 1]), UV(C_Lava, lt[k + 1])); mb.lava = false; }
                    else QuadUp(mb, a, b, e, f, UV(C_Crust, k == 0 || k == rad.Length - 2 ? 0.6f : 0.25f));
                }
            }
            return MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
        }

        // ---------------- Basalt / obsidian formations ----------------
        static GameObject BasaltCluster(string name, Vector2 c, float scale, Transform parent, bool glowing = false)
        {
            var mb = new MB();
            int n = RandI(4, 9);
            for (int i = 0; i < n; i++)
            {
                Vector2 o = new Vector2(Rand(-1f, 1f), Rand(-1f, 1f)) * 1.3f * scale;
                float rad = Rand(0.45f, 0.85f) * scale, hgt = Rand(1.2f, 4.8f) * scale * (i == 0 ? 1.3f : 1f);
                mb.Cyl(new Vector3(o.x, -0.6f, o.y), rad, rad * 0.92f, hgt + 0.6f, 6, C_ConeSide, C_Crust);
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(c);
            go.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            return go;
        }
        static GameObject Obelisk(string name, Vector2 c, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.3f, 0), new Vector3(2.2f, 0.6f, 2.2f), C_ConeSide);
            mb.Cyl(new Vector3(0, 0.6f, 0), 0.75f, 0.5f, 5.2f, 6, C_ConeSide, C_Crust);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(c);
            var cm = new MB(); Shard(cm, new Vector3(0, 5.6f, 0), new Vector3(0, 7.6f, 0), 0.45f, C_Red);
            var cr = MeshGO(name + "_Crystal", cm.ToMesh(name + "_Crystal"), matCrystal, go.transform, false);
            cr.transform.localPosition = Vector3.zero; cr.transform.localRotation = Quaternion.identity;
            return go;
        }
        static GameObject BrazierPost(string name, Vector3 pos, float scale, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.2f, 0), new Vector3(1.1f, 0.4f, 1.1f), C_ConeSide);
            mb.Cyl(new Vector3(0, 0.4f, 0), 0.32f, 0.26f, 1.5f, 6, C_ConeSide, C_ConeSide);
            mb.Cyl(new Vector3(0, 1.9f, 0), 0.35f, 0.75f, 0.45f, 8, C_Crust, C_Crust);
            mb.lava = true; mb.Cyl(new Vector3(0, 2.3f, 0), 0.62f, 0.62f, 0.02f, 8, C_Lava, C_Lava); mb.lava = false;
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.localScale = Vector3.one * scale;
            var f = SpawnPrefab(FX + "Fire/Cartoon/Tall/ToonTallFireRed.prefab", go.transform, pos + Vector3.up * 2.35f * scale, 0, 0.5f * scale);
            if (f != null) f.name = "FX_Fire";
            return go;
        }
        static GameObject ObsidianPillar(string name, Vector3 pos, float height, Transform parent)
        {
            var mb = new MB();
            mb.Cyl(new Vector3(0, -0.5f, 0), 1.5f, 1.4f, 1.1f, 8, C_ConeSide, C_Crust);
            mb.Cyl(new Vector3(0, 0.6f, 0), 1.0f, 0.75f, height, 6, C_ConeSide, C_Crust);
            mb.Cyl(new Vector3(0, 0.6f + height, 0), 1.0f, 1.25f, 0.5f, 6, C_Crust, C_Crust);
            mb.lava = true; mb.Cyl(new Vector3(0, 1.1f + height, 0), 1.1f, 1.1f, 0.02f, 6, C_Lava, C_Lava);
            // glowing vein on the pillar faces
            for (int k = 0; k < 3; k++)
            {
                float a = k * 120f * Mathf.Deg2Rad + 0.52f; Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                mb.Box(d * 0.93f + Vector3.up * (0.6f + height * 0.45f), new Vector3(0.18f, height * 0.75f, 0.12f), C_Lava, -a * Mathf.Rad2Deg + 90f);
            }
            mb.lava = false;
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos;
            var f = SpawnPrefab(FX + "Fire/Cartoon/Tall/ToonTallFireRed.prefab", go.transform, pos + Vector3.up * (1.15f + height), 0, 0.9f);
            if (f != null) f.name = "FX_PillarFire";
            return go;
        }

        // ---------------- Boss Arena: "Altar of Embers" ----------------
        // Basalt tiles floating on lava (glowing seams), central dais with the elemental seal, ring of obsidian
        // fire pillars + broken walls, lava moat, grand entrance with a stone bridge and a backdrop of basalt spires.
        static void ArenaTiles(MB mb, Vector2 c, float y, float rIn, float rOut, float tileLen, float gap, Cell cell, Cell accent, int accentEvery, float skirt)
        {
            Vector3 C = V3(c, 0);
            int rings = Mathf.Max(1, Mathf.RoundToInt((rOut - rIn) / 2.5f));
            for (int k = 0; k < rings; k++)
            {
                float r0 = rIn + (rOut - rIn) * k / rings + gap * 0.5f, r1 = rIn + (rOut - rIn) * (k + 1) / rings - gap * 0.5f, rm = (r0 + r1) * 0.5f;
                int seg = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * rm / tileLen));
                float off = (k % 2) * 0.5f;
                for (int i = 0; i < seg; i++)
                {
                    float ga = gap * 0.5f / rm;
                    float a0 = (i + off) * Mathf.PI * 2f / seg + ga, a1 = (i + 1 + off) * Mathf.PI * 2f / seg - ga;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                    Vector3 p0 = C + d0 * r0 + Vector3.up * y, p1 = C + d1 * r0 + Vector3.up * y, p2 = C + d1 * r1 + Vector3.up * y, p3 = C + d0 * r1 + Vector3.up * y;
                    bool acc = accentEvery > 0 && (k % accentEvery == accentEvery - 1);
                    float tv = ((i * 37 + k * 11) % 7) / 7f;
                    QuadUp(mb, p0, p1, p2, p3, UV(acc ? accent : cell, 0.2f + tv * 0.5f));
                    Vector3 dn = Vector3.down * skirt; Vector2 us = UV(C_Crust, 0.4f);
                    QuadDS(mb, p0, p1, p1 + dn, p0 + dn, us); QuadDS(mb, p1, p2, p2 + dn, p1 + dn, us);
                    QuadDS(mb, p2, p3, p3 + dn, p2 + dn, us); QuadDS(mb, p3, p0, p0 + dn, p3 + dn, us);
                }
            }
        }

        static void BuildBossArena(Transform root)
        {
            var arena = Group("Zone_05_BossArena", root);
            Vector2 entryP = paths.Find(p => p.name == "Path_Volcano_Main").pts[paths.Find(p => p.name == "Path_Volcano_Main").pts.Count - 1];
            Vector2 toEntry = (entryP - ArenaC).normalized;
            float entryAng = Mathf.Atan2(toEntry.y, toEntry.x) * Mathf.Rad2Deg;
            float y = 1.0f, baseY = 0.4f;

            // lava sea under the floor (seen through the seams)
            var lavaBed = new MB(); lavaBed.lava = true;
            for (int i = 0; i < 64; i++)
            {
                float a0 = i * Mathf.PI * 2f / 64, a1 = (i + 1) * Mathf.PI * 2f / 64;
                Vector3 cc = V3(ArenaC, y - 0.22f);
                TriUp(lavaBed, cc, cc + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (ArenaR + 0.2f), cc + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (ArenaR + 0.2f), UV(C_Lava, 0.45f));
            }
            lavaBed.lava = false;
            MeshGO("Arena_LavaBed", lavaBed.ToMesh("Arena_LavaBed"), matEnv, arena.transform, false);

            var mb = new MB();
            // floor tiles with glowing seams
            ArenaTiles(mb, ArenaC, y, 3.6f, ArenaR - 0.6f, 2.6f, 0.22f, C_ConeSide, C_PlazaDark, 3, 0.2f);
            // central dais: seal (red/gold) surrounded by a glowing ring
            mb.Cyl(V3(ArenaC, y - 0.2f), 3.4f, 3.3f, 0.5f, 8, C_ConeSide, C_ConeSide, false);
            ArenaTiles(mb, ArenaC, y + 0.3f, 2.05f, 3.3f, 1.6f, 0.18f, C_PlazaDark, C_PlazaDark, 0, 0.18f);
            mb.Cyl(V3(ArenaC, y - 0.2f), 1.75f, 1.75f, 0.52f, 16, C_Gold, C_Red);
            for (int k = 0; k < 8; k++)
            {
                float a = k * 45f;
                Vector3 d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                mb.Box(V3(ArenaC, y + 0.34f) + d * 0.95f, new Vector3(0.18f, 0.06f, 1.3f), C_Gold, a);
            }
            // rim curb + outer skirt down to the ground
            int segR = 96;
            for (int i = 0; i < segR; i++)
            {
                float a0 = i * Mathf.PI * 2f / segR, a1 = (i + 1) * Mathf.PI * 2f / segR;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 cc = V3(ArenaC, 0); float yt = y + 0.22f, r0 = ArenaR - 0.6f, r1 = ArenaR + 0.9f;
                QuadUp(mb, cc + d0 * r0 + Vector3.up * yt, cc + d1 * r0 + Vector3.up * yt, cc + d1 * r1 + Vector3.up * yt, cc + d0 * r1 + Vector3.up * yt, UV(C_PlazaDark, 0.6f));
                QuadDS(mb, cc + d0 * r0 + Vector3.up * yt, cc + d1 * r0 + Vector3.up * yt, cc + d1 * r0 + Vector3.up * (y - 0.25f), cc + d0 * r0 + Vector3.up * (y - 0.25f), UV(C_Crust, 0.4f));
                QuadDS(mb, cc + d0 * r1 + Vector3.up * yt, cc + d1 * r1 + Vector3.up * yt, cc + d1 * (r1 + 0.6f) + Vector3.up * (baseY - 0.4f), cc + d0 * (r1 + 0.6f) + Vector3.up * (baseY - 0.4f), UV(C_ConeSide, 0.8f));
            }
            // broken low walls between the pillars (jagged heights)
            int wallSeg = 48;
            for (int i = 0; i < wallSeg; i++)
            {
                float am = (i + 0.5f) * 360f / wallSeg;
                if (Mathf.Abs(Mathf.DeltaAngle(am, entryAng)) < 16f) continue;
                if (Perlin(i * 0.7f, 3.1f, 1f, 2f) < 0.32f) continue; // collapsed parts
                float rad = am * Mathf.Deg2Rad; Vector2 pw = ArenaC + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (ArenaR + 0.35f);
                float hgt = 0.6f + 1.4f * Perlin(i * 0.9f, 1.7f, 1f, 6f);
                mb.Box(V3(pw, y + 0.22f + hgt * 0.5f), new Vector3(0.9f, hgt, 2f * Mathf.PI * (ArenaR + 0.35f) / wallSeg + 0.05f), C_ConeSide, -am, 0.6f, 0.95f);
            }
            MeshGO("Arena_Floor", mb.ToMesh("Arena_Floor"), matEnv, arena.transform, true);

            // obsidian fire pillars
            int pc = 0;
            for (int i = 0; i < 8; i++)
            {
                float a = entryAng + 22.5f + i * 45f;
                if (Mathf.Abs(Mathf.DeltaAngle(a, entryAng)) < 30f) continue;
                Vector2 pp = ArenaC + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * (ArenaR + 1.7f);
                ObsidianPillar("ObsidianPillar_" + (pc++), V3(pp, y - 0.2f), (i % 2 == 0) ? 7.5f : 6f, arena.transform);
            }
            // lava moat with raised banks
            BuildLavaRing("Arena_LavaMoat", ArenaC, ArenaR + 4.2f, ArenaR + 7.2f, baseY, entryAng, 22f, arena.transform);
            // entrance: dark stone bridge + grand gate + flanking braziers
            Vector2 e0 = ArenaC + toEntry * (ArenaR + 0.9f), e1 = ArenaC + toEntry * (ArenaR + 9.5f);
            BuildStoneBridge("Arena_EntranceBridge", e0, e1, 6f, arena.transform, C_ConeSide);
            float yawIn = Mathf.Atan2(-toEntry.x, -toEntry.y) * Mathf.Rad2Deg;
            var gate = BuildGate("Arena_Gate", ArenaC + toEntry * (ArenaR + 1.4f), yawIn, 7.5f, arena.transform, C_ConeSide, C_Crust);
            gate.transform.position = V3(ArenaC + toEntry * (ArenaR + 1.4f), y - 0.05f);
            gate.transform.localScale = Vector3.one * 1.35f;
            Vector2 side = new Vector2(-toEntry.y, toEntry.x);
            foreach (int s in new[] { -1, 1 })
            {
                Vector2 bp = ArenaC + toEntry * (ArenaR + 11.5f) + side * s * 4.4f;
                BrazierPost("Arena_EntryBrazier_" + s, Ground(bp), 1.3f, arena.transform);
            }
            // backdrop: basalt spires behind the arena
            for (int i = 0; i < 9; i++)
            {
                float a = (entryAng + 180f + (i - 4) * 18f) * Mathf.Deg2Rad;
                Vector2 bp = ArenaC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (ArenaR + Rand(9.5f, 12f));
                if (!OnLand(bp, 1.5f)) continue;
                BasaltCluster("Arena_BasaltSpires_" + i, bp, Rand(1.3f, 2.0f), arena.transform);
            }
            var boss = Group("BossSpawn", arena.transform);
            boss.transform.position = V3(ArenaC, y + 0.36f);
            boss.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(toEntry.x, toEntry.y) * Mathf.Rad2Deg, 0);
            Occupy(ArenaC, ArenaR + 13f);
        }
    }
}
