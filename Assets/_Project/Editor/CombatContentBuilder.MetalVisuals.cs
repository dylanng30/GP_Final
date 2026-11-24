using Game.Combat.Abilities.Effects;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.Combat
{
    /// <summary>Dựng prefab hiển thị cho skill hệ Kim: tia sét nảy và sóng kiếm hình quạt.</summary>
    public static partial class CombatContentBuilder
    {
        private const string LightningArcPrefabPath = CombatPrefabFolder + "/LightningArc.prefab";
        private const string ConeWavePrefabPath = CombatPrefabFolder + "/ConeWave.prefab";
        private const string ConeWaveMaterialPath = CombatPrefabFolder + "/ConeWave_Mat.mat";
        private const string LightningMaterialPath = "Assets/Epic Toon FX/Materials/Misc/Magic/magic_line_ADD.mat";
        private const string VertexColorShaderName = "Sprites/Default";

        private const float LightningWidth = 0.18f;
        private static readonly Color LightningCoreColor = new Color(1f, 1f, 0.85f, 1f);
        private static readonly Color LightningEdgeColor = new Color(0.75f, 0.55f, 1f, 1f);

        private static LightningArc BuildLightningArcPrefab()
        {
            var arcObject = new GameObject("LightningArc");

            var line = arcObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = LightningWidth;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(LightningMaterialPath);
            line.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(LightningCoreColor, 0f),
                    new GradientColorKey(LightningEdgeColor, 0.5f),
                    new GradientColorKey(LightningCoreColor, 1f)
                },
                alphaKeys = new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
            };

            arcObject.AddComponent<LightningArc>();
            return SavePrefab<LightningArc>(arcObject, LightningArcPrefabPath);
        }

        private static ConeWaveVisual BuildConeWavePrefab()
        {
            var waveObject = new GameObject("ConeWave");
            waveObject.AddComponent<MeshFilter>();

            var meshRenderer = waveObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = GetOrCreateVertexColorMaterial(ConeWaveMaterialPath);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            waveObject.AddComponent<ConeWaveVisual>();
            return SavePrefab<ConeWaveVisual>(waveObject, ConeWavePrefabPath);
        }

        private static Material GetOrCreateVertexColorMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            EnsureFolder(CombatPrefabFolder);
            material = new Material(Shader.Find(VertexColorShaderName));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>Lưu đè prefab theo đường dẫn cũ để giữ GUID, rồi xóa object tạm.</summary>
        private static T SavePrefab<T>(GameObject source, string path) where T : Component
        {
            EnsureFolder(CombatPrefabFolder);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
            return prefab.GetComponent<T>();
        }
    }
}
