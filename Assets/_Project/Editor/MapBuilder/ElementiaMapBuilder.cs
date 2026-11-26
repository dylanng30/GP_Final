// Elementia map builder - entry point, village population, lighting, scene setup.
// Regenerates the whole map: Tools/Elementia/Build Map
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        [MenuItem("Tools/Elementia/Build Map")]
        public static void BuildMenu()
        {
            if (!EditorUtility.DisplayDialog("Elementia Map", "Regenerate the map scene?\n" + ScenePath + "\n(Generated meshes are rebuilt; manual edits in that scene will be lost.)", "Build", "Cancel")) return;
            Debug.Log(Build());
        }

        public static string Build(int seed = 20261125)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            rng = new System.Random(seed);
            meshCounter = 0; prefabCache.Clear(); kitPieces.Clear();

            // keep whatever the user had open
            EditorSceneManager.SaveOpenScenes();

            PrepareFolders();
            FixKit02Materials();
            IndexKit(KitDir + "/KIT_01/KIT_01.fbx");
            IndexKit(KitDir + "/KIT02_Stone/SM_Plain_Rock_Kit.fbx");
            IndexKit(KitDir + "/KIT02_Stone/SM_Mossy_Rock_Kit.fbx");
            IndexKit(KitDir + "/KIT02_Stone/SM_Desert_Rock_Kit.fbx");

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
            matEnv = MakeMat("M_Env_Palette", tex, 0.05f, Color.black, false);
            matLava = MakeMat("M_Env_Lava", tex, 0.15f, new Color(1.0f, 0.42f, 0.12f) * 1.7f, true);
            matWater = MakeMat("M_Env_Water", tex, 0.85f, new Color(0.1f, 0.3f, 0.45f) * 0.6f, true);
            matCrystal = MakeMat("M_Env_Crystal", tex, 0.8f, new Color(1f, 0.25f, 0.3f) * 1.8f, true);
            matSwamp = MakeMat("M_Env_SwampWater", tex, 0.8f, new Color(0.05f, 0.14f, 0.1f), true);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DefineLayout();
            TrimStreams();

            var root = Group("ELEMENTIA_MAP", null);
            var terrain = Group("Terrain", root.transform);
            foreach (var isl in islands) BuildIsland(isl, terrain.transform);
            var roads = Group("Roads", terrain.transform);
            // each road gets its own height layer so junctions don't z-fight
            for (int i = 0; i < paths.Count; i++) if (paths[i].cell.c != C_Plaza.c || paths[i].cell.r != C_Plaza.r) BuildRibbon(paths[i], roads.transform, (paths[i].name.StartsWith("Path_Volcano") ? 0.12f : 0.08f) + (i % 4) * 0.035f, matEnv, false);
            var water = Group("Water", terrain.transform);
            foreach (var st in streams)
            {
                BuildRibbon(st, water.transform, 0.12f, matWater, false, 0.15f);
                Vector2 stEnd = st.pts[st.pts.Count - 1];
                if (!(OnLand(stEnd, -4f) && IslandAt(stEnd).Norm(stEnd) < 0.9f)) BuildWaterfall(st, water.transform); // a stream ending in a lake has no fall
                Vector2 s0 = st.pts[0];
                BuildDisc(st.name + "_SpringRocks", s0, st.width * 1.6f, H(s0.x, s0.y) + 0.05f, C_Stone, matEnv, water.transform, 4.4f, 16);
                BuildDisc(st.name + "_Spring", s0, st.width * 1.25f, H(s0.x, s0.y) + 0.25f, C_Water, matWater, water.transform, 5.1f, 16);
            }

            BuildVillage(root.transform);
            BuildJungle(root.transform);
            BuildConnectingPath(root.transform);
            BuildVolcano(root.transform);
            Debug.Log(ConvertTreesToPines(root.transform));   // pines only (Tree01A / Tree01B), all locked to LOD2
            SetupMarkers(root.transform);
            SetupLighting();
            MarkStatic(root);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            FrameSceneView();
            return "Elementia map built in " + sw.Elapsed.TotalSeconds.ToString("F1") + "s -> " + ScenePath + " (" + root.GetComponentsInChildren<Transform>().Length + " objects, " + meshCounter + " meshes)";
        }

        static void PrepareFolders()
        {
            if (AssetDatabase.IsValidFolder(GenDir)) AssetDatabase.DeleteAsset(GenDir);
            AssetDatabase.CreateFolder(KitDir, "Generated");
            AssetDatabase.CreateFolder(GenDir, "Meshes");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/01_Scenes/Map")) AssetDatabase.CreateFolder("Assets/_Project/01_Scenes", "Map");
        }

        // KIT02 rock FBXs ship with an untextured embedded material -> remap to a textured stone palette material.
        static void FixKit02Materials()
        {
            string matPath = KitDir + "/KIT02_Stone/M_PaletteGrid_KIT02_Stone.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(KitDir + "/KIT02_Stone/Textures/T_PaletteGrid_KIT01_Stone.png");
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_PaletteGrid_KIT02_Stone" };
                mat.SetTexture("_BaseMap", tex); mat.SetTexture("_MainTex", tex); mat.SetFloat("_Smoothness", 0.05f);
                mat.enableInstancing = true;
                AssetDatabase.CreateAsset(mat, matPath);
            }
            foreach (var f in new[] { "SM_Plain_Rock_Kit", "SM_Mossy_Rock_Kit", "SM_Desert_Rock_Kit", "SM_Snowy_Rock_Kit" })
            {
                var imp = AssetImporter.GetAtPath(KitDir + "/KIT02_Stone/" + f + ".fbx") as ModelImporter;
                if (imp == null) continue;
                bool changed = false;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(imp.assetPath);
                var names = new HashSet<string>();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) if (m != null && m != mat) names.Add(m.name);
                foreach (var n in names) { imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), mat); changed = true; }
                if (changed) imp.SaveAndReimport();
            }
        }

        static void TrimStreams()
        {
            TrimToLand(streams);
            TrimToLand(lavaRivers);
        }
        static void TrimToLand(List<Path> list)
        {
            foreach (var st in list)
            {
                int start = 0;
                for (int i = 0; i < st.pts.Count; i++) if (OnLand(st.pts[i], 4f)) { start = i; break; }
                int cut = st.pts.Count;
                for (int i = start; i < st.pts.Count; i++) if (!OnLand(st.pts[i], -2.0f)) { cut = i; break; }
                st.pts = st.pts.GetRange(start, Mathf.Max(2, cut - start));
            }
        }

        static void BuildWaterfall(Path st, Transform parent)
        {
            Vector2 end = st.pts[st.pts.Count - 1], prev = st.pts[st.pts.Count - 2];
            Vector2 fwd = (end - prev).normalized, n = new Vector2(-fwd.y, fwd.x);
            float w = st.width * 0.5f, y0 = H(end.x, end.y) + 0.1f;
            var mb = new MB();
            float[] drop = { 0f, 2f, 6f, 12f, 20f, 26f };
            for (int k = 0; k < drop.Length - 1; k++)
            {
                float o0 = 0.4f + drop[k] * 0.12f, o1 = 0.4f + drop[k + 1] * 0.12f;
                Vector3 a = V3(end + fwd * o0, y0 - drop[k]), b = V3(end + fwd * o1, y0 - drop[k + 1]);
                Vector3 s = V3(n, 0) * w * (1f + k * 0.12f), s1 = V3(n, 0) * w * (1f + (k + 1) * 0.12f);
                mb.Quad(a - s, b - s1, b + s1, a + s, UV(C_Water, 0.3f), UV(C_Water, 0.7f), UV(C_Water, 0.7f), UV(C_Water, 0.3f));
                mb.Quad(a - s, a + s, b + s1, b - s1, UV(C_Water, 0.3f), UV(C_Water, 0.3f), UV(C_Water, 0.7f), UV(C_Water, 0.7f));
            }
            // connect river end to the fall lip
            Vector3 l0 = V3(end - n * w, y0), r0 = V3(end + n * w, y0), l1 = V3(end + fwd * 0.4f - n * w, y0), r1 = V3(end + fwd * 0.4f + n * w, y0);
            mb.Quad(l0, l1, r1, r0, UV(C_Water, 0.4f));
            MeshGO(st.name + "_Waterfall", mb.ToMesh(st.name + "_Waterfall"), matWater, parent, false);
            var mist = SpawnPrefab(FX + "Smoke/White/SmokeWhiteSoft.prefab", parent, V3(end + fwd * 3f, y0 - 22f), 0, 2.5f);
            if (mist != null) mist.name = "FX_WaterfallMist";
        }

        static Vector2 PlaceNear(Vector2 target, float r)
        {
            for (int k = 0; k < 200; k++)
            {
                float ang = k * 2.4f, rad = k * 0.6f;
                Vector2 p = target + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                if (Free(p, r, 0.5f)) { Occupy(p, r); return p; }
            }
            return target;
        }

        // BuildConnectingPath: see ElementiaMapBuilder.ConnectingPath.cs

        // ---------------- Markers, lighting, camera ----------------
        static void SetupMarkers(Transform root)
        {
            var m = Group("Gameplay", root);
            var spawn = Group("PlayerSpawn", m.transform);
            Vector2 sp = PlazaC + new Vector2(0, -10f);
            spawn.transform.position = V3(sp, PaveY + 0.02f);
            spawn.transform.rotation = Quaternion.Euler(0, 0f, 0); // establishing view: sacred tree + temple, then the player turns south to the gate
            // visual scale reference (character model), safe to delete
            var refModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/02_Prefabs/Hero/Hero_Water_000.prefab");
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/WATER_after.fbx");
            if (fbx != null)
            {
                var r = (GameObject)PrefabUtility.InstantiatePrefab(fbx, m.transform);
                r.name = "ScaleRef_Character (delete me)";
                r.transform.position = V3(sp + new Vector2(2.5f, 0), PaveY + 0.02f);
                r.transform.rotation = Quaternion.Euler(0, 180f, 0);
                // match hero prefab height (~2.6m) if available
                float hh = 1.16f; var rr = r.GetComponentsInChildren<Renderer>(); if (rr.Length > 0) { var b = rr[0].bounds; foreach (var x in rr) b.Encapsulate(x.bounds); hh = Mathf.Max(0.1f, b.size.y); }
                r.transform.localScale = Vector3.one * (heroHeight / hh);
            }
            string[] zones = { "Zone_01_StartingPoint", "Zone_02_Village", "Zone_03_ConnectingPath", "Zone_04_VolcanicArea", "Zone_05_BossArena", "Zone_06_JungleWest", "Zone_07_JungleEast", "Zone_08_BossStage1" };
            Vector2[] zp = { PlazaC, PlazaC + new Vector2(0, 10), new Vector2(4, 15), new Vector2(0, -85), ArenaC, new Vector2(-74, 104), new Vector2(84, 110), Arena2C };
            for (int i = 0; i < zones.Length; i++)
            {
                var z = Group("Marker_" + zones[i], m.transform); z.transform.position = V3(zp[i], H(zp[i].x, zp[i].y) + 0.2f);
            }
            for (int i = 0; i < clearings.Count; i++)
            {
                var c = Group("CombatArea_" + i + "_" + (clearingNames != null && i < clearingNames.Length ? clearingNames[i] : ""), m.transform); c.transform.position = Ground(new Vector2(clearings[i].x, clearings[i].y));
                var col = c.AddComponent<SphereCollider>(); col.isTrigger = true; col.radius = clearings[i].z;
            }
        }
        static float heroHeight = 2.6f;

        static void SetupLighting()
        {
            var sunGo = new GameObject("Directional Light (Sun)");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, 0.95f, 0.84f); sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.75f;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun;
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Holotna/Mountain/Misc/Skybox.mat");
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.52f, 0.52f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.27f, 0.24f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 230f; RenderSettings.fogEndDistance = 900f;
            RenderSettings.fogColor = new Color(0.7f, 0.8f, 0.9f);
            DynamicGI.UpdateEnvironment();

            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/Holotna/Mountain/Settings/Effects.asset");
            if (profile != null)
            {
                var vgo = new GameObject("Global Volume");
                var vol = vgo.AddComponent<UnityEngine.Rendering.Volume>(); vol.isGlobal = true; vol.sharedProfile = profile;
            }
            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>(); cam.farClipPlane = 1500f; cam.nearClipPlane = 0.3f; cam.fieldOfView = 50f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(150f, 140f, 120f);
            camGo.transform.LookAt(new Vector3(0, 0, -15f));
        }

        static void MarkStatic(GameObject root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.GetComponentInParent<ParticleSystem>() != null) continue;
                if (t.name.StartsWith("Marker_") || t.name.StartsWith("PlayerSpawn") || t.name.StartsWith("BossSpawn") || t.name.StartsWith("ScaleRef") || t.name.StartsWith("CombatArea")) continue;
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            }
        }

        static void FrameSceneView()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) return;
            sv.LookAt(new Vector3(MainlandC.x, 0, MainlandC.y - 8f), Quaternion.Euler(90f, 0f, 0f), 150f);   // top-down on the round mainland
            sv.Repaint();
        }
    }
}
