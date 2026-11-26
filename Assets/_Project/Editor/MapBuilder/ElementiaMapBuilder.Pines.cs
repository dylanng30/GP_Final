// Elementia map builder - tree pass.
// Every tree that is not already a Holotna pine is removed and replaced, on the same spot, by a pine prefab
// (Tree01A / Tree01B, strictly alternating). Every pine instance on the map is then locked to the LOD2 mesh
// of its LODGroup (LOD0/LOD1 renderers disabled, LODGroup reduced to the LOD2 level).
// Runs automatically at the end of Build(); can also be run on the open scene: Tools/Elementia/Convert Trees To Pines.
// Nothing that is not a tree is moved or deleted.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Elementia.EditorTools
{
    public static partial class ElementiaMapBuilder
    {
        const string PineA = "Tree01A", PineB = "Tree01B";
        const float PineMinSpacing = 2.0f;   // a replacement is skipped if a pine already stands closer than this

        class TreeSpot { public Transform parent; public Vector3 pos; public float yaw, scale; public string src; }

        [MenuItem("Tools/Elementia/Convert Trees To Pines (open scene)")]
        public static void ConvertTreesMenu()
        {
            Debug.Log(ConvertTreesInOpenScene());
        }

        public static string ConvertTreesInOpenScene()
        {
            var root = GameObject.Find("ELEMENTIA_MAP");
            if (root == null) return "[ElementiaMap] ELEMENTIA_MAP not found in the open scene";
            string report = ConvertTreesToPines(root.transform);
            var scene = root.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            return report;
        }

        static bool IsPine(GameObject go, out string which)
        {
            which = null;
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go)) return false;
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (src == null) return false;
            string path = AssetDatabase.GetAssetPath(src);
            if (path == HolotnaPrefabs + PineA + ".prefab") which = PineA;
            else if (path == HolotnaPrefabs + PineB + ".prefab") which = PineB;
            return which != null;
        }

        static float Hash01(Vector3 p, float salt)
        {
            float v = Mathf.Sin(p.x * 12.9898f + p.z * 78.233f + salt * 37.719f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        public static string ConvertTreesToPines(Transform root)
        {
            var spots = new List<TreeSpot>();
            var remove = new List<GameObject>();
            var meshAssets = new HashSet<string>();
            var pines = new List<GameObject>();
            var counts = new Dictionary<string, int>();
            System.Action<string> count = k => { int c; counts.TryGetValue(k, out c); counts[k] = c + 1; };

            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t == root) continue;
                string n = t.name;
                string which;
                if (IsPine(t.gameObject, out which)) { pines.Add(t.gameObject); continue; }

                // jungle trees merged per 40 m cell: one TreeCollider per tree gives its spot and size
                if (n == "TreeCollider" && t.parent != null && t.parent.name == "TreeColliders")
                {
                    var cap = t.GetComponent<CapsuleCollider>();
                    float h = cap != null ? cap.height : 6f, r = cap != null ? cap.radius : 0.4f;
                    float ratio = r / Mathf.Max(0.01f, h);
                    // trop: r 0.42s / h 6s, palm: 0.3s / 5s, mangrove: 0.35s / 4.5s
                    float baseH = ratio > 0.074f ? 4.5f : (ratio > 0.065f ? 6f : 5f);
                    float s = h / baseH;
                    Vector3 basePos = t.position - Vector3.up * h * 0.5f;
                    var veg = t.parent.parent;
                    spots.Add(new TreeSpot { parent = veg, pos = basePos + Vector3.down * 0.05f, yaw = Hash01(basePos, 1f) * 360f, scale = Mathf.Clamp(s, 0.85f, 1.25f), src = "JungleTree" });
                    count("JungleTree");
                    continue;
                }
                if (n == "TreeColliders") { remove.Add(t.gameObject); continue; }

                var mf = t.GetComponent<MeshFilter>();
                // merged tree chunk meshes: "<zone>_Trees_<i>"
                if (mf != null && n.Contains("_Trees_"))
                {
                    remove.Add(t.gameObject);
                    if (mf.sharedMesh != null) meshAssets.Add(AssetDatabase.GetAssetPath(mf.sharedMesh));
                    continue;
                }
                // single procedural trees: dead / scorched / swamp trees, ember maples, boss-grove landmark + sapling
                bool single = n.StartsWith("SwampDeadTree_") || n.StartsWith("ScorchedTree_") || n.StartsWith("DeadTree_") || n.StartsWith("EmberMaple_");
                bool landmark = n.StartsWith("BossStage1_AncientTree");
                bool sapling = n == "BossStage1_SpiritSapling";
                if (mf != null && (single || landmark || sapling))
                {
                    float sc = landmark ? 1.6f : (sapling ? 0.45f : Mathf.Lerp(0.8f, 1.05f, Hash01(t.position, 2f)));
                    spots.Add(new TreeSpot { parent = t.parent, pos = t.position + (sapling ? Vector3.zero : Vector3.down * 0.15f), yaw = t.eulerAngles.y, scale = sc, src = n.Split('_')[0] });
                    count(landmark ? "AncientTree" : (sapling ? "SpiritSapling" : n.Split('_')[0]));
                    remove.Add(t.gameObject);
                    if (mf.sharedMesh != null) meshAssets.Add(AssetDatabase.GetAssetPath(mf.sharedMesh));
                }
            }

            int kept = pines.Count;
            // existing pines + already placed replacements block new spots that would overlap them
            var taken = new List<Vector3>();
            foreach (var p in pines) taken.Add(p.transform.position);

            var pa = LoadPrefab(HolotnaPrefabs + PineA + ".prefab");
            var pb = LoadPrefab(HolotnaPrefabs + PineB + ".prefab");
            int placed = 0, skipped = 0;
            var groups = new Dictionary<Transform, Transform>();
            foreach (var s in spots)
            {
                bool clash = false;
                foreach (var q in taken) { float dx = q.x - s.pos.x, dz = q.z - s.pos.z; if (dx * dx + dz * dz < PineMinSpacing * PineMinSpacing) { clash = true; break; } }
                if (clash) { skipped++; continue; }
                Transform parent;
                if (!groups.TryGetValue(s.parent, out parent))
                {
                    parent = s.parent.Find("Pines");
                    if (parent == null) { parent = new GameObject("Pines").transform; parent.SetParent(s.parent, false); }
                    groups[s.parent] = parent;
                }
                var prefab = (placed % 2 == 0) ? pa : pb;   // strict A/B alternation
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                go.transform.position = s.pos;
                go.transform.rotation = Quaternion.Euler(0, s.yaw, 0);
                go.transform.localScale = Vector3.one * s.scale;
                go.name = prefab.name;
                pines.Add(go); taken.Add(s.pos); placed++;
            }

            foreach (var g in remove) if (g != null) Object.DestroyImmediate(g);
            int meshesDeleted = 0;
            foreach (var path in meshAssets)
                if (!string.IsNullOrEmpty(path) && path.StartsWith(GenDir) && AssetDatabase.DeleteAsset(path)) meshesDeleted++;

            // lock every pine to LOD2
            int lodOk = 0, lodMissing = 0;
            foreach (var p in pines) { if (p != null && ForceLOD2(p)) lodOk++; else lodMissing++; }
            foreach (var p in pines)
                if (p != null) foreach (var tr in p.GetComponentsInChildren<Transform>(true))
                        GameObjectUtility.SetStaticEditorFlags(tr.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);

            var sb = new System.Text.StringBuilder();
            sb.Append("[ElementiaMap] Trees -> pines: kept ").Append(kept).Append(" existing pines, replaced ");
            foreach (var kv in counts) sb.Append(kv.Key).Append('=').Append(kv.Value).Append(' ');
            sb.Append("-> placed ").Append(placed).Append(" pines (").Append(skipped).Append(" skipped: overlap). ");
            sb.Append("Removed ").Append(remove.Count).Append(" objects, ").Append(meshesDeleted).Append(" mesh assets. ");
            sb.Append("LOD2 set on ").Append(lodOk).Append(" pines").Append(lodMissing > 0 ? (", " + lodMissing + " without LODGroup") : "").Append('.');
            return sb.ToString();
        }

        // Keep only the LOD2 level: LOD0/LOD1 renderers off, LODGroup reduced to one level (LOD2 renderers, culled below 2% screen height).
        static bool ForceLOD2(GameObject pine)
        {
            var lg = pine.GetComponentInChildren<LODGroup>(true);
            if (lg == null) return false;
            var lods = lg.GetLODs();
            if (lods.Length == 0) return false;
            int k = Mathf.Min(2, lods.Length - 1);   // a group already reduced to one level holds the LOD2 renderers
            var keep = new List<Renderer>();
            foreach (var r in lods[k].renderers) if (r != null) keep.Add(r);
            if (keep.Count == 0) return false;
            // every other renderer of the tree (LOD0 / LOD1, also when they are no longer listed in the group) is switched off
            foreach (var r in lg.GetComponentsInChildren<Renderer>(true)) r.enabled = keep.Contains(r);
            lg.SetLODs(new[] { new LOD(0.02f, keep.ToArray()) });
            lg.RecalculateBounds();
            return true;
        }
    }
}
