// Elementia/ToonLit - cel-shaded character shader for URP.
// Same flat palette look as Dylanng/Unlit/Texture, but it reacts to the scene:
// main light colour + direction (hard toon band), received shadows, ambient (SH) light, rim light and fog.
Shader "Elementia/ToonLit"
{
    Properties
    {
        _BaseMap ("Base Map (palette)", 2D) = "white" {}
        [HDR] _BaseColor ("Tint", Color) = (1,1,1,1)
        [Header(Shading)]
        _Toonness ("Toonness (0 = same as map, 1 = hard cel band)", Range(0, 1)) = 0.2
        _ShadowColor ("Shadow Tint", Color) = (0.68, 0.64, 0.82, 1)
        _ShadowTintStrength ("Shadow Tint Strength", Range(0, 1)) = 0.35
        _ShadowThreshold ("Shadow Threshold", Range(-1, 1)) = 0.0
        _ShadowSoftness ("Band Softness", Range(0.001, 0.5)) = 0.05
        _ReceivedShadowStrength ("Received Shadow Strength", Range(0, 1)) = 0.85
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 1.0
        _LightIntensityClamp ("Max Light Intensity", Range(0.5, 3)) = 1.3
        [Header(Rim)]
        _RimColor ("Rim Color", Color) = (1, 0.95, 0.9, 1)
        _RimPower ("Rim Power", Range(1, 10)) = 4
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.25
        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Int) = 2
        // used by the URP Lit depth / shadow passes
        [HideInInspector] _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShadowColor;
                half _ShadowThreshold;
                half _ShadowSoftness;
                half _ReceivedShadowStrength;
                half _Toonness;
                half _ShadowTintStrength;
                half _AmbientStrength;
                half _LightIntensityClamp;
                half4 _RimColor;
                half _RimPower;
                half _RimStrength;
                half _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = nrm.normalWS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 N = normalize(input.normalWS);
                float3 V = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 lightCol = mainLight.color;
                half lum = max(max(lightCol.r, lightCol.g), lightCol.b);
                lightCol *= min(1.0h, _LightIntensityClamp / max(lum, 1e-3h));   // keep the palette from blowing out

                // Diffuse = same Lambert model as the URP Lit map materials, optionally pushed toward a toon band.
                half ndl = dot(N, mainLight.direction);
                half lambert = saturate(ndl);
                half band = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl);
                half shadow = lerp(1.0h, mainLight.shadowAttenuation, _ReceivedShadowStrength);
                half diff = lerp(lambert, band, _Toonness) * shadow;

                half3 ambient = max(SampleSH(N), 0.0h) * _AmbientStrength;
                // shadow tint only colours the unlit side (subtle, keeps the stylized feel)
                half3 tint = lerp(half3(1, 1, 1), _ShadowColor.rgb, (1.0h - diff) * _ShadowTintStrength);
                half3 col = albedo.rgb * (lightCol * diff + ambient * tint);

                half rim = pow(1.0h - saturate(dot(N, V)), _RimPower) * _RimStrength;
                col += rim * _RimColor.rgb * lightCol * diff;

                col = MixFog(col, input.fogFactor);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack "Universal Render Pipeline/Unlit"
}
