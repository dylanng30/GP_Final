// Elementia map builder - Starting village (Zone 01 + 02).
// Axial plan inspired by east-asian villages / temple towns (Liyue & Inazuma towns in Genshin, Ghost of Tsushima villages):
//   South gate -> paved avenue -> paved plaza (spawn, sacred tree) -> temple forecourt -> temple.
//   East-west cross road: west = blacksmith -> farmland, east = market & general store -> bridge.
//   Every building has a gameplay role and faces the space the player walks through.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        const float PaveY = 0.34f;   // plaza paving height (terrain is flattened to 0.12 below)

        // ---------- helpers ----------
        static void QuadUp(MB mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 u)
        {
            if (Vector3.Cross(b - a, c - a).y < 0) mb.Quad(a, d, c, b, u); else mb.Quad(a, b, c, d, u);
        }
        static bool InFootprint(Vector2 p, float r)
        {
            foreach (var b in buildings)
            {
                if (b.type == BType.TrainingGround || b.type == BType.Well) { if (Vector2.Distance(p, b.pos) < b.padR + r - 1.0f) return true; continue; }
                Vector2 d = p - b.pos; float a = -b.yaw * Mathf.Deg2Rad;
                // inverse yaw (Unity yaw rotates +Z toward +X)
                float lx = d.x * Mathf.Cos(a) + d.y * Mathf.Sin(a), lz = -d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a);
                float ex = b.type == BType.Temple ? 2.5f : 1.3f;
                if (Mathf.Abs(lx) < b.w * 0.5f + ex + r && Mathf.Abs(lz) < b.d * 0.5f + ex + r) return true;
            }
            return false;
        }
        static bool FreeProp(Vector2 p, float r, float roadMargin = 0.3f)
        {
            if (!OnLand(p, r)) return false;
            foreach (var pa in paths) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + r + roadMargin) return false; }
            foreach (var pa in streams) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + r + 0.6f) return false; }
            foreach (var f in fields) if (RectDist(p, f) < r + 0.8f) return false;
            if (Vector2.Distance(p, PlazaC) < PlazaR + 1.6f + r) return false;
            foreach (var pr in PavedAreas) if (RectDist(p, pr) < r + 0.3f) return false;
            if (InFootprint(p, r)) return false;
            foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + r) return false;
            return true;
        }
        static GameObject PlaceKit(string piece, Vector2 p, float yaw, float r, Transform parent, float scale = KitScale, bool collider = true)
        {
            if (!FreeProp(p, r)) return null;
            Occupy(p, r);
            return Kit(piece, parent, Ground(p), yaw, scale, collider);
        }

        // ---------- small procedural props ----------
        static GameObject StoneLantern(string name, Vector3 pos, float scale, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.15f, 0), new Vector3(0.95f, 0.3f, 0.95f), C_Stone);
            mb.Box(new Vector3(0, 0.38f, 0), new Vector3(0.7f, 0.16f, 0.7f), C_Stone, 45f);
            mb.Cyl(new Vector3(0, 0.46f, 0), 0.2f, 0.17f, 1.1f, 6, C_Stone, C_Stone);
            mb.Box(new Vector3(0, 1.62f, 0), new Vector3(0.8f, 0.14f, 0.8f), C_Stone);
            mb.Box(new Vector3(0, 1.95f, 0), new Vector3(0.62f, 0.52f, 0.62f), C_Stone);
            mb.Box(new Vector3(0, 1.95f, 0), new Vector3(0.66f, 0.3f, 0.34f), C_Gold);
            mb.Box(new Vector3(0, 1.95f, 0), new Vector3(0.34f, 0.3f, 0.66f), C_Gold);
            mb.Cyl(new Vector3(0, 2.21f, 0), 0.72f, 0.06f, 0.45f, 4, C_Stone, C_Stone);
            mb.Box(new Vector3(0, 2.72f, 0), new Vector3(0.16f, 0.2f, 0.16f), C_Stone);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.localScale = Vector3.one * scale;
            go.transform.rotation = Quaternion.Euler(0, 45f, 0);
            return go;
        }
        static GameObject Bench(string name, Vector3 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.5f, 0), new Vector3(2.2f, 0.14f, 0.6f), C_Wood);
            mb.Box(new Vector3(-0.85f, 0.22f, 0), new Vector3(0.3f, 0.44f, 0.5f), C_Stone);
            mb.Box(new Vector3(0.85f, 0.22f, 0), new Vector3(0.3f, 0.44f, 0.5f), C_Stone);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }
        static GameObject NoticeBoard(string name, Vector3 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(-1.1f, 1.3f, 0), new Vector3(0.2f, 2.6f, 0.2f), C_WoodDark);
            mb.Box(new Vector3(1.1f, 1.3f, 0), new Vector3(0.2f, 2.6f, 0.2f), C_WoodDark);
            mb.Box(new Vector3(0, 1.55f, 0), new Vector3(2.2f, 1.3f, 0.1f), C_Wood);
            mb.Box(new Vector3(-0.45f, 1.7f, 0.07f), new Vector3(0.6f, 0.7f, 0.03f), C_Wall);
            mb.Box(new Vector3(0.4f, 1.45f, 0.07f), new Vector3(0.55f, 0.6f, 0.03f), C_Field);
            HipRoof(mb, 1.5f, 0.45f, 2.55f, 0.4f, 0.12f, C_Roof, C_RoofRidge, false);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }
        static GameObject Dummy(string name, Vector3 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            Branch(mb, Vector3.zero, new Vector3(0, 2.3f, 0), 0.13f, 0.11f, C_WoodDark);
            mb.Box(new Vector3(0, 1.75f, 0), new Vector3(1.6f, 0.14f, 0.14f), C_WoodDark);
            mb.Cyl(new Vector3(0, 0.9f, 0), 0.42f, 0.38f, 1.0f, 8, C_Field, C_Field);
            mb.Cyl(new Vector3(0, 2.0f, 0), 0.3f, 0.26f, 0.5f, 8, C_Field, C_Field);
            mb.Box(new Vector3(0, 1.4f, 0.4f), new Vector3(0.5f, 0.5f, 0.06f), C_Red);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }
        static GameObject Scarecrow(string name, Vector3 pos, Transform parent)
        {
            var mb = new MB();
            Branch(mb, Vector3.zero, new Vector3(0, 2.4f, 0), 0.08f, 0.07f, C_WoodDark);
            mb.Box(new Vector3(0, 1.8f, 0), new Vector3(1.8f, 0.09f, 0.09f), C_WoodDark);
            mb.Box(new Vector3(0, 1.45f, 0), new Vector3(0.7f, 0.8f, 0.35f), C_Red);
            mb.Cyl(new Vector3(0, 2.1f, 0), 0.25f, 0.22f, 0.4f, 8, C_Field, C_Field);
            mb.Cyl(new Vector3(0, 2.45f, 0), 0.75f, 0.02f, 0.4f, 10, C_Field, C_Field);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, false);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            return go;
        }
        static GameObject Well(string name, Vector3 pos, Transform parent)
        {
            var mb = new MB();
            mb.Cyl(Vector3.zero, 1.15f, 1.1f, 0.95f, 12, C_Stone, C_Stone, false);
            mb.Cyl(Vector3.zero, 0.85f, 0.85f, 0.97f, 12, C_Stone, C_Stone, false);
            mb.Cyl(new Vector3(0, 0.7f, 0), 0.86f, 0.86f, 0.01f, 12, C_Water, C_Water);
            mb.Box(new Vector3(-1.0f, 1.2f, 0), new Vector3(0.18f, 2.4f, 0.18f), C_Wood);
            mb.Box(new Vector3(1.0f, 1.2f, 0), new Vector3(0.18f, 2.4f, 0.18f), C_Wood);
            mb.Box(new Vector3(0, 2.1f, 0), new Vector3(2.1f, 0.14f, 0.14f), C_WoodDark);
            mb.Box(new Vector3(0, 1.6f, 0), new Vector3(0.3f, 0.35f, 0.3f), C_WoodDark);
            HipRoof(mb, 1.4f, 1.0f, 2.35f, 0.6f, 0.15f, C_Roof, C_RoofRidge, false);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos;
            return go;
        }
        static GameObject IncenseBurner(string name, Vector3 pos, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.2f, 0), new Vector3(1.6f, 0.4f, 1.6f), C_Stone);
            mb.Cyl(new Vector3(0, 0.4f, 0), 0.55f, 0.75f, 0.7f, 8, C_Gold, C_WoodDark);
            mb.Box(new Vector3(-0.8f, 1.05f, 0), new Vector3(0.12f, 0.4f, 0.4f), C_Gold);
            mb.Box(new Vector3(0.8f, 1.05f, 0), new Vector3(0.12f, 0.4f, 0.4f), C_Gold);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos;
            var smoke = SpawnPrefab(FX + "Smoke/White/SmokeWhiteSoft.prefab", go.transform, pos + Vector3.up * 1.1f, 0, 0.35f);
            if (smoke != null) smoke.name = "FX_Incense";
            return go;
        }

        // ---------- paving ----------
        static void PavedRect(MB mb, Rect r, float y, float slabW, float slabL, bool alongZ, float skip = 0f, Cell? tileCell = null)
        {
            Cell tc = tileCell ?? C_Plaza;
            // grout base
            QuadUp(mb, new Vector3(r.xMin, y - 0.07f, r.yMin), new Vector3(r.xMax, y - 0.07f, r.yMin), new Vector3(r.xMax, y - 0.07f, r.yMax), new Vector3(r.xMin, y - 0.07f, r.yMax), UV(C_PlazaDark, 0.9f));
            float g = 0.07f;
            float len = alongZ ? r.height : r.width, wid = alongZ ? r.width : r.height;
            int rows = Mathf.Max(1, Mathf.RoundToInt(len / slabL));
            float rl = len / rows;
            for (int i = 0; i < rows; i++)
            {
                float off = (i % 2 == 0) ? 0f : slabW * 0.5f;
                for (float s = -off; s < wid - 0.01f; s += slabW)
                {
                    float s0 = Mathf.Max(0, s) + g, s1 = Mathf.Min(wid, s + slabW) - g;
                    if (s1 - s0 < 0.2f) continue;
                    float l0 = i * rl + g, l1 = (i + 1) * rl - g;
                    float hsh = (Mathf.Abs(Mathf.Sin(i * 12.9898f + s * 78.233f)) * 43758.5f) % 1f;
                    if (skip > 0f && ((hsh * 7.31f) % 1f) < skip) continue;
                    Vector2 u = UV(tc, hsh);
                    Vector3 a, b, c, d;
                    if (alongZ) { a = new Vector3(r.xMin + s0, y, r.yMin + l0); b = new Vector3(r.xMin + s1, y, r.yMin + l0); c = new Vector3(r.xMin + s1, y, r.yMin + l1); d = new Vector3(r.xMin + s0, y, r.yMin + l1); }
                    else { a = new Vector3(r.xMin + l0, y, r.yMin + s0); b = new Vector3(r.xMin + l1, y, r.yMin + s0); c = new Vector3(r.xMin + l1, y, r.yMin + s1); d = new Vector3(r.xMin + l0, y, r.yMin + s1); }
                    QuadUp(mb, a, b, c, d, u);
                }
            }
        }

        static float forecourtY = 0.3f;
        // highest terrain point under a rect (so paving never gets terrain poking through its grout)
        static float MaxH(Rect r)
        {
            float m = float.MinValue;
            for (float x = r.xMin; x <= r.xMax + 0.01f; x += 1f) for (float z = r.yMin; z <= r.yMax + 0.01f; z += 1f) m = Mathf.Max(m, H(x, z));
            return m;
        }

        static void BuildPlaza(Transform parent)
        {
            var mb = new MB();
            Vector3 C = V3(PlazaC, 0);
            float y = PaveY, rEdge = PlazaR - 0.6f, band = 2.3f;
            // grout base disc + side skirt
            int segB = 72;
            for (int i = 0; i < segB; i++)
            {
                float a0 = i * Mathf.PI * 2f / segB, a1 = (i + 1) * Mathf.PI * 2f / segB;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                mb.Tri(C + Vector3.up * (y - 0.07f), C + d1 * rEdge + Vector3.up * (y - 0.07f), C + d0 * rEdge + Vector3.up * (y - 0.07f), UV(C_PlazaDark, 0.9f));
            }
            // concentric tile rings (staggered), leaving the two processional axes free
            float[] rr = { 5.4f, 7.1f, 8.8f, 10.5f, 12.2f, 13.9f, rEdge };
            for (int k = 0; k < rr.Length - 1; k++)
            {
                float r0 = rr[k] + 0.06f, r1 = rr[k + 1] - 0.06f, rm = (r0 + r1) * 0.5f;
                int seg = Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * rm / 1.9f));
                float off = (k % 2) * 0.5f;
                bool accent = k == 2 || k == 5;
                for (int i = 0; i < seg; i++)
                {
                    float a0 = (i + off) * Mathf.PI * 2f / seg, a1 = (i + 1 + off) * Mathf.PI * 2f / seg, am = (a0 + a1) * 0.5f;
                    Vector2 mid = new Vector2(Mathf.Cos(am), Mathf.Sin(am)) * rm;
                    if (Mathf.Abs(mid.x) < band + 0.6f || Mathf.Abs(mid.y) < band + 0.6f) continue;
                    float ga = 0.06f / rm;
                    Vector3 d0 = new Vector3(Mathf.Cos(a0 + ga), 0, Mathf.Sin(a0 + ga)), d1 = new Vector3(Mathf.Cos(a1 - ga), 0, Mathf.Sin(a1 - ga));
                    Vector2 u = accent ? UV(C_PlazaDark, 0.25f + 0.2f * (i % 2)) : UV(C_Plaza, 0.2f + 0.25f * ((i + k) % 3) / 2f);
                    QuadUp(mb, C + d0 * r0 + Vector3.up * y, C + d1 * r0 + Vector3.up * y, C + d1 * r1 + Vector3.up * y, C + d0 * r1 + Vector3.up * y, u);
                }
            }
            // processional axes N-S and E-W (large rectangular slabs)
            float axisR = Mathf.Sqrt(rEdge * rEdge - band * band);
            PavedRect(mb, new Rect(PlazaC.x - band, PlazaC.y + 5.0f, band * 2, axisR - 5.0f), y + 0.005f, 1.53f, 1.15f, true);
            PavedRect(mb, new Rect(PlazaC.x - band, PlazaC.y - axisR, band * 2, axisR - 5.0f), y + 0.005f, 1.53f, 1.15f, true);
            PavedRect(mb, new Rect(PlazaC.x + 5.0f, PlazaC.y - band, axisR - 5.0f, band * 2), y + 0.005f, 1.53f, 1.15f, false);
            PavedRect(mb, new Rect(PlazaC.x - axisR, PlazaC.y - band, axisR - 5.0f, band * 2), y + 0.005f, 1.53f, 1.15f, false);
            // curb ring (raised, with outer skirt down to the ground)
            int segC = 96;
            for (int i = 0; i < segC; i++)
            {
                float a0 = i * Mathf.PI * 2f / segC, a1 = (i + 1) * Mathf.PI * 2f / segC;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)), d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1));
                float yt = y + 0.12f, r0 = rEdge, r1 = PlazaR + 0.3f;
                Vector2 u = UV(C_Stone, 0.25f);
                QuadUp(mb, C + d0 * r0 + Vector3.up * yt, C + d1 * r0 + Vector3.up * yt, C + d1 * r1 + Vector3.up * yt, C + d0 * r1 + Vector3.up * yt, u);
                mb.Quad(C + d0 * r0 + Vector3.up * yt, C + d0 * r0 + Vector3.up * (y - 0.07f), C + d1 * r0 + Vector3.up * (y - 0.07f), C + d1 * r0 + Vector3.up * yt, UV(C_Stone, 0.5f));
                mb.Quad(C + d0 * r1 + Vector3.up * yt, C + d1 * r1 + Vector3.up * yt, C + d1 * r1 + Vector3.up * -0.3f, C + d0 * r1 + Vector3.up * -0.3f, UV(C_Stone, 0.6f));
            }
            // central stepped octagon with the sacred tree
            mb.Cyl(C + Vector3.up * (y - 0.07f), 5.2f, 5.2f, 0.3f, 8, C_Stone, C_PlazaDark);
            mb.Cyl(C + Vector3.up * (y + 0.23f), 4.1f, 4.0f, 0.45f, 8, C_Stone, C_Stone);
            mb.Cyl(C + Vector3.up * (y + 0.68f), 4.05f, 4.05f, 0.1f, 8, C_Gold, C_Grass);
            var go = MeshGO("Plaza_Paving", mb.ToMesh("Plaza_Paving"), matEnv, parent, true);
            go.transform.rotation = Quaternion.identity;
            Holotna("Tree01B", parent, V3(PlazaC, y + 0.75f), 20f, 1.45f);
            for (int i = 0; i < 6; i++) { float a = i * 60f * Mathf.Deg2Rad; Holotna("Bush01", parent, V3(PlazaC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 2.9f, y + 0.75f), Rand(0, 360), 0.7f); }
            Occupy(PlazaC, PlazaR + 0.5f);
        }

        // balustrade around the plaza with 4 openings (N S E W)
        static void BuildBalustrade(Transform parent)
        {
            var mb = new MB();
            float R = PlazaR + 0.9f, gapHalf = 4.2f / R; // radians
            float[] openings = { 0f, Mathf.PI * 0.5f, Mathf.PI, Mathf.PI * 1.5f };
            int seg = Mathf.RoundToInt(2f * Mathf.PI * R / 3.0f);
            var postAngles = new List<float>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg, am = (a0 + a1) * 0.5f;
                bool open = false;
                foreach (var o in openings) if (Mathf.Abs(Mathf.DeltaAngle(am * Mathf.Rad2Deg, o * Mathf.Rad2Deg)) * Mathf.Deg2Rad < gapHalf + 0.03f) open = true;
                if (open) continue;
                Vector2 p0 = PlazaC + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * R, p1 = PlazaC + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * R;
                Vector2 m = (p0 + p1) * 0.5f; float len = Vector2.Distance(p0, p1); float yaw = Mathf.Atan2(p1.x - p0.x, p1.y - p0.y) * Mathf.Rad2Deg;
                float gy = Mathf.Min(H(p0.x, p0.y), H(p1.x, p1.y));
                mb.Box(new Vector3(m.x, gy + 0.15f, m.y), new Vector3(0.55f, 0.5f, len + 0.02f), C_Stone, yaw);
                mb.Box(new Vector3(m.x, gy + 1.0f, m.y), new Vector3(0.28f, 0.16f, len), C_Wall, yaw);
                mb.Box(new Vector3(m.x, gy + 0.62f, m.y), new Vector3(0.16f, 0.5f, len), C_Wall, yaw, 0.1f, 0.25f);
                postAngles.Add(a0); postAngles.Add(a1);
            }
            foreach (var a in postAngles)
            {
                Vector2 p = PlazaC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R;
                float gy = H(p.x, p.y);
                mb.Box(new Vector3(p.x, gy + 0.62f, p.y), new Vector3(0.42f, 1.25f, 0.42f), C_Stone, -a * Mathf.Rad2Deg);
                mb.Box(new Vector3(p.x, gy + 1.32f, p.y), new Vector3(0.3f, 0.18f, 0.3f), C_Stone, -a * Mathf.Rad2Deg + 45f);
            }
            MeshGO("Plaza_Balustrade", mb.ToMesh("Plaza_Balustrade"), matEnv, parent, true);
            // stone lanterns flanking every opening
            int li = 0;
            foreach (var o in openings)
                foreach (int s in new[] { -1, 1 })
                {
                    float a = o + s * (gapHalf + 0.9f / R);
                    Vector2 p = PlazaC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (R + 0.2f);
                    StoneLantern("StoneLantern_Gate_" + (li++), Ground(p), 1.15f, parent);
                    Occupy(p, 0.8f);
                }
        }

        static void BuildPavedRoads(Transform parent)
        {
            var mb = new MB();
            // avenue: from the gate to (just under) the plaza curb, so no terrain shows between them
            float aveTop = PlazaC.y - Mathf.Sqrt(PlazaR * PlazaR - 3.7f * 3.7f);
            float ay = MaxH(new Rect(-3.3f, 61f, 6.6f, aveTop - 61f)) + 0.12f;
            PavedRect(mb, new Rect(-3.3f, 61f, 6.6f, aveTop - 61f), ay, 1.65f, 1.3f, true);
            for (int s = -1; s <= 1; s += 2) mb.Box(new Vector3(s * 3.5f, ay - 0.05f, (61f + aveTop) * 0.5f), new Vector3(0.4f, 0.4f, aveTop - 61f), C_Stone, 0, 0.2f, 0.5f);
            // temple forecourt (starts under the plaza curb for the same reason)
            float fcBot = PlazaC.y + Mathf.Sqrt((PlazaR + 0.2f) * (PlazaR + 0.2f) - 9.3f * 9.3f);
            float fy = MaxH(new Rect(-9f, fcBot, 18f, 128.3f - fcBot)) + 0.12f;
            forecourtY = fy;
            PavedRect(mb, new Rect(-9f, fcBot, 18f, 128.3f - fcBot), fy, 1.8f, 1.4f, false);
            for (int s = -1; s <= 1; s += 2) mb.Box(new Vector3(s * 9.2f, fy - 0.03f, (fcBot + 128.3f) * 0.5f), new Vector3(0.4f, 0.4f, 128.3f - fcBot), C_Stone, 0, 0.2f, 0.5f);
            MeshGO("Paved_Avenue_and_Forecourt", mb.ToMesh("Paved_Avenue"), matEnv, parent, true);
        }

        // Asian style village wall (stone plinth, white plaster, tiled cap) along the southern edge with the main gate.
        static Vector2 BuildVillageWall(Transform parent)
        {
            var isl = islands[0];
            var pts = new List<Vector2>();
            for (float a = 238f; a <= 302.01f; a += 2f)
            {
                float r = a * Mathf.Deg2Rad;
                pts.Add(isl.c + new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * isl.Radius(r) * 0.86f);
            }
            var mb = new MB(); Vector2 gate = new Vector2(0, 63);
            for (int i = 0; i < pts.Count - 1; i++) if ((pts[i].x <= 0 && pts[i + 1].x > 0)) { float t = -pts[i].x / (pts[i + 1].x - pts[i].x); gate = Vector2.Lerp(pts[i], pts[i + 1], t); }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector2 a = pts[i], b = pts[i + 1], m = (a + b) * 0.5f;
                if (Mathf.Abs(m.x - gate.x) < 5.2f) continue;
                float len = Vector2.Distance(a, b) + 0.05f, yaw = Mathf.Atan2(b.x - a.x, b.y - a.y) * Mathf.Rad2Deg;
                float gy = Mathf.Min(H(a.x, a.y), H(b.x, b.y)) - 0.2f;
                mb.Box(new Vector3(m.x, gy + 0.45f, m.y), new Vector3(1.3f, 0.9f, len), C_Stone, yaw, 0.2f, 0.5f);
                mb.Box(new Vector3(m.x, gy + 1.9f, m.y), new Vector3(0.85f, 2.1f, len), C_Wall, yaw, 0.02f, 0.18f);
                mb.Box(new Vector3(m.x, gy + 3.05f, m.y), new Vector3(1.5f, 0.22f, len + 0.1f), C_RoofRidge, yaw);
                mb.Box(new Vector3(m.x, gy + 3.25f, m.y), new Vector3(1.0f, 0.22f, len + 0.1f), C_Roof, yaw);
                Occupy(m, 1.6f);
            }
            for (int i = 0; i < pts.Count; i += 3)
            {
                if (Mathf.Abs(pts[i].x - gate.x) < 5.2f) continue;
                float gy = H(pts[i].x, pts[i].y) - 0.2f;
                mb.Box(new Vector3(pts[i].x, gy + 1.7f, pts[i].y), new Vector3(1.0f, 3.0f, 1.0f), C_Wood);
            }
            foreach (int s in new[] { 0, pts.Count - 1 })
            {
                float gy = H(pts[s].x, pts[s].y) - 0.2f;
                mb.Box(new Vector3(pts[s].x, gy + 1.9f, pts[s].y), new Vector3(1.6f, 3.8f, 1.6f), C_Stone);
            }
            // wall stubs connecting to the gate posts
            foreach (int s in new[] { -1, 1 })
            {
                Vector2 p = gate + new Vector2(s * 4.75f, 0);
                mb.Box(new Vector3(p.x, H(p.x, p.y) + 1.6f, p.y), new Vector3(0.9f, 3.2f, 1.2f), C_Stone);
            }
            MeshGO("Village_Wall", mb.ToMesh("Village_Wall"), matEnv, parent, true);
            return gate;
        }

        static void DoorSteps(BDef b, Transform parent)
        {
            // stepping stones from the door to the nearest road
            Vector3 f = Quaternion.Euler(0, b.yaw, 0) * Vector3.forward;
            Vector2 fw = new Vector2(f.x, f.z);
            Vector2 p = b.pos + fw * (b.d * 0.5f + 2.0f);
            for (int i = 0; i < 6; i++)
            {
                bool onRoad = false;
                foreach (var pa in paths) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + 0.4f) onRoad = true; }
                if (onRoad || Vector2.Distance(p, PlazaC) < PlazaR + 1.5f) break;
                Kit("stone slabs" + (1 + i % 2), parent, Ground(p) + Vector3.up * 0.02f, b.yaw + Rand(-8, 8), 1.0f, false);
                p += fw * 1.55f;
            }
        }

        // ---------------- Village ----------------
        static void BuildVillage(Transform root)
        {
            var zone = Group("Zone_02_Village", root);
            var start = Group("Zone_01_StartingPoint_Plaza", root);
            var gB = Group("Buildings", zone.transform);
            var gProps = Group("Props", zone.transform);
            var gVeg = Group("Vegetation", zone.transform);
            var village = islands[0];

            // --- Plaza (spawn) ---
            BuildPlaza(start.transform);
            BuildBalustrade(start.transform);
            BuildPavedRoads(zone.transform);
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                Vector2 p = PlazaC + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 11.6f;
                StoneLantern("StoneLantern_Plaza_" + i, V3(p, PaveY), 1.0f, start.transform);
                float ab = a + 0.33f; Vector2 pb = PlazaC + new Vector2(Mathf.Cos(ab), Mathf.Sin(ab)) * 14.6f;
                Bench("Bench_" + i, V3(pb, PaveY), FaceYaw(pb, PlazaC), start.transform);
            }
            Vector2 boardP = PlazaC + new Vector2(-5.2f, -12.5f);
            NoticeBoard("NoticeBoard (Quests)", V3(boardP, PaveY), FaceYaw(boardP, PlazaC + new Vector2(0, -6)), start.transform);

            // --- Gate & wall ---
            Vector2 gate = BuildVillageWall(zone.transform);
            BuildGate("Village_Gate", gate, 0f, 9.5f, zone.transform);
            for (int s = -1; s <= 1; s += 2) for (int k = 0; k < 3; k++)
                {
                    Vector2 p = new Vector2(s * 5.4f, 70f + k * 6.5f);
                    if (!FreeProp(p, 0.6f, 0.2f)) continue;
                    Kit("lamp", gProps.transform, Ground(p), s < 0 ? 90f : -90f, KitScale); Occupy(p, 0.7f);
                }

            // --- Buildings ---
            foreach (var b in buildings)
            {
                Occupy(b.pos, 0.1f);
                switch (b.type)
                {
                    case BType.Temple:
                        BuildTemple(b.name, b.pos, b.yaw, gB.transform);
                        for (int s = -1; s <= 1; s += 2)
                        {
                            StoneLantern("StoneLantern_Temple_" + s, V3(new Vector2(s * 6.2f, 126.6f), forecourtY), 1.5f, gB.transform);
                            Kit("Banner", gB.transform, Ground(b.pos + new Vector2(s * 4.4f, -9.6f)), 180, KitScale * 1.2f);
                        }
                        IncenseBurner("IncenseBurner", V3(new Vector2(0, 123.3f), forecourtY), gB.transform);
                        break;
                    case BType.Elder:
                    case BType.Inn:
                    case BType.Shop:
                    case BType.Blacksmith:
                    case BType.Residence:
                    case BType.Farmhouse:
                        BuildHouse(b.name, b.pos, b.yaw, b.w, b.d, b.h, b.twoTier, gB.transform);
                        DoorSteps(b, gProps.transform);
                        DressBuilding(b, gProps.transform);
                        break;
                    case BType.Shrine:
                        BuildHouse(b.name, b.pos, b.yaw, b.w, b.d, 3.4f, false, gB.transform);
                        {
                            Vector3 f = Quaternion.Euler(0, b.yaw, 0) * Vector3.forward;
                            Vector2 tp = b.pos + new Vector2(f.x, f.z) * 5.2f;
                            var torii = BuildGate("Shrine_Torii", tp, b.yaw, 3.6f, gB.transform);
                            torii.transform.localScale = Vector3.one * 0.75f;
                            StoneLantern("StoneLantern_Shrine", Ground(tp + new Vector2(f.z, -f.x) * 2.4f), 0.9f, gB.transform);
                        }
                        break;
                    case BType.Watchtower:
                        BuildWatchtower(b.name, b.pos, b.yaw, gB.transform);
                        break;
                    case BType.Well:
                        Well(b.name, Ground(b.pos), gB.transform);
                        break;
                    case BType.TrainingGround:
                        BuildTrainingGround(b, gB.transform);
                        break;
                }
            }

            // --- Market street along the east road (between plaza and stream) ---
            var market = Group("MarketStreet", zone.transform);
            for (int i = 0; i < 3; i++)
            {
                Vector2 p = new Vector2(23f + i * 6.2f, 109.4f);
                var t = PlaceKit("a tent", p, 180f, 2.0f, market.transform);
                if (t == null) continue;
                PlaceKit("a pot" + (1 + i), p + new Vector2(2.6f, -0.6f), Rand(0, 360), 0.5f, market.transform, KitScale, false);
            }

            // --- Farmland ---
            var gFields = Group("RiceFields", zone.transform);
            for (int i = 0; i < fields.Count; i++) BuildRiceField("RiceField_" + i, fields[i], gFields.transform);
            Scarecrow("Scarecrow_0", V3(fields[0].center, HLow(fields[0].center.x, fields[0].center.y) + 0.15f), gFields.transform);
            Scarecrow("Scarecrow_1", V3(fields[3].center, HLow(fields[3].center.x, fields[3].center.y) + 0.15f), gFields.transform);

            // --- Vegetation: dense tree belt along the island edge, groves, then ground cover ---
            int tcount = 0, tries = 0;
            while (tcount < 110 && tries++ < 4000)
            {
                float ang = Rand(0, Mathf.PI * 2f); float R = village.Radius(ang) * Rand(0.82f, 0.94f);
                Vector2 p = village.c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * R;
                if (!Free(p, 2.3f) || InFootprint(p, 2.3f)) continue;
                Holotna(Rand(0, 1) < 0.5f ? "Tree01A" : "Tree01B", gVeg.transform, Ground(p) + Vector3.down * 0.2f, Rand(0, 360), Rand(0.8f, 1.15f));
                Occupy(p, 2.3f); tcount++;
            }
            Scatter(village, 0.8f, 10, 3f, 2.0f, (p, k) => { if (!InFootprint(p, 3f)) Holotna(k % 2 == 0 ? "Tree01A" : "Tree01B", gVeg.transform, Ground(p) + Vector3.down * 0.2f, Rand(0, 360), Rand(0.85f, 1.1f)); });
            Scatter(village, 0.93f, 45, 1.2f, 0.6f, (p, k) => { if (!InFootprint(p, 1.2f)) Holotna("Bush01", gVeg.transform, Ground(p), Rand(0, 360), Rand(0.8f, 1.2f)); });
            Scatter(village, 0.9f, 10, 1.4f, 0.8f, (p, k) => { if (!InFootprint(p, 1.4f)) Kit("SM_Mossy_Rock_Group" + (1 + k % 14), gVeg.transform, Ground(p) + Vector3.down * 0.1f, Rand(0, 360), Rand(1.0f, 1.6f)); });
            Scatter(village, 0.94f, 1300, 0.45f, -0.2f, (p, k) => { if (!InFootprint(p, 0.3f)) Holotna("Grass01" + "ABC"[k % 3], gVeg.transform, Ground(p), Rand(0, 360), Rand(0.9f, 1.35f)); }, false);
            Scatter(village, 0.94f, 240, 0.4f, -0.1f, (p, k) => { if (!InFootprint(p, 0.3f)) Holotna(new[] { "Flowers01A", "Flowers01B", "Flower01A", "Flower01B" }[k % 4], gVeg.transform, Ground(p), Rand(0, 360), Rand(0.9f, 1.25f)); }, false);

            // --- Bridges where roads cross the stream ---
            int bi = 0;
            foreach (var pa in paths) foreach (var st in streams)
                    for (int i = 0; i < pa.pts.Count - 1; i++) for (int j = 0; j < st.pts.Count - 1; j++)
                        {
                            Vector2 hit;
                            if (!SegIntersect(pa.pts[i], pa.pts[i + 1], st.pts[j], st.pts[j + 1], out hit)) continue;
                            Vector2 dir = (pa.pts[i + 1] - pa.pts[i]).normalized;
                            var br = Holotna("Bridge01", zone.transform, Ground(hit) + Vector3.down * 0.15f, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 1f);
                            if (br != null) { br.name = "StreamBridge_" + (bi++); br.transform.localScale = new Vector3(1.7f, 1.3f, (st.width + 4f) / 9.07f); }
                        }
        }

        // Gameplay dressing per building role. Every prop is collision checked.
        static void DressBuilding(BDef b, Transform parent)
        {
            Quaternion q = Quaternion.Euler(0, b.yaw, 0);
            System.Func<float, float, Vector2> L = (lx, lz) => { Vector3 o = q * new Vector3(lx, 0, lz); return b.pos + new Vector2(o.x, o.z); };
            float fz = b.d * 0.5f + 1.9f, sx = b.w * 0.5f + 1.9f;
            var g = Group(b.name + "_Props", parent);
            switch (b.type)
            {
                case BType.Elder:
                    StoneLantern("Lantern_L", Ground(L(-2.6f, fz)), 0.95f, g.transform); StoneLantern("Lantern_R", Ground(L(2.6f, fz)), 0.95f, g.transform);
                    Holotna("Bush01", g.transform, Ground(L(-sx, 1.5f)), 0, 1.1f); Holotna("Bush01", g.transform, Ground(L(sx, 1.5f)), 0, 1.1f);
                    break;
                case BType.Inn:
                    Kit("Banner", g.transform, Ground(L(-2.4f, fz - 0.3f)), b.yaw, KitScale); Kit("Banner", g.transform, Ground(L(2.4f, fz - 0.3f)), b.yaw, KitScale);
                    Bench("Bench_Inn_L", Ground(L(-4.4f, fz + 0.4f)), b.yaw + 180f, g.transform); Bench("Bench_Inn_R", Ground(L(4.4f, fz + 0.4f)), b.yaw + 180f, g.transform);
                    PlaceKit("a pot4", L(sx, -1f), 0, 0.5f, g.transform, KitScale, false); PlaceKit("a pot6", L(sx, 0.4f), 0, 0.5f, g.transform, KitScale, false);
                    break;
                case BType.Shop:
                    PlaceKit("Trolley", L(sx + 0.3f, 1.0f), b.yaw + 90, 1.0f, g.transform);
                    PlaceKit("a pot1", L(-sx, 1.4f), 0, 0.5f, g.transform, KitScale, false); PlaceKit("a pot2", L(-sx, 0.2f), 0, 0.5f, g.transform, KitScale, false); PlaceKit("a pot3", L(-sx - 0.9f, 0.8f), 0, 0.5f, g.transform, KitScale, false);
                    break;
                case BType.Blacksmith:
                    {
                        Vector2 forge = L(sx + 1.0f, 1.0f);
                        var fm = new MB(); fm.Box(new Vector3(0, 0.45f, 0), new Vector3(2.0f, 0.9f, 1.6f), C_Stone); fm.Box(new Vector3(0, 0.92f, 0), new Vector3(1.4f, 0.06f, 1.0f), C_Crust);
                        fm.Box(new Vector3(0, 2.0f, -0.5f), new Vector3(0.7f, 2.4f, 0.6f), C_Stone);
                        var fgo = MeshGO("Forge", fm.ToMesh("Forge"), matEnv, g.transform, true); fgo.transform.position = Ground(forge); fgo.transform.rotation = Quaternion.Euler(0, b.yaw, 0);
                        var fire = SpawnPrefab(FX + "Fire/Cartoon/Radial/ToonRadialFireRed.prefab", g.transform, Ground(forge) + Vector3.up * 0.95f, 0, 0.45f); if (fire != null) fire.name = "FX_ForgeFire";
                        var am = new MB(); am.Box(new Vector3(0, 0.35f, 0), new Vector3(0.5f, 0.7f, 0.5f), C_WoodDark); am.Box(new Vector3(0, 0.8f, 0), new Vector3(0.9f, 0.25f, 0.4f), C_PlazaDark);
                        var anvil = MeshGO("Anvil", am.ToMesh("Anvil"), matEnv, g.transform, true); anvil.transform.position = Ground(L(sx + 1.0f, fz - 0.4f)); anvil.transform.rotation = q;
                        Occupy(forge, 1.6f);
                        Kit("Sword", g.transform, Ground(L(-sx + 0.3f, 1.2f)), b.yaw + 90, KitScale, false);
                        Kit("Shield", g.transform, Ground(L(-sx + 0.3f, 0.2f)), b.yaw + 90, KitScale, false);
                        Kit("Pickaxe", g.transform, Ground(L(-sx + 0.3f, -0.8f)), b.yaw + 90, KitScale, false);
                        PlaceKit("A log of firewood", L(sx + 1.0f, -2.2f), b.yaw, 0.8f, g.transform, KitScale, false);
                        break;
                    }
                case BType.Residence:
                    PlaceKit("A log of firewood", L(sx, -1.5f), b.yaw, 0.8f, g.transform, KitScale, false);
                    PlaceKit("a pot5", L(-sx, 1.5f), 0, 0.5f, g.transform, KitScale, false);
                    Holotna("Bush01", g.transform, Ground(L(-sx, -1.5f)), 0, 1.0f);
                    break;
                case BType.Farmhouse:
                    PlaceKit("Trolley", L(sx + 0.4f, 0.5f), b.yaw, 1.0f, g.transform);
                    PlaceKit("stump", L(-sx, 1.2f), 0, 0.6f, g.transform, KitScale, false);
                    PlaceKit("A log of firewood", L(-sx, -1.2f), b.yaw, 0.8f, g.transform, KitScale, false);
                    break;
            }
        }

        static void BuildTrainingGround(BDef b, Transform parent)
        {
            var g = Group(b.name, parent);
            float r = 7f;
            BuildDisc("Training_Floor", b.pos, r, HLow(b.pos.x, b.pos.y) + 0.14f, C_Path, matEnv, g.transform, 9.1f, 24);
            // post-and-rail fence with an opening toward the access path
            Vector2 entry = paths.Find(p => p.name == "Path_Village_Training").pts[paths.Find(p => p.name == "Path_Village_Training").pts.Count - 1];
            float entryAng = Mathf.Atan2(entry.y - b.pos.y, entry.x - b.pos.x) * Mathf.Rad2Deg;
            var mb = new MB(); int seg = 22;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * 360f / seg, a1 = (i + 1) * 360f / seg;
                if (Mathf.Abs(Mathf.DeltaAngle((a0 + a1) * 0.5f, entryAng)) < 20f) continue;
                Vector2 p0 = b.pos + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * (r + 0.4f);
                Vector2 p1 = b.pos + new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad)) * (r + 0.4f);
                Vector2 m = (p0 + p1) * 0.5f; float len = Vector2.Distance(p0, p1); float yaw = Mathf.Atan2(p1.x - p0.x, p1.y - p0.y) * Mathf.Rad2Deg;
                float gy = H(m.x, m.y);
                mb.Box(new Vector3(p0.x, gy + 0.6f, p0.y), new Vector3(0.22f, 1.3f, 0.22f), C_WoodDark);
                mb.Box(new Vector3(m.x, gy + 1.05f, m.y), new Vector3(0.1f, 0.14f, len), C_Wood, yaw);
                mb.Box(new Vector3(m.x, gy + 0.55f, m.y), new Vector3(0.1f, 0.14f, len), C_Wood, yaw);
            }
            MeshGO("Training_Fence", mb.ToMesh("Training_Fence"), matEnv, g.transform, true);
            for (int i = 0; i < 3; i++)
            {
                float a = (entryAng + 140f + i * 40f) * Mathf.Deg2Rad;
                Vector2 p = b.pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 4.2f;
                Dummy("TrainingDummy_" + i, V3(p, HLow(p.x, p.y) + 0.14f), FaceYaw(p, b.pos), g.transform);
            }
            Vector2 rack = b.pos + new Vector2(Mathf.Cos((entryAng + 60f) * Mathf.Deg2Rad), Mathf.Sin((entryAng + 60f) * Mathf.Deg2Rad)) * 5.6f;
            Kit("barriers", g.transform, V3(rack, HLow(rack.x, rack.y) + 0.14f), FaceYaw(rack, b.pos), KitScale, true);
            Kit("Sword", g.transform, V3(rack + (b.pos - rack).normalized * 1.3f, HLow(rack.x, rack.y) + 0.14f), FaceYaw(rack, b.pos), KitScale, false);
            Occupy(b.pos, r + 1f);
        }
    }
}
