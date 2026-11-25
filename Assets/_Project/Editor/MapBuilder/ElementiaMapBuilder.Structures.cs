// Elementia map builder - procedural stylized structures (asian houses, temple, towers, gate, bridges, fields).
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        static void TriDS(MB mb, Vector3 a, Vector3 b, Vector3 c, Vector2 u) { mb.Tri(a, b, c, u); mb.Tri(a, c, b, u); }

        // Hip roof with upturned corners. Local space, eave at y, centered at origin.
        static void HipRoof(MB mb, float halfX, float halfZ, float y, float ridgeH, float upturn, Cell roof, Cell ridge, bool ornaments)
        {
            bool alongX = halfX >= halfZ;
            float hx = halfX, hz = halfZ;
            Vector3 e0 = new Vector3(-hx, y + upturn, -hz), e1 = new Vector3(hx, y + upturn, -hz), e2 = new Vector3(hx, y + upturn, hz), e3 = new Vector3(-hx, y + upturn, hz);
            Vector3 m0 = new Vector3(0, y, -hz), m1 = new Vector3(hx, y, 0), m2 = new Vector3(0, y, hz), m3 = new Vector3(-hx, y, 0); // sagging edge mids
            Vector3 r0, r1;
            if (alongX) { float rx = Mathf.Max(0.2f, hx - hz * 0.95f); r0 = new Vector3(-rx, y + ridgeH, 0); r1 = new Vector3(rx, y + ridgeH, 0); }
            else { float rz = Mathf.Max(0.2f, hz - hx * 0.95f); r0 = new Vector3(0, y + ridgeH, -rz); r1 = new Vector3(0, y + ridgeH, rz); }
            Vector2 u = UV(roof, 0.35f), u2 = UV(roof, 0.6f);
            if (alongX)
            {
                // back (-z): e0 m0 e1 + ridge
                TriDS(mb, e0, m0, r0, u); TriDS(mb, m0, r1, r0, u); TriDS(mb, m0, e1, r1, u);
                TriDS(mb, e3, r0, m2, u); TriDS(mb, m2, r0, r1, u); TriDS(mb, m2, r1, e2, u);
                TriDS(mb, e0, r0, m3, u2); TriDS(mb, m3, r0, e3, u2);
                TriDS(mb, e1, m1, r1, u2); TriDS(mb, m1, e2, r1, u2);
            }
            else
            {
                TriDS(mb, e0, r0, m0, u2); TriDS(mb, m0, r0, e1, u2);
                TriDS(mb, e3, m2, r1, u2); TriDS(mb, m2, e2, r1, u2);
                TriDS(mb, e0, m3, r0, u); TriDS(mb, m3, r1, r0, u); TriDS(mb, m3, e3, r1, u);
                TriDS(mb, e1, r0, m1, u); TriDS(mb, m1, r0, r1, u); TriDS(mb, m1, r1, e2, u);
            }
            // ridge cap + end ornaments
            Vector3 rc = (r0 + r1) * 0.5f; float len = Vector3.Distance(r0, r1) + 0.8f;
            mb.Box(rc + Vector3.up * 0.12f, alongX ? new Vector3(len, 0.35f, 0.45f) : new Vector3(0.45f, 0.35f, len), ridge);
            Cell orn = ornaments ? C_Gold : ridge;
            mb.Box(r0 + Vector3.up * 0.45f, new Vector3(0.45f, 0.7f, 0.45f), orn);
            mb.Box(r1 + Vector3.up * 0.45f, new Vector3(0.45f, 0.7f, 0.45f), orn);
            // eave fascia (dark band under the roof edge)
            Vector2 uf = UV(ridge, 0.5f);
            TriDS(mb, e0 + Vector3.down * 0.25f, e1 + Vector3.down * 0.25f, e1, uf); TriDS(mb, e0 + Vector3.down * 0.25f, e1, e0, uf);
            TriDS(mb, e3 + Vector3.down * 0.25f, e2, e2 + Vector3.down * 0.25f, uf); TriDS(mb, e3 + Vector3.down * 0.25f, e3, e2, uf);
        }

        static float FootprintGround(Vector2 pos, float yaw, float w, float d)
        {
            float maxY = float.MinValue; Quaternion q = Quaternion.Euler(0, yaw, 0);
            for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++)
                {
                    Vector3 o = q * new Vector3(i * w * 0.5f, 0, j * d * 0.5f);
                    maxY = Mathf.Max(maxY, H(pos.x + o.x, pos.y + o.z));
                }
            return maxY;
        }

        // Asian-style village house. Front door faces local +Z.
        static GameObject BuildHouse(string name, Vector2 pos, float yaw, float w, float d, float h, bool twoTier, Transform parent)
        {
            var mb = new MB();
            float fy = 0.45f;
            mb.Box(new Vector3(0, fy - 0.9f, 0), new Vector3(w + 0.9f, 1.8f, d + 0.9f), C_Stone, 0, 0.15f, 0.5f);
            mb.Box(new Vector3(0, fy + h * 0.5f, 0), new Vector3(w, h, d), C_Wall, 0, 0.05f, 0.2f);
            mb.Box(new Vector3(0, fy + 0.3f, 0), new Vector3(w + 0.14f, 0.6f, d + 0.14f), C_WoodDark);
            mb.Box(new Vector3(0, fy + h - 0.18f, 0), new Vector3(w + 0.24f, 0.36f, d + 0.24f), C_Wood);
            mb.Box(new Vector3(0, fy + h * 0.55f, 0), new Vector3(w + 0.18f, 0.2f, d + 0.18f), C_Wood);
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                    mb.Box(new Vector3(sx * w * 0.5f, fy + h * 0.5f, sz * d * 0.5f), new Vector3(0.42f, h, 0.42f), C_Wood);
            int nPosts = Mathf.Max(1, Mathf.RoundToInt(w / 2.6f) - 1);
            for (int i = 1; i <= nPosts; i++)
            {
                float x = -w * 0.5f + w * i / (nPosts + 1);
                if (Mathf.Abs(x) < 1.2f) continue;
                mb.Box(new Vector3(x, fy + h * 0.5f, d * 0.5f + 0.05f), new Vector3(0.28f, h, 0.14f), C_Wood);
                mb.Box(new Vector3(x, fy + h * 0.5f, -d * 0.5f - 0.05f), new Vector3(0.28f, h, 0.14f), C_Wood);
                // windows between posts on the long sides
                mb.Box(new Vector3(x + (x > 0 ? -1.1f : 1.1f), fy + h * 0.62f, d * 0.5f + 0.04f), new Vector3(1.1f, 0.95f, 0.1f), C_Window);
            }
            mb.Box(new Vector3(0, fy + h * 0.62f, 0), new Vector3(w + 0.2f, 0.9f, 1.0f), C_Window);   // side windows (both ends)
            // door + steps
            mb.Box(new Vector3(0, fy + 1.45f, d * 0.5f + 0.06f), new Vector3(1.9f, 2.9f, 0.14f), C_WoodDark);
            mb.Box(new Vector3(0, fy + 3.0f, d * 0.5f + 0.08f), new Vector3(2.4f, 0.25f, 0.2f), C_Wood);
            mb.Box(new Vector3(0, fy * 0.5f, d * 0.5f + 0.9f), new Vector3(2.6f, fy + 0.2f, 1.0f), C_Stone);
            float ov = 1.1f;
            if (!twoTier)
            {
                HipRoof(mb, w * 0.5f + ov, d * 0.5f + ov, fy + h + 0.1f, 0.42f * Mathf.Min(w, d) + 0.6f, 0.4f, C_Roof, C_RoofRidge, false);
            }
            else
            {
                HipRoof(mb, w * 0.5f + ov, d * 0.5f + ov, fy + h + 0.1f, 1.1f, 0.4f, C_Roof, C_RoofRidge, false);
                float w2 = w * 0.62f, d2 = d * 0.62f, h2 = h * 0.65f, y2 = fy + h + 0.2f;
                mb.Box(new Vector3(0, y2 + h2 * 0.5f, 0), new Vector3(w2, h2, d2), C_Wall, 0, 0.05f, 0.2f);
                for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                        mb.Box(new Vector3(sx * w2 * 0.5f, y2 + h2 * 0.5f, sz * d2 * 0.5f), new Vector3(0.36f, h2, 0.36f), C_Wood);
                mb.Box(new Vector3(0, y2 + h2 * 0.6f, d2 * 0.5f + 0.04f), new Vector3(w2 * 0.5f, 0.8f, 0.1f), C_Window);
                HipRoof(mb, w2 * 0.5f + ov, d2 * 0.5f + ov, y2 + h2 + 0.1f, 0.45f * Mathf.Min(w2, d2) + 0.6f, 0.45f, C_Roof, C_RoofRidge, false);
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = new Vector3(pos.x, FootprintGround(pos, yaw, w, d), pos.y);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        // Central temple / village hall: raised base, red pillar veranda, two-tier roof with gold ornaments.
        static GameObject BuildTemple(string name, Vector2 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            float w = 16f, d = 11f, h = 4.8f, baseH = 1.3f;
            mb.Box(new Vector3(0, baseH - 1.6f, 0), new Vector3(w + 5f, 3.2f, d + 5f), C_Stone, 0, 0.1f, 0.5f);
            for (int s = 0; s < 4; s++) // front stairs (each step reaches the ground)
            {
                float topY = baseH - s * 0.325f;
                mb.Box(new Vector3(0, topY * 0.5f - 0.3f, d * 0.5f + 2.5f + 0.6f * s + 0.3f), new Vector3(6f, topY + 0.6f, 0.6f), C_Stone, 0, 0.2f, 0.4f);
            }
            mb.Box(new Vector3(0, baseH + h * 0.5f, 0), new Vector3(w, h, d), C_Wall, 0, 0.05f, 0.2f);
            mb.Box(new Vector3(0, baseH + 0.3f, 0), new Vector3(w + 0.14f, 0.6f, d + 0.14f), C_Red);
            mb.Box(new Vector3(0, baseH + h - 0.2f, 0), new Vector3(w + 2.6f, 0.4f, d + 2.6f), C_Red);
            // veranda pillars
            for (float x = -w * 0.5f - 1.2f; x <= w * 0.5f + 1.25f; x += (w + 2.4f) / 6f)
            {
                mb.Box(new Vector3(x, baseH + h * 0.5f, d * 0.5f + 1.2f), new Vector3(0.5f, h, 0.5f), C_Red);
                mb.Box(new Vector3(x, baseH + h * 0.5f, -d * 0.5f - 1.2f), new Vector3(0.5f, h, 0.5f), C_Red);
            }
            for (float z = -d * 0.5f - 1.2f; z <= d * 0.5f + 1.25f; z += (d + 2.4f) / 4f)
            {
                mb.Box(new Vector3(-w * 0.5f - 1.2f, baseH + h * 0.5f, z), new Vector3(0.5f, h, 0.5f), C_Red);
                mb.Box(new Vector3(w * 0.5f + 1.2f, baseH + h * 0.5f, z), new Vector3(0.5f, h, 0.5f), C_Red);
            }
            // big doors + windows
            mb.Box(new Vector3(0, baseH + 1.7f, d * 0.5f + 0.06f), new Vector3(3.6f, 3.4f, 0.14f), C_WoodDark);
            mb.Box(new Vector3(-5f, baseH + 2.4f, d * 0.5f + 0.05f), new Vector3(2.2f, 1.4f, 0.12f), C_Window);
            mb.Box(new Vector3(5f, baseH + 2.4f, d * 0.5f + 0.05f), new Vector3(2.2f, 1.4f, 0.12f), C_Window);
            mb.Box(new Vector3(0, baseH + h + 0.6f, d * 0.5f + 2.2f), new Vector3(3.2f, 1.1f, 0.2f), C_Gold); // name plaque
            HipRoof(mb, w * 0.5f + 2.6f, d * 0.5f + 2.6f, baseH + h + 0.1f, 1.6f, 0.7f, C_Roof, C_RoofRidge, true);
            float w2 = w * 0.6f, d2 = d * 0.6f, h2 = 3.0f, y2 = baseH + h + 0.4f;
            mb.Box(new Vector3(0, y2 + h2 * 0.5f, 0), new Vector3(w2, h2, d2), C_Wall, 0, 0.05f, 0.2f);
            mb.Box(new Vector3(0, y2 + h2 - 0.2f, 0), new Vector3(w2 + 0.3f, 0.4f, d2 + 0.3f), C_Red);
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                    mb.Box(new Vector3(sx * w2 * 0.5f, y2 + h2 * 0.5f, sz * d2 * 0.5f), new Vector3(0.45f, h2, 0.45f), C_Red);
            HipRoof(mb, w2 * 0.5f + 1.8f, d2 * 0.5f + 1.8f, y2 + h2 + 0.1f, 0.42f * d2 + 0.9f, 0.75f, C_Roof, C_RoofRidge, true);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = new Vector3(pos.x, FootprintGround(pos, yaw, w + 5, d + 5), pos.y);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static GameObject BuildWatchtower(string name, Vector2 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            float s = 1.7f, ph = 7.0f;
            mb.Box(new Vector3(0, -0.4f, 0), new Vector3(2 * s + 1.4f, 1.0f, 2 * s + 1.4f), C_Stone);
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                    mb.Box(new Vector3(sx * s, (ph + 2.6f) * 0.5f, sz * s), new Vector3(0.38f, ph + 2.6f, 0.38f), C_Wood);
            mb.Box(new Vector3(0, ph, 0), new Vector3(2 * s + 1.0f, 0.35f, 2 * s + 1.0f), C_WoodDark);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f; Quaternion q = Quaternion.Euler(0, a, 0);
                mb.Box(q * new Vector3(0, ph + 0.75f, s + 0.4f), new Vector3(2 * s + 1.0f, 0.18f, 0.18f), C_Wood, a);
                mb.Box(q * new Vector3(0, ph + 0.35f, s + 0.4f), new Vector3(2 * s + 1.0f, 0.5f, 0.08f), C_Wall, a);
                mb.Box(q * new Vector3(0, ph * 0.45f, s), new Vector3(2 * s, 0.22f, 0.22f), C_Wood, a);
            }
            // ladder
            for (int r = 0; r < 12; r++) mb.Box(new Vector3(0, 0.5f + r * 0.55f, s + 0.35f), new Vector3(0.9f, 0.1f, 0.1f), C_WoodDark);
            mb.Box(new Vector3(-0.45f, ph * 0.5f, s + 0.35f), new Vector3(0.1f, ph, 0.1f), C_WoodDark);
            mb.Box(new Vector3(0.45f, ph * 0.5f, s + 0.35f), new Vector3(0.1f, ph, 0.1f), C_WoodDark);
            HipRoof(mb, s + 1.3f, s + 1.3f, ph + 2.6f, 1.6f, 0.35f, C_Roof, C_RoofRidge, false);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(pos);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        // Red asian gate over the road (local X = across the road).
        static GameObject BuildGate(string name, Vector2 pos, float yaw, float span, Transform parent)
        {
            return BuildGate(name, pos, yaw, span, parent, C_Red, C_Red);
        }
        static GameObject BuildGate(string name, Vector2 pos, float yaw, float span, Transform parent, Cell post, Cell beam)
        {
            var mb = new MB();
            float hgt = 6.2f, hs = span * 0.5f;
            for (int sx = -1; sx <= 1; sx += 2)
            {
                mb.Box(new Vector3(sx * hs, 0.3f, 0), new Vector3(1.2f, 0.6f, 1.2f), C_Stone);
                mb.Box(new Vector3(sx * hs, hgt * 0.5f, 0), new Vector3(0.7f, hgt, 0.7f), post);
            }
            mb.Box(new Vector3(0, hgt - 1.1f, 0), new Vector3(span + 0.6f, 0.45f, 0.45f), beam);
            mb.Box(new Vector3(0, hgt + 0.1f, 0), new Vector3(span + 2.6f, 0.55f, 0.9f), C_RoofRidge);
            mb.Box(new Vector3(0, hgt + 0.55f, 0), new Vector3(span + 3.2f, 0.3f, 1.1f), C_Roof);
            mb.Box(new Vector3(0, hgt - 0.5f, 0.05f), new Vector3(1.6f, 0.8f, 0.5f), C_Gold);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(pos);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static GameObject BuildStoneBridge(string name, Vector2 a, Vector2 b, float width, Transform parent, Cell? stoneCell = null, float lift = 0.35f)
        {
            var mb = new MB();
            Cell C_Stone = stoneCell ?? ElementiaMapBuilder.C_Stone;
            Vector2 dir = b - a; float len = dir.magnitude;
            float y = Mathf.Max(H(a.x, a.y), H(b.x, b.y)) + lift;
            mb.Box(new Vector3(0, -0.25f, 0), new Vector3(width, 0.6f, len), C_Stone);
            mb.Box(new Vector3(0, -0.9f, 0), new Vector3(width - 0.6f, 0.8f, len - 1.6f), C_PlazaDark);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                mb.Box(new Vector3(sx * (width * 0.5f - 0.2f), 0.3f, 0), new Vector3(0.4f, 0.6f, len), C_Stone);
                mb.Box(new Vector3(sx * (width * 0.5f - 0.2f), 0.55f, len * 0.5f - 0.3f), new Vector3(0.6f, 1.1f, 0.6f), C_Stone);
                mb.Box(new Vector3(sx * (width * 0.5f - 0.2f), 0.55f, -len * 0.5f + 0.3f), new Vector3(0.6f, 1.1f, 0.6f), C_Stone);
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            Vector2 m = (a + b) * 0.5f;
            go.transform.position = new Vector3(m.x, y, m.y);
            go.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 0);
            return go;
        }

        static GameObject BuildRiceField(string name, Rect r, Transform parent)
        {
            var mb = new MB();
            float y = 0f;
            int rows = Mathf.Max(2, Mathf.RoundToInt(r.height / 0.9f));
            for (int i = 0; i < rows; i++)
            {
                float z0 = r.yMin + r.height * i / rows, z1 = r.yMin + r.height * (i + 1) / rows;
                Cell c = i % 2 == 0 ? C_FieldGreen : C_Field;
                Vector2 u = UV(c, 0.5f);
                mb.Quad(new Vector3(r.xMin, y, z0), new Vector3(r.xMin, y, z1), new Vector3(r.xMax, y, z1), new Vector3(r.xMax, y, z0), u);
            }
            // dikes
            Vector3 c0 = new Vector3(r.center.x, 0, r.center.y);
            mb.Box(new Vector3(r.center.x, 0.12f, r.yMin), new Vector3(r.width + 0.6f, 0.4f, 0.6f), C_Path);
            mb.Box(new Vector3(r.center.x, 0.12f, r.yMax), new Vector3(r.width + 0.6f, 0.4f, 0.6f), C_Path);
            mb.Box(new Vector3(r.xMin, 0.12f, r.center.y), new Vector3(0.6f, 0.4f, r.height), C_Path);
            mb.Box(new Vector3(r.xMax, 0.12f, r.center.y), new Vector3(0.6f, 0.4f, r.height), C_Path);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, false);
            go.transform.position = new Vector3(0, HLow(r.center.x, r.center.y) + 0.15f, 0);
            return go;
        }
    }
}
