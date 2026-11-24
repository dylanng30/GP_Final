// Màu phẳng thay tạm cho material của nhân vật trong vài khung hình khi trúng đòn.
Shader "Game/Feedback/HitFlash"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "HitFlash"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _RimPower;
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
                // Giữ chút khối bằng viền sáng, tránh nhân vật thành một mảng phẳng.
                half ndv = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half rim = pow(1.0h - ndv, _RimPower);
                half3 color = _BaseColor.rgb * (0.8h + 0.5h * rim);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }
}
