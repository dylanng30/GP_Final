using Dylanng;
using TMPro;
using UnityEngine;

namespace Game.UIs.FloatingText
{
    public enum GradientDirection
    {
        Vertical = 0,
        Horizontal = 1,
        FourCorners = 2,
        SingleColor = 3
    }

    [CreateAssetMenu(fileName = "DamageGradient_New", menuName = "Dylanng/UIs/Damage Gradient Data")]
    public class DamageGradientSO : ScriptableData
    {
        [Header("--- Gradient Mode & Mapping ---")]
        public GradientDirection Direction = GradientDirection.Vertical;
        [Tooltip("Line giúp gradient theo toàn bộ chữ (tránh bị lặp màu từng ký tự)")]
        public TextureMappingOptions HorizontalMapping = TextureMappingOptions.Line;
        public TextureMappingOptions VerticalMapping = TextureMappingOptions.Line;

        [Header("--- Vertical / Horizontal Colors ---")]
        [Tooltip("Màu nửa trên (nếu Dọc) hoặc Màu bên trái (nếu Ngang)")]
        public Color TopOrLeftColor = Color.white;

        [Tooltip("Màu nửa dưới (nếu Dọc) hoặc Màu bên phải (nếu Ngang)")]
        public Color BottomOrRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Header("--- Four Corners Mode (Tùy chọn nếu dùng FourCorners) ---")]
        public Color TopLeftColor = Color.white;
        public Color TopRightColor = Color.white;
        public Color BottomLeftColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        public Color BottomRightColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        public VertexGradient GetVertexGradient()
        {
            switch (Direction)
            {
                case GradientDirection.Vertical:
                    return new VertexGradient(TopOrLeftColor, TopOrLeftColor, BottomOrRightColor, BottomOrRightColor);
                case GradientDirection.Horizontal:
                    return new VertexGradient(TopOrLeftColor, BottomOrRightColor, TopOrLeftColor, BottomOrRightColor);
                case GradientDirection.FourCorners:
                    return new VertexGradient(TopLeftColor, TopRightColor, BottomLeftColor, BottomRightColor);
                case GradientDirection.SingleColor:
                default:
                    return new VertexGradient(TopOrLeftColor, TopOrLeftColor, TopOrLeftColor, TopOrLeftColor);
            }
        }

        public VertexGradient GetVertexGradientDynamic(GradientDirection overrideDirection)
        {
            switch (overrideDirection)
            {
                case GradientDirection.Vertical:
                    return new VertexGradient(TopOrLeftColor, TopOrLeftColor, BottomOrRightColor, BottomOrRightColor);
                case GradientDirection.Horizontal:
                    return new VertexGradient(TopOrLeftColor, BottomOrRightColor, TopOrLeftColor, BottomOrRightColor);
                case GradientDirection.FourCorners:
                    return new VertexGradient(TopLeftColor, TopRightColor, BottomLeftColor, BottomRightColor);
                case GradientDirection.SingleColor:
                default:
                    return new VertexGradient(TopOrLeftColor, TopOrLeftColor, TopOrLeftColor, TopOrLeftColor);
            }
        }

        private TMP_ColorGradient cachedPreset;

        public TMP_ColorGradient GetTMPColorGradient()
        {
            return GetTMPColorGradientDynamic(Direction);
        }

        public TMP_ColorGradient GetTMPColorGradientDynamic(GradientDirection overrideDirection)
        {
            if (cachedPreset == null)
            {
                cachedPreset = ScriptableObject.CreateInstance<TMP_ColorGradient>();
            }

            switch (overrideDirection)
            {
                case GradientDirection.Vertical:
                    cachedPreset.colorMode = ColorMode.VerticalGradient;
                    cachedPreset.topLeft = TopOrLeftColor;
                    cachedPreset.topRight = TopOrLeftColor;
                    cachedPreset.bottomLeft = BottomOrRightColor;
                    cachedPreset.bottomRight = BottomOrRightColor;
                    break;
                case GradientDirection.Horizontal:
                    cachedPreset.colorMode = ColorMode.HorizontalGradient;
                    cachedPreset.topLeft = TopOrLeftColor;
                    cachedPreset.bottomLeft = TopOrLeftColor;
                    cachedPreset.topRight = BottomOrRightColor;
                    cachedPreset.bottomRight = BottomOrRightColor;
                    break;
                case GradientDirection.FourCorners:
                    cachedPreset.colorMode = ColorMode.FourCornersGradient;
                    cachedPreset.topLeft = TopLeftColor;
                    cachedPreset.topRight = TopRightColor;
                    cachedPreset.bottomLeft = BottomLeftColor;
                    cachedPreset.bottomRight = BottomRightColor;
                    break;
                case GradientDirection.SingleColor:
                default:
                    cachedPreset.colorMode = ColorMode.Single;
                    cachedPreset.topLeft = TopOrLeftColor;
                    cachedPreset.topRight = TopOrLeftColor;
                    cachedPreset.bottomLeft = TopOrLeftColor;
                    cachedPreset.bottomRight = TopOrLeftColor;
                    break;
            }

            return cachedPreset;
        }

        public void SetColors(Color topOrLeft, Color bottomOrRight, GradientDirection dir = GradientDirection.Vertical)
        {
            Direction = dir;
            TopOrLeftColor = topOrLeft;
            BottomOrRightColor = bottomOrRight;
            TopLeftColor = topOrLeft;
            TopRightColor = topOrLeft;
            BottomLeftColor = bottomOrRight;
            BottomRightColor = bottomOrRight;
        }
    }
}
