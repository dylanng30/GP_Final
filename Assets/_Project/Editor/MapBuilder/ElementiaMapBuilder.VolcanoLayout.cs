// Elementia map builder - Volcanic area (Zone 04) layout.
// Design (refs: Death Mountain in Zelda BotW, Volcanic Hollow in Monster Hunter, Mt. Doom approach):
//  * One landmark volcano (Mt. Ignis) visible from everywhere -> the player always knows where they are.
//  * Critical path: Entry camp (checkpoint) -> Ash Field (fight 1) -> lava river bridge (choke point)
//    -> Obsidian Ruins (fight 2, pillars as cover) -> Gate of Flames (two volcanoes framing the road)
//    -> pre-boss shrine (checkpoint) -> Boss Arena.
//  * Optional branches with rewards: Crystal Hollow (elite + chest) and Lava Lake Overlook (chest + vista).
//  * Smaller volcanoes are placed as ridges on the edges to frame the playable space, never inside fights.
using System.Collections.Generic;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        class ConeDef { public string name; public Vector2 c; public float r, h; public bool active; public ConeDef(string n, float x, float z, float r, float h, bool a) { name = n; c = new Vector2(x, z); this.r = r; this.h = h; active = a; } }

        static GameObject Marker(string name, Vector2 p, Transform parent, float yOff = 0.2f)
        {
            var m = Group(name, parent); m.transform.position = Ground(p) + Vector3.up * yOff; return m;
        }

        static GameObject TreasureChest(string name, Vector3 pos, float yaw, Transform parent)
        {
            var mb = new MB();
            mb.Box(new Vector3(0, 0.38f, 0), new Vector3(1.3f, 0.76f, 0.85f), C_Wood);
            mb.Box(new Vector3(0, 0.88f, 0), new Vector3(1.36f, 0.3f, 0.9f), C_WoodDark);
            mb.Box(new Vector3(-0.45f, 0.55f, 0), new Vector3(0.12f, 1.0f, 0.95f), C_Gold);
            mb.Box(new Vector3(0.45f, 0.55f, 0), new Vector3(0.12f, 1.0f, 0.95f), C_Gold);
            mb.Box(new Vector3(0, 0.62f, 0.46f), new Vector3(0.22f, 0.28f, 0.06f), C_Gold);
            var go = MeshGO(name, mb.ToMesh(name), matEnv, parent, true);
            go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            return go;
        }

        static void BuildLavaFall(Path river, Transform parent)
        {
            Vector2 end = river.pts[river.pts.Count - 1], prev = river.pts[river.pts.Count - 2];
            if (OnLand(end, -4f) && IslandAt(end).Norm(end) < 0.9f) return; // river does not reach the cliff
            Vector2 fwd = (end - prev).normalized, n = new Vector2(-fwd.y, fwd.x);
            float w = river.width * 0.5f, y0 = H(end.x, end.y) + 0.24f;
            var mb = new MB();
            float[] drop = { 0f, 2f, 6f, 12f, 20f, 28f };
            for (int k = 0; k < drop.Length - 1; k++)
            {
                float o0 = 0.5f + drop[k] * 0.1f, o1 = 0.5f + drop[k + 1] * 0.1f;
                Vector3 a = V3(end + fwd * o0, y0 - drop[k]), b = V3(end + fwd * o1, y0 - drop[k + 1]);
                Vector3 s0 = V3(n, 0) * w * (1f - k * 0.08f), s1 = V3(n, 0) * w * (1f - (k + 1) * 0.08f);
                mb.Quad(a - s0, b - s1, b + s1, a + s0, UV(C_Lava, 0.5f));
                mb.Quad(a - s0, a + s0, b + s1, b - s1, UV(C_Lava, 0.5f));
            }
            Vector3 l0 = V3(end - n * w, y0), r0 = V3(end + n * w, y0), l1 = V3(end + fwd * 0.5f - n * w, y0), r1 = V3(end + fwd * 0.5f + n * w, y0);
            mb.Quad(l0, l1, r1, r0, UV(C_Lava, 0.5f)); mb.Quad(l0, r0, r1, l1, UV(C_Lava, 0.5f));
            MeshGO(river.name + "_LavaFall", mb.ToMesh(river.name + "_LavaFall"), matLava, parent, false);
            var sm = SpawnPrefab(FX + "Smoke/Dark/SmokeDarkSoft.prefab", parent, V3(end + fwd * 2.5f, y0 - 24f), 0, 3f);
            if (sm != null) sm.name = "FX_LavaFallSmoke";
        }

        static void BuildRuins(Vector2 c, Transform parent)
        {
            var g = Group("ObsidianRuins", parent);
            // cracked stone floor (some slabs missing -> dark grout shows)
            var mb = new MB();
            Rect floor = new Rect(c.x - 9.5f, c.y - 9f, 19f, 18f);
            float ay = MaxH(floor) + 0.35f;   // raised platform: clearly above the road ribbon
            PavedRect(mb, floor, ay, 1.9f, 1.6f, true, 0.22f, C_PlazaDark);
            // platform edge (curb + skirt down to the terrain)
            mb.Box(new Vector3(floor.center.x, ay - 0.45f, floor.yMin), new Vector3(floor.width + 0.6f, 1.0f, 0.6f), C_ConeSide);
            mb.Box(new Vector3(floor.center.x, ay - 0.45f, floor.yMax), new Vector3(floor.width + 0.6f, 1.0f, 0.6f), C_ConeSide);
            mb.Box(new Vector3(floor.xMin, ay - 0.45f, floor.center.y), new Vector3(0.6f, 1.0f, floor.height), C_ConeSide);
            mb.Box(new Vector3(floor.xMax, ay - 0.45f, floor.center.y), new Vector3(0.6f, 1.0f, floor.height), C_ConeSide);
            // central altar (west side, off the walking line)
            Vector2 altar = c + new Vector2(-6.2f, 0);
            mb.Box(new Vector3(altar.x, ay + 0.25f, altar.y), new Vector3(3.4f, 0.5f, 3.4f), C_Stone);
            mb.Box(new Vector3(altar.x, ay + 0.75f, altar.y), new Vector3(2.4f, 0.5f, 2.4f), C_ConeSide);
            mb.Box(new Vector3(altar.x, ay + 1.15f, altar.y), new Vector3(1.6f, 0.3f, 1.6f), C_Crust);
            // entrance steps where the road meets the raised platform
            var mainRoad = paths.Find(p => p.name == "Path_Volcano_Main");
            foreach (int sgn in new[] { 1, -1 })
            {
                float ez = sgn > 0 ? floor.yMax : floor.yMin;
                Vector2 bestP = mainRoad.pts[0]; float bd = 999;
                foreach (var q in mainRoad.pts) { float d = Mathf.Abs(q.y - ez); if (d < bd) { bd = d; bestP = q; } }
                for (int s = 0; s < 2; s++)
                {
                    float top = ay - 0.12f - s * 0.13f;
                    mb.Box(new Vector3(bestP.x, top - 0.4f, ez + sgn * (0.6f + s * 0.55f)), new Vector3(5.6f, 0.8f, 0.6f), C_Stone, 0, 0.2f, 0.5f);
                }
            }
            MeshGO("Ruins_Floor_Altar", mb.ToMesh("Ruins_Floor_Altar"), matEnv, g.transform, true);
            var fire = SpawnPrefab(FX + "Fire/Cartoon/Tall/ToonTallFireRed.prefab", g.transform, new Vector3(altar.x, ay + 1.3f, altar.y), 0, 1.2f);
            if (fire != null) fire.name = "FX_AltarFire";
            BuildCrystals("Ruins_AltarCrystals", altar + new Vector2(-2.2f, 0), 1.4f, g.transform);
            // colonnades along both long sides (alternating intact / broken -> cover for combat)
            string[] tall = { "column1.1", "column1.3", "Column6", "column1.4" }, broken = { "Column2", "Column3" };
            for (int sx = -1; sx <= 1; sx += 2)
                for (int i = 0; i < 5; i++)
                {
                    Vector2 p = c + new Vector2(sx * 8.6f, -7.5f + i * 3.75f);
                    string piece = (i % 2 == 1) ? broken[(i / 2 + (sx > 0 ? 1 : 0)) % 2] : tall[(i / 2 + (sx > 0 ? 2 : 0)) % 4];
                    Kit(piece, g.transform, new Vector3(p.x, ay - 0.05f, p.y), sx > 0 ? 90 : -90, KitScale * 1.35f);
                }
            // broken walls on the corners
            Kit("wall", g.transform, new Vector3(c.x - 8.2f, ay - 0.05f, c.y + 10.2f), 0, KitScale * 1.1f);
            Kit("wall", g.transform, new Vector3(c.x + 8.2f, ay - 0.05f, c.y - 10.2f), 180, KitScale * 1.1f);
            // entrance gates (dark stone, same language as the boss arena gate) where the road enters / leaves
            var main = paths.Find(p => p.name == "Path_Volcano_Main");
            foreach (float zEdge in new[] { c.y + 9.6f, c.y - 9.6f })
            {
                int best = 0; float bd = 999;
                for (int i = 0; i < main.pts.Count; i++) { float d = Mathf.Abs(main.pts[i].y - zEdge); if (d < bd) { bd = d; best = i; } }
                int i0 = Mathf.Clamp(best, 1, main.pts.Count - 2);
                Vector2 fwd = (main.pts[i0 + 1] - main.pts[i0 - 1]).normalized;
                var gate = BuildGate("Ruins_Gate", main.pts[i0], Mathf.Atan2(fwd.x, fwd.y) * Mathf.Rad2Deg, 7.0f, g.transform, C_ConeSide, C_Crust);
                gate.transform.localScale = Vector3.one * 0.85f;
                gate.transform.position = Ground(main.pts[i0]) + Vector3.down * 0.05f;
            }
            Occupy(c, 11f);
        }

        static void BuildCheckpoint(string name, Vector2 p, float yaw, Transform parent, bool camp)
        {
            var g = Group(name, parent);
            if (camp)
            {
                Kit("a tent", g.transform, Ground(p + new Vector2(2.5f, 2.5f)), yaw + 200, KitScale);
                Kit("barriers", g.transform, Ground(p + new Vector2(4.5f, -2.5f)), yaw + 30, KitScale);
                Kit("Sword", g.transform, Ground(p + new Vector2(0.8f, 3.8f)), 70, KitScale, false);
                Kit("Shield", g.transform, Ground(p + new Vector2(1.6f, 4.0f)), 20, KitScale, false);
                Kit("A log of firewood", g.transform, Ground(p + new Vector2(-2.6f, 0.4f)), 10, KitScale, false);
            }
            else
            {
                var mb = new MB();
                mb.Cyl(Vector3.zero, 4.2f, 4.0f, 0.35f, 8, C_Stone, C_PlazaDark);
                var plat = MeshGO(name + "_Platform", mb.ToMesh(name + "_Platform"), matEnv, g.transform, true);
                plat.transform.position = Ground(p) + Vector3.down * 0.1f;
                Vector2 side = new Vector2(Mathf.Cos(yaw * Mathf.Deg2Rad), -Mathf.Sin(yaw * Mathf.Deg2Rad));
                StoneLantern(name + "_LanternL", Ground(p + side * 3.1f) + Vector3.up * 0.25f, 1.1f, g.transform);
                StoneLantern(name + "_LanternR", Ground(p - side * 3.1f) + Vector3.up * 0.25f, 1.1f, g.transform);
                Kit("Banner", g.transform, Ground(p + side * 1.6f + new Vector2(side.y, -side.x) * 2.6f) + Vector3.up * 0.25f, yaw, KitScale * 1.2f);
            }
            Kit("Bonfire", g.transform, Ground(p) + Vector3.up * (camp ? 0f : 0.25f), 0, KitScale * 1.2f, false);
            var f = SpawnPrefab(FX + "Fire/Cartoon/Radial/ToonRadialFireRed.prefab", g.transform, Ground(p) + Vector3.up * (camp ? 0.05f : 0.3f), 0, 0.8f);
            if (f != null) f.name = "FX_CheckpointFire";
            Marker("Checkpoint_" + name, p, g.transform);
            Occupy(p, camp ? 6.5f : 5f);
        }

        static void RockCover(Vector3 cl, int count, Transform parent, string prefix)
        {
            Vector2 c = new Vector2(cl.x, cl.y);
            string[] groups = { "SM_Plain_Rock_Group4", "SM_Plain_Rock_Group5", "SM_Plain_Rock_Group7", "SM_Plain_Rock_Group13", "SM_Plain_Rock_Group6" };
            int made = 0;
            for (int t = 0; t < 60 && made < count; t++)
            {
                float a = Rand(0, Mathf.PI * 2f); Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * Rand(0.45f, 0.8f);
                bool ok = true;
                foreach (var pa in paths) { Vector2 q; if (DistToPolyline(p, pa.pts, out q) < pa.width * 0.5f + 1.8f) ok = false; }
                foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + 1.5f) ok = false;
                if (!ok) continue;
                Kit(groups[made % groups.Length], parent, Ground(p) + Vector3.down * 0.15f, Rand(0, 360), Rand(1.8f, 2.6f));
                Occupy(p, 1.6f); made++;
            }
        }

        static void BuildVolcano(Transform root)
        {
            var zone = Group("Zone_04_VolcanicArea", root);
            var gameplay = Group("Gameplay_Volcano", zone.transform);
            var isl = VolcanoIsland;
            BuildBossArena(root);

            // ---- Lava ----
            var gLava = Group("Lava", zone.transform);
            foreach (var lr in lavaRivers)
            {
                BuildLavaChannel(lr.name, lr.pts, lr.width, gLava.transform);
                BuildLavaFall(lr, gLava.transform);
            }
            int li = 0;
            foreach (var lp in lavaPools)
            {
                Vector2 c = new Vector2(lp.x, lp.y);
                BuildLavaPool("LavaPool_" + li, c, lp.z, gLava.transform, li * 3.3f + 1f);
                var ff = SpawnPrefab(FX + "Fireflies/FireFliesRed.prefab", gLava.transform, V3(c, HLow(c.x, c.y) + 0.8f), 0, 1.0f + lp.z * 0.12f);
                if (ff != null) ff.name = "FX_Embers";
                Occupy(c, lp.z + 1.3f);
                li++;
            }
            int bi = 0;
            foreach (var pa in paths) foreach (var lr in lavaRivers)
                    for (int i = 0; i < pa.pts.Count - 1; i++) for (int j = 0; j < lr.pts.Count - 1; j++)
                        {
                            Vector2 hit;
                            if (!SegIntersect(pa.pts[i], pa.pts[i + 1], lr.pts[j], lr.pts[j + 1], out hit)) continue;
                            Vector2 dir = (pa.pts[i + 1] - pa.pts[i]).normalized; float half = lr.width * 0.5f + 2.8f;
                            BuildStoneBridge("LavaBridge_" + (bi++), hit - dir * half, hit + dir * half, pa.width + 1.2f, gLava.transform, C_ConeSide, 0.85f);
                            Occupy(hit, half);
                        }

            // ---- Landmark + framing volcanoes ----
            var gCones = Group("Volcanoes", zone.transform);
            BuildCone("Mt_Ignis (Landmark)", new Vector2(30, -95), 26f, 34f, true, gCones.transform, 3.3f, 3, 5f, new[] { 168f, 282f, 4f }, false);
            Occupy(new Vector2(30, -95), 26f);
            var cones = new List<ConeDef>
            {
                new ConeDef("GateOfFlames_East", -13, -121, 9f, 9.5f, true),
                new ConeDef("GateOfFlames_West", -47, -108, 8.5f, 8f, false),
                new ConeDef("Ridge_West_A", -56, -55, 7f, 6f, true),
                new ConeDef("Ridge_West_B", -70, -79, 6f, 5f, false),
                new ConeDef("Ridge_North", 12, -68, 6f, 5.5f, false),
                new ConeDef("Ridge_NorthEast", 36, -40, 5.5f, 4.5f, false),
                new ConeDef("Ridge_East", 72, -80, 6f, 5f, false),
                new ConeDef("Ridge_SouthEast", 52, -125, 7f, 6f, true),
                new ConeDef("Ridge_South", -6, -147, 6f, 5f, false),
            };
            int ci = 0;
            foreach (var cd in cones) { BuildCone(cd.name, cd.c, cd.r, cd.h, cd.active, gCones.transform, 7.1f + ci++ * 1.9f, 1, 2.2f); Occupy(cd.c, cd.r); }

            // ---- Checkpoints ----
            BuildCheckpoint("EntryCamp", new Vector2(14, -37), 0, zone.transform, true);
            BuildCheckpoint("PreBossShrine", new Vector2(-24, -128), FaceYaw(new Vector2(-24, -128), new Vector2(-34, -122)), zone.transform, false);

            // ---- Encounter spaces ----
            var gEnc = Group("Encounters", zone.transform);
            // 1. Ash Field: open ground framed by dead trees, a few boulders as cover
            {
                Vector3 cl = clearings[0]; Vector2 c = new Vector2(cl.x, cl.y);
                var g = Group("AshField", gEnc.transform);
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + 20f) * Mathf.Deg2Rad; Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (cl.z + Rand(1.5f, 3f));
                    if (Free(p, 1.2f, 0.8f)) { BuildDeadTree("DeadTree_Ash_" + i, p, Rand(1.0f, 1.4f), g.transform); Occupy(p, 1.2f); }
                }
                RockCover(cl, 3, g.transform, "Ash");
                for (int i = 0; i < 3; i++) Marker("EnemySpawn_AshField_" + i, c + new Vector2(Mathf.Cos(i * 2.1f), Mathf.Sin(i * 2.1f)) * 5f, gameplay.transform);
            }
            // 2. Obsidian Ruins
            {
                Vector3 cl = clearings[1]; Vector2 c = new Vector2(cl.x, cl.y);
                BuildRuins(c, gEnc.transform);
                for (int i = 0; i < 4; i++) Marker("EnemySpawn_Ruins_" + i, c + new Vector2((i % 2 == 0 ? -1 : 1) * 4.5f, (i < 2 ? -1 : 1) * 4.5f), gameplay.transform);
            }
            // 3. Crystal Hollow (optional elite + treasure)
            {
                Vector3 cl = clearings[2]; Vector2 c = new Vector2(cl.x, cl.y);
                var g = Group("CrystalHollow", gEnc.transform);
                for (int i = 0; i < 11; i++)
                {
                    float a = (i * 32.7f + 200f) * Mathf.Deg2Rad; Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * cl.z * Rand(0.75f, 1.05f);
                    if (!OnLand(p, 1f)) continue;
                    bool onRoad = false; foreach (var pa in paths) { Vector2 q; if (DistToPolyline(p, pa.pts, out q) < pa.width * 0.5f + 1.2f) onRoad = true; }
                    if (onRoad) continue;
                    BuildCrystals("Crystal_Hollow_" + i, p, Rand(1.4f, 2.4f), g.transform);
                }
                Vector2 chestP = c + (c - new Vector2(43, -55)).normalized * (cl.z * 0.6f);
                TreasureChest("TreasureChest_CrystalHollow", Ground(chestP), FaceYaw(chestP, c), g.transform);
                Marker("Treasure_CrystalHollow", chestP, gameplay.transform);
                Marker("EliteSpawn_CrystalHollow", c, gameplay.transform);
                Occupy(c, cl.z);
            }
            // 4. Lava Lake Overlook (optional treasure + vista)
            {
                Vector3 cl = clearings[3]; Vector2 c = new Vector2(cl.x, cl.y);
                var g = Group("LavaLakeOverlook", gEnc.transform);
                var mb = new MB(); mb.Cyl(Vector3.zero, 5.2f, 5.0f, 0.4f, 10, C_Stone, C_PlazaDark);
                var plat = MeshGO("Overlook_Platform", mb.ToMesh("Overlook_Platform"), matEnv, g.transform, true);
                plat.transform.position = Ground(c) + Vector3.up * 0.05f;
                Vector2 toLake = (new Vector2(lavaPools[0].x, lavaPools[0].y) - c).normalized, side = new Vector2(-toLake.y, toLake.x);
                Kit("Column4", g.transform, Ground(c + toLake * 4.2f + side * 3f) + Vector3.up * 0.2f, 0, KitScale * 1.3f);
                Kit("Column2", g.transform, Ground(c + toLake * 4.2f - side * 3f) + Vector3.up * 0.2f, 0, KitScale * 1.3f);
                TreasureChest("TreasureChest_Overlook", Ground(c + toLake * 2.2f) + Vector3.up * 0.25f, FaceYaw(c + toLake * 2.2f, c), g.transform);
                Marker("Treasure_Overlook", c + toLake * 2.2f, gameplay.transform);
                Occupy(c, cl.z);
            }

            // ---- Road dressing: boulders framing the roads, crystals on the lava banks ----
            var gRocks = Group("Rocks", zone.transform);
            string[] groupsR = { "SM_Plain_Rock_Group3", "SM_Plain_Rock_Group6", "SM_Plain_Rock_Group14", "SM_Plain_Rock_Group12", "SM_Plain_Rock_Group10" };
            int rk = 0;
            foreach (var pa in paths)
            {
                if (!pa.name.StartsWith("Path_Volcano")) continue;
                for (int i = 6; i < pa.pts.Count - 4; i += 9)
                {
                    Vector2 fwd = (pa.pts[i + 1] - pa.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                    Vector2 p = pa.pts[i] + n * (pa.width * 0.5f + Rand(1.8f, 3.2f)) * (rk % 2 == 0 ? 1 : -1);
                    if (!Free(p, 1.1f, 0.4f) || InClearing(p, 1f)) { rk++; continue; }
                    Kit(groupsR[rk % groupsR.Length], gRocks.transform, Ground(p) + Vector3.down * 0.15f, Rand(0, 360), Rand(1.2f, 2.0f));
                    Kit("SM_Plain_Rock" + (1 + rk % 32), gRocks.transform, Ground(p + fwd * 1.6f), Rand(0, 360), Rand(1.3f, 2f), false);
                    Occupy(p, 1.3f); rk++;
                }
            }
            var gCrys = Group("Crystals", zone.transform);
            var river = lavaRivers[0];
            for (int i = 8, k = 0; i < river.pts.Count - 6; i += 16, k++)
            {
                Vector2 fwd = (river.pts[i + 1] - river.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                Vector2 p = river.pts[i] + n * (river.width * 0.5f + 2.6f) * (k % 2 == 0 ? 1 : -1);
                if (!Free(p, 0.9f, 0.6f)) continue;
                BuildCrystals("Crystal_Bank_" + k, p, Rand(0.9f, 1.4f), gCrys.transform); Occupy(p, 1f);
            }
            // ---- Wayfinding (same rule as the village lamps): fire posts along the critical path, obelisks at optional branches ----
            var gWay = Group("Wayfinding", zone.transform);
            var mainR = paths.Find(p => p.name == "Path_Volcano_Main");
            for (int i = 14, k = 0; i < mainR.pts.Count - 10; i += 22, k++)
            {
                Vector2 fwd = (mainR.pts[i + 1] - mainR.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                Vector2 p = mainR.pts[i] + n * (mainR.width * 0.5f + 1.3f) * (k % 2 == 0 ? 1 : -1);
                if (!Free(p, 0.7f, 0.2f) || InClearing(p, 0.5f)) { p = mainR.pts[i] - n * (mainR.width * 0.5f + 1.3f) * (k % 2 == 0 ? 1 : -1); if (!Free(p, 0.7f, 0.2f) || InClearing(p, 0.5f)) continue; }
                BrazierPost("FirePost_" + k, Ground(p), 1.0f, gWay.transform); Occupy(p, 0.9f);
            }
            foreach (var bn in new[] { "Path_Volcano_CrystalHollow", "Path_Volcano_Overlook" })
            {
                var br = paths.Find(p => p.name == bn);
                int i = Mathf.Min(16, br.pts.Count - 2);
                Vector2 fwd = (br.pts[i + 1] - br.pts[i]).normalized, n = new Vector2(-fwd.y, fwd.x);
                Vector2 p = br.pts[i] + n * (br.width * 0.5f + 1.6f);
                Obelisk("Obelisk_" + bn.Replace("Path_Volcano_", ""), p, gWay.transform); Occupy(p, 1.5f);
            }

            // ---- Island rim: basalt formations instead of vegetation (whole island reads as one volcanic biome) ----
            var gRim = Group("BasaltRim", zone.transform);
            int made = 0;
            for (float ang = 0; ang < 360f; ang += Rand(9f, 15f))
            {
                float a = ang * Mathf.Deg2Rad; float R = isl.Radius(a) * Rand(0.86f, 0.92f);
                Vector2 p = isl.c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R;
                if (!Free(p, 2.2f, 1.5f) || InClearing(p, 2f)) continue;
                BasaltCluster("Basalt_" + (made++), p, Rand(1.0f, 1.8f), gRim.transform); Occupy(p, 2.6f);
            }
            for (int s = 0; s < islands.Count; s++)
                if (islands[s].name == "Islet_SouthWest") BasaltCluster("Basalt_Islet", islands[s].c, 1.2f, gRim.transform);
        }
    }
}
