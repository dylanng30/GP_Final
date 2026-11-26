// Elementia map builder - layout data, height field, floating islands, ribbons (paths / water / lava).
using System.Collections.Generic;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        enum Region { Village, Strip, Volcano, Arena, Jungle }

        class Island
        {
            public string name; public Vector2 c; public float rx, rz, rot, amp, seed, depth; public Region region;
            // optional bay: the outline is pulled in around bayAng (deg) - used where the mainland wraps around the village gate
            public float bayAng, bayDepth, bayFull = 20f, bayFade = 60f;
            public Island(string n, Region reg, Vector2 c, float rx, float rz, float rot, float amp, float depth, float seed)
            { name = n; region = reg; this.c = c; this.rx = rx; this.rz = rz; this.rot = rot; this.amp = amp; this.depth = depth; this.seed = seed; }
            public float Radius(float ang)
            {
                float a = ang - rot * Mathf.Deg2Rad;
                float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
                float r = rx * rz / Mathf.Sqrt(rz * rz * ca * ca + rx * rx * sa * sa);
                float n = Mathf.PerlinNoise(Mathf.Cos(ang) * 1.6f + seed, Mathf.Sin(ang) * 1.6f + seed * 0.7f) - 0.5f;
                float n2 = Mathf.PerlinNoise(Mathf.Cos(ang) * 4.5f + seed * 2.1f, Mathf.Sin(ang) * 4.5f + seed) - 0.5f;
                r *= 1f + amp * 2f * n + amp * 0.6f * n2;
                if (bayDepth > 0f)
                {
                    float dA = Mathf.Abs(Mathf.DeltaAngle(ang * Mathf.Rad2Deg, bayAng));
                    r *= 1f - bayDepth * (1f - Smooth(bayFull, bayFade, dA));
                }
                return r;
            }
            // 0 at center, 1 at the outline
            public float Norm(Vector2 p)
            {
                Vector2 d = p - c; float ang = Mathf.Atan2(d.y, d.x);
                return d.magnitude / Mathf.Max(Radius(ang), 0.01f);
            }
        }

        // ---------------- Layout (meters; +Z = North, +X = East) ----------------
        static readonly Vector2 PlazaC = new Vector2(0, 104);
        const float PlazaR = 17f;
        static readonly Vector2 ArenaC = new Vector2(-50, -140);
        const float ArenaR = 18f;
        static readonly Vector2 BridgeA = new Vector2(3, 57);   // village side
        static readonly Vector2 BridgeB = new Vector2(7, 46);   // strip side
        static readonly Vector2 MainlandC = new Vector2(0, 118); // centre of the round mainland (village sits slightly south of it, gate on the coast)

        static List<Island> islands;
        static List<Path> paths;          // walkable roads
        static List<Path> streams;        // water ribbons
        static List<Path> lavaRivers;     // lava ribbons
        static List<Vector3> lavaPools;   // x,z,radius
        static List<Vector3> clearings;   // x,z,radius (combat spaces)
        static string[] clearingNames;
        static List<Rect> fields;         // rice fields (village)
        static List<Vector3> occupied;    // x,z,radius

        static void DefineLayout()
        {
            islands = new List<Island>
            {
                new Island("Island_Village", Region.Village, new Vector2(0, 108), 64, 52, 8, 0.10f, 1.0f, 3.1f),
                new Island("Island_Strip_A", Region.Strip, new Vector2(8, 40), 11, 9, 20, 0.14f, 0.55f, 5.3f),
                new Island("Island_Strip_B", Region.Strip, new Vector2(14, 28), 12, 10, -10, 0.14f, 0.6f, 7.7f),
                new Island("Island_Strip_C", Region.Strip, new Vector2(4, 15), 14, 10, 25, 0.15f, 0.6f, 9.2f),
                new Island("Island_Strip_D", Region.Strip, new Vector2(-6, 2), 12, 10, -15, 0.15f, 0.6f, 11.4f),
                new Island("Island_Strip_E", Region.Strip, new Vector2(-2, -12), 15, 11, 10, 0.12f, 0.7f, 13.6f),
                // broad volcanic island: widened east/west and grown south (north coast stays at the connecting path)
                new Island("Island_Volcano", Region.Volcano, new Vector2(0, -110), 122, 84, -4, 0.12f, 1.25f, 17.9f),
                new Island("Island_BossArena", Region.Arena, ArenaC, 30, 28, 0, 0.08f, 1.1f, 21.3f),
                new Island("Islet_West", Region.Strip, new Vector2(-24, 30), 5, 4, 0, 0.2f, 0.4f, 23.1f),
                new Island("Islet_East", Region.Strip, new Vector2(34, 8), 4, 3.5f, 0, 0.2f, 0.35f, 25.7f),
                new Island("Islet_SouthWest", Region.Volcano, new Vector2(-140, -128), 6, 5, 0, 0.2f, 0.45f, 27.2f),
                // Mainland: one broad, roughly round island wrapped around the village in every direction.
                // A shallow bay on the south side keeps the village gate + bridge to the connecting path on the coast.
                new Island("Island_Mainland", Region.Jungle, MainlandC, 130, 118, 0, 0.09f, 1.2f, 33.3f) { bayAng = -90f, bayDepth = 0.5f, bayFull = 12f, bayFade = 45f },
            };
            jungleIslands = islands.FindAll(i => i.region == Region.Jungle);

            paths = new List<Path>
            {
                // Village (axial layout): gate -> paved avenue -> plaza -> temple forecourt. Dirt lanes to the districts.
                new Path("Path_Village_Avenue", 7f, C_Plaza, new Vector2(0, 61), new Vector2(0, 74), new Vector2(0, PlazaC.y - PlazaR + 0.5f)),
                new Path("Path_Village_GateRoad", 5f, C_Path, BridgeA, new Vector2(1.2f, 59.4f), new Vector2(0.2f, 61.1f)),
                new Path("Path_Village_Temple", 7f, C_Plaza, new Vector2(0, PlazaC.y + PlazaR - 0.5f), new Vector2(0, 126.5f)),
                new Path("Path_Village_West", 4.5f, C_Path, new Vector2(-PlazaR + 0.5f, PlazaC.y), new Vector2(-27, 103.5f), new Vector2(-35, 101), new Vector2(-39.5f, 98.5f)),
                new Path("Path_Village_East", 4.5f, C_Path, new Vector2(PlazaR - 0.5f, PlazaC.y), new Vector2(30, 104), new Vector2(42, 104.5f), new Vector2(53, 106)),
                new Path("Path_Village_Training", 3.5f, C_Path, new Vector2(3.5f, 72.2f), new Vector2(14, 72.6f), new Vector2(23, 76.5f)),
                new Path("Path_Village_Shrine", 3f, C_Path, new Vector2(-8, 127), new Vector2(-17, 133), new Vector2(-28, 137), new Vector2(-33.5f, 137.5f)),
                new Path("Path_Farm_Lane_NS", 2.4f, C_Path, new Vector2(-39.5f, 98.5f), new Vector2(-39.5f, 88.25f), new Vector2(-39.5f, 78.5f)),
                new Path("Path_Farm_Lane_EW", 2.4f, C_Path, new Vector2(-49.5f, 88.25f), new Vector2(-39.5f, 88.25f), new Vector2(-29.5f, 88.25f)),
                // Connecting path across the strip islands
                new Path("Path_Connecting", 4.5f, C_Path, BridgeB, new Vector2(10, 38), new Vector2(15, 29), new Vector2(9, 19), new Vector2(0, 12),
                         new Vector2(-8, 3), new Vector2(-6, -8), new Vector2(1, -17), new Vector2(3, -28)),
                // Volcanic critical path: Entry outpost -> Ash Field -> Lava crossing -> Obsidian Ruins -> Gate of Flames -> Boss arena
                new Path("Path_Volcano_Main", 5f, C_PathDark, new Vector2(3, -28), new Vector2(1, -40), new Vector2(-4, -55), new Vector2(-12, -68),
                         new Vector2(-18, -82), new Vector2(-22, -92), new Vector2(-26, -100), new Vector2(-29, -112), new Vector2(-34, -122), new Vector2(-39, -130)),
                // Optional branches (rewards): Crystal Hollow (north-east) and the Lava Lake Overlook (south)
                new Path("Path_Volcano_CrystalHollow", 4f, C_PathDark, new Vector2(-4, -55), new Vector2(10, -51), new Vector2(27, -51), new Vector2(43, -55), new Vector2(56, -60), new Vector2(66, -62)),
                new Path("Path_Volcano_Overlook", 3.5f, C_PathDark, new Vector2(-24, -104), new Vector2(-10, -112), new Vector2(2, -122), new Vector2(6, -136), new Vector2(8, -152)),
                // loop through the widened east / south: Crystal Hollow -> ember ruins on the east ridge -> south of the lava lake -> Overlook
                new Path("Path_Volcano_EastLoop", 3.5f, C_PathDark, new Vector2(66, -62), new Vector2(82, -76), new Vector2(94, -96), new Vector2(94, -118), new Vector2(86, -136),
                         new Vector2(70, -152), new Vector2(52, -166), new Vector2(34, -172), new Vector2(16, -164), new Vector2(8, -152)),
                // ---- Mainland roads: they leave the village in every direction and loop back ----
                // West (Whispering Marsh): west lane -> Forest Edge -> south to the Marsh Heart (south-west swamp)
                new Path("Path_Jungle_West", 4.5f, C_Path, new Vector2(-39.5f, 98.5f), new Vector2(-50, 100.2f), new Vector2(-62, 102), new Vector2(-74, 104),
                         new Vector2(-73, 92), new Vector2(-69, 80), new Vector2(-68, 70)),
                // north-west: Forest Edge -> Mirror Lake shore
                new Path("Path_Jungle_West_Lake", 3.5f, C_Path, new Vector2(-74, 104), new Vector2(-80, 116), new Vector2(-78, 128), new Vector2(-72, 138), new Vector2(-66, 146)),
                // loop: forest shrine lane -> Mirror Lake shore
                new Path("Path_Jungle_ShrineLoop", 3f, C_Path, new Vector2(-28, 137), new Vector2(-31, 147), new Vector2(-40, 154), new Vector2(-52, 152), new Vector2(-60, 149), new Vector2(-66, 146)),
                // loop: farm lane -> Marsh Heart
                new Path("Path_Jungle_FarmTrail", 3f, C_Path, new Vector2(-39.5f, 78.5f), new Vector2(-46, 74), new Vector2(-56, 71), new Vector2(-68, 70)),
                // East (Deep Jungle) -> north-east Vine Ruins -> pre-boss shrine -> Boss Stage 1 (north, behind the village)
                new Path("Path_Jungle_East", 4.5f, C_Path, new Vector2(53, 106), new Vector2(64, 107), new Vector2(74, 108.5f), new Vector2(84, 110),
                         new Vector2(92, 120), new Vector2(97, 131), new Vector2(96, 142), new Vector2(90, 154), new Vector2(79, 164), new Vector2(65, 172), BossApproach, Arena2Entry),
                // south-east: Jungle Trail -> past the east lake -> Canopy Hollow
                new Path("Path_Jungle_East_Hollow", 3.5f, C_Path, new Vector2(84, 110), new Vector2(87, 99), new Vector2(84, 86), new Vector2(82, 75), new Vector2(86, 64)),
                // loop: training ground -> east farmland -> Canopy Hollow
                new Path("Path_Jungle_FarmEast", 3f, C_Path, new Vector2(23, 76.5f), new Vector2(26, 70.5f), new Vector2(40, 70.1f), new Vector2(73, 70.1f), new Vector2(80, 67), new Vector2(86, 64)),
                // north: pre-boss junction -> spring above the village
                new Path("Path_Jungle_North_Spring", 3f, C_Path, BossApproach, new Vector2(43, 173), new Vector2(37, 167)),
            };

            streams = new List<Path>
            {
                new Path("Stream_Village", 3.6f, C_Water, new Vector2(28, 160), new Vector2(36, 146), new Vector2(43, 128), new Vector2(47, 112),
                         new Vector2(49, 100), new Vector2(57, 96.5f), new Vector2(67, 93), new Vector2(77, 90), new Vector2(85, 88.5f), new Vector2(93, 88)), // feeds the east jungle lake
            };

            // One main lava river born at the foot of Mt. Ignis, crossing the critical path once (bridge) and falling off the west cliff.
            lavaRivers = new List<Path>
            {
                new Path("Lava_River_Main", 3.6f, C_Lava, new Vector2(22, -98), new Vector2(6, -90), new Vector2(-6, -84), new Vector2(-20, -80), new Vector2(-38, -84),
                         new Vector2(-56, -90), new Vector2(-72, -95), new Vector2(-92, -98), new Vector2(-110, -100), new Vector2(-128, -102)),
                new Path("Lava_River_East", 2.6f, C_Lava, new Vector2(69, -106), new Vector2(72, -116), new Vector2(74, -125)),
                new Path("Lava_River_South", 2.8f, C_Lava, new Vector2(51, -128), new Vector2(46, -136), new Vector2(40, -143), new Vector2(35, -147)),   // feeds the lava lake
            };
            lavaPools = new List<Vector3>
            {
                new Vector3(34, -150, 11),   // Lava lake beside the overlook
                new Vector3(74, -128, 5f),   // fed by the east river
                new Vector3(-64, -70, 4),
                new Vector3(-90, -150, 5f),  // west, behind the arena
                new Vector3(16, -180, 5f),   // south coast
            };

            // Encounter spaces, difficulty rising along the route
            clearings = new List<Vector3>
            {
                new Vector3(-4, -55, 11),    // 0 Ash Field: first fight, open ground
                new Vector3(-26, -100, 13),  // 1 Obsidian Ruins: fight among pillars
                new Vector3(66, -62, 10),    // 2 Crystal Hollow: optional elite + treasure
                new Vector3(8, -152, 7.5f),  // 3 Lava Lake Overlook: optional treasure / vista
                new Vector3(4, 15, 6.5f),    // 4 Wilds Skirmish (connecting path): first small fight outside the village
                // Jungle (JungleClearStart = 5)
                new Vector3(-74, 104, 8.5f), // 5 Forest Edge: first jungle fight (west)
                new Vector3(-68, 70, 10f),   // 6 Marsh Heart: fight among swamp pools (south-west)
                new Vector3(-66, 146, 6.5f), // 7 Mirror Lake shore: optional elite + treasure (north-west)
                new Vector3(84, 110, 9f),    // 8 Jungle Trail: first jungle fight (east)
                new Vector3(96, 142, 11f),   // 9 Vine Ruins: fight among overgrown ruins (north-east)
                new Vector3(86, 64, 7.5f),   // 10 Canopy Hollow: optional elite + treasure (south-east)
                new Vector3(Arena2C.x, Arena2C.y, Arena2R), // 11 Boss Stage 1 arena
            };
            clearingNames = new[] { "AshField", "ObsidianRuins", "CrystalHollow_Elite", "LavaLakeOverlook", "WildsSkirmish",
                                    "Jungle_ForestEdge", "Jungle_MarshHeart", "Jungle_MirrorLake_Elite", "Jungle_Trail", "Jungle_VineRuins", "Jungle_CanopyHollow_Elite", "BossStage1_HeartOfTheGrove" };

            // 2x2 rice paddies, separated by the farm lanes (x=-39.5, z=88.25)
            fields = new List<Rect>
            {
                new Rect(-48.5f, 89.75f, 7.5f, 7f), new Rect(-38f, 89.75f, 7.5f, 7f),
                new Rect(-48.5f, 79.5f, 7.5f, 7f), new Rect(-38f, 79.5f, 7.5f, 7f),
                // east farmland on the south-east lobe, split by the farm lane (z = 70.1)
                new Rect(44f, 71.6f, 7.5f, 7f), new Rect(54.25f, 71.6f, 7.5f, 7f), new Rect(64.5f, 71.6f, 7.5f, 7f),
                new Rect(44f, 61.6f, 7.5f, 7f), new Rect(54.25f, 61.6f, 7.5f, 7f), new Rect(64.5f, 61.6f, 7.5f, 7f),
            };

            DefineVillageBuildings();
            occupied = new List<Vector3>();
        }

        // ---------------- Village plan: every building has a gameplay purpose ----------------
        public enum BType { Temple, Elder, Inn, Shop, Blacksmith, Residence, Farmhouse, Shrine, Watchtower, Well, TrainingGround }
        public class BDef
        {
            public string name; public BType type; public Vector2 pos; public float yaw, w, d, h; public bool twoTier; public float padR;
            public BDef(string n, BType t, Vector2 p, float yaw, float w, float d, float h, bool two)
            { name = n; type = t; pos = p; this.yaw = yaw; this.w = w; this.d = d; this.h = h; twoTier = two; padR = 0.5f * Mathf.Sqrt(w * w + d * d) + 1.2f; }
        }
        static List<BDef> buildings;
        // paved avenue + temple forecourt (nothing may be scattered on them)
        static readonly Rect[] PavedAreas = { new Rect(-3.9f, 60.5f, 7.8f, 28f), new Rect(-9.6f, 117f, 19.2f, 11.6f) };
        static float FaceYaw(Vector2 from, Vector2 to) { Vector2 d = to - from; return Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg; }

        static void DefineVillageBuildings()
        {
            Vector2 elder = new Vector2(-25, 125), inn = new Vector2(25, 125);
            buildings = new List<BDef>
            {
                new BDef("Temple_Main (Save Point / Story)", BType.Temple, new Vector2(0, 138.5f), 180, 21, 16, 4.8f, true),
                new BDef("House_VillageElder (Quest Giver)", BType.Elder, elder, FaceYaw(elder, PlazaC), 12, 9, 4.3f, true),
                new BDef("House_Inn (Rest / Heal)", BType.Inn, inn, FaceYaw(inn, PlazaC), 12, 9, 4.3f, true),
                new BDef("House_GeneralStore (Shop)", BType.Shop, new Vector2(26, 96.5f), 0, 10, 7.5f, 4.0f, false),
                new BDef("House_Blacksmith (Upgrade)", BType.Blacksmith, new Vector2(-25, 96f), 0, 9, 7f, 3.9f, false),
                new BDef("House_Residence_West", BType.Residence, new Vector2(-12, 79), 90, 9, 7f, 3.8f, false),
                new BDef("House_Residence_East", BType.Residence, new Vector2(12, 79), -90, 9, 7f, 3.8f, false),
                new BDef("House_Farmhouse", BType.Farmhouse, new Vector2(-45, 110), 180, 9, 7f, 3.7f, false),
                new BDef("Shrine_Forest", BType.Shrine, new Vector2(-38.5f, 140), FaceYaw(new Vector2(-38.5f, 140), new Vector2(-28, 137)), 4.5f, 4f, 2.8f, false),
                new BDef("Well_Village", BType.Well, new Vector2(-20, 84), 0, 2.6f, 2.6f, 1f, false),
                new BDef("Watchtower_GateWest", BType.Watchtower, new Vector2(-9.5f, 67.6f), 0, 4.6f, 4.6f, 7f, false),
                new BDef("Watchtower_GateEast", BType.Watchtower, new Vector2(9.5f, 67.6f), 0, 4.6f, 4.6f, 7f, false),
                new BDef("TrainingGround (Tutorial Combat)", BType.TrainingGround, new Vector2(30, 80), 0, 14f, 14f, 0f, false),
            };
            foreach (var b in buildings) if (b.type == BType.TrainingGround) b.padR = 7.5f;
        }

        // ---------------- Height field ----------------
        static float HLow(float x, float z)
        {
            float v = 0.18f * Fbm(x, z, 0.02f, 1.3f);
            float vol = 0.6f * Fbm(x, z, 0.018f, 5.1f);
            return Mathf.Lerp(vol, v, Smooth(-34f, -16f, z));
        }

        static Island VolcanoIsland { get { return islands[6]; } }

        static float H(float x, float z)
        {
            Vector2 p = new Vector2(x, z);
            float village = 0.35f * Fbm(x, z, 0.05f, 3.3f);
            float strip = 0.45f * Fbm(x, z, 0.07f, 7.1f);
            float volcano = 1.3f * Fbm(x, z, 0.045f, 11.7f) + 0.6f;
            // rugged ridges toward the volcanic island edge
            float e = VolcanoIsland.Norm(p);
            volcano += 4.5f * Smooth(0.72f, 0.97f, e) * Perlin(x, z, 0.06f, 4.4f);
            if (z < -24f) volcano += VolcanoFoothills(p);
            float h = z > 56 ? village : Mathf.Lerp(volcano, strip, Smooth(-34f, -16f, z));
            float low = HLow(x, z);
            h += JungleRelief(p);
            h = SwampBlend(p, h);

            float flat = 0f; // 0..1 how much to flatten toward HLow
            foreach (var pa in paths) { Vector2 c; float d = DistToPolyline(p, pa.pts, out c); flat = Mathf.Max(flat, 1f - Smooth(pa.width * 0.5f + 0.5f, pa.width * 0.5f + 3.5f, d)); }
            foreach (var pa in lavaRivers) { Vector2 c; float d = DistToPolyline(p, pa.pts, out c); flat = Mathf.Max(flat, 1f - Smooth(pa.width * 0.5f + 0.8f, pa.width * 0.5f + 3f, d)); }
            foreach (var pa in streams) { Vector2 c; float d = DistToPolyline(p, pa.pts, out c); flat = Mathf.Max(flat, 1f - Smooth(pa.width * 0.5f + 0.6f, pa.width * 0.5f + 2.5f, d)); }
            foreach (var cl in clearings) flat = Mathf.Max(flat, 1f - Smooth(cl.z, cl.z + 4f, Vector2.Distance(p, new Vector2(cl.x, cl.y))));
            foreach (var lp in lavaPools) flat = Mathf.Max(flat, 1f - Smooth(lp.z + 0.8f, lp.z + 3.5f, Vector2.Distance(p, new Vector2(lp.x, lp.y))));
            foreach (var f in fields) { float d = RectDist(p, f); flat = Mathf.Max(flat, 1f - Smooth(0.3f, 2.5f, d)); }
            foreach (var b in buildings) flat = Mathf.Max(flat, 1f - Smooth(b.padR, b.padR + 3f, Vector2.Distance(p, b.pos)));
            h = Mathf.Lerp(h, low, flat);

            // plaza and arena platforms
            float dp = Vector2.Distance(p, PlazaC);
            h = Mathf.Lerp(0.12f, h, Smooth(PlazaR + 1.5f, PlazaR + 5f, dp));
            float da = Vector2.Distance(p, ArenaC);
            h = Mathf.Lerp(0.4f, h, Smooth(ArenaR + 7f, ArenaR + 12f, da));
            return JungleFinal(p, h);
        }
        static float RectDist(Vector2 p, Rect r)
        {
            float dx = Mathf.Max(r.xMin - p.x, 0, p.x - r.xMax), dz = Mathf.Max(r.yMin - p.y, 0, p.y - r.yMax);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
        static Vector3 Ground(Vector2 p) { return new Vector3(p.x, H(p.x, p.y), p.y); }

        // Is p (with radius r) free of roads, water, lava, fields, plaza and other objects?
        static bool Free(Vector2 p, float r, float roadMargin = 1.0f)
        {
            if (!OnLand(p, r)) return false;
            foreach (var pa in paths) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + r + roadMargin) return false; }
            foreach (var pa in streams) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + r + 0.8f) return false; }
            foreach (var pa in lavaRivers) { Vector2 c; if (DistToPolyline(p, pa.pts, out c) < pa.width * 0.5f + r + 1.0f) return false; }
            foreach (var lp in lavaPools) if (Vector2.Distance(p, new Vector2(lp.x, lp.y)) < lp.z + r + 1.5f) return false;
            foreach (var f in fields) if (RectDist(p, f) < r + 1.0f) return false;
            if (Vector2.Distance(p, PlazaC) < PlazaR + r + 1.6f) return false;
            foreach (var pr in PavedAreas) if (RectDist(p, pr) < r + 0.5f) return false;
            foreach (var b in buildings) if (Vector2.Distance(p, b.pos) < b.padR + r) return false;
            if (Vector2.Distance(p, ArenaC) < ArenaR + 7f + r) return false;
            if (Vector2.Distance(p, (BridgeA + BridgeB) * 0.5f) < 9f + r) return false;
            foreach (var lk in jungleLakes) if (Vector2.Distance(p, new Vector2(lk.x, lk.y)) < lk.z * 1.3f + r) return false;
            if (Vector2.Distance(p, Arena2C) < Arena2R + 3f + r) return false;
            foreach (var o in occupied) if (Vector2.Distance(p, new Vector2(o.x, o.y)) < o.z + r) return false;
            return true;
        }
        static bool InClearing(Vector2 p, float r) { foreach (var cl in clearings) if (Vector2.Distance(p, new Vector2(cl.x, cl.y)) < cl.z + r) return true; return false; }
        static bool OnLand(Vector2 p, float r)
        {
            foreach (var isl in islands)
            {
                Vector2 d = p - isl.c; float rad = isl.Radius(Mathf.Atan2(d.y, d.x));
                if (d.magnitude + r < rad * 0.94f) return true;
            }
            return false;
        }
        static Island IslandAt(Vector2 p)
        {
            Island best = null; float bn = 99f;
            foreach (var isl in islands) { float n = isl.Norm(p); if (n < bn) { bn = n; best = isl; } }
            return best;
        }
        static void Occupy(Vector2 p, float r) { occupied.Add(new Vector3(p.x, p.y, r)); }

        // ---------------- Island mesh ----------------
        static GameObject BuildIsland(Island isl, Transform parent)
        {
            float rmax = Mathf.Max(isl.rx, isl.rz) * (1f + isl.amp);
            int K = Mathf.Max(4, Mathf.CeilToInt(rmax / 2.3f));
            int S = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * rmax / 2.4f), 24, 240);
            var top = new Vector3[K + 1, S];
            for (int s = 0; s < S; s++)
            {
                float ang = s * Mathf.PI * 2f / S; float R = isl.Radius(ang);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                for (int k = 0; k <= K; k++)
                {
                    float f = k / (float)K;
                    Vector2 p = isl.c + dir * R * f;
                    float y = H(p.x, p.y) - OverlapSink(isl, p);
                    if (k == K) y -= 0.15f;
                    top[k, s] = new Vector3(p.x, y, p.y);
                }
            }
            var mb = new MB();
            Vector3 center = new Vector3(isl.c.x, H(isl.c.x, isl.c.y) - OverlapSink(isl, isl.c), isl.c.y);
            for (int s = 0; s < S; s++)
            {
                int s1 = (s + 1) % S;
                // center fan
                AddTopTri(mb, isl, center, top[1, s1], top[1, s]);
                for (int k = 1; k < K; k++)
                {
                    AddTopTri(mb, isl, top[k, s], top[k, s1], top[k + 1, s1]);
                    AddTopTri(mb, isl, top[k, s], top[k + 1, s1], top[k + 1, s]);
                }
            }
            // Cliff skirt: rings (radius factor, depth) going down to a rocky tip
            float[] rf = { 1.0f, 0.99f, 0.95f, 0.86f, 0.70f, 0.48f, 0.25f };
            float[] dy = { 0f, -1.0f, -5.5f, -11f, -16.5f, -21.5f, -25.5f };
            int L = rf.Length;
            var ring = new Vector3[L, S];
            for (int s = 0; s < S; s++)
            {
                float ang = s * Mathf.PI * 2f / S; float R = isl.Radius(ang);
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                for (int l = 0; l < L; l++)
                {
                    if (l == 0) { ring[l, s] = top[K, s]; continue; }
                    float jag = l >= 2 ? (Perlin(ang * 9f, l * 3.1f, 1f, isl.seed) - 0.5f) * 0.16f : 0f;
                    float dyj = l >= 2 ? (Perlin(ang * 7f, l * 1.7f, 1f, isl.seed + 3f) - 0.5f) * 3.5f : 0f;
                    Vector2 p = isl.c + dir * R * (rf[l] + jag);
                    ring[l, s] = new Vector3(p.x, top[K, s].y + (dy[l] + dyj) * isl.depth, p.y);
                }
            }
            Vector3 tip = new Vector3(isl.c.x, center.y - 30f * isl.depth, isl.c.y);
            bool volcanic = isl.region == Region.Volcano || isl.region == Region.Arena;
            for (int s = 0; s < S; s++)
            {
                int s1 = (s + 1) % S;
                for (int l = 0; l < L - 1; l++)
                {
                    Cell cell = l == 0 ? (volcanic ? C_Volcanic : C_Grass) : (volcanic ? C_CliffDark : C_Cliff);
                    float tA = l == 0 ? 0.6f : (l / (float)(L - 1)), tB = (l + 1) / (float)(L - 1);
                    if (l == 0) tB = 0.65f;
                    mb.Quad(ring[l, s], ring[l, s1], ring[l + 1, s1], ring[l + 1, s], UV(cell, tA), UV(cell, tA), UV(cell, tB), UV(cell, tB));
                }
                mb.Tri(ring[L - 1, s], ring[L - 1, s1], tip, UV(C_CliffDark, 0.95f));
            }
            var go = MeshGO(isl.name, mb.ToMesh(isl.name), matEnv, parent, true);
            return go;
        }

        // 0 near the village, 1 at the volcano (drives ground colour and vegetation on the connecting path)
        static float Dryness(float z) { return Smooth(34f, -20f, z); }

        // Where two islands overlap they share the same height field -> coplanar surfaces z-fight.
        // The lower-priority island is pushed under the other one inside the overlap.
        static int Priority(Island isl)
        {
            int p = isl.region == Region.Arena ? 400 : (isl.region == Region.Volcano || isl.region == Region.Village) ? 300 : 200;
            if (isl.region == Region.Jungle) p = isl.name.Contains("Boss") ? 260 : 250;
            if (isl.name.StartsWith("Islet")) p = 100;
            return p - islands.IndexOf(isl);
        }
        static float OverlapSink(Island isl, Vector2 p)
        {
            float sink = 0f; int me = Priority(isl);
            foreach (var other in islands)
            {
                if (other == isl || Priority(other) < me) continue;
                float n = other.Norm(p);
                sink = Mathf.Max(sink, (1f - Smooth(0.86f, 0.99f, n)) * 0.6f);
            }
            return sink;
        }

        static void AddTopTri(MB mb, Island isl, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 m = (a + b + c) / 3f; Vector2 p = new Vector2(m.x, m.z);
            Cell cell; float t = Perlin(p.x, p.y, 0.19f, isl.seed);
            float patch = Perlin(p.x, p.y, 0.035f, isl.seed + 40f);
            switch (isl.region)
            {
                case Region.Village:
                    {
                        float dp = Vector2.Distance(p, PlazaC);
                        if (dp < PlazaR) cell = ((int)(dp / 3.2f) % 2 == 0) ? C_Plaza : C_PlazaDark;
                        else cell = patch > 0.63f ? C_GrassDark : C_Grass;
                        break;
                    }
                case Region.Strip:
                    {
                        // biome gradient village -> volcano: lush grass, then dry grass, then ash
                        float v = Dryness(p.y) + (patch - 0.5f) * 0.6f;
                        if (v < 0.38f) cell = patch > 0.6f ? C_GrassDark : C_Grass;
                        else if (v < 0.72f) cell = patch > 0.55f ? C_Field : C_GrassDark;
                        else cell = patch > 0.5f ? C_Ash : C_Volcanic;
                        break;
                    }
                case Region.Jungle:
                    cell = JungleGroundCell(p, a, b, c, patch, t);
                    break;
                default:
                    {
                        float e = isl.Norm(p);
                        float rim = Perlin(p.x, p.y, 0.08f, isl.seed + 9f);
                        // one volcanic biome: dark ash ground with scorched brown patches, darker toward the rim
                        if (e > 0.88f && rim > 0.45f) cell = C_Crust;
                        else cell = patch > 0.6f ? C_Ash : C_Volcanic;
                        break;
                    }
            }
            mb.Tri(a, b, c, UV(cell, t));
        }

        // ---------------- Ribbons ----------------
        static GameObject BuildRibbon(Path pa, Transform parent, float yOffset, Material mat, bool collider, float edgeNoise = 0.12f)
        {
            var mb = new MB();
            var pts = pa.pts;
            Vector3 prevL = Vector3.zero, prevR = Vector3.zero; Vector2 uvPrev = Vector2.zero;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 fwd = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]).normalized;
                if (i > 0 && i < pts.Count - 1) fwd = (pts[i + 1] - pts[i - 1]).normalized;
                Vector2 n = new Vector2(-fwd.y, fwd.x);
                float taper = Mathf.Min(1f, Mathf.Min(i, pts.Count - 1 - i) / 2.5f) * 0.35f + 0.65f;
                float wl = pa.width * 0.5f * taper * (1f + edgeNoise * 2f * (Perlin(pts[i].x, pts[i].y, 0.4f, 2f) - 0.5f));
                float wr = pa.width * 0.5f * taper * (1f + edgeNoise * 2f * (Perlin(pts[i].x, pts[i].y, 0.4f, 8f) - 0.5f));
                Vector2 l = pts[i] + n * wl, r = pts[i] - n * wr;
                Vector3 L3 = new Vector3(l.x, H(l.x, l.y) + yOffset, l.y), R3 = new Vector3(r.x, H(r.x, r.y) + yOffset, r.y);
                Vector2 u = UV(pa.cell, Perlin(pts[i].x, pts[i].y, 0.3f, 5f));
                if (i > 0) mb.Quad(prevL, L3, R3, prevR, uvPrev, u, u, uvPrev);
                prevL = L3; prevR = R3; uvPrev = u;
            }
            return MeshGO(pa.name, mb.ToMesh(pa.name), mat, parent, collider);
        }

        static GameObject BuildDisc(string name, Vector2 c, float r, float y, Cell cell, Material mat, Transform parent, float seed, int seg = 28)
        {
            var mb = new MB();
            Vector3 cc = new Vector3(c.x, y, c.y);
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                float r0 = r * (0.85f + 0.3f * Mathf.PerlinNoise(Mathf.Cos(a0) * 1.5f + seed, Mathf.Sin(a0) * 1.5f + seed));
                float r1 = r * (0.85f + 0.3f * Mathf.PerlinNoise(Mathf.Cos(a1) * 1.5f + seed, Mathf.Sin(a1) * 1.5f + seed));
                Vector3 p0 = cc + new Vector3(Mathf.Cos(a0) * r0, 0, Mathf.Sin(a0) * r0), p1 = cc + new Vector3(Mathf.Cos(a1) * r1, 0, Mathf.Sin(a1) * r1);
                mb.Tri(cc, p1, p0, UV(cell, 0.5f), UV(cell, 0.2f), UV(cell, 0.2f));
            }
            return MeshGO(name, mb.ToMesh(name), mat, parent, false);
        }
    }
}
