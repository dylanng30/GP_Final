using UnityEngine;

namespace Game.Shaders.Crystal
{
    [RequireComponent(typeof(Renderer))]
    public class CrystalShaderController : BaseShaderController
    {
        [SerializeField] private CrystalShaderSO crystalShaderData;
        
        private MaterialPropertyBlock _propBlock;
        
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
        private static readonly int TopLineId = Shader.PropertyToID("_TopLine");
        private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");
        private static readonly int BottomLineId = Shader.PropertyToID("_BottomLine");
        
        public override void ApplyShaderSettings()
        {
            if (crystalShaderData == null || sRenderer == null) return;
            
            sRenderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetFloat(MetallicId, crystalShaderData.Metallic);
            _propBlock.SetFloat(SmoothnessId, crystalShaderData.Smoothness);
            _propBlock.SetColor(BaseColorId, crystalShaderData.BaseColor);
            _propBlock.SetColor(TopColorId, crystalShaderData.TopColor);
            _propBlock.SetFloat(TopLineId, crystalShaderData.TopLine);
            _propBlock.SetColor(BottomColorId, crystalShaderData.BottomColor);
            _propBlock.SetFloat(BottomLineId, crystalShaderData.BottomLine);
            
            sRenderer.SetPropertyBlock(_propBlock);
        }
    }
}