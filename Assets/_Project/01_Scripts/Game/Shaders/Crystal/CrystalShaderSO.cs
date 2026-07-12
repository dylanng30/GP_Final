using Dylanng.Core.Base;
using UnityEngine;

namespace Game.Shaders.Crystal
{
    [CreateAssetMenu(fileName = "NewCrystalShader", menuName = "Game/Shaders/Crystal")]
    public class CrystalShaderSO : ScriptableData
    {
        [Range(0, 1)] public float Metallic;
        [Range(0, 1)] public float Smoothness;
        [ColorUsage(true, true)] public Color BaseColor;
        [ColorUsage(true, true)] public Color TopColor;
        [Range(0, 1)] public float TopLine;
        [ColorUsage(true, true)] public Color BottomColor;
        [Range(0, 1)] public float BottomLine;
    }
}