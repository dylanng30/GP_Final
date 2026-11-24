// Phát sáng cộng màu theo viền (fresnel). Dùng cho bóng mờ khi dash/vung kiếm và viền ánh kim khi biến hình.
Shader "Game/Feedback/FresnelGlow"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 0.85, 0.4, 1)
        _Power ("Rim Power", Range(0.5, 8)) = 2.5
        _Intensity ("Intensity", Range(0, 6)) = 1.6
        _InnerAlpha ("Inner Alpha", Range(0, 1)) = 0.15
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "FresnelGlow"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Power;
                half _Intensity;
                half _InnerAlpha;
                half _Alpha;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half ndv = saturate(abs(dot(normalize(input.normalWS), normalize(input.viewDirWS))));
                half rim = pow(1.0h - ndv, _Power);
                half glow = (_InnerAlpha + rim) * _Intensity * _Alpha * _BaseColor.a;
                return half4(_BaseColor.rgb * glow, 0.0h);
            }
            ENDHLSL
        }
    }
}
