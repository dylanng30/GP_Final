// Vệt sáng cộng màu cho TrailRenderer khi dash. Màu lấy từ vertex color của trail nhân với _BaseColor.
Shader "Game/Feedback/AdditiveTrail"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Trail"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Mờ dần ở hai mép bề rộng của vệt.
                half across = 1.0h - abs(input.uv.y * 2.0h - 1.0h);
                half alpha = input.color.a * _BaseColor.a * across * across;
                return half4(input.color.rgb * _BaseColor.rgb * _Intensity * alpha, 0.0h);
            }
            ENDHLSL
        }
    }
}
