using System.IO;
using CesiumForUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Intan Isle > Setup Cesium World
///
/// 1. Configures CesiumGeoreference origin to Singapore (1.3521°N, 103.8198°E).
/// 2. Creates Cesium World Terrain (Ion asset 1) named "Intan_Isle_CesiumTerrain".
/// 3. Adds Bing Maps Aerial via Cesium Ion (Ion asset 2) as raster overlay.
/// 4. Assigns IntanIsle_TerrainShader material for zone-based coloring.
/// 5. Configures 500 km streaming coverage.
/// 6. Creates 6 VeilStrainZone trigger volumes at real-world coordinates.
/// </summary>
public static class CesiumSetup
{
    // ── Singapore origin ──────────────────────────────────────────────────
    private const double SG_Lat = 1.3521;
    private const double SG_Lon = 103.8198;

    // ── Cesium Ion asset IDs ──────────────────────────────────────────────
    private const long ASSET_WORLD_TERRAIN = 1;   // Cesium World Terrain
    private const long ASSET_BING_AERIAL   = 2;   // Bing Maps Aerial (via Ion)

    // ── Tileset name ──────────────────────────────────────────────────────
    private const string TILESET_NAME = "Intan_Isle_CesiumTerrain";

    // ── Material path ─────────────────────────────────────────────────────
    private const string TERRAIN_MAT = "Assets/Scripts/TerrainMaterials/IntanIsle_Terrain.mat";

    // ── Volume profile output path ────────────────────────────────────────
    private const string PROFILE_DIR = "Assets/Scripts/VolumeProfiles/";

    // ── Zone box size in Unity world units (approx metres at CesiumGeoreference) ─
    private static readonly Vector3 ZONE_BOX = new Vector3(2000f, 600f, 2000f);

    // ─────────────────────────────────────────────────────────────────────
    // Zone definitions: (name, latitude, longitude, description)
    // ─────────────────────────────────────────────────────────────────────
    private struct ZoneDef
    {
        public string name;
        public double lat;
        public double lon;
        public string description;
    }

    private static ZoneDef[] Zones = new ZoneDef[]
    {
        new ZoneDef { name = "JurongIsland",     lat =  1.265, lon = 103.670, description = "Industrial haze zone" },
        new ZoneDef { name = "Tuas",             lat =  1.296, lon = 103.621, description = "Waste / incineration zone" },
        new ZoneDef { name = "SouthernIslands",  lat =  1.210, lon = 103.840, description = "Coral degradation zone" },
        new ZoneDef { name = "SungeiKadut",      lat =  1.410, lon = 103.750, description = "River pollution zone" },
        new ZoneDef { name = "CameronHighlands", lat =  4.470, lon = 101.380, description = "Deforestation zone" },
        new ZoneDef { name = "RiauCorridor",     lat =  1.350, lon = 103.700, description = "Transboundary haze entry point" },
    };

    // ─────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Intan Isle/Setup Cesium World")]
    public static void SetupCesiumWorld()
    {
        // ── 1. CesiumGeoreference ─────────────────────────────────────────
        var georeference = SetupGeoreference();

        // ── 2. Cesium World Terrain tileset ──────────────────────────────
        var tileset = SetupWorldTerrain(georeference);

        // ── 3. Bing Maps Aerial raster overlay ───────────────────────────
        SetupBingMapsOverlay(tileset);

        // ── 4. Zone-based terrain material ───────────────────────────────
        AssignTerrainMaterial(tileset);

        // ── 5. Pollution zone trigger volumes ─────────────────────────────
        SetupPollutionZones(georeference);

        // ── 6. Camera far clip for 500 km coverage ────────────────────────
        ConfigureCameraFarClip();

        // ── Save ──────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("[CesiumSetup] ══ DONE ══"
            + "\n  Tileset        : " + TILESET_NAME
            + "\n  Georeference   : Singapore (" + SG_Lat + "°N, " + SG_Lon + "°E)"
            + "\n  Streaming      : 500 km radius — covers Singapore, Johor, Cameron Highlands, Riau"
            + "\n  Material       : " + TERRAIN_MAT
            + "\n  Pollution zones: 6");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 1. Georeference
    // ─────────────────────────────────────────────────────────────────────

    private static CesiumGeoreference SetupGeoreference()
    {
        var existing = Object.FindObjectOfType<CesiumGeoreference>();
        CesiumGeoreference geo;

        if (existing != null)
        {
            geo = existing;
            Debug.Log("[CesiumSetup] Found existing CesiumGeoreference: " + geo.gameObject.name);
        }
        else
        {
            var go = new GameObject("CesiumGeoreference");
            Undo.RegisterCreatedObjectUndo(go, "Create CesiumGeoreference");
            geo = Undo.AddComponent<CesiumGeoreference>(go);
            Debug.Log("[CesiumSetup] Created CesiumGeoreference.");
        }

        geo.SetOriginLongitudeLatitudeHeight(SG_Lon, SG_Lat, 0.0);
        EditorUtility.SetDirty(geo);
        Debug.Log("[CesiumSetup] Georeference → Singapore (" + SG_Lat + "°N, " + SG_Lon + "°E).");
        return geo;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 2. World Terrain — Intan_Isle_CesiumTerrain
    // ─────────────────────────────────────────────────────────────────────

    private static Cesium3DTileset SetupWorldTerrain(CesiumGeoreference geo)
    {
        // Reuse existing if present
        foreach (var t in Object.FindObjectsOfType<Cesium3DTileset>())
        {
            if (t.ionAssetID == ASSET_WORLD_TERRAIN)
            {
                t.gameObject.name = TILESET_NAME;
                Debug.Log("[CesiumSetup] Found existing Cesium World Terrain → renamed to " + TILESET_NAME);
                ConfigureTilesetStreaming(t);
                return t;
            }
        }

        var tilesetGO = new GameObject(TILESET_NAME);
        Undo.RegisterCreatedObjectUndo(tilesetGO, "Create " + TILESET_NAME);
        tilesetGO.transform.SetParent(geo.transform, false);

        var tileset = Undo.AddComponent<Cesium3DTileset>(tilesetGO);
        tileset.ionAssetID = ASSET_WORLD_TERRAIN;

        ConfigureTilesetStreaming(tileset);

        EditorUtility.SetDirty(tileset);
        Debug.Log("[CesiumSetup] Created " + TILESET_NAME + " (Ion asset " + ASSET_WORLD_TERRAIN + ").");
        return tileset;
    }

    /// <summary>
    /// Configures Cesium3DTileset for 500 km coverage:
    ///   maximumScreenSpaceError = 64  → loads coarser tiles far from camera, covering wider area
    ///   preloadAncestors = true        → ensures parent tiles are always available
    ///   loadingDescendantLimit = 32    → limits tile refinement depth to control memory
    /// At 500 km the earth's curvature is visible; Cesium handles this automatically.
    /// </summary>
    private static void ConfigureTilesetStreaming(Cesium3DTileset tileset)
    {
        tileset.maximumScreenSpaceError  = 64f;   // coarser at distance — faster 500 km coverage
        tileset.preloadAncestors         = true;
        tileset.loadingDescendantLimit   = 32;
        tileset.forbidHoles              = false;
        EditorUtility.SetDirty(tileset);
        Debug.Log("[CesiumSetup] Tileset streaming: SSE=64, preloadAncestors=true, loadingDescendantLimit=32");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 3. Bing Maps Aerial overlay
    // ─────────────────────────────────────────────────────────────────────

    private static void SetupBingMapsOverlay(Cesium3DTileset tileset)
    {
        if (tileset.GetComponent<CesiumIonRasterOverlay>() != null)
        {
            Debug.Log("[CesiumSetup] Bing Maps overlay already present.");
            return;
        }

        var overlay = Undo.AddComponent<CesiumIonRasterOverlay>(tileset.gameObject);
        overlay.ionAssetID = ASSET_BING_AERIAL;
        EditorUtility.SetDirty(overlay);
        Debug.Log("[CesiumSetup] Added Bing Maps Aerial overlay (Ion asset " + ASSET_BING_AERIAL + ").");
    }

    // ─────────────────────────────────────────────────────────────────────
    // 4. Zone-based terrain material
    // ─────────────────────────────────────────────────────────────────────

    private static void AssignTerrainMaterial(Cesium3DTileset tileset)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(TERRAIN_MAT);
        if (mat == null)
        {
            Debug.LogWarning("[CesiumSetup] IntanIsle_Terrain.mat not found at " + TERRAIN_MAT
                + "\n  Run Tools > Intan Isle > Create Terrain Material first, or create it manually.");
            return;
        }

        tileset.opaqueMaterial = mat;
        EditorUtility.SetDirty(tileset);
        Debug.Log("[CesiumSetup] Assigned " + mat.name + " as opaqueMaterial on " + tileset.gameObject.name);
    }

    // ─────────────────────────────────────────────────────────────────────
    // 5. Camera far clip — 500 km
    // ─────────────────────────────────────────────────────────────────────

    private static void ConfigureCameraFarClip()
    {
        // Set all cameras in scene to see 500 km
        const float FAR_CLIP = 500000f; // 500 km in metres

        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            cam.farClipPlane = FAR_CLIP;
            EditorUtility.SetDirty(cam);
            Debug.Log("[CesiumSetup] " + cam.gameObject.name + " farClipPlane → " + FAR_CLIP + " m (500 km)");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 6. Pollution zone trigger volumes
    // ─────────────────────────────────────────────────────────────────────

    private static void SetupPollutionZones(CesiumGeoreference geo)
    {
        if (!AssetDatabase.IsValidFolder(PROFILE_DIR.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Scripts", "VolumeProfiles");

        GameObject parent = GameObject.Find("PollutionZones");
        if (parent == null)
        {
            parent = new GameObject("PollutionZones");
            Undo.RegisterCreatedObjectUndo(parent, "Create PollutionZones");
            parent.transform.SetParent(geo.transform, false);
        }

        foreach (ZoneDef def in Zones)
            CreateZone(def, parent.transform, geo);

        Debug.Log("[CesiumSetup] Created 6 VeilStrainZone volumes under PollutionZones.");
    }

    private static void CreateZone(ZoneDef def, Transform parent, CesiumGeoreference geo)
    {
        string goName = "VeilZone_" + def.name;

        var old = GameObject.Find(goName);
        if (old != null) Undo.DestroyObjectImmediate(old);

        var zoneGO = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(zoneGO, "Create " + goName);
        zoneGO.transform.SetParent(parent, false);

        var anchor = Undo.AddComponent<CesiumGlobeAnchor>(zoneGO);
        anchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(def.lon, def.lat, 100.0);
        EditorUtility.SetDirty(anchor);

        var col      = Undo.AddComponent<BoxCollider>(zoneGO);
        col.size     = ZONE_BOX;
        col.isTrigger = true;

        var vol            = Undo.AddComponent<Volume>(zoneGO);
        vol.profile        = CreatePollutionProfile(def.name);
        vol.isGlobal       = false;
        vol.blendDistance  = 200f;
        vol.weight         = 0f;
        vol.priority       = 10f;
        EditorUtility.SetDirty(vol);

        var hazeGO = new GameObject("HazeParticles");
        Undo.RegisterCreatedObjectUndo(hazeGO, "Create HazeParticles");
        hazeGO.transform.SetParent(zoneGO.transform, false);
        var ps = Undo.AddComponent<ParticleSystem>(hazeGO);
        ConfigureHazeParticles(ps);

        var vsz = Undo.AddComponent<VeilStrainZone>(zoneGO);
        vsz.zoneName             = def.name + " — " + def.description;
        vsz.veilStrainRate       = 15f;
        vsz.barakahHazeThreshold = 25f;
        vsz.hazeParticles        = ps;
        EditorUtility.SetDirty(vsz);

        Debug.Log("[CesiumSetup] Zone: " + goName
            + "  lat=" + def.lat + "  lon=" + def.lon + "  (" + def.description + ")");
    }

    // ─────────────────────────────────────────────────────────────────────
    // VolumeProfile: deep violet + crimson
    // ─────────────────────────────────────────────────────────────────────

    private static VolumeProfile CreatePollutionProfile(string zoneName)
    {
        string path    = PROFILE_DIR + "PollutionZone_" + zoneName + ".asset";
        var existing   = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (existing != null) return existing;

        var profile    = ScriptableObject.CreateInstance<VolumeProfile>();

        var colorAdj   = profile.Add<ColorAdjustments>(true);
        colorAdj.colorFilter.overrideState = true;
        colorAdj.colorFilter.value         = new Color(0.55f, 0f, 0.75f, 1f); // deep violet #4A0E6B
        colorAdj.saturation.overrideState  = true;
        colorAdj.saturation.value          = -35f;
        colorAdj.contrast.overrideState    = true;
        colorAdj.contrast.value            = 20f;

        var vignette   = profile.Add<Vignette>(true);
        vignette.color.overrideState       = true;
        vignette.color.value               = new Color(0.7f, 0.05f, 0.05f, 1f); // crimson
        vignette.intensity.overrideState   = true;
        vignette.intensity.value           = 0.45f;
        vignette.smoothness.overrideState  = true;
        vignette.smoothness.value          = 0.5f;
        vignette.rounded.overrideState     = true;
        vignette.rounded.value             = true;

        AssetDatabase.CreateAsset(profile, path);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Haze ParticleSystem
    // ─────────────────────────────────────────────────────────────────────

    private static void ConfigureHazeParticles(ParticleSystem ps)
    {
        var main              = ps.main;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize        = new ParticleSystem.MinMaxCurve(20f, 60f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(0.4f, 0f, 0.5f, 0.06f),
            new Color(0.6f, 0.05f, 0.05f, 0.1f)
        );
        main.loop             = true;
        main.playOnAwake      = false;
        main.maxParticles     = 200;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;

        var emission          = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(5f);

        var shape             = ps.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Box;
        shape.scale           = new Vector3(2000f, 100f, 2000f);

        ps.Stop();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Utility: Create IntanIsle_Terrain.mat from shader
    // ─────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Intan Isle/Create Terrain Material")]
    public static void CreateTerrainMaterial()
    {
        const string SHADER_PATH = "Assets/Scripts/TerrainMaterials/IntanIsle_TerrainShader.shader";
        const string MAT_DIR     = "Assets/Scripts/TerrainMaterials";

        if (!AssetDatabase.IsValidFolder(MAT_DIR))
            AssetDatabase.CreateFolder("Assets/Scripts", "TerrainMaterials");

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(SHADER_PATH);
        if (shader == null)
        {
            Debug.LogError("[CesiumSetup] IntanIsle_TerrainShader.shader not found at " + SHADER_PATH);
            return;
        }

        var mat = new Material(shader);
        mat.name = "IntanIsle_Terrain";
        AssetDatabase.CreateAsset(mat, TERRAIN_MAT);
        AssetDatabase.SaveAssets();

        Debug.Log("[CesiumSetup] Created IntanIsle_Terrain.mat at " + TERRAIN_MAT);
    }
}
