// ════════════════════════════════════════════════════════════════
// INTAN ISLE — PHYSICAL WORLD SHADER
// Ghibli-Toon × Monet Light
//
// Technique: Cel/toon shading with 3-step diffuse ramp
//   Shadow / Midtone / Highlight — never harsh, never direct
// Zone colouring: locked palette, 14 zone types
//   Driven by _IntanIsle_ZoneType (set by ZoneShaderLinker)
// Day phase: warm saffron dawn → bright midday → amber-violet dusk
// ════════════════════════════════════════════════════════════════

Shader "Intan Isle/PhysicalWorldShader"
{
    Properties
    {
        [Header(Painterly Surface)]
        _NormalMap        ("Painterly Normal Map",    2D)    = "bump"  {}
        _NormalStrength   ("Normal Strength",  Range(0, 3))  = 1.2

        [Header(Toon Ramp)]
        _ShadowThresh     ("Shadow Threshold", Range(0, 1))  = 0.30
        _MidThresh        ("Mid Threshold",    Range(0, 1))  = 0.65
        _RampSmooth       ("Ramp Smoothness",  Range(0, 0.2))= 0.04
        _ShadowMult       ("Shadow Darkening", Range(0, 1))  = 0.42
        _MidMult          ("Midtone Mult",     Range(0, 1))  = 0.78

        [Header(Rim Light — Monet Edge Glow)]
        [HDR] _RimColor   ("Rim Color",               Color) = (0.95, 0.88, 0.75, 1)
        _RimPower         ("Rim Power",    Range(0.5, 8))    = 3.5
        _RimIntensity     ("Rim Intensity",Range(0,   1))    = 0.28

        [Header(Zone Blend)]
        _ZoneBlendDist    ("Zone Edge Blend (m)", Range(10, 2000)) = 500.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"          = "Opaque"
            "Queue"               = "Geometry"
            "RenderPipeline"      = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
        }

        // ── ForwardLit ───────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalMap_ST;
                float  _NormalStrength;
                float  _ShadowThresh;
                float  _MidThresh;
                float  _RampSmooth;
                float  _ShadowMult;
                float  _MidMult;
                half4  _RimColor;
                float  _RimPower;
                float  _RimIntensity;
                float  _ZoneBlendDist;
            CBUFFER_END

            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            // ── Global zone/day state ─────────────────────────────
            float _IntanIsle_ZoneType;
            float _IntanIsle_ZoneBlend;
            float _IntanIsle_PrevZoneType;
            float _IntanIsle_DayPhase;
            float _IntanIsle_TimeNorm;
            float _IntanIsle_IsVeiledWorld;

            // ════════════════════════════════════════════════════
            // LOCKED PALETTE — colour lookup by zone type
            // Returns (r,g,b) for the given zone index and
            // lighting variant v (0=shadow, 0.5=mid, 1=highlight).
            // ════════════════════════════════════════════════════

            half3 ZoneColour(float zi, float v)
            {
                // Base (shadow) and highlight colours per zone
                // Index matches ZoneType enum
                half3 shadow[14];
                half3 hilite[14];

                shadow[ 0] = half3(0.102, 0.239, 0.063); // FOREST shadow
                hilite[ 0] = half3(0.290, 0.549, 0.247); // FOREST hilite #4A8C3F

                shadow[ 1] = half3(0.063, 0.157, 0.031); // ANCIENT_FOREST shadow
                hilite[ 1] = half3(0.176, 0.353, 0.106); // ANCIENT_FOREST hilite #2D5A1B

                shadow[ 2] = half3(0.122, 0.259, 0.082); // PROTECTED_FOREST (+ orchid tint)
                hilite[ 2] = half3(0.345, 0.627, 0.290); // + orchid underpaint

                shadow[ 3] = half3(0.059, 0.200, 0.239); // WATERWAY shadow #1B4A5A
                hilite[ 3] = half3(0.251, 0.878, 0.816); // WATERWAY hilite #40E0D0

                shadow[ 4] = half3(0.082, 0.169, 0.082); // MANGROVE
                hilite[ 4] = half3(0.196, 0.349, 0.157);

                shadow[ 5] = half3(0.306, 0.247, 0.039); // KAMPUNG_HERITAGE #8B6914
                hilite[ 5] = half3(0.769, 0.631, 0.078);

                shadow[ 6] = half3(0.184, 0.298, 0.039); // FOOD_SECURITY
                hilite[ 6] = half3(0.420, 0.690, 0.188);

                shadow[ 7] = half3(0.427, 0.376, 0.275); // DEFORESTATION dust
                hilite[ 7] = half3(0.769, 0.659, 0.545); // pale dust #C4A882

                shadow[ 8] = half3(0.231, 0.267, 0.302); // CORAL_DEGRADATION grey-blue
                hilite[ 8] = half3(0.478, 0.549, 0.604);

                shadow[ 9] = half3(0.133, 0.133, 0.133); // POLLUTION ash #4A4A4A
                hilite[ 9] = half3(0.380, 0.200, 0.200); // + crimson tint

                shadow[10] = half3(0.102, 0.031, 0.157); // TRANSBOUNDARY_HAZE violet
                hilite[10] = half3(0.290, 0.055, 0.420); // deep violet #4A0E6B

                shadow[11] = half3(0.008, 0.008, 0.039); // RIVER_POLLUTION oil dark
                hilite[11] = half3(0.039, 0.039, 0.145); // very dark navy

                shadow[12] = half3(0.082, 0.196, 0.063); // SACRED_FOREST + gold shimmer
                hilite[12] = half3(0.290, 0.549, 0.247); // + warm gold at tips

                shadow[13] = half3(0.039, 0.039, 0.039); // TOXIC oil-black
                hilite[13] = half3(0.110, 0.020, 0.020); // + dark crimson

                int idx = (int)clamp(zi, 0, 13);
                half3 s  = shadow[idx];
                half3 h  = hilite[idx];
                return lerp(s, h, v);
            }

            // ── Day-phase colour tint ─────────────────────────────
            half3 DayTint(float phase, float timeNorm)
            {
                // Phase 0 = Dawn (saffron gold), 2 = Midday, 3 = Dusk (amber-violet), 5 = Veiled Night
                half3 dawn   = half3(0.957, 0.784, 0.259); // #F4C842
                half3 morn   = half3(0.900, 0.950, 0.870);
                half3 midday = half3(1.000, 0.980, 0.950);
                half3 dusk   = half3(0.870, 0.600, 0.730); // sarong amber-violet
                half3 night  = half3(0.550, 0.650, 0.900);
                half3 veil   = half3(0.250, 0.550, 0.450);

                half3 phases[6] = { dawn, morn, midday, dusk, night, veil };

                int   p0 = (int)clamp(floor(phase), 0, 5);
                int   p1 = (p0 + 1) % 6;
                float t  = frac(phase);
                return lerp(phases[p0], phases[p1], t);
            }

            // ─────────────────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 tangentWS  : TEXCOORD2;
                float3 bitangentWS: TEXCOORD3;
                float2 uv         : TEXCOORD4;
                float  fogFactor  : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionWS  = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS  = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                float3 tanWS    = TransformObjectToWorldDir(IN.tangentOS.xyz);
                OUT.tangentWS   = tanWS;
                OUT.bitangentWS = cross(OUT.normalWS, tanWS) * IN.tangentOS.w;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _NormalMap);
                OUT.fogFactor   = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // ── Normal map (painterly surface) ────────────────
                half3 normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uv),
                    _NormalStrength);
                float3x3 TBN    = float3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);
                float3   normalWS = normalize(mul(normalTS, TBN));

                // ── Zone colour ────────────────────────────────────
                half3 zoneCol = ZoneColour(_IntanIsle_ZoneType, 0.5);
                if (_IntanIsle_ZoneBlend > 0.01)
                {
                    half3 prevCol = ZoneColour(_IntanIsle_PrevZoneType, 0.5);
                    zoneCol = lerp(zoneCol, prevCol, saturate(_IntanIsle_ZoneBlend));
                }

                // Protected Forest: orchid purple undertone
                if ((int)_IntanIsle_ZoneType == 2)
                    zoneCol = lerp(zoneCol, half3(0.608, 0.349, 0.714), 0.18); // orchid #9B59B6

                // Sacred Forest: gold shimmer from above
                if ((int)_IntanIsle_ZoneType == 12)
                    zoneCol = lerp(zoneCol, half3(0.957, 0.784, 0.259), 0.25 * saturate(normalWS.y));

                // ── Toon diffuse ramp ──────────────────────────────
                Light mainLight = GetMainLight();
                float NdotL     = dot(normalWS, mainLight.direction);

                // 3-step ramp: shadow / midtone / highlight
                float step1 = smoothstep(_ShadowThresh - _RampSmooth, _ShadowThresh + _RampSmooth, NdotL);
                float step2 = smoothstep(_MidThresh    - _RampSmooth, _MidThresh    + _RampSmooth, NdotL);

                half3 shadow   = zoneCol * _ShadowMult;
                half3 midtone  = zoneCol * _MidMult;
                half3 highlight= zoneCol;

                half3 col = lerp(shadow, midtone,  step1);
                     col  = lerp(col,   highlight, step2);

                // Monet — never black shadows; ambient lift
                col = max(col, zoneCol * 0.25);

                // ── Light colour × mainLight ───────────────────────
                col *= mainLight.color;

                // ── Day phase tint (subtle atmospheric colour cast) ─
                half3 dayTint = DayTint(_IntanIsle_DayPhase, _IntanIsle_TimeNorm);
                col = lerp(col, col * dayTint, 0.35);

                // ── Soft rim light (Ghibli — edge glow) ──────────
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  rim     = pow(1.0 - saturate(dot(viewDir, normalWS)), _RimPower);
                col           += _RimColor.rgb * rim * _RimIntensity;

                // ── Fog ───────────────────────────────────────────
                col = MixFog(col, IN.fogFactor);

                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ── Shadow caster ─────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ── Depth only ────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
