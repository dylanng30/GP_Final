// Vòng cảnh báo dưới đất cho skill diện rộng. _Progress chạy từ 0 đến 1 trong thời gian cast.
Shader "Game/Feedback/GroundTelegraph"
{
    Properties
    {
        _BaseColor ("Edge Color", Color) = (1, 0.25, 0.2, 1)
        _FillColor ("Fill Color", Color) = (1, 0.35, 0.25, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0.01, 0.3)) = 0.06
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.15
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Telegraph"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _FillColor;
                half _Progress;
                half _EdgeWidth;
                half _BaseAlpha;
                half _Fade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float r = length(input.uv * 2.0 - 1.0);
                clip(1.0 - r);

                half edge = smoothstep(1.0 - _EdgeWidth - 0.03, 1.0 - _EdgeWidth, r);
                half inside = 1.0h - smoothstep(_Progress - 0.015, _Progress, r);
                half front = smoothstep(_Progress - 0.08, _Progress, r) * inside;
                half pulse = 0.75h + 0.25h * sin(_Time.y * 18.0);

                half alpha = _BaseAlpha + inside * 0.22h + front * 0.45h + edge * 0.85h * pulse;
                half3 color = lerp(_FillColor.rgb, _BaseColor.rgb, saturate(edge + front));
                return half4(color, saturate(alpha) * _BaseColor.a * _Fade);
            }
            ENDHLSL
        }
    }
}
