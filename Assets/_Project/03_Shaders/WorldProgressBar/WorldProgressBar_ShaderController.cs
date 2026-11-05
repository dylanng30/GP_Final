using System;
using UnityEngine;

namespace Game.Shaders.WorldProgressBar
{
    public class WorldProgressBar_ShaderController : BaseShaderController
    {
        [Header("--- SETTINGS ---")]
        [SerializeField] private Color progressColor;
        [SerializeField] private Color backgroundColor;

        private static readonly int ProgressPercentageId = Shader.PropertyToID("_ProgressPercentage");
        private static readonly int ProgressColorId = Shader.PropertyToID("_ProgressColor");
        private static readonly int BackgroundColorId = Shader.PropertyToID("_BackgroundColor");

        protected override void Awake()
        {
            base.Awake();
        }

        public override void ApplyShaderSettings()
        {
            if (sRenderer == null) return;
            
            sRenderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetColor(BackgroundColorId, backgroundColor);
            _propBlock.SetColor(ProgressColorId, progressColor);
            _propBlock.SetVector(ProgressPercentageId, new Vector2(-1, 0));
            
            sRenderer.SetPropertyBlock(_propBlock);
        }

        public void SetVisible(bool isVisible)
        {
            if (sRenderer != null) sRenderer.enabled = isVisible;
        }

        public void UpdateProgress(float percentage)
        {
            if (sRenderer == null) return;
            
            sRenderer.GetPropertyBlock(_propBlock);
            
            _propBlock.SetVector(ProgressPercentageId, new Vector2(-percentage, 0));
            
            sRenderer.SetPropertyBlock(_propBlock);
        }
    }
}