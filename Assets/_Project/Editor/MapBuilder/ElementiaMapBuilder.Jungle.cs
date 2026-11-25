// Elementia map builder - Jungle expansion of the starting island (no houses, monster territory).
//   Zone 06 Jungle West  "Whispering Marsh": Forest Edge fight -> camp -> Marsh Heart (swamp pools); spur to the Mirror Lake shore (elite + treasure)
//   Zone 07 Jungle East  "Deep Jungle":      Jungle Trail fight -> Vine Ruins -> pre-boss shrine -> Boss Stage 1; spur to Canopy Hollow (elite + treasure)
//   Zone 08 Boss Stage 1 "Heart of the Grove": mossy stone ring under a giant ancient tree, enclosed by mountains, one canyon entrance.
// Land is contiguous with the village island. Outer rims rise into mountain ridges (natural boundary), the interior has
// rolling hills, isolated peaks, lakes and a swamp; roads and fight clearings run through flat valleys.
using System.Collections.Generic;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        // ---------------- Layout ----------------
        // Boss Stage 1 sits on the north side of the mainland, behind the temple, reached from the north-east.
        static readonly Vector2 Arena2C = new Vector2(14, 202);
        const float Arena2R = 19f, Arena2Y = 0.45f;
        static readonly Vector2 BossApproach = new Vector2(52, 178);   // junction in front of the canyon entrance
        static Vector2 Arena2Entry { get { return Arena2C + (BossApproach - Arena2C).normalized * (Arena2R + 2f); } }
        const int JungleClearStart = 5;
        static List<Island> jungleIslands = new List<Island>();
        static readonly Vector3[] jungleLakes = { new Vector3(-70, 163, 11f), new Vector3(100, 88, 9f) };   // x, z, radius (Mirror Lake NW, east lake)
        const float LakeWaterY = -0.6f;
        static readonly Vector2 SwampC = new Vector2(-90, 76);   // south-west; Marsh Heart clearing sits on its eastern edge (a dry mud island among the pools)
        const float SwampR = 20f, SwampWaterY = -0.45f;
        // x, z, radius, height
        static readonly Vector4[] junglePeaks =
        {
            new Vector4(-100, 128, 13, 14), new Vector4(-104, 100, 10, 10),                                   // west
            new Vector4(-18, 190, 14, 18), new Vector4(-40, 198, 12, 13), new Vector4(44, 212, 12, 15),       // around the boss grove (north)
            new Vector4(62, 142, 12, 11), new Vector4(108, 124, 10, 12),                                       // north-east / east
        };
        // flat pads kept free of relief: west camp, pre-boss shrine, giant tree behind the boss arena
        static readonly Vector3[] jungleFlats = { new Vector3(-88, 108, 7.5f), new Vector3(75, 178, 6.5f), new Vector3(-10, 216, 6f) };

        // ---------------- Palette ----------------
        static readonly Cell C_JGrass = new Cell(6, 3, 0.4f, 0.72f);
        static readonly Cell C_JDark = new Cell(4, 3, 0.05f, 0.45f);
        static readonly Cell C_Moss = new Cell(3, 1, 0.62f, 0.95f);
        static readonly Cell C_RockMoss = new Cell(4, 1, 0.25f, 0.85f);
        static readonly Cell C_RockGrey = new Cell(7, 0, 0.45f, 0.9f);
        static readonly Cell C_Mud = new Cell(7, 1, 0.55f, 0.95f);
        static readonly Cell C_Sand = new Cell(7, 3, 0.1f, 0.5f);
        static readonly Cell C_SwampW = new Cell(3, 2, 0.25f, 0.6f);
        static readonly Cell C_Leaf1 = new Cell(1, 2, 0.05f, 0.45f);
        static readonly Cell C_Leaf2 = new Cell(6, 3, 0.55f, 0.95f);
        static readonly Cell C_Leaf3 = new Cell(4, 3, 0.0f, 0.4f);
        static readonly Cell C_Leaf4 = new Cell(5, 2, 0.15f, 0.6f);
        static readonly Cell C_Bark = new Cell(7, 1, 0.45f, 0.85f);
        static readonly Cell C_Pink = new Cell(4, 0, 0.1f, 0.5f);

        // ---------------- Height field ----------------
        // 0 near roads / clearings / water / pads, 1 in the wild
        static float JungleOpenMask(Vector2 p)
        {
            float m = 1f; Vector2 c;
            foreach (var pa in paths)
            {
                if (!pa.name.StartsWith("Path_Jungle")) continue;
                float hw = pa.width * 0.5f;
                m = Mathf.Min(m, Smooth(hw + 1.5f, hw + 13f, DistToPolyline(p, pa.pts, out c)));
                if (m <= 0f) return 0f;
            }
            foreach (var st in streams) { float hw = st.width * 0.5f; m = Mathf.Min(m, Smooth(hw + 2f, hw + 11f, DistToPolyline(p, st.pts, out c))); }
            for (int i = JungleClearStart; i < clearings.Count; i++) { var cl = clearings[i]; m = Mathf.Min(m, Smooth(cl.z + 1f, cl.z + 11f, Vector2.Distance(p, new Vector2(cl.x, cl.y)))); }
            foreach (var lk in jungleLakes) m = Mathf.Min(m, Smooth(lk.z * 1.35f, lk.z * 1.35f + 9f, Vector2.Distance(p, new Vector2(lk.x, lk.y))));
            foreach (var f in jungleFlats) m = Mathf.Min(m, Smooth(f.z, f.z + 9f, Vector2.Distance(p, new Vector2(f.x, f.y))));
            m = Mathf.Min(m, Smooth(SwampR * 0.85f, SwampR + 8f, Vector2.Distance(p, SwampC)));
            foreach (var f in fields) m = Mathf.Min(m, Smooth(2f, 14f, RectDist(p, f)));   // farmland stays flat
            return m;
        }

        static float JungleRelief(Vector2 p)
        {
            if (jungleIslands == null || jungleIslands.Count == 0) return 0f;
            bool inside = false; float ridge = 0f;
            foreach (var j in jungleIslands)
            {
                float n = j.Norm(p);
                if (n < 1.15f) inside = true;
                ridge = Mathf.Max(ridge, Smooth(0.7f, 0.97f, n) * (1f - Smooth(1.0f, 1.12f, n)));
            }
            if (!inside) return 0f;
            float vn = islands[0].Norm(p);
            float fade = Smooth(0.95f, 1.3f, vn);            // open, gentle seam with the village
            if (fade <= 0f) return 0f;
            float mask = JungleOpenMask(p);
            if (mask <= 0f) return 0f;
            ridge *= Smooth(1.05f, 1.45f, vn) * (0.55f + 0.6f * Perlin(p.x, p.y, 0.045f, 52.3f)) * 14f;   // low hills on the outer rim (forest reaches the coast)
            float hills = 6.5f * Mathf.Max(0f, Fbm(p.x, p.y, 0.03f, 31.7f) + 0.12f);
            foreach (var pk in junglePeaks)
            {
                float d = Vector2.Distance(p, new Vector2(pk.x, pk.y));
                if (d < pk.z) { float t = 1f - d / pk.z; hills = Mathf.Max(hills, pk.w * t * t * (3f - 2f * t) + Perlin(p.x, p.y, 0.2f, 9.1f) * 1.5f); }
            }
            return (hills + ridge) * mask * fade;
        }

        // swamp: low bumpy ground around the water level -> natural pools and mud islets
        static float SwampBlend(Vector2 p, float h)
        {
            float d = Vector2.Distance(p, SwampC);
            if (d >= SwampR) return h;
            float s = 1f - Smooth(SwampR * 0.7f, SwampR, d);
            float g = SwampWaterY - 0.05f + 0.7f * Fbm(p.x, p.y, 0.085f, 41.3f);
            return Mathf.Lerp(h, g, s);
        }

        // lakes are carved last (irregular shore), boss arena platform
        static float JungleFinal(Vector2 p, float h)
        {
            foreach (var lk in jungleLakes)
            {
                Vector2 dv = p - new Vector2(lk.x, lk.y); float d = dv.magnitude;
                if (d > lk.z * 1.6f) continue;
                float ang = Mathf.Atan2(dv.y, dv.x);
                float nr = lk.z * (1f + 0.4f * (Mathf.PerlinNoise(Mathf.Cos(ang) * 1.3f + lk.x * 0.07f, Mathf.Sin(ang) * 1.3f + lk.y * 0.07f) - 0.5f));
                h = Mathf.Min(h, Mathf.Lerp(LakeWaterY - 1.8f, h, Smooth(nr * 0.45f, nr * 1.25f, d)));
            }
            float da = Vector2.Distance(p, Arena2C);
            if (da < Arena2R + 11f) h = Mathf.Lerp(Arena2Y, h, Smooth(Arena2R + 5f, Arena2R + 11f, da));
            return h;
        }

        static Cell JungleGroundCell(Vector2 p, Vector3 a, Vector3 b, Vector3 c, float patch, float t)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized; float up = Mathf.Abs(n.y);
            float y = (a.y + b.y + c.y) / 3f;
            if (islands[0].Norm(p) < 1.12f) return patch > 0.63f ? C_GrassDark : C_Grass;   // blends into the village lawn
            if (up < 0.6f) return patch > 0.45f ? C_RockGrey : C_Cliff;
            if (y > 9f && up < 0.82f) return patch > 0.5f ? C_JDark : C_Moss;
            if (Vector2.Distance(p, SwampC) < SwampR * 0.95f) return y < SwampWaterY + 0.3f ? C_Mud : (patch > 0.5f ? C_Moss : C_JDark);
            foreach (var lk in jungleLakes) if (Vector2.Distance(p, new Vector2(lk.x, lk.y)) < lk.z * 1.5f && y < LakeWaterY + 0.5f) return C_Sand;
            if (Vector2.Distance(p, Arena2C) < Arena2R + 4f) return patch > 0.5f ? C_Moss : C_JGrass;
            return patch > 0.58f ? C_JDark : (t > 0.55f ? C_GrassDark : C_JGrass);
        }

        static bool Dry(Vector2 p, float margin = 0.15f)
        {
            float h = H(p.x, p.y);
            if (Vector2.Distance(p, SwampC) < SwampR * 1.15f && h < SwampWaterY + margin) return false;
            foreach (var lk in jungleLakes) if (Vector2.Distance(p, new Vector2(lk.x, lk.y)) < lk.z * 1.5f && h < LakeWaterY + margin) return false;
            return true;
        }
        static bool InSwamp(Vector2 p) { return Vector2.Distance(p, SwampC) < SwampR; }
        static bool NearRoad(Vector2 p, float margin)
        {
            foreach (var pa in paths) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + margin) return true; }
            return false;
        }

        // ---------------- Batching: many small meshes merged per 40 m cell ----------------
        class Chunks
        {
            public readonly Dictionary<long, MB> map = new Dictionary<long, MB>();
            public string name; public Material mat;
            public Chunks(string n, Material m) { name = n; mat = m; }
            public void Add(MB src, Vector3 pos, float yaw, float s)
            {
                long key = ((long)Mathf.FloorToInt(pos.x / 40f) << 32) ^ (uint)Mathf.FloorToInt(pos.z / 40f);
                MB dst; if (!map.TryGetValue(key, out dst)) { dst = new MB(); map[key] = dst; }
                dst.Append(src, pos, Quaternion.Euler(0, yaw, 0), s);
            }
            public void Flush(Transform parent)
            {
                int i = 0;
                foreach (var kv in map) { string n = name + "_" + (i++); MeshGO(n, kv.Value.ToMesh(n), mat, parent, false); }
            }
        }

        // ---------------- Procedural flora (local space, base at origin) ----------------
        static void DiskDown(MB mb, Vector3 c, float r, int seg, Cell cell)
        {
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                mb.Tri(c, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, UV(cell, 0.85f));
            }
        }
        static void LeafBlob(MB mb, Vector3 cc, float r, Cell leaf)
        {
            mb.Cyl(cc - Vector3.up * 0.35f * r, r * 0.6f, r, 0.35f * r, 8, leaf, leaf, false);
            mb.Cyl(cc, r, r * 0.25f, 0.45f * r, 8, leaf, leaf);
            DiskDown(mb, cc - Vector3.up * 0.35f * r, r * 0.6f, 8, leaf);
        }

        // broadleaf rainforest tree: buttress roots, bent trunk, umbrella canopy (~7-9 m)
        static MB TropicalTemplate(Cell leaf, float hgt)
        {
            var mb = new MB();
            Vector3 lean = new Vector3(Rand(-0.6f, 0.6f), 0, Rand(-0.6f, 0.6f));
            Vector3 mid = Vector3.up * hgt * 0.5f + lean * 0.4f, top = Vector3.up * hgt + lean;
            Branch(mb, Vector3.down * 0.4f, mid, 0.42f, 0.32f, C_Bark);
            Branch(mb, mid, top, 0.32f, 0.2f, C_Bark);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + Rand(-0.3f, 0.3f); Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Branch(mb, Vector3.up * 1.3f + d * 0.15f, d * 1.35f + Vector3.down * 0.25f, 0.24f, 0.07f, C_Bark);
            }
            var ends = new List<Vector3> { top + Vector3.up * 0.4f };
            int limbs = RandI(3, 5);
            for (int i = 0; i < limbs; i++)
            {
                float a = i * Mathf.PI * 2f / limbs + Rand(-0.4f, 0.4f);
                Vector3 s0 = Vector3.Lerp(mid, top, Rand(0.4f, 0.9f));
                Vector3 e = s0 + new Vector3(Mathf.Cos(a) * Rand(1.8f, 2.8f), Rand(0.8f, 1.6f), Mathf.Sin(a) * Rand(1.8f, 2.8f));
                Branch(mb, s0, e, 0.16f, 0.07f, C_Bark); ends.Add(e);
            }
            foreach (var e in ends) LeafBlob(mb, e + Vector3.up * 0.1f, Rand(1.7f, 2.4f), leaf);
            return mb;
        }

        static MB PalmTemplate(float hgt)
        {
            var mb = new MB();
            Vector3 lean = new Vector3(Rand(-1f, 1f), 0, Rand(-1f, 1f)).normalized * Rand(1.0f, 2.2f);
            Vector3 prev = Vector3.down * 0.3f; int seg = 6;
            for (int i = 1; i <= seg; i++)
            {
                float f = i / (float)seg; Vector3 pt = Vector3.up * hgt * f + lean * f * f;
                Branch(mb, prev, pt, Mathf.Lerp(0.32f, 0.2f, (i - 1f) / seg), Mathf.Lerp(0.32f, 0.2f, f), i % 2 == 0 ? C_Bark : C_Wood);
                prev = pt;
            }
            Vector3 crown = prev;
            int fr = 8;
            for (int k = 0; k < fr; k++)
            {
                float a = k * Mathf.PI * 2f / fr + Rand(-0.2f, 0.2f);
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), side = new Vector3(-d.z, 0, d.x);
                float L = Rand(2.8f, 3.6f);
                Vector3 p1 = crown + d * L * 0.45f + Vector3.up * 0.6f, p2 = crown + d * L * 0.8f + Vector3.up * 0.1f, p3 = crown + d * L + Vector3.down * Rand(0.9f, 1.5f);
                Cell lc = k % 2 == 0 ? C_Leaf1 : C_Leaf2;
                TriDS(mb, crown, p1 - side * 0.55f, p1 + side * 0.55f, UV(lc, 0.7f));
                TriDS(mb, p1 - side * 0.55f, p2 - side * 0.45f, p2 + side * 0.45f, UV(lc, 0.5f));
                TriDS(mb, p1 - side * 0.55f, p2 + side * 0.45f, p1 + side * 0.55f, UV(lc, 0.5f));
                TriDS(mb, p2 - side * 0.45f, p3, p2 + side * 0.45f, UV(lc, 0.3f));
            }
            for (int k = 0; k < 3; k++) mb.Box(crown + new Vector3(Mathf.Cos(k * 2.1f), -0.35f, Mathf.Sin(k * 2.1f)) * 0.3f, Vector3.one * 0.3f, C_WoodDark);
            return mb;
        }

        // swamp tree on stilt roots
        static MB MangroveTemplate()
        {
            var mb = new MB(); float rootH = 1.7f, h = Rand(4.5f, 6f);
            Vector3 b0 = Vector3.up * rootH;
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + Rand(-0.2f, 0.2f); Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 knee = b0 * 0.7f + d * Rand(0.9f, 1.3f) + Vector3.up * 0.4f;
                Branch(mb, b0, knee, 0.14f, 0.11f, C_Bark);
                Branch(mb, knee, d * Rand(1.6f, 2.2f) + Vector3.down * 0.5f, 0.11f, 0.08f, C_Bark);
            }
            Vector3 top = Vector3.up * h + new Vector3(Rand(-0.4f, 0.4f), 0, Rand(-0.4f, 0.4f));
            Branch(mb, b0, top, 0.3f, 0.16f, C_Bark);
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + Rand(-0.3f, 0.3f);
                LeafBlob(mb, top + new Vector3(Mathf.Cos(a) * 1.3f, Rand(-0.2f, 0.5f), Mathf.Sin(a) * 1.3f), Rand(1.4f, 1.9f), i == 0 ? C_Leaf3 : C_Leaf4);
            }
            return mb;
        }

        static MB FernTemplate(Cell leaf)
        {
            var mb = new MB(); int n = RandI(6, 9);
            for (int k = 0; k < n; k++)
            {
                float a = k * Mathf.PI * 2f / n + Rand(-0.25f, 0.25f);
                Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), side = new Vector3(-d.z, 0, d.x);
                float L = Rand(0.8f, 1.3f), w = 0.22f;
                Vector3 p0 = Vector3.up * 0.05f, p1 = d * L * 0.45f + Vector3.up * Rand(0.45f, 0.7f), p2 = d * L + Vector3.up * Rand(0.05f, 0.25f);
                TriDS(mb, p0, p1 - side * w, p1 + side * w, UV(leaf, 0.6f));
                TriDS(mb, p1 - side * w, p2, p1 + side * w, UV(leaf, 0.3f));
            }
            return mb;
        }

        static MB ReedTemplate()
        {
            var mb = new MB(); int n = RandI(6, 10);
            for (int k = 0; k < n; k++)
            {
                Vector3 b = new Vector3(Rand(-0.45f, 0.45f), -0.2f, Rand(-0.45f, 0.45f));
                Vector3 tip = b + new Vector3(Rand(-0.25f, 0.25f), Rand(1.1f, 1.9f), Rand(-0.25f, 0.25f));
                Branch(mb, b, tip, 0.035f, 0.015f, k % 2 == 0 ? C_Leaf4 : C_Moss);
                if (k % 3 == 0) Branch(mb, Vector3.Lerp(b, tip, 0.72f), Vector3.Lerp(b, tip, 0.9f), 0.07f, 0.07f, C_Bark);   // cattail
            }
            return mb;
        }

        static MB LilyTemplate(bool flower)
        {
            var mb = new MB(); int seg = 8; float r = Rand(0.45f, 0.7f);
            for (int i = 1; i < seg; i++)   // first wedge left out = the notch
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                mb.Tri(Vector3.zero, new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, UV(C_Leaf2, 0.4f));
            }
            if (flower) mb.Cyl(Vector3.zero, 0.1f, 0.2f, 0.14f, 6, C_Pink, C_Gold);
            return mb;
        }

        static MB GiantTreeTemplate()
        {
            var mb = new MB(); float hT = 13f;
            Vector3 a0 = Vector3.down * 0.5f, a1 = new Vector3(0.6f, 5f, 0.3f), a2 = new Vector3(-0.3f, 10f, 0.5f), a3 = new Vector3(0.2f, hT, 0);
            Branch(mb, a0, a1, 3.2f, 2.4f, C_Bark); Branch(mb, a1, a2, 2.4f, 1.8f, C_Bark); Branch(mb, a2, a3, 1.8f, 1.3f, C_Bark);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7 + Rand(-0.2f, 0.2f); Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                Vector3 knee = Vector3.up * 1.2f + d * Rand(3.2f, 4.2f);
                Branch(mb, Vector3.up * 3.8f + d * 1.4f, knee, 1.2f, 0.7f, C_Bark);
                Branch(mb, knee, d * Rand(6.5f, 8.5f) + Vector3.down * 0.6f, 0.7f, 0.3f, C_Bark);
            }
            var ends = new List<Vector3> { a3 + Vector3.up * 1.5f };
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f + Rand(-0.3f, 0.3f);
                Vector3 s0 = Vector3.Lerp(a2, a3, Rand(0f, 0.8f));
                Vector3 e = s0 + new Vector3(Mathf.Cos(a) * Rand(5.5f, 8f), Rand(2.5f, 4.5f), Mathf.Sin(a) * Rand(5.5f, 8f));
                Branch(mb, s0, e, 0.9f, 0.35f, C_Bark); ends.Add(e);
            }
            int k = 0;
            foreach (var e in ends) { LeafBlob(mb, e + Vector3.up * 0.5f, Rand(4.2f, 5.6f), k % 2 == 0 ? C_Leaf3 : C_Leaf1); k++; }
            return mb;
        }

        class Flora { public List<MB> trop = new List<MB>(), palm = new List<MB>(), mangrove = new List<MB>(), fern = new List<MB>(), reed = new List<MB>(), lily = new List<MB>(); }
        static Flora MakeFlora()
        {
            var f = new Flora();
            Cell[] leaves = { C_Leaf1, C_Leaf3, C_Leaf4, C_Leaf2, C_Leaf3, C_Leaf1, C_Leaf4 };
            for (int i = 0; i < leaves.Length; i++) f.trop.Add(TropicalTemplate(leaves[i], Rand(6f, 8.5f)));
            for (int i = 0; i < 4; i++) f.palm.Add(PalmTemplate(Rand(6.5f, 8.5f)));
            for (int i = 0; i < 3; i++) f.mangrove.Add(MangroveTemplate());
            f.fern.Add(FernTemplate(C_Leaf1)); f.fern.Add(FernTemplate(C_Leaf2)); f.fern.Add(FernTemplate(C_Leaf4)); f.fern.Add(FernTemplate(C_Leaf3));
            for (int i = 0; i < 3; i++) f.reed.Add(ReedTemplate());
            f.lily.Add(LilyTemplate(false)); f.lily.Add(LilyTemplate(false)); f.lily.Add(LilyTemplate(true));
            return f;
        }
        static T Pick<T>(List<T> l) { return l[RandI(0, l.Count)]; }

        static void TrunkCollider(Transform parent, Vector3 basePos, float r, float h)
        {
            var g = new GameObject("TreeCollider"); g.transform.SetParent(parent, false);
            g.transform.position = basePos + Vector3.up * h * 0.5f;
            var c = g.AddComponent<CapsuleCollider>(); c.radius = r; c.height = h;
        }

        // ---------------- Small structures ----------------
        static GameObject FallenLog(string name, Vector2 p, float yaw, float len, Transform parent)
        {
            var mb = new MB();
            Branch(mb, new Vector3(0, 0.35f, -len * 0.5f), new Vector3(0, 0.4f, len * 0.5f), 0.5f, 0.4f, C_Bark);
            mb.Box(new Vector3(0, 0.85f, Rand(-len * 0.2f, len * 0.2f)), new Vector3(0.6f, 0.12f, len * 0.4f), C_Moss);
            Branch(mb, new Vector3(0, 0.6f, len * 0.2f), new Vector3(0.9f, 1.3f, len * 0.25f), 0.15f, 0.06f, C_Bark);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(p) + Vector3.down * 0.15f; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static void MossyCover(Vector3 cl, int count, Transform parent)
        {
            Vector2 c = new Vector2(cl.x, cl.y);
            string[] groups = { "SM_Mossy_Rock_Group4", "SM_Mossy_Rock_Group7", "SM_Mossy_Rock_Group9", "SM_Mossy_Rock_Group11", "SM_Mossy_Rock_Group13" };
            int made = 0;
            for (int t = 0; t < 60 && made < count; t++)
            {
                float a = Rand(0, Mathf.PI * 2f); Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * Rand(0.45f, 0.8f);
                if (NearRoad(p, 1.8f) || !Dry(p)) continue;
                bool clash = false; foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + 1.5f) clash = true;
                if (clash) continue;
                Kit(groups[made % groups.Length], parent, Ground(p) + Vector3.down * 0.15f, Rand(0, 360), Rand(1.8f, 2.5f));
                Occupy(p, 1.6f); made++;
            }
        }

        // logs placed on the clearing border as cover / framing
        static void ClearingLogs(Vector3 cl, int count, Transform parent, string prefix)
        {
            Vector2 c = new Vector2(cl.x, cl.y); int made = 0;
            for (int t = 0; t < 40 && made < count; t++)
            {
                float a = Rand(0, Mathf.PI * 2f); Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * Rand(0.6f, 0.9f);
                if (NearRoad(p, 3.2f) || !Dry(p)) continue;
                bool clash = false; foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + 2.4f) clash = true;
                if (clash) continue;
                FallenLog(prefix + "_FallenLog_" + made, p, FaceYaw(p, c) + 90f + Rand(-20, 20), Rand(3.5f, 4.8f), parent);
                Occupy(p, 2.4f); made++;
            }
        }

        static void EnemyMarkers(string prefix, Vector3 cl, int count, Transform parent)
        {
            Vector2 c = new Vector2(cl.x, cl.y);
            for (int i = 0; i < count; i++)
            {
                float a = (i * 360f / count + 30f) * Mathf.Deg2Rad;
                Marker("EnemySpawn_" + prefix + "_" + i, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * 0.5f, parent);
            }
        }

        static void VineRuins(Vector3 cl, Transform parent)
        {
            Vector2 c = new Vector2(cl.x, cl.y);
            var g = Group("VineRuins", parent);
            var mb = new MB();
            for (int i = 0; i < 10; i++)
            {
                float a = (i * 36f + Rand(-10f, 10f)) * Mathf.Deg2Rad;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * Rand(0.72f, 1.0f);
                if (NearRoad(p, 2.0f)) continue;
                bool clash = false; foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + 1.4f) clash = true;
                if (clash) continue;
                if (i % 3 == 0) Kit("Column" + (2 + (i / 3) % 3), g.transform, Ground(p) + Vector3.down * 0.1f, Rand(0, 360), KitScale * 1.25f);
                else
                {
                    float len = Rand(2.5f, 4f), hgt = Rand(0.8f, 2.4f), yaw = FaceYaw(p, c) + 90f, gy = H(p.x, p.y) - 0.3f;
                    mb.Box(V3(p, gy + hgt * 0.5f), new Vector3(len, hgt, 0.8f), C_Stone, yaw, 0.15f, 0.8f);
                    mb.Box(V3(p, gy + hgt + 0.06f), new Vector3(len + 0.12f, 0.18f, 0.92f), C_Moss, yaw);
                    mb.Box(V3(p, gy + hgt * 0.5f) + Quaternion.Euler(0, yaw, 0) * new Vector3(Rand(-0.8f, 0.8f), 0, 0.42f), new Vector3(0.35f, hgt * 0.9f, 0.08f), C_Leaf1, yaw);   // hanging vine
                }
                Occupy(p, 1.7f);
            }
            MeshGO("VineRuins_Walls", mb.ToMesh("VineRuins_Walls"), matEnv, g.transform, true);
        }

        // ---------------- Zone build ----------------
        static void BuildJungle(Transform root)
        {
            var zW = Group("Zone_06_JungleWest_WhisperingMarsh", root);
            var zE = Group("Zone_07_JungleEast_DeepJungle", root);
            var gpW = Group("Gameplay_JungleWest", zW.transform);
            var gpE = Group("Gameplay_JungleEast", zE.transform);
            var flora = MakeFlora();

            // ---- water ----
            var wW = Group("Water", zW.transform); var wE = Group("Water", zE.transform);
            BuildDisc("Lake_MirrorLake", new Vector2(jungleLakes[0].x, jungleLakes[0].y), jungleLakes[0].z * 1.45f, LakeWaterY, C_Water, matWater, wW.transform, 6.3f, 40);
            BuildDisc("Lake_EastJungle", new Vector2(jungleLakes[1].x, jungleLakes[1].y), jungleLakes[1].z * 1.45f, LakeWaterY, C_Water, matWater, wE.transform, 7.7f, 36);
            BuildClippedDisc("Swamp_Water", SwampC, SwampR * 1.1f, SwampWaterY, C_SwampW, matSwamp, wW.transform, 64);

            BuildBossStage1(root, flora);

            // ---- checkpoints + signposts at the village exits ----
            Vector2 camp = new Vector2(jungleFlats[0].x, jungleFlats[0].y);
            BuildCheckpoint("JungleWestCamp", camp, FaceYaw(camp, new Vector2(-77, 110)), zW.transform, true);
            Vector2 pre = new Vector2(jungleFlats[1].x, jungleFlats[1].y);
            BuildCheckpoint("PreBossShrine_Stage1", pre, FaceYaw(pre, new Vector2(72, 168)), zE.transform, false);
            Vector2 sW = PlaceNear(new Vector2(-53, 97f), 0.8f); Signpost("Signpost_JungleWest", Ground(sW), FaceYaw(sW, PlazaC), zW.transform);
            Vector2 sE = PlaceNear(new Vector2(60, 102.5f), 0.8f); Signpost("Signpost_JungleEast", Ground(sE), FaceYaw(sE, PlazaC), zE.transform);

            // ---- encounters: west ----
            {
                var cl = clearings[5]; var g = Group("ForestEdge", zW.transform);
                ClearingLogs(cl, 2, g.transform, "ForestEdge"); MossyCover(cl, 2, g.transform);
                EnemyMarkers("JungleW_ForestEdge", cl, 3, gpW.transform);
            }
            {
                var cl = clearings[6]; Vector2 c = new Vector2(cl.x, cl.y); var g = Group("MarshHeart", zW.transform);
                for (int i = 0; i < 7; i++)
                {
                    float a = (i * 51f + 15f) * Mathf.Deg2Rad; Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (cl.z + Rand(1.5f, 3.5f));
                    if (!OnLand(p, 1.5f) || NearRoad(p, 1.5f)) continue;
                    BuildDeadTree("SwampDeadTree_" + i, p, Rand(1.0f, 1.4f), g.transform); Occupy(p, 1.4f);
                }
                foreach (var o in new[] { new Vector2(-6, 5), new Vector2(7, -4), new Vector2(-12, -10) })
                {
                    var mist = SpawnPrefab(FX + "Smoke/White/SmokeWhiteSoft.prefab", g.transform, V3(c + o, SwampWaterY + 0.3f), 0, 2.2f); if (mist != null) mist.name = "FX_SwampMist";
                }
                var ff = SpawnPrefab(FX + "Fireflies/FireFliesGreen.prefab", g.transform, V3(c, 1.2f), 0, 2.2f); if (ff != null) ff.name = "FX_SwampFireflies";
                EnemyMarkers("JungleW_MarshHeart", cl, 4, gpW.transform);
                Marker("EliteSpawn_JungleW_MarshHeart", c, gpW.transform);
            }
            {
                var cl = clearings[7]; Vector2 c = new Vector2(cl.x, cl.y); var g = Group("MirrorLakeShore", zW.transform);
                Vector2 lakeC = new Vector2(jungleLakes[0].x, jungleLakes[0].y), toLake = (lakeC - c).normalized, side = new Vector2(-toLake.y, toLake.x);
                // wooden dock into the lake
                var dm = new MB(); Vector2 d0 = c + toLake * (cl.z - 1.2f); float len = 7.5f, dy = 0.18f, dyaw = FaceYaw(c, lakeC);
                dm.Box(V3(d0 + toLake * len * 0.5f, dy), new Vector3(2.2f, 0.16f, len), C_Wood, dyaw);
                for (int k = 0; k < 4; k++) foreach (int s in new[] { -1, 1 })
                    {
                        Vector2 pp = d0 + toLake * (0.6f + k * (len - 1.2f) / 3f) + side * s * 1.0f;
                        dm.Box(V3(pp, dy - 1.4f), new Vector3(0.22f, 2.9f, 0.22f), C_WoodDark, dyaw);
                    }
                MeshGO("MirrorLake_Dock", dm.ToMesh("MirrorLake_Dock"), matEnv, g.transform, true);
                Vector2 shrineP = c + side * (cl.z - 0.5f);
                WaysideShrine("MirrorLake_Shrine", Ground(shrineP), FaceYaw(shrineP, c), g.transform); Occupy(shrineP, 1.8f);
                Vector2 chestP = c - side * (cl.z - 1.8f) + toLake * 1.5f;
                TreasureChest("TreasureChest_MirrorLake", Ground(chestP), FaceYaw(chestP, c), g.transform); Occupy(chestP, 1f);
                Marker("Treasure_MirrorLake", chestP, gpW.transform);
                Marker("EliteSpawn_JungleW_MirrorLake", c, gpW.transform);
            }
            // ---- encounters: east ----
            {
                var cl = clearings[8]; var g = Group("JungleTrail", zE.transform);
                ClearingLogs(cl, 2, g.transform, "JungleTrail"); MossyCover(cl, 2, g.transform);
                EnemyMarkers("JungleE_Trail", cl, 3, gpE.transform);
            }
            {
                var cl = clearings[9];
                VineRuins(cl, zE.transform);
                EnemyMarkers("JungleE_VineRuins", cl, 4, gpE.transform);
            }
            {
                var cl = clearings[10]; Vector2 c = new Vector2(cl.x, cl.y); var g = Group("CanopyHollow", zE.transform);
                var hp = paths.Find(p => p.name == "Path_Jungle_East_Hollow").pts;
                Vector2 entry = hp[Mathf.Max(0, hp.Count - 7)], back = (c - entry).normalized;
                Vector2 chestP = c + back * (cl.z - 2f);
                TreasureChest("TreasureChest_CanopyHollow", Ground(chestP), FaceYaw(chestP, c), g.transform); Occupy(chestP, 1f);
                Marker("Treasure_CanopyHollow", chestP, gpE.transform);
                Marker("EliteSpawn_JungleE_CanopyHollow", c, gpE.transform);
                var ff = SpawnPrefab(FX + "Fireflies/FireFliesYellow.prefab", g.transform, Ground(c) + Vector3.up * 1.5f, 0, 1.6f); if (ff != null) ff.name = "FX_HollowFireflies";
            }

            // ---- wayfinding: stone lanterns along the jungle roads ----
            foreach (var pn in new[] { "Path_Jungle_West", "Path_Jungle_East" })
            {
                var pa = paths.Find(p => p.name == pn); var parent = pn.EndsWith("West") ? zW.transform : zE.transform;
                for (int i = 18, k = 0; i < pa.pts.Count - 8; i += 24, k++)
                {
                    Vector2 fwd = (pa.pts[i + 1] - pa.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                    Vector2 p = pa.pts[i] + n * (pa.width * 0.5f + 1.2f) * (k % 2 == 0 ? 1 : -1);
                    if (!Free(p, 0.6f, 0.2f) || InClearing(p, 0.5f) || !Dry(p)) continue;
                    StoneLantern(pn + "_Lantern_" + k, Ground(p), 0.9f, parent); Occupy(p, 0.8f);
                }
            }

            // ---- water edges ----
            var gwW = new Chunks("JungleWest_WaterPlants", matEnv); var gwE = new Chunks("JungleEast_WaterPlants", matEnv);
            for (int li = 0; li < jungleLakes.Length; li++)
            {
                var lk = jungleLakes[li]; Vector2 lc = new Vector2(lk.x, lk.y); var ch = li == 0 ? gwW : gwE;
                for (int k = 0; k < 70; k++)
                {
                    float a = Rand(0, Mathf.PI * 2f); Vector2 p = lc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * lk.z * Rand(0.85f, 1.25f);
                    float h = H(p.x, p.y);
                    if (Mathf.Abs(h - LakeWaterY) > 0.35f || NearRoad(p, 0.6f) || InClearing(p, 0.3f)) continue;
                    ch.Add(Pick(flora.reed), V3(p, h), Rand(0, 360), Rand(0.8f, 1.2f));
                }
                for (int k = 0; k < 26; k++)
                {
                    float a = Rand(0, Mathf.PI * 2f); Vector2 p = lc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * lk.z * Rand(0.25f, 0.8f);
                    if (H(p.x, p.y) > LakeWaterY - 0.25f) continue;
                    ch.Add(Pick(flora.lily), V3(p, LakeWaterY + 0.05f), Rand(0, 360), Rand(0.8f, 1.3f));
                }
            }
            for (int k = 0, made = 0; k < 1400 && made < 260; k++)
            {
                Vector2 p = SwampC + Random2() * SwampR * 1.05f;
                float h = H(p.x, p.y);
                if (NearRoad(p, 0.5f) || InClearing(p, 0.5f)) continue;
                if (h < SwampWaterY - 0.12f) { if (k % 2 == 0) { gwW.Add(Pick(flora.lily), V3(p, SwampWaterY + 0.05f), Rand(0, 360), Rand(0.8f, 1.3f)); made++; } }
                else if (h < SwampWaterY + 0.3f) { gwW.Add(Pick(flora.reed), V3(p, h), Rand(0, 360), Rand(0.8f, 1.3f)); made++; }
            }
            gwW.Flush(wW.transform); gwE.Flush(wE.transform);

            // ---- vegetation ----
            // one mainland, vegetation split into zones: boss grove (north), west half, east half
            var main = jungleIslands[0];
            System.Func<Vector2, bool> inGrove = p => Vector2.Distance(p, Arena2C) < 46f;
            JungleVegetation(main, Group("Vegetation", zW.transform).transform, flora, 540, p => p.x < 0f && !inGrove(p), "Mainland_West");
            JungleVegetation(main, Group("Vegetation", zE.transform).transform, flora, 500, p => p.x >= 0f && !inGrove(p), "Mainland_East");
            JungleVegetation(main, Group("Vegetation", root.Find("Zone_08_BossStage1_HeartOfTheGrove")).transform, flora, 150, inGrove, "Mainland_Grove");
        }
        // water disc whose edge is pulled inside the island outline (never spills over the cliff)
        static GameObject BuildClippedDisc(string name, Vector2 c, float r, float y, Cell cell, Material mat, Transform parent, int seg)
        {
            var rr = new float[seg];
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg; Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float R = r * (0.92f + 0.16f * Mathf.PerlinNoise(Mathf.Cos(a) * 1.5f + 8.8f, Mathf.Sin(a) * 1.5f + 8.8f));
                while (R > 1f && !OnLand(c + dir * R, 1.5f)) R -= 0.5f;
                rr[i] = R;
            }
            var mb = new MB(); Vector3 cc = V3(c, y);
            for (int i = 0; i < seg; i++)
            {
                int j = (i + 1) % seg;
                float a0 = i * Mathf.PI * 2f / seg, a1 = j * Mathf.PI * 2f / seg;
                mb.Tri(cc, cc + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rr[j], cc + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rr[i], UV(cell, 0.5f), UV(cell, 0.2f), UV(cell, 0.2f));
            }
            return MeshGO(name, mb.ToMesh(name), mat, parent, false);
        }
        static Vector2 Random2() { float a = Rand(0, Mathf.PI * 2f), r = Mathf.Sqrt(Rand(0, 1)); return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }

        static void JungleVegetation(Island isl, Transform parent, Flora fl, int fillTrees, System.Func<Vector2, bool> zone, string tag)
        {
            var trees = new Chunks(tag + "_Trees", matEnv);
            var under = new Chunks(tag + "_Undergrowth", matEnv);
            // sampling box = the zone's part of the island bounding box
            float bx0 = isl.c.x - isl.rx * 1.1f, bx1 = isl.c.x + isl.rx * 1.1f, bz0 = isl.c.y - isl.rz * 1.1f, bz1 = isl.c.y + isl.rz * 1.1f;
            { float ax0 = 1e9f, ax1 = -1e9f, az0 = 1e9f, az1 = -1e9f;
              for (float x = bx0; x <= bx1; x += 4f) for (float z = bz0; z <= bz1; z += 4f) if (zone(new Vector2(x, z)) && isl.Norm(new Vector2(x, z)) < 1f) { ax0 = Mathf.Min(ax0, x); ax1 = Mathf.Max(ax1, x); az0 = Mathf.Min(az0, z); az1 = Mathf.Max(az1, z); }
              if (ax1 > ax0) { bx0 = ax0 - 4f; bx1 = ax1 + 4f; bz0 = az0 - 4f; bz1 = az1 + 4f; } }
            System.Func<Vector2> sample = () => new Vector2(Rand(bx0, bx1), Rand(bz0, bz1));
            var cols = Group("TreeColliders", parent);
            var prefabs = Group("Prefabs", parent);
            int dead = 0;
            System.Func<Vector2, float, bool> ok = (p, r) => zone(p) && isl.Norm(p) < 0.97f && islands[0].Norm(p) > 1.04f && Free(p, r, 0.4f) && !InClearing(p, r * 0.6f);

            System.Action<Vector2> tree = p =>
            {
                float h = H(p.x, p.y); Vector3 g = new Vector3(p.x, h - 0.15f, p.y);
                if (InSwamp(p))
                {
                    if (dead < 8 && Rand(0, 1) < 0.25f) { BuildDeadTree("SwampDeadTree_" + tag + "_" + dead, p, Rand(1f, 1.4f), prefabs.transform); dead++; return; }
                    float s = Rand(0.85f, 1.2f); trees.Add(Pick(fl.mangrove), g, Rand(0, 360), s); TrunkCollider(cols.transform, g, 0.35f * s, 4.5f * s); return;
                }
                float roll = Rand(0, 1);
                if (h > 7f || roll < 0.18f) { Holotna(Rand(0, 1) < 0.5f ? "Tree01A" : "Tree01B", prefabs.transform, g, Rand(0, 360), Rand(0.9f, 1.3f)); return; }
                bool nearWater = false; foreach (var lk in jungleLakes) if (Vector2.Distance(p, new Vector2(lk.x, lk.y)) < lk.z * 2.4f) nearWater = true;
                if (roll < (nearWater ? 0.6f : 0.34f)) { float s = Rand(0.85f, 1.15f); trees.Add(Pick(fl.palm), g, Rand(0, 360), s); TrunkCollider(cols.transform, g, 0.3f * s, 5f * s); return; }
                float st = Rand(0.85f, 1.35f); trees.Add(Pick(fl.trop), g, Rand(0, 360), st); TrunkCollider(cols.transform, g, 0.42f * st, 6f * st);
            };

            // 1) roads framed by trees
            foreach (var pa in paths)
            {
                if (!pa.name.StartsWith("Path_Jungle")) continue;
                for (int i = 3; i < pa.pts.Count - 2; i += 4)
                {
                    Vector2 fwd = (pa.pts[i + 1] - pa.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                    foreach (int side in new[] { 1, -1 })
                    {
                        Vector2 p = pa.pts[i] + n * side * (pa.width * 0.5f + Rand(2.4f, 5f)) + fwd * Rand(-1.5f, 1.5f);
                        if (!ok(p, 1.8f) || !Dry(p)) continue;
                        tree(p); Occupy(p, 1.8f);
                    }
                }
            }
            // 2) clearings enclosed by a ring of trees
            for (int ci = JungleClearStart; ci < clearings.Count - 1; ci++)
            {
                var cl = clearings[ci]; Vector2 c = new Vector2(cl.x, cl.y);
                if (isl.Norm(c) > 1f) continue;
                for (int k = 0; k < 16; k++)
                {
                    float a = (k * 22.5f + Rand(-8f, 8f)) * Mathf.Deg2Rad;
                    Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (cl.z + Rand(2.5f, 5.5f));
                    if (!ok(p, 1.8f) || !Dry(p)) continue;
                    tree(p); Occupy(p, 1.8f);
                }
            }
            // 3) fill: dense forest over hills and ridges
            for (int made = 0, t = 0; made < fillTrees && t < fillTrees * 30; t++)
            {
                Vector2 p = sample();
                bool swamp = InSwamp(p);
                if (!ok(p, swamp ? 2.6f : 1.9f) || (!swamp && !Dry(p))) continue;
                tree(p); Occupy(p, swamp ? 2.6f : 1.9f); made++;
            }
            // 4) understorey
            for (int made = 0, t = 0; made < fillTrees / 3 && t < fillTrees * 20; t++)
            {
                Vector2 p = sample();
                if (!ok(p, 1.1f) || !Dry(p)) continue;
                Holotna("Bush01", prefabs.transform, Ground(p), Rand(0, 360), Rand(0.9f, 1.4f)); Occupy(p, 1.0f); made++;
            }
            for (int made = 0, t = 0; made < fillTrees / 12 && t < fillTrees * 10; t++)
            {
                Vector2 p = sample();
                if (!ok(p, 1.6f) || !Dry(p)) continue;
                Kit("SM_Mossy_Rock_Group" + (1 + made % 14), prefabs.transform, Ground(p) + Vector3.down * 0.2f, Rand(0, 360), Rand(1.4f, 2.6f)); Occupy(p, 1.6f); made++;
            }
            for (int made = 0, t = 0; made < fillTrees * 2 && t < fillTrees * 16; t++)
            {
                Vector2 p = sample();
                if (!zone(p) || isl.Norm(p) > 0.97f || islands[0].Norm(p) < 1.04f || !Free(p, 0.6f, -0.1f) || InClearing(p, 0.2f) || !Dry(p)) continue;
                under.Add(Pick(fl.fern), Ground(p), Rand(0, 360), Rand(0.8f, 1.5f)); made++;
            }
            for (int made = 0, t = 0; made < fillTrees * 2 && t < fillTrees * 12; t++)
            {
                Vector2 p = sample();
                if (!zone(p) || isl.Norm(p) > 0.97f || islands[0].Norm(p) < 1.04f || !Free(p, 0.4f, -0.2f) || !Dry(p)) continue;
                Holotna("Grass01" + "ABC"[made % 3], prefabs.transform, Ground(p), Rand(0, 360), Rand(1.0f, 1.5f)); made++;
            }
            for (int made = 0, t = 0; made < fillTrees / 4 && t < fillTrees * 6; t++)
            {
                Vector2 p = sample();
                if (!zone(p) || isl.Norm(p) > 0.95f || islands[0].Norm(p) < 1.04f || !Free(p, 0.4f, -0.1f) || InClearing(p, 0.2f) || !Dry(p)) continue;
                Holotna(made % 2 == 0 ? "Flower01A" : "Flower01B", prefabs.transform, Ground(p), Rand(0, 360), Rand(0.9f, 1.3f)); made++;
            }
            trees.Flush(parent); under.Flush(parent);
        }

        // ---------------- Boss Stage 1: "Heart of the Grove" ----------------
        // Mossy stone ring with grass in the seams, druid dais with a spirit sapling, ring of standing stones,
        // a giant ancient tree behind the arena (landmark seen from the whole east jungle), mountains all around
        // and a single canyon entrance through a mossy torii gate.
        static void BuildBossStage1(Transform root, Flora fl)
        {
            var g = Group("Zone_08_BossStage1_HeartOfTheGrove", root);
            var gp = Group("Gameplay_BossStage1", g.transform);
            var east = paths.Find(p => p.name == "Path_Jungle_East");
            Vector2 entryP = east.pts[east.pts.Count - 1];
            Vector2 toEntry = (entryP - Arena2C).normalized, side = new Vector2(-toEntry.y, toEntry.x);
            float entryAng = Mathf.Atan2(toEntry.y, toEntry.x) * Mathf.Rad2Deg;
            float y = Arena2Y + 0.16f;

            var mb = new MB();
            ArenaTiles(mb, Arena2C, y, 4.4f, Arena2R - 0.8f, 2.8f, 0.4f, C_Stone, C_RockMoss, 3, 0.4f);
            mb.Cyl(V3(Arena2C, Arena2Y - 0.2f), 4.1f, 3.9f, 0.75f, 12, C_Stone, C_RockMoss);
            mb.Cyl(V3(Arena2C, Arena2Y + 0.55f), 2.6f, 2.4f, 0.35f, 12, C_RockMoss, C_Moss);
            int segR = 96;
            for (int i = 0; i < segR; i++)
            {
                float a0 = i * Mathf.PI * 2f / segR, a1 = (i + 1) * Mathf.PI * 2f / segR;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                Vector3 cc = V3(Arena2C, 0); float yt = y + 0.18f, r0 = Arena2R - 0.6f, r1 = Arena2R + 0.6f;
                QuadUp(mb, cc + d0 * r0 + Vector3.up * yt, cc + d1 * r0 + Vector3.up * yt, cc + d1 * r1 + Vector3.up * yt, cc + d0 * r1 + Vector3.up * yt, UV(C_Stone, 0.6f));
                QuadDS(mb, cc + d0 * r0 + Vector3.up * yt, cc + d1 * r0 + Vector3.up * yt, cc + d1 * r0 + Vector3.up * (y - 0.3f), cc + d0 * r0 + Vector3.up * (y - 0.3f), UV(C_RockMoss, 0.5f));
                QuadDS(mb, cc + d0 * r1 + Vector3.up * yt, cc + d1 * r1 + Vector3.up * yt, cc + d1 * (r1 + 0.3f) + Vector3.up * (Arena2Y - 0.35f), cc + d0 * (r1 + 0.3f) + Vector3.up * (Arena2Y - 0.35f), UV(C_RockMoss, 0.7f));
            }
            MeshGO("BossStage1_Floor", mb.ToMesh("BossStage1_Floor"), matEnv, g.transform, true);

            // standing stones (some broken), gap at the entrance
            var sm = new MB(); int ns = 12;
            for (int i = 0; i < ns; i++)
            {
                float a = entryAng + (i + 0.5f) * 360f / ns;
                if (Mathf.Abs(Mathf.DeltaAngle(a, entryAng)) < 22f) continue;
                Vector2 sp = Arena2C + new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad)) * (Arena2R + 2.3f);
                float hgt = (i % 3 == 1) ? Rand(2.0f, 2.8f) : Rand(4.8f, 6.6f), gy = H(sp.x, sp.y) - 0.3f, yaw = FaceYaw(sp, Arena2C) + Rand(-8f, 8f);
                sm.Box(V3(sp, gy + hgt * 0.5f), new Vector3(2.0f, hgt, 1.2f), C_RockGrey, yaw, 0.2f, 0.9f);
                sm.Box(V3(sp, gy + hgt + 0.08f), new Vector3(2.14f, 0.2f, 1.34f), C_Moss, yaw);
                if (hgt > 3f) sm.Box(V3(sp, gy + hgt * 0.62f) + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, 0.62f), new Vector3(0.6f, 0.6f, 0.04f), C_Leaf1, yaw);   // rune plate
                Occupy(sp, 1.3f);
            }
            MeshGO("BossStage1_StandingStones", sm.ToMesh("BossStage1_StandingStones"), matEnv, g.transform, true);

            // spirit sapling on the dais + giant ancient tree behind the arena
            var sap = MeshGO("BossStage1_SpiritSapling", TropicalTemplate(C_Leaf2, 3.2f).ToMesh("BossStage1_SpiritSapling"), matEnv, g.transform, false);
            sap.transform.position = V3(Arena2C, Arena2Y + 0.9f); sap.transform.localScale = Vector3.one * 0.55f;
            Vector2 treeP = new Vector2(jungleFlats[2].x, jungleFlats[2].y);
            var giant = MeshGO("BossStage1_AncientTree (Landmark)", GiantTreeTemplate().ToMesh("BossStage1_AncientTree"), matEnv, g.transform, false);
            giant.transform.position = Ground(treeP) + Vector3.down * 0.3f; giant.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            TrunkCollider(giant.transform, giant.transform.position, 3f, 13f);
            Occupy(treeP, 8.5f);
            foreach (var o in new[] { Vector2.zero, new Vector2(-8, 9), new Vector2(7, -10), treeP - Arena2C })
            {
                var ff = SpawnPrefab(FX + "Fireflies/FireFliesGreen.prefab", g.transform, V3(Arena2C + o, Arena2Y + 1.6f), 0, 1.8f); if (ff != null) ff.name = "FX_GroveSpirits";
            }

            // entrance: mossy torii in the canyon + lanterns
            float yawIn = FaceYaw(entryP, Arena2C);
            Vector2 gateP = Arena2C + toEntry * (Arena2R + 1.8f);
            var gate = BuildGate("BossStage1_Gate", gateP, yawIn, 7.5f, g.transform, C_RockMoss, C_Bark);
            gate.transform.localScale = Vector3.one * 1.3f;
            Occupy(gateP + side * 4.9f, 1f); Occupy(gateP - side * 4.9f, 1f);
            foreach (int s in new[] { -1, 1 })
            {
                Vector2 lp = Arena2C + toEntry * (Arena2R + 6.5f) + side * s * 4.4f;
                StoneLantern("BossStage1_Lantern_" + s, Ground(lp), 1.2f, g.transform); Occupy(lp, 0.9f);
            }

            var boss = Group("BossSpawn_Stage1", gp.transform);
            boss.transform.position = V3(Arena2C, Arena2Y + 0.9f);
            boss.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(toEntry.x, toEntry.y) * Mathf.Rad2Deg, 0);
            Marker("ArenaEntry_BossStage1", entryP, gp.transform);
            Occupy(Arena2C, Arena2R + 1.5f);
        }
    }
}
