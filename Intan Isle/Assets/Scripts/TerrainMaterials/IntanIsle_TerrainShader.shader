// IntanIsle_TerrainShader
// URP HLSL shader for Cesium World Terrain (opaqueMaterial).
//
// Zone-based coloring driven by world-space XZ position:
//   — Emerald green  #2D5A1B  for clean forest / vegetation areas
//   — Deep violet    #4A0E6B  for pollution / industrial zones
//   — Crimson fringe #8B0000  bleeds into violet at zone edges
//
// Zone centre positions are pre-computed as Unity world-space offsets
// from the CesiumGeoreference origin (Singapore, 1.3521°N 103.8198°E).
// 1 Unity unit ≈ 1 m at sea level near Singapore.
//
// Zone offsets (east = +X, north = +Z):
//   [0] JurongIsland     (-16623, -9668)   r = 5 km
//   [1] Tuas             (-22061, -6227)   r = 5 km
//   [2] SouthernIslands  (  2241,-15773)   r = 8 km
//   [3] SungeiKadut      ( -7749,  6427)   r = 5 km
//   [4] CameronHighlands (-270791,346086)  r = 50 km
//   [5] RiauCorridor     (-13293,  -233)   r = 10 km
//
// Usage:
//   Assign as Cesium3DTileset.opaqueMaterial OR as a regular renderer material.
//   Run Tools > Intan Isle > Create Terrain Material to auto-generate the .mat.

Shader "Intan Isle/IntanIsle_TerrainShader"
{
    Properties
    {
        // Base colors
        [HDR] _ForestColor      ("Forest / Vegetation Color",   Color) = (0.176, 0.353, 0.106, 1)  // #2D5A1B
        [HDR] _PollutionColor   ("Pollution Zone Color",        Color) = (0.290, 0.055, 0.420, 1)  // #4A0E6B
        [HDR] _CrimsonEdge      ("Crimson Edge Color",          Color) = (0.545, 0.0,   0.0,   1)  // #8B0000
        [HDR] _OceanColor       ("Ocean / Low-area Color",      Color) = (0.020, 0.090, 0.200, 1)  // deep navy

        // Zone controls (expose so you can fine-tune in Inspector)
        _ZoneBlendSharpness     ("Zone Blend Sharpness",        Range(0.0001, 0.01)) = 0.001
        _ForestBaseHeight       ("Elevation above which = forest (m)", Float) = 10.0

        // Noise for organic edges
        _NoiseScale             ("Edge Noise Scale",            Float) = 0.0005
        _NoiseStrength          ("Edge Noise Strength",         Range(0, 2000)) = 800.0

        // Optional: overlay Bing Maps tint (0 = pure zone color, 1 = only satellite)
        _SatelliteBlend         ("Satellite Blend",             Range(0, 1)) = 0.0
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

        // ── Forward Lit pass ─────────────────────────────────────────────
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

            // ── Shader properties ─────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4  _ForestColor;
                half4  _PollutionColor;
                half4  _CrimsonEdge;
                half4  _OceanColor;
                float  _ZoneBlendSharpness;
                float  _ForestBaseHeight;
                float  _NoiseScale;
                float  _NoiseStrength;
                float  _SatelliteBlend;
            CBUFFER_END

            // ── Struct definitions ────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Simple value noise (2D) ───────────────────────────────────
            // Used to soften zone edges so they don't look like hard circles.
            float Hash(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep

                return lerp(lerp(Hash(i + float2(0,0)), Hash(i + float2(1,0)), f.x),
                            lerp(Hash(i + float2(0,1)), Hash(i + float2(1,1)), f.x),
                            f.y);
            }

            // ── Zone influence (0 = outside, 1 = fully inside) ───────────
            // centre.xy = (worldX, worldZ), centre.z = radius, centre.w = max influence
            float ZoneInfluence(float2 worldXZ, float4 centre, float noiseOffset)
            {
                float dist   = length(worldXZ - centre.xy);
                float edge   = dist - centre.z + noiseOffset;
                float t      = 1.0 - saturate(edge * _ZoneBlendSharpness);
                return t * centre.w;
            }

            // ── Vertex ────────────────────────────────────────────────────
            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);
                return OUT;
            }

            // ── Fragment ──────────────────────────────────────────────────
            half4 Frag(Varyings IN) : SV_Target
            {
                float2 xz = IN.positionWS.xz;

                // Noise for organic zone edges (avoids perfect circles)
                float n = (ValueNoise(xz * _NoiseScale) - 0.5) * _NoiseStrength;

                // ── Zone centre positions (world-space XZ) ────────────────
                // (x = east offset m, y = north offset m, z = radius m, w = influence 0-1)
                float4 zJurongIsland     = float4(-16623.0, -9668.0,  5000.0,  1.0);
                float4 zTuas             = float4(-22061.0, -6227.0,  5000.0,  1.0);
                float4 zSouthernIslands  = float4(  2241.0,-15773.0,  8000.0,  0.9);
                float4 zSungeiKadut      = float4( -7749.0,  6427.0,  5000.0,  1.0);
                float4 zCameronHighlands = float4(-270791.0,346086.0,50000.0,  0.8);
                float4 zRiauCorridor     = float4(-13293.0,  -233.0, 10000.0,  1.0);

                // ── Accumulate total pollution influence ──────────────────
                float p = 0.0;
                p = max(p, ZoneInfluence(xz, zJurongIsland,     n));
                p = max(p, ZoneInfluence(xz, zTuas,             n));
                p = max(p, ZoneInfluence(xz, zSouthernIslands,  n));
                p = max(p, ZoneInfluence(xz, zSungeiKadut,      n));
                p = max(p, ZoneInfluence(xz, zCameronHighlands, n));
                p = max(p, ZoneInfluence(xz, zRiauCorridor,     n));
                p = saturate(p);

                // ── Height-based base: ocean vs forest ────────────────────
                float belowSea = saturate((_ForestBaseHeight - IN.positionWS.y) * 0.1);
                half4 baseColor = lerp(_ForestColor, _OceanColor, belowSea);

                // ── Crimson at zone edge (p ~0.3-0.7), violet at core ─────
                half4 crimsonBlend = lerp(baseColor,      _CrimsonEdge,   saturate(p * 2.0));
                half4 violetBlend  = lerp(crimsonBlend,   _PollutionColor, saturate((p - 0.5) * 2.0));
                half4 zoneColor    = violetBlend;

                // ── Simple Lambert lighting ───────────────────────────────
                float3 normalWS   = normalize(IN.normalWS);
                Light mainLight   = GetMainLight();
                float NdotL       = saturate(dot(normalWS, mainLight.direction));
                float diffuse     = NdotL * 0.7 + 0.3; // min 0.3 ambient

                half4 litColor    = half4(zoneColor.rgb * diffuse * mainLight.color, 1.0);

                // ── Fog ───────────────────────────────────────────────────
                litColor.rgb      = MixFog(litColor.rgb, IN.fogFactor);

                return litColor;
            }
            ENDHLSL
        }

        // ── Shadow caster pass ────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ── Depth-only pass ───────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
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
    CustomEditor "UnityEditor.ShaderGraphUI"
}
