using UnityEngine;
using TMPro;
using DG.Tweening;
using Dylanng;

namespace Game.UIs.FloatingText
{
    public class DamageText : PoolableObject, ILateUpdatable
    {
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private float floatSpeed = 1f;
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private float aliveTime = 1f;

        public override void OnSpawn()
        {
            base.OnSpawn();
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Register(this);
            }
        }

        public override void OnDespawn()
        {
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
            }
            base.OnDespawn();
        }

        private DamageGradientSO currentGradientSO;
        private GradientDirection currentDirection;
        private float wordMinX, wordMaxX, wordMinY, wordMaxY;

        public void Setup(float damageAmount, DamageGradientSO gradientSO, bool overrideGlobal = false, GradientDirection globalDir = GradientDirection.Vertical, TextureMappingOptions hMap = TextureMappingOptions.Line, TextureMappingOptions vMap = TextureMappingOptions.Line)
        {
            if (damageText != null)
            {
                damageText.text = $"-{Mathf.RoundToInt(damageAmount).ToString()}";
                damageText.color = Color.white;
                damageText.alpha = 1f;

                // Tắt chế độ VertexGradient mặc định của TMP vì nó luôn lặp lại màu trên từng ký tự riêng lẻ
                damageText.enableVertexGradient = false;
                damageText.colorGradientPreset = null;

                // Cập nhật mesh ngay lập tức để tính toán đúng bounding box của cả cụm từ (-120)
                damageText.ForceMeshUpdate();

                if (gradientSO != null)
                {
                    currentGradientSO = gradientSO;
                    currentDirection = overrideGlobal ? globalDir : gradientSO.Direction;

                    CalculateWordBounds();
                    ApplyWordWideGradient(1f);
                }
                else
                {
                    currentGradientSO = null;
                }
            }

            AnimateAndDespawn();
        }

        public void SetupLabel(string text, Color color)
        {
            currentGradientSO = null;
            if (damageText != null)
            {
                damageText.text = text;
                damageText.enableVertexGradient = false;
                damageText.colorGradientPreset = null;
                damageText.color = color;
            }

            AnimateAndDespawn();
        }

        public void Setup(float damageAmount, Color color)
        {
            currentGradientSO = null;
            if (damageText != null)
            {
                damageText.text = $"-{Mathf.RoundToInt(damageAmount).ToString()}";
                damageText.enableVertexGradient = false;
                damageText.colorGradientPreset = null;
                damageText.color = color;
                
                // Reset alpha
                Color c = damageText.color;
                c.a = 1f;
                damageText.color = c;
            }

            AnimateAndDespawn();
        }

        private void CalculateWordBounds()
        {
            if (damageText == null || damageText.textInfo == null) return;
            var textInfo = damageText.textInfo;

            wordMinX = float.MaxValue;
            wordMaxX = float.MinValue;
            wordMinY = float.MaxValue;
            wordMaxY = float.MinValue;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                wordMinX = Mathf.Min(wordMinX, charInfo.vertex_BL.position.x, charInfo.vertex_TL.position.x);
                wordMaxX = Mathf.Max(wordMaxX, charInfo.vertex_TR.position.x, charInfo.vertex_BR.position.x);
                wordMinY = Mathf.Min(wordMinY, charInfo.vertex_BL.position.y, charInfo.vertex_BR.position.y);
                wordMaxY = Mathf.Max(wordMaxY, charInfo.vertex_TL.position.y, charInfo.vertex_TR.position.y);
            }

            if (wordMinX == float.MaxValue)
            {
                wordMinX = wordMaxX = wordMinY = wordMaxY = 0f;
            }
        }

        private void ApplyWordWideGradient(float alpha)
        {
            if (damageText == null || damageText.textInfo == null || currentGradientSO == null) return;
            var textInfo = damageText.textInfo;

            float width = wordMaxX - wordMinX;
            float height = wordMaxY - wordMinY;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                var colors32 = textInfo.meshInfo[materialIndex].colors32;

                for (int v = 0; v < 4; v++)
                {
                    Vector3 pos = textInfo.meshInfo[materialIndex].vertices[vertexIndex + v];
                    float tX = (width > 0.0001f) ? Mathf.Clamp01((pos.x - wordMinX) / width) : 0f;
                    float tY = (height > 0.0001f) ? Mathf.Clamp01((pos.y - wordMinY) / height) : 0f;

                    Color c = GetInterpolatedColor(tX, tY);
                    c.a *= alpha;
                    colors32[vertexIndex + v] = (Color32)c;
                }
            }

            damageText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private Color GetInterpolatedColor(float tX, float tY)
        {
            switch (currentDirection)
            {
                case GradientDirection.Horizontal:
                    return Color.Lerp(currentGradientSO.TopOrLeftColor, currentGradientSO.BottomOrRightColor, tX);
                case GradientDirection.Vertical:
                    return Color.Lerp(currentGradientSO.BottomOrRightColor, currentGradientSO.TopOrLeftColor, tY);
                case GradientDirection.FourCorners:
                    Color bottomLerp = Color.Lerp(currentGradientSO.BottomLeftColor, currentGradientSO.BottomRightColor, tX);
                    Color topLerp = Color.Lerp(currentGradientSO.TopLeftColor, currentGradientSO.TopRightColor, tX);
                    return Color.Lerp(bottomLerp, topLerp, tY);
                case GradientDirection.SingleColor:
                default:
                    return currentGradientSO.TopOrLeftColor;
            }
        }

        private void AnimateAndDespawn()
        {
            // Move upward
            transform.DOMoveY(transform.position.y + floatSpeed, aliveTime).SetEase(Ease.OutCubic);
 
            // Scale up slightly on spawn
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
 
            // Fade out near the end of lifetime
            if (damageText != null)
            {
                if (currentGradientSO != null)
                {
                    DOVirtual.Float(1f, 0f, fadeDuration, (alpha) =>
                    {
                        ApplyWordWideGradient(alpha);
                    }).SetDelay(aliveTime - fadeDuration);
                }
                else
                {
                    damageText.DOFade(0f, fadeDuration).SetDelay(aliveTime - fadeDuration);
                }
            }
 
            // Return to pool when finished
            DOVirtual.DelayedCall(aliveTime, () =>
            {
                ReturnToPool();
            });
        }

        private void ReturnToPool()
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                poolManager.Despawn("DamageText", this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void OnLateUpdate(float deltaTime)
        {
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            transform.DOKill();
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
            }
        }
    }
}
