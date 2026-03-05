// ════════════════════════════════════════════════════════════════
// INTAN ISLE — VEILED WORLD SHADER
// Avatar Bioluminescence (restrained) × TeamLab Living Light
//
// Design law:
//   - Glow on VEIN LINES only, not entire surface
//   - Glow = communication, not decoration
//   - If it feels like a nightclub, reduce by 70%
//   - Restraint. Silence as mechanic.
//
// Zone-specific:
//   ANCIENT_FOREST  : golden spores emission, soft
//   PROTECTED_FOREST: white spirit shimmer, barely visible
//   SACRED_FOREST   : warm white light shafts (emission upward)
//   KAMPUNG_HERITAGE: warm amber lantern glow
//   POLLUTION/TOXIC : cracked dark crimson tendrils from ground
//   TRANSBOUNDARY   : thick violet-grey, fog-like opacity increase
//
// Atmosphere:
//   Base sky/fog: midnight teal #0A1F1A
//   Bloom: threshold 0.8, intensity 1.2
//   Chromatic aberration: 0.3 (subtle, not painful)
// ════════════════════════════════════════════════════════════════

Shader "Intan Isle/VeiledWorldShader"
{
    Properties
    {
        [Header(Surface)]
        _MainTex          ("Base Texture",            2D)    = "white" {}
        _VeinMask         ("Vein Mask (R=vein lines)",2D)    = "black" {}
        _NormalMap        ("Normal Map",              2D)    = "bump"  {}

        [Header(Bioluminescence)]
        [HDR] _BioLumColor("Vein Glow Colour",       Color) = (0.0, 1.0, 0.533, 1)  // #00FF88
        _BioLumIntensity  ("Vein Intensity", Range(0, 3))   = 1.2
        _BioLumPulseSpeed ("Pulse Speed",   Range(0, 4))    = 0.8

        [Header(Spore Particles — driven by particle system)]
        // Actual spores = separate ParticleSystem using this shader as override

        [Header(Zone Effects)]
        _CrimsonTendril   ("Crimson Tendril (TOXIC)", Range(0, 1)) = 0.0
        _VioletFog        ("Violet Fog (HAZE)",       Range(0, 1)) = 0.0

        [Header(Stained Glass — Islamic Geometric Light)]
        _StainedGlassCol  ("Stained Glass Tint",      Color) = (0.957, 0.784, 0.259, 0.4) // saffron
        _StainedGlassMask ("Canopy Density (stained-glass driver)", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"          = "Transparent"
            "Queue"               = "Transparent"
            "RenderPipeline"      = "UniversalPipeline"
            "UniversalMaterialType" = "Unlit"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        // ── ForwardLit ───────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _VeinMask_ST;
                half4  _BioLumColor;
                float  _BioLumIntensity;
                float  _BioLumPulseSpeed;
                float  _CrimsonTendril;
                float  _VioletFog;
                half4  _StainedGlassCol;
                float  _StainedGlassMask;
            CBUFFER_END

            TEXTURE2D(_MainTex);   SAMPLER(sampler_MainTex);
            TEXTURE2D(_VeinMask);  SAMPLER(sampler_VeinMask);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);

            // ── Global state ──────────────────────────────────────
            float _IntanIsle_ZoneType;
            float _IntanIsle_DayPhase;
            float _IntanIsle_BioLum;
            float _IntanIsle_BioLumIntensity;
            float _IntanIsle_VeilDissolve;
            float _Time_x; // unity built-in _Time.x

            // ── Stained-glass geometric pattern ───────────────────
            // Generates soft hexagonal / arabesque light patch on ground
            // based on world XZ position. Slow-moving with virtual wind.
            float StainedGlassPattern(float2 worldXZ, float time)
            {
                // Hexagonal tiling: |cos(x*f) * cos(z*f) * cos((x+z)*f)|
                float2 p  = worldXZ * 0.04 + float2(sin(time * 0.05), cos(time * 0.04)) * 2.0;
                float hex = abs(cos(p.x) * cos(p.y) * cos((p.x + p.y) * 0.866));
                // Star pattern overlay
                float star = abs(sin(p.x * 2.0) * sin(p.y * 2.0));
                float pattern = saturate(hex * 0.6 + star * 0.4);
                // Only visible under dense canopy
                return pattern * _StainedGlassMask;
            }

            // ─────────────────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float  time    = _Time.x;
                int    zone    = (int)clamp(_IntanIsle_ZoneType, 0, 13);
                bool   healthy = zone <= 6;

                // ── Base texture ──────────────────────────────────
                half4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // ── Vein mask bioluminescence (VEIN LINES ONLY) ───
                float veinMask = SAMPLE_TEXTURE2D(_VeinMask, sampler_VeinMask, IN.uv).r;
                float pulse    = 0.7 + 0.3 * sin(time * _BioLumPulseSpeed + IN.positionWS.y * 0.1);
                float bioGlow  = veinMask * pulse * _BioLumIntensity
                               * _IntanIsle_BioLumIntensity       // day/night from EmotionalDayNight
                               * _IntanIsle_BioLum;               // enabled only night/veiled

                // Wounded zones: veins are darker, break signal
                if (!healthy)
                    bioGlow *= 0.25 * (0.5 + 0.5 * sin(time * 6.0 + IN.positionWS.x)); // flicker

                half3 bioColor = _BioLumColor.rgb;
                if (!healthy) bioColor = half3(0.4, 0.0, 0.0); // crimson in toxic zones

                // ── Zone-specific emission layers ─────────────────

                // ANCIENT_FOREST: golden light spores (upward emission from surface normals)
                float goldenSpores = 0.0;
                if (zone == 1) // ANCIENT_FOREST
                    goldenSpores = saturate(IN.normalWS.y) * 0.3
                                 * (0.5 + 0.5 * sin(time * 0.3 + IN.positionWS.x * 0.02));

                // SACRED_FOREST: warm white light shafts
                float sacredShaft = 0.0;
                if (zone == 12) // SACRED_FOREST
                    sacredShaft = saturate(IN.normalWS.y) * 0.4
                                * (0.8 + 0.2 * sin(time * 0.2));

                // KAMPUNG_HERITAGE: warm amber lantern glow on structures
                float kampungGlow = 0.0;
                if (zone == 5) // KAMPUNG_HERITAGE
                    kampungGlow = 0.25 * (0.7 + 0.3 * sin(time * 1.5));

                // TOXIC/POLLUTION: dark crimson tendrils from ground
                float tendrilGlow = 0.0;
                if (zone >= 9) // POLLUTION and above
                    tendrilGlow = (1.0 - saturate(IN.positionWS.y * 0.1))  // strongest at ground
                                * 0.15
                                * (0.5 + 0.5 * sin(time * 0.8 + IN.positionWS.xz.x * 0.05));

                // ── Stained-glass ground light (Islamic geometric) ─
                float sgPattern = StainedGlassPattern(IN.positionWS.xz, time);
                // Only on near-horizontal surfaces (ground)
                float groundFactor = saturate(dot(IN.normalWS, float3(0,1,0)));
                half3 sgColor = half3(0, 0, 0);
                // Rotate through: saffron, jade, orchid, turquoise
                float rot = frac(time * 0.05 + IN.positionWS.x * 0.0001);
                if      (rot < 0.25) sgColor = half3(0.957, 0.784, 0.259) * 0.5; // saffron
                else if (rot < 0.50) sgColor = half3(0.290, 0.549, 0.247) * 0.5; // jade
                else if (rot < 0.75) sgColor = half3(0.608, 0.349, 0.714) * 0.5; // orchid
                else                 sgColor = half3(0.251, 0.878, 0.816) * 0.5; // turquoise
                half3 sgContrib = sgColor * sgPattern * groundFactor * 0.6;

                // ── Atmosphere: midnight teal base ────────────────
                half3 atmBase = half3(0.039, 0.122, 0.102); // #0A1F1A

                // ── Compose final colour ──────────────────────────
                half3 col = baseTex.rgb * 0.3 + atmBase * 0.4; // dark, atmospheric

                // Bioluminescent vein overlay
                col += bioColor * bioGlow;

                // Zone-specific contributions
                col += half3(0.957, 0.784, 0.259) * goldenSpores; // golden spores
                col += half3(0.940, 0.972, 1.000) * sacredShaft;  // sacred light shafts
                col += half3(1.000, 0.549, 0.000) * kampungGlow;  // amber lantern #FF8C00
                col += half3(0.545, 0.000, 0.000) * tendrilGlow;  // crimson tendrils

                // Stained-glass ground light
                col += sgContrib;

                // ── TRANSBOUNDARY_HAZE: violet fog density ────────
                if (zone == 10)
                {
                    half3 hazeCol = half3(0.290, 0.055, 0.420); // #4A0E6B
                    col = lerp(col, hazeCol, 0.6);
                }

                // ── Dissolve alpha — VeiledWorld fade-in ─────────
                float baseAlpha = healthy ? 0.82f : 0.65f;
                float alpha     = baseAlpha * saturate(_IntanIsle_VeilDissolve);

                // ── Fog ───────────────────────────────────────────
                // Veiled world uses deep teal fog — handled in RenderSettings by EmotionalDayNight
                col = MixFog(col, IN.fogFactor);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
