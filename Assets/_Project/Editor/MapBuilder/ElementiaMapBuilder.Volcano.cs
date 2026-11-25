// Elementia map builder - volcanic area: cones, lava, crystals, dead trees, ruins, boss arena.
using System.Collections.Generic;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        const string FX = "Assets/Epic Toon FX/Prefabs/Environment/";

        // BuildCone / lava channels / boss arena: see ElementiaMapBuilder.VolcanoLava.cs
        static GameObject BuildCrystals(string name, Vector2 c, float scale, Transform parent)
        {
            var mb = new MB();
            int n = RandI(3, 6);
            for (int i = 0; i < n; i++)
            {
                Vector3 b = new Vector3(Rand(-0.6f, 0.6f), -0.2f, Rand(-0.6f, 0.6f)) * scale;
                Vector3 tipDir = (Vector3.up * 2f + new Vector3(b.x, 0, b.z).normalized * Rand(0.2f, 0.9f)).normalized;
                float len = Rand(1.0f, 2.4f) * scale * (i == 0 ? 1.4f : 1f), rad = Rand(0.18f, 0.32f) * scale * (i == 0 ? 1.3f : 1f);
                Shard(mb, b, b + tipDir * len, rad, C_Red);
            }
            var go = MeshGO(name, mb.ToMesh(name), matCrystal, parent, false);
            go.transform.position = Ground(c);
            go.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            return go;
        }
        // hexagonal prism with pointed tip from a to b
        static void Shard(MB mb, Vector3 a, Vector3 b, float r, Cell cell)
        {
            Vector3 ax = (b - a).normalized;
            Vector3 u = Vector3.Cross(ax, Mathf.Abs(ax.y) > 0.9f ? Vector3.right : Vector3.up).normalized, v = Vector3.Cross(ax, u);
            Vector3 neck = Vector3.Lerp(a, b, 0.75f);
            for (int i = 0; i < 6; i++)
            {
                float a0 = i * Mathf.PI / 3f, a1 = (i + 1) * Mathf.PI / 3f;
                Vector3 o0 = (u * Mathf.Cos(a0) + v * Mathf.Sin(a0)) * r, o1 = (u * Mathf.Cos(a1) + v * Mathf.Sin(a1)) * r;
                mb.Quad(a + o0, neck + o0, neck + o1, a + o1, UV(cell, 0.8f), UV(cell, 0.2f), UV(cell, 0.2f), UV(cell, 0.8f));
                mb.Tri(neck + o0, b, neck + o1, UV(cell, 0.05f));
            }
        }

        static void Branch(MB mb, Vector3 a, Vector3 b, float ra, float rb, Cell cell)
        {
            Vector3 ax = (b - a).normalized;
            Vector3 u = Vector3.Cross(ax, Mathf.Abs(ax.y) > 0.9f ? Vector3.right : Vector3.up).normalized, v = Vector3.Cross(ax, u);
            for (int i = 0; i < 5; i++)
            {
                float a0 = i * Mathf.PI * 2f / 5f, a1 = (i + 1) * Mathf.PI * 2f / 5f;
                Vector3 d0 = u * Mathf.Cos(a0) + v * Mathf.Sin(a0), d1 = u * Mathf.Cos(a1) + v * Mathf.Sin(a1);
                mb.Quad(a + d0 * ra, b + d0 * rb, b + d1 * rb, a + d1 * ra, UV(cell, 0.7f), UV(cell, 0.3f), UV(cell, 0.3f), UV(cell, 0.7f));
            }
        }
        static GameObject BuildDeadTree(string name, Vector2 c, float s, Transform parent)
        {
            var mb = new MB();
            Vector3 top = new Vector3(Rand(-0.4f, 0.4f), 4.2f, Rand(-0.4f, 0.4f)) * s;
            Branch(mb, Vector3.down * 0.3f, top, 0.42f * s, 0.16f * s, C_WoodDark);
            int n = RandI(3, 6);
            for (int i = 0; i < n; i++)
            {
                Vector3 bs = Vector3.Lerp(Vector3.zero, top, Rand(0.45f, 0.9f));
                float ang = Rand(0, Mathf.PI * 2f);
                Vector3 be = bs + new Vector3(Mathf.Cos(ang), Rand(0.5f, 1.1f), Mathf.Sin(ang)) * Rand(1.2f, 2.2f) * s;
                Branch(mb, bs, be, 0.14f * s, 0.04f * s, C_WoodDark);
                Vector3 tw = be + new Vector3(Mathf.Cos(ang + 1f), 0.6f, Mathf.Sin(ang + 1f)) * 0.8f * s;
                Branch(mb, be, tw, 0.05f * s, 0.02f * s, C_WoodDark);
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(c);
            go.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            return go;
        }

        // BuildVolcano: see ElementiaMapBuilder.VolcanoLayout.cs

        // Random free points on an island (normalized radius < maxNorm)
        static void Scatter(Island isl, float maxNorm, int count, float radius, float roadMargin, System.Action<Vector2, int> spawn, bool occupy = true)
        {
            int made = 0, tries = 0;
            while (made < count && tries++ < count * 40)
            {
                Vector2 p = isl.c + new Vector2(Rand(-isl.rx, isl.rx), Rand(-isl.rz, isl.rz)) * 1.1f;
                if (isl.Norm(p) > maxNorm) continue;
                if (!Free(p, radius, roadMargin)) continue;
                if (InClearing(p, radius)) continue;
                spawn(p, made);
                if (occupy) Occupy(p, radius);
                made++;
            }
        }
        static void ScatterRim(Island isl, Transform parent, int trees, int bushes, int grass, int flowers)
        {
            int t = 0, b = 0, g = 0, f = 0, tries = 0;
            while ((t < trees || b < bushes || g < grass || f < flowers) && tries++ < 8000)
            {
                float ang = Rand(0, Mathf.PI * 2f); float R = isl.Radius(ang) * Rand(0.84f, 0.93f);
                Vector2 p = isl.c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * R;
                if (t < trees && Free(p, 2.2f)) { Holotna(Rand(0, 1) < 0.5f ? "Tree01A" : "Tree01B", parent, Ground(p) + Vector3.down * 0.2f, Rand(0, 360), Rand(0.75f, 1.05f)); Occupy(p, 2.2f); t++; continue; }
                if (b < bushes && Free(p, 1.2f)) { Holotna("Bush01", parent, Ground(p), Rand(0, 360), Rand(0.8f, 1.2f)); Occupy(p, 1.0f); b++; continue; }
                if (g < grass && Free(p, 0.4f, 0f)) { Holotna("Grass01" + "ABC"[RandI(0, 3)], parent, Ground(p), Rand(0, 360), Rand(0.9f, 1.3f)); g++; continue; }
                if (f < flowers && Free(p, 0.4f, 0f)) { Holotna(Rand(0, 1) < 0.5f ? "Flowers01A" : "Flowers01B", parent, Ground(p), Rand(0, 360), Rand(0.9f, 1.2f)); f++; }
            }
        }
    }
}
