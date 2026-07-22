using System;
using Dylanng.Core.Base;
using UnityEngine;

namespace Game.Shaders
{
    [RequireComponent(typeof(MeshRenderer))]
    public abstract class BaseShaderController : MonoBehaviourBase
    {
        [SerializeField] protected MeshRenderer sRenderer;

        protected MaterialPropertyBlock _propBlock;
        
        protected virtual void OnValidate()
        {
            LoadMeshRenderer();
        }

        protected override void Awake()
        {
            base.Awake();
            LoadMeshRenderer();
            
            _propBlock = new MaterialPropertyBlock();
            
            ApplyShaderSettings();
        }

        public abstract void ApplyShaderSettings();
        
        private void LoadMeshRenderer()
        {
            if (sRenderer == null)
            {
                sRenderer = GetComponent<MeshRenderer>();
            }
        }
    }
}