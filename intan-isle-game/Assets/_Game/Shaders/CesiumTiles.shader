// Custom Cesium tile shader for URP 14 (Unity 2022.3 LTS).
// Replaces CesiumDefaultTilesetShader.shadergraph which has
// compatibility issues with URP 14.0.12.
//
// Cesium's tile loader sets _baseColorTexture at runtime.
// This shader reads that property and renders via standard
// URP unlit path with vertex colors.

Shader "Intan Isle/CesiumTiles"
{
    Properties
    {
        _baseColorTexture ("Base Color Texture", 2D) = "white" {}
        _baseColorFactor ("Base Color Factor", Color) = (1, 1, 1, 1)
        _overlayTexture_0 ("Overlay 0", 2D) = "black" {}
        _overlayTranslationAndScale_0 ("Overlay 0 TS", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "CesiumTilesForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_baseColorTexture);
            SAMPLER(sampler_baseColorTexture);
            float4 _baseColorTexture_ST;

            TEXTURE2D(_overlayTexture_0);
            SAMPLER(sampler_overlayTexture_0);
            float4 _overlayTranslationAndScale_0;

            CBUFFER_START(UnityPerMaterial)
                half4 _baseColorFactor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvOverlay : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
                float4 color : COLOR;
                float fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _baseColorTexture);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                // Overlay UV: apply translation and scale
                output.uvOverlay = input.uv1 * _overlayTranslationAndScale_0.zw
                                 + _overlayTranslationAndScale_0.xy;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample base color texture (set by Cesium at runtime)
                half4 baseColor = SAMPLE_TEXTURE2D(_baseColorTexture, sampler_baseColorTexture, input.uv);
                baseColor *= _baseColorFactor;
                baseColor *= input.color;

                // Blend overlay if present (e.g. Bing Maps imagery)
                half4 overlay = SAMPLE_TEXTURE2D(_overlayTexture_0, sampler_overlayTexture_0, input.uvOverlay);
                baseColor.rgb = lerp(baseColor.rgb, overlay.rgb, overlay.a);

                // Simple directional lighting
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 lighting = mainLight.color * NdotL + half3(0.15, 0.15, 0.18);

                half3 finalColor = baseColor.rgb * lighting;
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass (no Shadows.hlsl — avoids LerpWhiteTo in URP 14)
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Depth only pass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
