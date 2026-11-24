using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Feedback
{
    /// <summary>
    /// Vẽ bóng mờ của nhân vật: chụp lại mesh (bake SkinnedMesh, giữ nguyên MeshRenderer) tại một thời điểm
    /// rồi vẽ bằng Graphics.RenderMesh với material phát sáng viền, mờ dần theo thời gian.
    /// Không tạo GameObject nên rẻ và không bị gameplay va chạm.
    /// </summary>
    public class AfterimageRenderer : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");

        private class Ghost
        {
            public readonly List<Part> Parts = new List<Part>();
            public Color Color;
            public float SpawnTime;
            public float Lifetime;
        }

        private struct Part
        {
            public Mesh Mesh;
            public bool IsBaked;
            public int SubMeshCount;
            public Matrix4x4 Matrix;
        }

        private readonly List<Ghost> _ghosts = new List<Ghost>();
        private readonly Stack<Ghost> _ghostPool = new Stack<Ghost>();
        private readonly Stack<Mesh> _meshPool = new Stack<Mesh>();
        private readonly List<Renderer> _rendererBuffer = new List<Renderer>();
        private MaterialPropertyBlock _block;
        private Material _material;

        public void Initialize(Material material)
        {
            _material = material;
            _block = new MaterialPropertyBlock();
        }

        public void Capture(GameObject root, Color color, float lifetime)
        {
            if (_material == null || root == null) return;

            Ghost ghost = _ghostPool.Count > 0 ? _ghostPool.Pop() : new Ghost();
            ghost.Parts.Clear();
            ghost.Color = color;
            ghost.SpawnTime = Time.time;
            ghost.Lifetime = Mathf.Max(0.05f, lifetime);

            root.GetComponentsInChildren(false, _rendererBuffer);
            foreach (Renderer candidate in _rendererBuffer)
            {
                if (!HitReaction.IsCharacterRenderer(candidate)) continue;

                if (candidate is SkinnedMeshRenderer skinned)
                {
                    if (skinned.sharedMesh == null) continue;

                    Mesh baked = _meshPool.Count > 0 ? _meshPool.Pop() : new Mesh { name = "AfterimageBake" };
                    skinned.BakeMesh(baked, true);
                    Transform t = skinned.transform;
                    ghost.Parts.Add(new Part
                    {
                        Mesh = baked,
                        IsBaked = true,
                        SubMeshCount = baked.subMeshCount,
                        Matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one)
                    });
                }
                else if (candidate.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    ghost.Parts.Add(new Part
                    {
                        Mesh = filter.sharedMesh,
                        IsBaked = false,
                        SubMeshCount = filter.sharedMesh.subMeshCount,
                        Matrix = candidate.transform.localToWorldMatrix
                    });
                }
            }

            if (ghost.Parts.Count == 0)
            {
                _ghostPool.Push(ghost);
                return;
            }
            _ghosts.Add(ghost);
        }

        private void LateUpdate()
        {
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                Ghost ghost = _ghosts[i];
                float t = (Time.time - ghost.SpawnTime) / ghost.Lifetime;
                if (t >= 1f)
                {
                    Release(ghost);
                    _ghosts.RemoveAt(i);
                    continue;
                }

                _block.SetColor(BaseColorId, ghost.Color);
                _block.SetFloat(AlphaId, (1f - t) * (1f - t));
                var renderParams = new RenderParams(_material)
                {
                    matProps = _block,
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    receiveShadows = false
                };

                foreach (Part part in ghost.Parts)
                {
                    if (part.Mesh == null) continue;
                    for (int sub = 0; sub < part.SubMeshCount; sub++)
                    {
                        Graphics.RenderMesh(renderParams, part.Mesh, sub, part.Matrix);
                    }
                }
            }
        }

        private void Release(Ghost ghost)
        {
            foreach (Part part in ghost.Parts)
            {
                if (part.IsBaked && part.Mesh != null) _meshPool.Push(part.Mesh);
            }
            ghost.Parts.Clear();
            _ghostPool.Push(ghost);
        }

        private void OnDestroy()
        {
            foreach (Ghost ghost in _ghosts) Release(ghost);
            foreach (Mesh mesh in _meshPool) Destroy(mesh);
        }
    }
}
