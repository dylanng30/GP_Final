// Elementia map builder - Connecting path (Zone 03).
// Purpose: lead the player from the safe village to the volcano and let the world change gradually
// (transition biome, as between regions in Genshin / Zelda BotW):
//   rope bridge -> signpost + wayside shrine (last safe stop) -> pine forest -> viewpoint with Mt. Ignis on the horizon
//   -> ruined stone gate (threshold) -> ember-maple grove + first small skirmish -> scorched trees, ash
//   -> burnt threshold gate at the volcano border.
// Vegetation frames the road (planted along both sides) instead of filling the islands randomly.
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        static readonly Cell C_LeafRed = new Cell(2, 2, 0.15f, 0.45f);
        static readonly Cell C_LeafOrange = new Cell(6, 2, 0.55f, 0.85f);
        static readonly Cell C_LeafAmber = new Cell(7, 2, 0.2f, 0.5f);

        // stylized low-poly broadleaf tree (ember maple)
        static GameObject EmberTree(string name, Vector2 c, float s, Cell leaf, Transform parent)
        {
            var mb = new MB();
            Vector3 top = new Vector3(Rand(-0.2f, 0.2f), 2.6f, Rand(-0.2f, 0.2f)) * s;
            Branch(mb, Vector3.down * 0.3f, top, 0.3f * s, 0.18f * s, C_WoodDark);
            for (int b = 0; b < 2; b++)
            {
                float a = Rand(0, Mathf.PI * 2f);
                Branch(mb, top * 0.7f, top * 0.7f + new Vector3(Mathf.Cos(a), 0.9f, Mathf.Sin(a)) * 1.2f * s, 0.12f * s, 0.06f * s, C_WoodDark);
            }
            int blobs = RandI(3, 5);
            for (int i = 0; i < blobs; i++)
            {
                float a = i * Mathf.PI * 2f / blobs + Rand(-0.3f, 0.3f);
                Vector3 cc = top + new Vector3(Mathf.Cos(a) * 1.1f, Rand(0.2f, 1.0f), Mathf.Sin(a) * 1.1f) * s * (i == 0 ? 0.2f : 1f);
                float r = Rand(1.3f, 1.8f) * s, h = r * 1.5f;
                mb.Cyl(cc, r * 0.5f, r, h * 0.45f, 7, leaf, leaf, false);
                mb.Cyl(cc + Vector3.up * h * 0.45f, r, r * 0.3f, h * 0.55f, 7, leaf, leaf);
            }
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = Ground(c);
            go.transform.rotation = Quaternion.Euler(0, Rand(0, 360), 0);
            return go;
        }

        static GameObject Signpost(string name, Vector3 pos, float yawToVillage, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 1.4f, 0), new Vector3(0.22f, 2.8f, 0.22f), C_WoodDark);
            mb.Box(new Vector3(0.75f, 2.3f, 0), new Vector3(1.5f, 0.38f, 0.08f), C_Wood);     // -> village
            mb.Box(new Vector3(-0.7f, 1.8f, 0), new Vector3(1.4f, 0.38f, 0.08f), C_Red);      // -> volcano
            mb.Box(new Vector3(0, 2.85f, 0), new Vector3(0.4f, 0.12f, 0.4f), C_Roof);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yawToVillage - 90f, 0);
            return go;
        }

        static GameObject WaysideShrine(string name, Vector3 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.3f, 0), new Vector3(2.0f, 0.6f, 1.6f), C_Stone);
            mb.Box(new Vector3(0, 1.05f, 0), new Vector3(1.2f, 0.9f, 1.0f), C_Wood);
            mb.Box(new Vector3(0, 1.05f, 0.51f), new Vector3(0.6f, 0.6f, 0.04f), C_WoodDark);
            mb.Box(new Vector3(0, 0.75f, 0.85f), new Vector3(0.9f, 0.3f, 0.3f), C_Stone);   // offering step
            HipRoof(mb, 1.0f, 0.85f, 1.55f, 0.55f, 0.18f, C_Roof, C_RoofRidge, false);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static void PlantRoadside(Path pa, Transform parent, float spacing, int seedOff)
        {
            int k = seedOff;
            for (int i = 4; i < pa.pts.Count - 3; i += Mathf.Max(3, Mathf.RoundToInt(spacing)))
            {
                Vector2 fwd = (pa.pts[i + 1] - pa.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                foreach (int side in new[] { 1, -1 })
                {
                    k++;
                    Vector2 p = pa.pts[i] + n * side * (pa.width * 0.5f + Rand(2.2f, 4.6f)) + fwd * Rand(-1.5f, 1.5f);
                    if (!OnLand(p, 1.0f) || !Free(p, 1.3f, 0.4f) || InClearing(p, 1f)) continue;
                    float d = Dryness(p.y) + Rand(-0.12f, 0.12f);
                    if (d < 0.35f) Holotna(k % 2 == 0 ? "Tree01A" : "Tree01B", parent, Ground(p) + Vector3.down * 0.2f, Rand(0, 360), Rand(0.7f, 1.0f));
                    else if (d < 0.75f) EmberTree("EmberMaple_" + k, p, Rand(0.85f, 1.2f), (k % 3 == 0) ? C_LeafOrange : (k % 3 == 1 ? C_LeafRed : C_LeafAmber), parent);
                    else BuildDeadTree("ScorchedTree_" + k, p, Rand(0.9f, 1.25f), parent);
                    Occupy(p, 2.0f);
                }
            }
        }

        static void BuildConnectingPath(Transform root)
        {
            var zone = Group("Zone_03_ConnectingPath", root);
            var gameplay = Group("Gameplay_Connecting", zone.transform);
            var conn = paths.Find(p => p.name == "Path_Connecting");

            // 1) Rope bridge out of the village
            Vector2 dir = (BridgeB - BridgeA).normalized;
            var br = Holotna("Bridge01", zone.transform, V3((BridgeA + BridgeB) * 0.5f, Mathf.Max(H(BridgeA.x, BridgeA.y), H(BridgeB.x, BridgeB.y)) - 0.25f), Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, 1f);
            if (br != null) { br.name = "Bridge_VillageToWilds"; br.transform.localScale = new Vector3(1.8f, 1.6f, (Vector2.Distance(BridgeA, BridgeB) + 4.5f) / 9.07f); }

            // 2) Signpost + wayside shrine: the last safe stop
            var stop = Group("WaysideStop", zone.transform);
            Vector2 signP = conn.pts[3] + new Vector2(-(conn.width * 0.5f + 1.2f), 0);
            Signpost("Signpost", Ground(signP), FaceYaw(signP, PlazaC), stop.transform); Occupy(signP, 0.8f);
            Vector2 shrineP = new Vector2(1.2f, 40.5f);
            float shrineYaw = FaceYaw(shrineP, conn.pts[Mathf.Min(10, conn.pts.Count - 1)]);
            WaysideShrine("WaysideShrine", Ground(shrineP), shrineYaw, stop.transform);
            Vector3 sf = Quaternion.Euler(0, shrineYaw, 0) * Vector3.right;
            StoneLantern("WaysideShrine_Lantern", Ground(shrineP + new Vector2(sf.x, sf.z) * 1.9f), 0.85f, stop.transform);
            Occupy(shrineP, 2.2f);
            Marker("Checkpoint_WaysideShrine", shrineP, gameplay.transform);

            // 3) Viewpoint on the east edge of strip B, facing Mt. Ignis
            var view = Group("Viewpoint_MtIgnis", zone.transform);
            Vector2 vp = new Vector2(22.5f, 27.5f);
            if (OnLand(vp, 1.2f))
            {
                float yawView = FaceYaw(vp, new Vector2(30, -95));
                Bench("Viewpoint_Bench", Ground(vp), yawView + 180f, view.transform);
                StoneLantern("Viewpoint_Lantern", Ground(vp + new Vector2(1.8f, 1.2f)), 0.85f, view.transform);
                Occupy(vp, 2f);
            }

            // 4) Ruined stone gate = threshold into the wilds; 6) burnt gate at the volcano border
            foreach (var g in new[] { new { z = 30.5f, name = "RuinedGate_Wilds", post = C_Stone, beam = C_PlazaDark, scale = 0.9f },
                                      new { z = -21.5f, name = "BurntGate_VolcanoBorder", post = C_ConeSide, beam = C_Crust, scale = 0.95f } })
            {
                int best = 1; float bd = 999;
                for (int i = 1; i < conn.pts.Count - 1; i++) { float d = Mathf.Abs(conn.pts[i].y - g.z); if (d < bd) { bd = d; best = i; } }
                Vector2 fwd = (conn.pts[best + 1] - conn.pts[best - 1]).normalized;
                var gate = BuildGate(g.name, conn.pts[best], Mathf.Atan2(fwd.x, fwd.y) * Mathf.Rad2Deg + 0f, 6.6f, zone.transform, g.post, g.beam);
                gate.transform.localScale = Vector3.one * g.scale;
                Vector2 n = new Vector2(-fwd.y, fwd.x);
                // gate flanked by broken columns / rubble so it reads as a ruin
                Kit("Column3", zone.transform, Ground(conn.pts[best] + n * 5.2f), Rand(0, 360), KitScale * 1.1f);
                Kit(g.z > 0 ? "SM_Mossy_Rock_Group3" : "SM_Plain_Rock_Group5", zone.transform, Ground(conn.pts[best] - n * 5.0f) + Vector3.down * 0.1f, Rand(0, 360), 1.6f);
                Occupy(conn.pts[best] + n * 5.2f, 1.2f); Occupy(conn.pts[best] - n * 5.0f, 1.4f); Occupy(conn.pts[best] + n * 3.3f, 0.6f); Occupy(conn.pts[best] - n * 3.3f, 0.6f);
            }

            // 5) Wilds skirmish: small clearing among ember maples with rock cover
            {
                Vector3 cl = clearings[4]; Vector2 c = new Vector2(cl.x, cl.y);
                var g = Group("WildsSkirmish", zone.transform);
                for (int t = 0, made = 0; t < 40 && made < 3; t++)
                {
                    float a = Rand(0, Mathf.PI * 2f); Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * Rand(0.55f, 0.85f);
                    Vector2 q; if (DistToPolyline(p, conn.pts, out q) < conn.width * 0.5f + 1.5f || !OnLand(p, 1.2f)) continue;
                    bool clash = false; foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + 1.2f) clash = true;
                    if (clash) continue;
                    Kit("SM_Mossy_Rock_Group" + (4 + made * 3), g.transform, Ground(p) + Vector3.down * 0.1f, Rand(0, 360), Rand(1.6f, 2.2f));
                    Occupy(p, 1.4f); made++;
                }
                for (int i = 0; i < 2; i++) Marker("EnemySpawn_Wilds_" + i, c + new Vector2(i == 0 ? -3f : 3f, i == 0 ? 2f : -2f), gameplay.transform);
            }

            // Vegetation: trees planted along the road (pine -> ember maple -> scorched), sparse rim, ground cover fading with dryness
            var gVeg = Group("Vegetation", zone.transform);
            PlantRoadside(conn, gVeg.transform, 4.5f, 0);
            for (int s = 1; s <= 5; s++)
            {
                var isl = islands[s];
                Scatter(isl, 0.86f, 4, 1.1f, 0.6f, (p, k) =>
                {
                    float d = Dryness(p.y);
                    if (d < 0.5f) Holotna("Bush01", gVeg.transform, Ground(p), Rand(0, 360), Rand(0.8f, 1.15f));
                    else Kit("SM_Plain_Rock_Group" + (1 + k % 3), gVeg.transform, Ground(p) + Vector3.down * 0.1f, Rand(0, 360), Rand(1.0f, 1.5f));
                });
                Scatter(isl, 0.9f, 120, 0.4f, -0.3f, (p, k) => { if (Rand(0, 1) > Dryness(p.y) * 1.1f) Holotna("Grass01" + "ABC"[k % 3], gVeg.transform, Ground(p), Rand(0, 360), Rand(0.85f, 1.25f)); }, false);
                Scatter(isl, 0.9f, 14, 0.4f, -0.2f, (p, k) => { if (Dryness(p.y) < 0.4f) Holotna(k % 2 == 0 ? "Flowers01A" : "Flowers01B", gVeg.transform, Ground(p), Rand(0, 360), Rand(0.9f, 1.2f)); }, false);
            }
            // floating islets: one tree each, matching the biome at their position
            for (int s = 8; s <= 10; s++)
            {
                var isl = islands[s];
                if (isl.region == Region.Volcano) continue;
                if (Dryness(isl.c.y) < 0.35f) Holotna("Tree01A", gVeg.transform, Ground(isl.c), Rand(0, 360), 0.7f);
                else EmberTree("EmberMaple_Islet_" + s, isl.c, 0.9f, C_LeafRed, gVeg.transform);
                Scatter(isl, 0.85f, 10, 0.4f, 0f, (p, k) => Holotna("Grass01A", gVeg.transform, Ground(p), Rand(0, 360), 1.1f), false);
            }
        }
    }
}
