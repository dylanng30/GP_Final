using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Level Blockout Builder – Three Island World
/// Menu: Tools > Blockout > Build Level
/// Populates the Blockout.unity scene with a full 3-area level blockout.
/// Scale: ~1000u wide total (Area 03 is widest at 1000u).
/// </summary>
public class BlockoutBuilder : EditorWindow
{
    // ─────────────────────────────────────────────────────────────
    //  CONSTANTS – World positions for each island center
    // ─────────────────────────────────────────────────────────────

    // Area 01 – Green Village  (Bắc / North)
    static readonly Vector3 A01_CENTER = new Vector3(0, 0, 1800f);
    const float A01_W = 800f;
    const float A01_D = 700f;
    const float A01_Y = 60f;   // island surface Y

    // Area 02 – Ancient Forest  (Giữa / Middle)
    static readonly Vector3 A02_CENTER = new Vector3(0, 0, 300f);
    const float A02_W = 450f;
    const float A02_D = 300f;
    const float A02_Y = 40f;

    // Area 03 – Volcanic Wasteland  (Nam / South)
    static readonly Vector3 A03_CENTER = new Vector3(0, 0, -1100f);
    const float A03_W = 1000f;
    const float A03_D = 850f;
    const float A03_Y = 0f;

    // Shared sizes
    const float ROAD_W   = 35f;
    const float HOUSE_W  = 40f;
    const float HOUSE_H  = 40f;
    const float HOUSE_D  = 40f;
    const float CLIFF_H  = 45f;
    const float CLIFF_T  = 18f;   // thickness

    // ─────────────────────────────────────────────────────────────
    //  MATERIALS dictionary  (created once, reused everywhere)
    // ─────────────────────────────────────────────────────────────
    static readonly (string name, Color color)[] MAT_DEFS = new[]
    {
        ("BLK_GrassGround",      new Color(0.298f, 0.686f, 0.314f)),   // #4CAF50
        ("BLK_ForestGround",     new Color(0.180f, 0.490f, 0.196f)),   // #2E7D32
        ("BLK_VolcanicGround",   new Color(0.329f, 0.431f, 0.478f)),   // #546E7A
        ("BLK_Building",         new Color(0.620f, 0.620f, 0.620f)),   // #9E9E9E
        ("BLK_Stone",            new Color(0.471f, 0.565f, 0.612f)),   // #78909C
        ("BLK_Water",            new Color(0.086f, 0.396f, 0.753f)),   // #1565C0
        ("BLK_Lava",             new Color(0.902f, 0.290f, 0.098f)),   // #E64A19
        ("BLK_Path",             new Color(0.843f, 0.800f, 0.784f)),   // #D7CCC8
        ("BLK_Vegetation",       new Color(0.220f, 0.557f, 0.235f)),   // #388E3C
        ("BLK_DarkVegetation",   new Color(0.106f, 0.369f, 0.125f)),   // #1B5E20
        ("BLK_Farmland",         new Color(0.545f, 0.765f, 0.290f)),   // #8BC34A
        ("BLK_LavaRock",         new Color(0.216f, 0.278f, 0.310f)),   // #37474F
        ("BLK_Cliff",            new Color(0.310f, 0.278f, 0.247f)),   // #4F4740
    };

    static System.Collections.Generic.Dictionary<string, Material> _mats;

    // ─────────────────────────────────────────────────────────────
    //  MENU ENTRY
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Blockout/Build Level %#b")]
    public static void BuildLevel()
    {
        if (!EditorUtility.DisplayDialog("Build Level Blockout",
            "This will CLEAR and rebuild all objects inside 'Level_Blockout' in the current scene.\n\nContinue?",
            "Yes, Build", "Cancel"))
            return;

        EnsureMaterialsExist();
        ClearExistingBlockout();
        Build();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[BlockoutBuilder] ✅ Level blockout built successfully!");
    }

    [MenuItem("Tools/Blockout/Clear Level")]
    public static void ClearLevel()
    {
        ClearExistingBlockout();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[BlockoutBuilder] Level_Blockout cleared.");
    }

    // ─────────────────────────────────────────────────────────────
    //  MATERIALS
    // ─────────────────────────────────────────────────────────────
    static void EnsureMaterialsExist()
    {
        string fullMatDir = Path.Combine(Application.dataPath, "_Project/02_Art/Materials/Blockout");
        if (!Directory.Exists(fullMatDir))
        {
            Directory.CreateDirectory(fullMatDir);
            AssetDatabase.Refresh();
        }

        string matDir = "Assets/_Project/02_Art/Materials/Blockout";
        _mats = new System.Collections.Generic.Dictionary<string, Material>();

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        foreach (var (name, color) in MAT_DEFS)
        {
            string path = $"{matDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                SetMatColor(mat, color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                SetMatColor(mat, color);
                EditorUtility.SetDirty(mat);
            }
            _mats[name] = mat;
        }
        AssetDatabase.SaveAssets();
    }

    static void SetMatColor(Material mat, Color color)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        mat.color = color;
    }

    static Material M(string name) => _mats != null && _mats.ContainsKey(name) ? _mats[name] : null;

    // ─────────────────────────────────────────────────────────────
    //  CLEAR
    // ─────────────────────────────────────────────────────────────
    static void ClearExistingBlockout()
    {
        var existing = GameObject.Find("Level_Blockout");
        if (existing != null)
            Object.DestroyImmediate(existing);
    }

    // ─────────────────────────────────────────────────────────────
    //  MAIN BUILD
    // ─────────────────────────────────────────────────────────────
    static void Build()
    {
        // Root
        var root       = new GameObject("Level_Blockout");
        var envRoot    = Child(root, "Environment");
        var navRoot    = Child(root, "Navigation");
        var landmarks  = Child(root, "Landmarks");
        var lighting   = Child(root, "Lighting");
        var cameras    = Child(root, "Cameras");

        // ── AREAS ──
        BuildArea01(envRoot, navRoot, landmarks);
        BuildArea02(envRoot, navRoot);
        BuildArea03(envRoot, navRoot, landmarks);

        // ── INTER-AREA CONNECTION ──
        BuildConnectionBridge(navRoot);

        // ── CAMERAS ──
        BuildCameras(cameras);

        // ── LIGHTING ──
        SetupLighting(lighting);
    }

    // ─────────────────────────────────────────────────────────────
    //  AREA 01 – GREEN VILLAGE
    // ─────────────────────────────────────────────────────────────
    static void BuildArea01(GameObject env, GameObject nav, GameObject lmk)
    {
        var a01 = Child(env, "Area_01_GreenVillage");
        var terr = Child(a01, "Terrain");
        var bldg = Child(a01, "Buildings");
        var veg  = Child(a01, "Vegetation");
        var watr = Child(a01, "Water");
        var prop = Child(a01, "Props");

        Vector3 c = A01_CENTER;
        float sy = A01_Y;

        // ── Island Base (flat slab representing the island surface)
        CreateBox(terr, "Island_Surface",
            new Vector3(c.x, sy - 5f, c.z),
            new Vector3(A01_W, 10f, A01_D),
            M("BLK_GrassGround"));

        // ── Cliff edges (4 sides + chamfer effect via 4 thin slabs)
        BuildIslandCliffs(terr, c, A01_W, A01_D, sy, "A01", M("BLK_Cliff"));

        // ── Farmland (NW corner) – 3 terrace levels
        for (int i = 0; i < 3; i++)
        {
            CreateBox(terr, $"Farmland_Terrace_{i}",
                new Vector3(c.x - 280f, sy + (i * 12f), c.z + 200f - i * 30f),
                new Vector3(180f - i * 20f, 12f, 120f - i * 15f),
                M("BLK_Farmland"));
        }

        // ── River channel (runs NE to S)
        CreateBox(watr, "River_Channel",
            new Vector3(c.x + 180f, sy + 2f, c.z + 50f),
            new Vector3(30f, 5f, 400f),
            M("BLK_Water"));

        // ── Waterfall (NE cliff)
        CreateBox(watr, "Waterfall_Drop",
            new Vector3(c.x + 200f, sy - 30f, c.z + 280f),
            new Vector3(30f, 60f, 8f),
            M("BLK_Water"));

        // ── Defensive Wall (south side, wraps the village perimeter)
        // South wall
        CreateBox(prop, "Wall_South",
            new Vector3(c.x, sy + 15f, c.z - 310f),
            new Vector3(A01_W * 0.75f, 30f, 12f),
            M("BLK_Stone"));
        // West wall segment
        CreateBox(prop, "Wall_West",
            new Vector3(c.x - 280f, sy + 15f, c.z - 150f),
            new Vector3(12f, 30f, 280f),
            M("BLK_Stone"));
        // East wall segment
        CreateBox(prop, "Wall_East",
            new Vector3(c.x + 250f, sy + 15f, c.z - 150f),
            new Vector3(12f, 30f, 250f),
            M("BLK_Stone"));

        // ── Gate (south wall center)
        CreateBox(prop, "Gate_LeftPillar",
            new Vector3(c.x - 38f, sy + 20f, c.z - 310f),
            new Vector3(18f, 40f, 20f),
            M("BLK_Stone"));
        CreateBox(prop, "Gate_RightPillar",
            new Vector3(c.x + 38f, sy + 20f, c.z - 310f),
            new Vector3(18f, 40f, 20f),
            M("BLK_Stone"));
        CreateBox(prop, "Gate_Arch",
            new Vector3(c.x, sy + 40f, c.z - 310f),
            new Vector3(94f, 12f, 22f),
            M("BLK_Stone"));

        // ── Watchtowers (2x)
        BuildWatchtower(prop, "Watchtower_SW", new Vector3(c.x - 290f, sy, c.z - 305f));
        BuildWatchtower(prop, "Watchtower_SE", new Vector3(c.x + 265f, sy, c.z - 305f));

        // ── Central Plaza
        CreateBox(terr, "Central_Plaza",
            new Vector3(c.x + 30f, sy + 1f, c.z + 50f),
            new Vector3(220f, 4f, 200f),
            M("BLK_Path"));

        // ── Central Temple (landmark)
        BuildTemple(lmk, "Central_Temple", new Vector3(c.x + 30f, sy, c.z + 80f));

        // ── Residential Houses (12 houses around plaza)
        Vector3[] housePositions = new Vector3[]
        {
            new Vector3(c.x - 160f, sy, c.z + 120f),
            new Vector3(c.x - 200f, sy, c.z + 20f),
            new Vector3(c.x - 160f, sy, c.z - 80f),
            new Vector3(c.x - 80f,  sy, c.z - 150f),
            new Vector3(c.x + 20f,  sy, c.z - 180f),
            new Vector3(c.x + 130f, sy, c.z - 130f),
            new Vector3(c.x + 210f, sy, c.z - 40f),
            new Vector3(c.x + 220f, sy, c.z + 80f),
            new Vector3(c.x + 160f, sy, c.z + 180f),
            new Vector3(c.x + 50f,  sy, c.z + 220f),
            new Vector3(c.x - 80f,  sy, c.z + 230f),
            new Vector3(c.x - 180f, sy, c.z + 190f),
        };
        for (int i = 0; i < housePositions.Length; i++)
            BuildHouse(bldg, $"House_{i + 1:00}", housePositions[i]);

        // ── Vegetation – tree clusters
        Vector3[] treeClusterPos = new Vector3[]
        {
            new Vector3(c.x - 300f, sy, c.z + 280f),
            new Vector3(c.x + 300f, sy, c.z + 280f),
            new Vector3(c.x - 330f, sy, c.z + 0f),
            new Vector3(c.x + 330f, sy, c.z + 100f),
            new Vector3(c.x + 20f,  sy, c.z + 310f),
        };
        foreach (var tp in treeClusterPos)
            BuildTreeCluster(veg, tp, 3, 35f, 80f, M("BLK_Vegetation"));

        // ── Road network (Area 01 internal)
        var mainPath01 = Child(nav, "Area01_Paths");
        // Main road: Gate → Plaza
        BuildRoadSegment(mainPath01, "A01_Road_Main",
            new Vector3(c.x, sy + 2f, c.z - 250f),
            new Vector3(ROAD_W, 2f, 360f));
        // Branch West → Farmland
        BuildRoadSegment(mainPath01, "A01_Road_West",
            new Vector3(c.x - 150f, sy + 2f, c.z + 100f),
            new Vector3(250f, 2f, ROAD_W));
        // Branch East → Waterfall
        BuildRoadSegment(mainPath01, "A01_Road_East",
            new Vector3(c.x + 150f, sy + 2f, c.z + 150f),
            new Vector3(100f, 2f, ROAD_W));
    }

    // ─────────────────────────────────────────────────────────────
    //  AREA 02 – ANCIENT FOREST
    // ─────────────────────────────────────────────────────────────
    static void BuildArea02(GameObject env, GameObject nav)
    {
        var a02 = Child(env, "Area_02_AncientForest");
        var terr = Child(a02, "Terrain");
        var ruin = Child(a02, "Ruins");
        var veg  = Child(a02, "Vegetation");
        var prop = Child(a02, "Props");

        Vector3 c = A02_CENTER;
        float sy = A02_Y;

        // Island base
        CreateBox(terr, "Island_Surface",
            new Vector3(c.x, sy - 5f, c.z),
            new Vector3(A02_W, 10f, A02_D),
            M("BLK_ForestGround"));
        BuildIslandCliffs(terr, c, A02_W, A02_D, sy, "A02", M("BLK_Cliff"));

        // Elevated terrain sections (S-curve path with height variation)
        CreateBox(terr, "Elevated_N",
            new Vector3(c.x - 60f, sy + 18f, c.z + 80f),
            new Vector3(200f, 18f, 100f),
            M("BLK_ForestGround"));
        CreateBox(terr, "Elevated_S",
            new Vector3(c.x + 60f, sy + 12f, c.z - 70f),
            new Vector3(180f, 12f, 100f),
            M("BLK_ForestGround"));

        // Stone Gates (3 arches along the path)
        BuildStoneGate(prop, "Gate_A02_N", new Vector3(c.x - 40f, sy + 20f, c.z + 100f), 0f);
        BuildStoneGate(prop, "Gate_A02_M", new Vector3(c.x,       sy + 8f,  c.z + 0f),   15f);
        BuildStoneGate(prop, "Gate_A02_S", new Vector3(c.x + 50f, sy + 12f, c.z - 100f), -10f);

        // Ruins clusters
        BuildRuinCluster(ruin, "Ruin_01", new Vector3(c.x - 150f, sy, c.z + 60f));
        BuildRuinCluster(ruin, "Ruin_02", new Vector3(c.x + 120f, sy, c.z - 50f));

        // Forest clusters – darker green
        Vector3[] treePosA02 = new Vector3[]
        {
            new Vector3(c.x - 160f, sy, c.z + 120f),
            new Vector3(c.x + 180f, sy, c.z + 50f),
            new Vector3(c.x - 140f, sy, c.z - 100f),
            new Vector3(c.x + 150f, sy, c.z - 120f),
        };
        foreach (var tp in treePosA02)
            BuildTreeCluster(veg, tp, 4, 30f, 90f, M("BLK_DarkVegetation"));

        // Rock formations
        BuildRockFormation(prop, "Rocks_A02_01", new Vector3(c.x + 80f,  sy, c.z + 70f));
        BuildRockFormation(prop, "Rocks_A02_02", new Vector3(c.x - 100f, sy, c.z - 60f));

        // Path – winding S-curve approximated with 3 plane segments
        var pathA02 = Child(nav, "Area02_Paths");
        BuildRoadSegment(pathA02, "A02_Path_N",
            new Vector3(c.x - 25f, sy + 3f, c.z + 110f),
            new Vector3(ROAD_W, 3f, 130f));
        BuildRoadSegment(pathA02, "A02_Path_M",
            new Vector3(c.x + 10f, sy + 3f, c.z + 0f),
            new Vector3(ROAD_W, 3f, 120f));
        BuildRoadSegment(pathA02, "A02_Path_S",
            new Vector3(c.x + 30f, sy + 3f, c.z - 105f),
            new Vector3(ROAD_W, 3f, 100f));
    }

    // ─────────────────────────────────────────────────────────────
    //  AREA 03 – VOLCANIC WASTELAND
    // ─────────────────────────────────────────────────────────────
    static void BuildArea03(GameObject env, GameObject nav, GameObject lmk)
    {
        var a03 = Child(env, "Area_03_VolcanicWasteland");
        var terr = Child(a03, "Terrain");
        var mtn  = Child(a03, "Mountains");
        var lava = Child(a03, "Lava");
        var ruin = Child(a03, "Ruins");
        var prop = Child(a03, "Props");

        Vector3 c = A03_CENTER;
        float sy = A03_Y;

        // Island base
        CreateBox(terr, "Island_Surface",
            new Vector3(c.x, sy - 5f, c.z),
            new Vector3(A03_W, 10f, A03_D),
            M("BLK_VolcanicGround"));
        BuildIslandCliffs(terr, c, A03_W, A03_D, sy, "A03", M("BLK_Cliff"));

        // ── Mountains / Rock formations (15 mountains, varied sizes)
        (Vector3 pos, Vector3 size)[] mountains = new[]
        {
            (new Vector3(c.x - 350f, sy, c.z + 300f), new Vector3(120f, 120f, 120f)),
            (new Vector3(c.x + 350f, sy, c.z + 280f), new Vector3(150f, 140f, 150f)),
            (new Vector3(c.x - 280f, sy, c.z + 100f), new Vector3(100f,  90f, 100f)),
            (new Vector3(c.x + 300f, sy, c.z + 80f),  new Vector3(130f, 110f, 120f)),
            (new Vector3(c.x - 380f, sy, c.z - 50f),  new Vector3(160f, 180f, 160f)),
            (new Vector3(c.x + 380f, sy, c.z - 80f),  new Vector3(140f, 150f, 140f)),
            (new Vector3(c.x - 200f, sy, c.z - 200f), new Vector3( 90f,  80f,  90f)),
            (new Vector3(c.x + 220f, sy, c.z - 180f), new Vector3(110f, 100f, 110f)),
            (new Vector3(c.x - 300f, sy, c.z - 320f), new Vector3(130f, 120f, 130f)),
            (new Vector3(c.x + 320f, sy, c.z - 300f), new Vector3(120f, 130f, 120f)),
            (new Vector3(c.x - 100f, sy, c.z + 330f), new Vector3( 80f,  70f,  80f)),
            (new Vector3(c.x + 120f, sy, c.z + 320f), new Vector3( 90f,  85f,  90f)),
            (new Vector3(c.x - 150f, sy, c.z - 350f), new Vector3( 70f,  65f,  70f)),
            (new Vector3(c.x + 160f, sy, c.z - 370f), new Vector3( 80f,  75f,  80f)),
            (new Vector3(c.x,        sy, c.z + 380f), new Vector3(100f,  95f, 100f)),
        };
        for (int i = 0; i < mountains.Length; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Mountain_{i + 1:00}";
            go.transform.SetParent(mtn.transform);
            go.transform.position = mountains[i].pos + Vector3.up * (mountains[i].size.y * 0.45f);
            go.transform.localScale = mountains[i].size;
            SetMaterial(go, M("BLK_LavaRock"));
            Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        }

        // ── Lava Rivers (3 rivers crossing E-W)
        CreateBox(lava, "Lava_River_N",
            new Vector3(c.x + 50f, sy + 2f, c.z + 200f),
            new Vector3(600f, 4f, 40f),
            M("BLK_Lava"));
        CreateBox(lava, "Lava_River_M",
            new Vector3(c.x - 80f, sy + 2f, c.z - 50f),
            new Vector3(500f, 4f, 40f),
            M("BLK_Lava"));
        CreateBox(lava, "Lava_River_S",
            new Vector3(c.x + 30f, sy + 2f, c.z - 270f),
            new Vector3(400f, 4f, 35f),
            M("BLK_Lava"));

        // ── Volcanic craters (4 craters)
        float[] craterRads = { 60f, 80f, 50f, 45f };
        Vector3[] craterPos = {
            new Vector3(c.x - 60f,  sy, c.z + 150f),
            new Vector3(c.x + 150f, sy, c.z - 120f),
            new Vector3(c.x - 200f, sy, c.z - 200f),
            new Vector3(c.x + 50f,  sy, c.z - 330f),
        };
        for (int i = 0; i < 4; i++)
        {
            var crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            crater.name = $"Crater_{i + 1}";
            crater.transform.SetParent(prop.transform);
            crater.transform.position = craterPos[i];
            crater.transform.localScale = new Vector3(craterRads[i] * 2f, 5f, craterRads[i] * 2f);
            SetMaterial(crater, M("BLK_LavaRock"));
            Object.DestroyImmediate(crater.GetComponent<CapsuleCollider>());

            // Inner lava pool
            var pool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pool.name = $"Crater_{i + 1}_LavaPool";
            pool.transform.SetParent(prop.transform);
            pool.transform.position = craterPos[i] + Vector3.up * 3f;
            pool.transform.localScale = new Vector3(craterRads[i] * 1.2f, 3f, craterRads[i] * 1.2f);
            SetMaterial(pool, M("BLK_Lava"));
            Object.DestroyImmediate(pool.GetComponent<CapsuleCollider>());
        }

        // ── Stone Gates (4 along path)
        BuildStoneGate(prop, "Gate_A03_01", new Vector3(c.x - 50f,  sy, c.z + 300f), 0f);
        BuildStoneGate(prop, "Gate_A03_02", new Vector3(c.x + 100f, sy, c.z + 100f), 0f);
        BuildStoneGate(prop, "Gate_A03_03", new Vector3(c.x - 80f,  sy, c.z - 150f), 0f);
        BuildStoneGate(prop, "Gate_A03_04", new Vector3(c.x + 60f,  sy, c.z - 320f), 0f);

        // ── Final Landmark – circular stone platform (S end)
        BuildFinalLandmark(lmk, new Vector3(c.x, sy, c.z - 380f));

        // ── Main path through volcanic area
        var pathA03 = Child(nav, "Area03_Paths");
        BuildRoadSegment(pathA03, "A03_Path_N",
            new Vector3(c.x - 20f, sy + 3f, c.z + 310f),
            new Vector3(ROAD_W, 3f, 100f));
        BuildRoadSegment(pathA03, "A03_Path_MN",
            new Vector3(c.x + 30f, sy + 3f, c.z + 180f),
            new Vector3(ROAD_W, 3f, 150f));
        BuildRoadSegment(pathA03, "A03_Path_MM",
            new Vector3(c.x - 10f, sy + 3f, c.z + 30f),
            new Vector3(ROAD_W, 3f, 160f));
        BuildRoadSegment(pathA03, "A03_Path_MS",
            new Vector3(c.x + 20f, sy + 3f, c.z - 120f),
            new Vector3(ROAD_W, 3f, 150f));
        BuildRoadSegment(pathA03, "A03_Path_S",
            new Vector3(c.x,       sy + 3f, c.z - 280f),
            new Vector3(ROAD_W, 3f, 150f));
    }

    // ─────────────────────────────────────────────────────────────
    //  CONNECTION BRIDGE  (Area 01 → Area 02)
    // ─────────────────────────────────────────────────────────────
    static void BuildConnectionBridge(GameObject nav)
    {
        // Bridge deck
        float bridgeMidZ = (A01_CENTER.z + A01_CENTER.z - A01_D * 0.5f + A02_CENTER.z + A02_D * 0.5f) * 0.5f;
        float bridgeY    = (A01_Y + A02_Y) * 0.5f + 10f;
        float bridgeLen  = A01_CENTER.z - A01_D * 0.5f - (A02_CENTER.z + A02_D * 0.5f) + 30f;

        CreateBox(nav, "Bridge_A01_A02",
            new Vector3(0f, bridgeY, A01_CENTER.z - A01_D * 0.5f - bridgeLen * 0.5f + 15f),
            new Vector3(50f, 12f, Mathf.Abs(bridgeLen)),
            M("BLK_Stone"));

        // Bridge pillars
        for (int i = 0; i < 3; i++)
        {
            float pz = A01_CENTER.z - A01_D * 0.5f - 20f - i * (Mathf.Abs(bridgeLen) / 3f);
            CreateBox(nav, $"Bridge_Pillar_{i}",
                new Vector3(0f, bridgeY - 30f, pz),
                new Vector3(18f, 60f, 18f),
                M("BLK_Stone"));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  CAMERAS
    // ─────────────────────────────────────────────────────────────
    static void BuildCameras(GameObject camRoot)
    {
        // ── Overview Camera (Isometric Orthographic)
        var overviewGO = new GameObject("Camera_Overview");
        overviewGO.transform.SetParent(camRoot.transform);

        // Center of the whole level
        float midZ = (A01_CENTER.z + A03_CENTER.z) * 0.5f;
        overviewGO.transform.position = new Vector3(0f, 2200f, midZ - 600f);
        overviewGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

        var cam = overviewGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 2000f;
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 10000f;
        cam.tag = "Untagged"; // keep Main Camera tag on existing cam

        // ── Gameplay Preview Camera (perspective, 45° angle)
        var gpGO = new GameObject("Camera_GameplayPreview");
        gpGO.transform.SetParent(camRoot.transform);
        gpGO.transform.position = new Vector3(300f, 600f, A01_CENTER.z - 400f);
        gpGO.transform.rotation = Quaternion.Euler(50f, -25f, 0f);

        var gpCam = gpGO.AddComponent<Camera>();
        gpCam.orthographic = false;
        gpCam.fieldOfView = 60f;
        gpCam.nearClipPlane = 1f;
        gpCam.farClipPlane = 8000f;
        gpCam.enabled = false; // disabled by default, enable to preview
    }

    // ─────────────────────────────────────────────────────────────
    //  LIGHTING
    // ─────────────────────────────────────────────────────────────
    static void SetupLighting(GameObject lightRoot)
    {
        var dlGO = new GameObject("Directional_Light_Blockout");
        dlGO.transform.SetParent(lightRoot.transform);
        dlGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        var dl = dlGO.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.intensity = 1.2f;
        dl.color = new Color(1f, 0.95f, 0.85f);
        dl.shadows = LightShadows.Soft;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPER – PREFAB-LIKE SUB-BUILDERS
    // ─────────────────────────────────────────────────────────────

    static void BuildIslandCliffs(GameObject parent, Vector3 c, float w, float d, float sy, string tag, Material mat)
    {
        float halfW = w * 0.5f;
        float halfD = d * 0.5f;
        float cliffY = sy - CLIFF_H * 0.5f - 5f;
        // North
        CreateBox(parent, $"{tag}_Cliff_N", new Vector3(c.x, cliffY, c.z + halfD), new Vector3(w + CLIFF_T * 2f, CLIFF_H, CLIFF_T), mat);
        // South
        CreateBox(parent, $"{tag}_Cliff_S", new Vector3(c.x, cliffY, c.z - halfD), new Vector3(w + CLIFF_T * 2f, CLIFF_H, CLIFF_T), mat);
        // East
        CreateBox(parent, $"{tag}_Cliff_E", new Vector3(c.x + halfW, cliffY, c.z), new Vector3(CLIFF_T, CLIFF_H, d), mat);
        // West
        CreateBox(parent, $"{tag}_Cliff_W", new Vector3(c.x - halfW, cliffY, c.z), new Vector3(CLIFF_T, CLIFF_H, d), mat);
    }

    static void BuildTemple(GameObject parent, string name, Vector3 pos)
    {
        var root = Child(parent, name);
        // Base platform
        CreateBox(root, "Platform", pos + new Vector3(0, 5f, 0), new Vector3(120f, 10f, 100f), M("BLK_Stone"));
        // Main hall body
        CreateBox(root, "Hall_Body", pos + new Vector3(0, 40f, 0), new Vector3(90f, 60f, 80f), M("BLK_Building"));
        // Roof (flat for blockout)
        CreateBox(root, "Roof", pos + new Vector3(0, 75f, 0), new Vector3(100f, 18f, 88f), M("BLK_Stone"));
        // Entrance steps
        CreateBox(root, "Steps", pos + new Vector3(0, 8f, -55f), new Vector3(50f, 16f, 20f), M("BLK_Stone"));
        // Landmark tree / totem
        var totem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        totem.name = "CentralTree";
        totem.transform.SetParent(root.transform);
        totem.transform.position = pos + new Vector3(0f, 55f, 55f);
        totem.transform.localScale = new Vector3(15f, 40f, 15f);
        SetMaterial(totem, M("BLK_Vegetation"));
        Object.DestroyImmediate(totem.GetComponent<CapsuleCollider>());
    }

    static void BuildHouse(GameObject parent, string name, Vector3 pos)
    {
        var root = Child(parent, name);
        // Body
        CreateBox(root, "Body", pos + new Vector3(0, HOUSE_H * 0.5f, 0),
            new Vector3(HOUSE_W, HOUSE_H, HOUSE_D), M("BLK_Building"));
        // Roof wedge (approximated as flat box)
        CreateBox(root, "Roof", pos + new Vector3(0, HOUSE_H + 8f, 0),
            new Vector3(HOUSE_W + 5f, 16f, HOUSE_D + 5f), M("BLK_Stone"));
    }

    static void BuildWatchtower(GameObject parent, string name, Vector3 pos)
    {
        var root = Child(parent, name);
        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name = "Tower_Shaft";
        shaft.transform.SetParent(root.transform);
        shaft.transform.position = pos + new Vector3(0, 40f, 0);
        shaft.transform.localScale = new Vector3(22f, 40f, 22f);
        SetMaterial(shaft, M("BLK_Stone"));
        Object.DestroyImmediate(shaft.GetComponent<CapsuleCollider>());

        CreateBox(root, "Tower_Top", pos + new Vector3(0, 86f, 0),
            new Vector3(32f, 14f, 32f), M("BLK_Stone"));
    }

    static void BuildStoneGate(GameObject parent, string name, Vector3 pos, float rotY)
    {
        var root = Child(parent, name);
        root.transform.position = pos;
        root.transform.rotation = Quaternion.Euler(0, rotY, 0);

        // Left pillar
        CreateBox(root, "Pillar_L", new Vector3(-38f, 35f, 0),  new Vector3(18f, 70f, 20f), M("BLK_Stone"), root.transform);
        // Right pillar
        CreateBox(root, "Pillar_R", new Vector3( 38f, 35f, 0),  new Vector3(18f, 70f, 20f), M("BLK_Stone"), root.transform);
        // Arch top
        CreateBox(root, "Arch",     new Vector3(0f,  72f, 0),   new Vector3(94f, 14f, 22f), M("BLK_Stone"), root.transform);
    }

    static void BuildFinalLandmark(GameObject parent, Vector3 pos)
    {
        var root = Child(parent, "FinalLandmark");

        // Circular base platform
        var base1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        base1.name = "Platform_Outer";
        base1.transform.SetParent(root.transform);
        base1.transform.position = pos + Vector3.up * 5f;
        base1.transform.localScale = new Vector3(180f, 10f, 180f);
        SetMaterial(base1, M("BLK_Stone"));
        Object.DestroyImmediate(base1.GetComponent<CapsuleCollider>());

        var base2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        base2.name = "Platform_Inner";
        base2.transform.SetParent(root.transform);
        base2.transform.position = pos + Vector3.up * 18f;
        base2.transform.localScale = new Vector3(100f, 12f, 100f);
        SetMaterial(base2, M("BLK_Stone"));
        Object.DestroyImmediate(base2.GetComponent<CapsuleCollider>());

        // Central obelisk
        CreateBox(root, "Obelisk", pos + new Vector3(0, 80f, 0),
            new Vector3(20f, 120f, 20f), M("BLK_Building"));
        CreateBox(root, "Obelisk_Cap", pos + new Vector3(0, 148f, 0),
            new Vector3(28f, 18f, 28f), M("BLK_Stone"));

        // Surrounding pillars (8 pillars around obelisk)
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Pillar_{i}";
            pillar.transform.SetParent(root.transform);
            pillar.transform.position = pos + new Vector3(Mathf.Sin(angle) * 70f, 35f, Mathf.Cos(angle) * 70f);
            pillar.transform.localScale = new Vector3(10f, 35f, 10f);
            SetMaterial(pillar, M("BLK_Stone"));
            Object.DestroyImmediate(pillar.GetComponent<CapsuleCollider>());
        }
    }

    static void BuildRuinCluster(GameObject parent, string name, Vector3 pos)
    {
        var root = Child(parent, name);
        CreateBox(root, "Ruin_Wall_A", pos + new Vector3(0,   20f,  0),   new Vector3(60f, 40f, 10f), M("BLK_Stone"));
        CreateBox(root, "Ruin_Wall_B", pos + new Vector3(35f, 12f, 25f),  new Vector3(10f, 24f, 60f), M("BLK_Stone"));
        CreateBox(root, "Ruin_Block",  pos + new Vector3(-20f, 8f, 20f),  new Vector3(25f, 15f, 25f), M("BLK_Stone"));
    }

    static void BuildRockFormation(GameObject parent, string name, Vector3 pos)
    {
        var root = Child(parent, name);
        for (int i = 0; i < 3; i++)
        {
            var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = $"Rock_{i}";
            rock.transform.SetParent(root.transform);
            float offset = i * 28f;
            rock.transform.position = pos + new Vector3(offset * 0.5f, 20f + i * 5f, offset * 0.3f);
            rock.transform.localScale = new Vector3(40f - i * 8f, 35f - i * 8f, 40f - i * 8f);
            SetMaterial(rock, M("BLK_Stone"));
            Object.DestroyImmediate(rock.GetComponent<SphereCollider>());
        }
    }

    static void BuildTreeCluster(GameObject parent, Vector3 center, int count, float radius, float trunkHeight, Material mat)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            float r = radius * (0.5f + 0.5f * ((i % 2 == 0) ? 1f : 0.6f));
            Vector3 pos = center + new Vector3(Mathf.Sin(angle) * r, 0f, Mathf.Cos(angle) * r);

            // Trunk
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = $"Tree_Trunk_{i}";
            trunk.transform.SetParent(parent.transform);
            trunk.transform.position = pos + Vector3.up * (trunkHeight * 0.4f);
            trunk.transform.localScale = new Vector3(10f, trunkHeight * 0.4f, 10f);
            SetMaterial(trunk, M("BLK_Building"));
            Object.DestroyImmediate(trunk.GetComponent<CapsuleCollider>());

            // Canopy
            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = $"Tree_Canopy_{i}";
            canopy.transform.SetParent(parent.transform);
            canopy.transform.position = pos + Vector3.up * trunkHeight;
            canopy.transform.localScale = new Vector3(40f, 50f, 40f);
            SetMaterial(canopy, mat);
            Object.DestroyImmediate(canopy.GetComponent<SphereCollider>());
        }
    }

    static void BuildRoadSegment(GameObject parent, string name, Vector3 pos, Vector3 size)
    {
        CreateBox(parent, name, pos, size, M("BLK_Path"));
    }

    // ─────────────────────────────────────────────────────────────
    //  UTILITY
    // ─────────────────────────────────────────────────────────────

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;
        return go;
    }

    static GameObject CreateBox(GameObject parent, string name, Vector3 worldPos, Vector3 size, Material mat,
        Transform relativeTo = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        if (relativeTo != null)
            go.transform.localPosition = worldPos;
        else
            go.transform.position = worldPos;
        go.transform.localScale = size;
        SetMaterial(go, mat);
        // Remove collider to keep scene clean (add back if NavMesh needed)
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        return go;
    }

    static void SetMaterial(GameObject go, Material mat)
    {
        if (mat == null) return;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;
    }
}

