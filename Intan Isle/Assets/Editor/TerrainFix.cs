using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Intan Isle > Fix Terrain Shader
///
/// Root cause of magenta terrain:
///   MicroSplat.shader was auto-generated for the Built-in (Standard) pipeline.
///   It uses CGPROGRAM + LightMode=ForwardBase — both invisible to URP, so Unity
///   falls back to the hot-pink "missing shader" colour.
///
/// Fix:
///   1. Creates a new material using "Universal Render Pipeline/Terrain/Lit"
///   2. Saves it to Assets/Scripts/TerrainMaterials/URPTerrain.mat
///   3. Assigns it to WaterfallSanctuary_Terrain.materialTemplate
///   4. Ensures GrassLayer (grass01.tga, 6m tile) is the first terrain layer
///   5. Reports everything to the Console
/// </summary>
public static class TerrainFix
{
    private const string SAVE_DIR     = "Assets/Scripts/TerrainMaterials";
    private const string MAT_PATH     = SAVE_DIR + "/URPTerrain.mat";
    private const string GRASS_LAYER  = "Assets/Scripts/TerrainLayers/GrassLayer.asset";
    private const string DIRT_LAYER   = "Assets/Scripts/TerrainLayers/DirtLayer.asset";
    private const string ROCK_LAYER   = "Assets/Scripts/TerrainLayers/RockLayer.asset";
    private const string URP_SHADER   = "Universal Render Pipeline/Terrain/Lit";
    private const string DBG_SHADER   = "Intan Isle/TerrainDebug";
    private const string DBG_MAT_PATH = SAVE_DIR + "/TerrainDebug.mat";

    // ── Diagnose ─────────────────────────────────────────────────────────────
    [MenuItem("Tools/Intan Isle/Diagnose Terrain")]
    public static void DiagnoseTerrain()
    {
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null) { Debug.LogError("[TerrainDiag] Terrain not found."); return; }

        var terrain = terrainGO.GetComponent<Terrain>();
        var td      = terrain.terrainData;

        Debug.Log("[TerrainDiag] ═══ Terrain State ═══\n"
                  + "  materialTemplate : " + ReportCurrentShader(terrain) + "\n"
                  + "  terrainLayers    : " + td.terrainLayers.Length + "\n"
                  + "  terrain active   : " + terrainGO.activeInHierarchy + "\n"
                  + "  terrain pos      : " + terrainGO.transform.position + "\n"
                  + "  td.size          : " + td.size);

        for (int i = 0; i < td.terrainLayers.Length; i++)
        {
            var l       = td.terrainLayers[i];
            string tex  = (l != null && l.diffuseTexture != null) ? l.diffuseTexture.name : "NULL";
            Debug.Log("  layer[" + i + "] " + (l != null ? l.name : "NULL") + "  diffuse=" + tex);
        }

        var urpShader = Shader.Find(URP_SHADER);
        Debug.Log("  Shader.Find(URP/Terrain/Lit) : " + (urpShader == null ? "NOT FOUND" : "OK — " + urpShader.name));

        var dbgShader = Shader.Find(DBG_SHADER);
        Debug.Log("  Shader.Find(TerrainDebug)    : " + (dbgShader == null ? "NOT FOUND" : "OK"));
    }

    // ── Apply debug flat-green shader ─────────────────────────────────────────
    [MenuItem("Tools/Intan Isle/Terrain Debug Shader (flat green)")]
    public static void ApplyDebugShader()
    {
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null) { Debug.LogError("[TerrainDebug] Terrain not found."); return; }

        var terrain = terrainGO.GetComponent<Terrain>();

        var shader = Shader.Find(DBG_SHADER);
        if (shader == null)
        {
            Debug.LogError("[TerrainDebug] 'Intan Isle/TerrainDebug' shader not found. "
                           + "Make sure Assets/Scripts/TerrainMaterials/TerrainDebug.shader exists and compiled.");
            return;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(DBG_MAT_PATH);
        if (mat == null)
        {
            if (!AssetDatabase.IsValidFolder(SAVE_DIR))
                AssetDatabase.CreateFolder("Assets/Scripts", "TerrainMaterials");
            mat = new Material(shader) { name = "TerrainDebug" };
            AssetDatabase.CreateAsset(mat, DBG_MAT_PATH);
            AssetDatabase.SaveAssets();
        }

        terrain.materialTemplate = mat;
        EditorUtility.SetDirty(terrain);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("[TerrainDebug] Applied flat-green debug shader.\n"
                  + "  GREEN = terrain renderer works; fix is in URP/Terrain/Lit setup.\n"
                  + "  MAGENTA = terrain renderer itself is broken; check URP pipeline asset in Project Settings > Graphics.");
    }

    [MenuItem("Tools/Intan Isle/Fix Terrain Shader")]
    public static void FixTerrainShader()
    {
        // ── Find terrain ──────────────────────────────────────────────────────
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null)
        {
            Debug.LogError("[TerrainFix] WaterfallSanctuary_Terrain not found. Is the scene open?");
            return;
        }

        var terrain = terrainGO.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("[TerrainFix] No Terrain component on WaterfallSanctuary_Terrain.");
            return;
        }

        var td = terrain.terrainData;
        Debug.Log("[TerrainFix] Found terrain: " + terrainGO.name
                  + "  size=" + td.size
                  + "  current shader=" + ReportCurrentShader(terrain));

        // ── 1. Find / create URP terrain material ─────────────────────────────
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (mat == null)
        {
            // Verify URP shader is available
            var shader = Shader.Find(URP_SHADER);
            if (shader == null)
            {
                Debug.LogError("[TerrainFix] Shader not found: \"" + URP_SHADER + "\"\n"
                               + "Make sure URP is installed and the project uses a URP render pipeline asset.");
                return;
            }

            // Create save folder
            if (!AssetDatabase.IsValidFolder(SAVE_DIR))
                AssetDatabase.CreateFolder("Assets/Scripts", "TerrainMaterials");

            mat = new Material(shader);
            mat.name = "URPTerrain";
            AssetDatabase.CreateAsset(mat, MAT_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log("[TerrainFix] Created new material: " + MAT_PATH
                      + "  shader=\"" + URP_SHADER + "\"");
        }
        else
        {
            Debug.Log("[TerrainFix] Loaded existing material: " + MAT_PATH
                      + "  shader=" + mat.shader.name);
        }

        // ── 2. Assign material to terrain ─────────────────────────────────────
        terrain.materialTemplate = mat;
        EditorUtility.SetDirty(terrain);
        Debug.Log("[TerrainFix] terrain.materialTemplate assigned: " + mat.name);

        // ── 3. Force-clear MicroSplat texture arrays, assign standard layers ─
        //  MicroSplat replaces td.terrainLayers with its own texture-array system.
        //  Force-reset to null first so Unity re-generates the splatmap correctly.
        td.terrainLayers = new TerrainLayer[0];
        EnsureTerrainLayers(td, terrain);

        // ── 4. Mark dirty + save ──────────────────────────────────────────────
        EditorUtility.SetDirty(td);
        EditorUtility.SetDirty(terrain);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("[TerrainFix] Done. Terrain now uses \"" + URP_SHADER + "\".\n"
                  + "  Press Play — the terrain should render green/grass.\n"
                  + "  Console should show no shader errors now.");
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static void EnsureTerrainLayers(TerrainData td, Terrain terrain)
    {
        // Load our pre-made layers (created by EnvironmentSetup)
        var grass = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GRASS_LAYER);
        var dirt  = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DIRT_LAYER);
        var rock  = AssetDatabase.LoadAssetAtPath<TerrainLayer>(ROCK_LAYER);

        if (grass == null)
        {
            // GrassLayer.asset missing — create a minimal one on the spot from grass01.tga
            const string TEX_PATH = "Assets/Fantasy Forest Environment Free Sample/Textures/grass01.tga";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEX_PATH);
            if (tex == null)
            {
                Debug.LogError("[TerrainFix] grass01.tga not found at " + TEX_PATH);
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Scripts/TerrainLayers"))
                AssetDatabase.CreateFolder("Assets/Scripts", "TerrainLayers");

            grass = new TerrainLayer();
            grass.diffuseTexture = tex;
            grass.tileSize = new Vector2(6f, 6f);
            AssetDatabase.CreateAsset(grass, GRASS_LAYER);
            AssetDatabase.SaveAssets();
            Debug.Log("[TerrainFix] Created GrassLayer.asset on the fly from grass01.tga.");
        }

        // Check if already set correctly
        var existing = td.terrainLayers;
        if (existing.Length > 0 && existing[0] == grass)
        {
            Debug.Log("[TerrainFix] GrassLayer already layer 0. Layers: " + existing.Length);
            terrain.Flush();
            return;
        }

        // Rebuild layer list: Grass first, then Dirt, then Rock
        var layers = new System.Collections.Generic.List<TerrainLayer>();
        layers.Add(grass);
        if (dirt != null)  layers.Add(dirt);
        if (rock != null)  layers.Add(rock);

        // Keep any existing layers not in our set
        foreach (var l in existing)
        {
            if (l != null && l != grass && l != dirt && l != rock)
                layers.Add(l);
        }

        td.terrainLayers = layers.ToArray();
        EditorUtility.SetDirty(td);

        // Force splatmap regeneration
        terrain.Flush();

        Debug.Log("[TerrainFix] Terrain layers assigned and flushed:");
        for (int i = 0; i < td.terrainLayers.Length; i++)
        {
            var l = td.terrainLayers[i];
            string texName = (l != null && l.diffuseTexture != null) ? l.diffuseTexture.name : "null";
            Debug.Log("  [" + i + "] " + (l != null ? l.name : "null") + "  tex=" + texName);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static string ReportCurrentShader(Terrain terrain)
    {
        if (terrain.materialTemplate == null)
            return "(null — Unity default terrain shader)";
        var s = terrain.materialTemplate.shader;
        return s == null ? "(missing shader)" : "\"" + s.name + "\"";
    }
}
