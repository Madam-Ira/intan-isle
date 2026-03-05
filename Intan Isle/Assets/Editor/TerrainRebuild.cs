using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Intan Isle > Rebuild Terrain From Scratch
///
/// Deletes the MicroSplat-corrupted WaterfallSanctuary_Terrain and its binary
/// TerrainData, then creates a clean replacement with proper URP layers.
/// </summary>
public static class TerrainRebuild
{
    // ── Paths ─────────────────────────────────────────────────────────────────
    private const string NEW_TD_PATH   = "Assets/Scenes/WaterfallSanctuary_TerrainData.asset";
    private const string LAYER_DIR     = "Assets/Scripts/TerrainLayers";
    private const string GRASS_PATH    = LAYER_DIR + "/FreshGrass.asset";
    private const string DIRT_PATH     = LAYER_DIR + "/FreshDirt.asset";
    private const string ROCK_PATH     = LAYER_DIR + "/FreshRock.asset";

    private const string GRASS_TEX    = "Assets/Fantasy Forest Environment Free Sample/Textures/grass01.tga";
    private const string DIRT_TEX     = "Assets/Fantasy Forest Environment Free Sample/Textures/dirt01.tga";
    private const string ROCK_TEX     = "Assets/Fantasy Forest Environment Free Sample/Textures/bark01_bottom.tga";

    [MenuItem("Tools/Intan Isle/Rebuild Terrain From Scratch")]
    public static void RebuildTerrain()
    {
        // ── 1. Find and destroy old terrain ──────────────────────────────────
        var oldGO = GameObject.Find("WaterfallSanctuary_Terrain");
        string oldTdPath = null;

        if (oldGO != null)
        {
            var oldTerrain = oldGO.GetComponent<Terrain>();
            if (oldTerrain != null && oldTerrain.terrainData != null)
                oldTdPath = AssetDatabase.GetAssetPath(oldTerrain.terrainData);

            Object.DestroyImmediate(oldGO);
            Debug.Log("[TerrainRebuild] Deleted old WaterfallSanctuary_Terrain GameObject.");
        }

        if (!string.IsNullOrEmpty(oldTdPath) && File.Exists(oldTdPath))
        {
            AssetDatabase.DeleteAsset(oldTdPath);
            Debug.Log("[TerrainRebuild] Deleted old TerrainData asset: " + oldTdPath);
        }

        // ── 2. Create fresh TerrainData ───────────────────────────────────────
        var td = new TerrainData();
        td.heightmapResolution = 513;           // power-of-2 + 1 (standard)
        td.size                = new Vector3(500f, 100f, 500f);
        td.alphamapResolution  = 512;
        td.baseMapResolution   = 1024;
        td.SetDetailResolution(512, 8);

        // Save TerrainData asset BEFORE assigning layers (required by Unity)
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        AssetDatabase.CreateAsset(td, NEW_TD_PATH);
        AssetDatabase.SaveAssets();
        Debug.Log("[TerrainRebuild] Created fresh TerrainData: " + NEW_TD_PATH);

        // ── 3. Create 3 fresh TerrainLayer assets ─────────────────────────────
        if (!AssetDatabase.IsValidFolder(LAYER_DIR))
            AssetDatabase.CreateFolder("Assets/Scripts", "TerrainLayers");

        var grassLayer = MakeLayer("FreshGrass", GRASS_TEX, GRASS_PATH, 6f);
        var dirtLayer  = MakeLayer("FreshDirt",  DIRT_TEX,  DIRT_PATH,  4f);
        var rockLayer  = MakeLayer("FreshRock",  ROCK_TEX,  ROCK_PATH,  3f);

        if (grassLayer == null)
        {
            Debug.LogError("[TerrainRebuild] Failed to create GrassLayer — grass01.tga not found.");
            return;
        }

        // ── 4. Assign layers to TerrainData ───────────────────────────────────
        td.terrainLayers = new TerrainLayer[]
        {
            grassLayer,
            dirtLayer != null ? dirtLayer : grassLayer,
            rockLayer  != null ? rockLayer  : grassLayer,
        };
        EditorUtility.SetDirty(td);

        Debug.Log("[TerrainRebuild] Assigned terrain layers:");
        for (int i = 0; i < td.terrainLayers.Length; i++)
        {
            var l = td.terrainLayers[i];
            Debug.Log("  [" + i + "] " + l.name + "  tex=" + (l.diffuseTexture?.name ?? "null"));
        }

        // ── 5. Paint entire alphamap — 100% layer 0 (grass) ──────────────────
        int res    = td.alphamapResolution;
        int nLayer = td.alphamapLayers;
        var maps   = new float[res, res, nLayer];

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                maps[y, x, 0] = 1f;
                for (int l = 1; l < nLayer; l++)
                    maps[y, x, l] = 0f;
            }

        td.SetAlphamaps(0, 0, maps);
        EditorUtility.SetDirty(td);
        Debug.Log("[TerrainRebuild] Alphamap painted: " + res + "×" + res
                  + "  layers=" + nLayer + "  layer[0]=1.0 across all pixels.");

        // ── 6. Create Terrain GameObject ──────────────────────────────────────
        var terrainGO = Terrain.CreateTerrainGameObject(td);
        terrainGO.name = "WaterfallSanctuary_Terrain";
        terrainGO.transform.position = Vector3.zero;

        var terrain = terrainGO.GetComponent<Terrain>();
        terrain.materialTemplate = null;     // URP handles terrain automatically
        terrain.drawInstanced    = false;
        EditorUtility.SetDirty(terrain);

        var col = terrainGO.GetComponent<TerrainCollider>();
        if (col != null) col.terrainData = td;

        Undo.RegisterCreatedObjectUndo(terrainGO, "Create WaterfallSanctuary_Terrain");
        Debug.Log("[TerrainRebuild] Created terrain GameObject at (0,0,0)"
                  + "  size=500×100×500  materialTemplate=null");

        // ── 7. Re-snap trees to new terrain surface ───────────────────────────
        var treesParent = GameObject.Find("FantasyForest_Trees");
        int snapped = 0;
        if (treesParent != null)
        {
            foreach (Transform child in treesParent.transform)
            {
                float y = terrain.SampleHeight(child.position) + terrainGO.transform.position.y;
                child.position = new Vector3(child.position.x, y, child.position.z);
                snapped++;
            }
            EditorUtility.SetDirty(treesParent);
            Debug.Log("[TerrainRebuild] Re-snapped " + snapped + " trees to new terrain surface.");
        }
        else
        {
            Debug.Log("[TerrainRebuild] FantasyForest_Trees not found — skipping tree re-snap.");
        }

        // ── 8. Save everything ────────────────────────────────────────────────
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("[TerrainRebuild] ══════════ DONE ══════════"
                  + "\n  TerrainData           : " + NEW_TD_PATH
                  + "\n  Terrain size          : " + td.size
                  + "\n  heightmapResolution   : " + td.heightmapResolution
                  + "\n  alphamapResolution    : " + td.alphamapResolution + " × " + td.alphamapResolution
                  + "\n  terrainLayers         : " + td.terrainLayers.Length
                  + "\n  materialTemplate      : null (URP default)"
                  + "\n  Trees re-snapped      : " + snapped
                  + "\n  Press Play — terrain should be fully grass with no magenta.");
    }

    // ── Helper: create a TerrainLayer asset ───────────────────────────────────

    private static TerrainLayer MakeLayer(string layerName, string texPath,
                                          string savePath, float tileSize)
    {
        // Delete stale asset so we get a truly fresh one
        if (File.Exists(savePath))
            AssetDatabase.DeleteAsset(savePath);

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex == null)
        {
            Debug.LogWarning("[TerrainRebuild] Texture not found: " + texPath);
            return null;
        }

        var layer = new TerrainLayer
        {
            name            = layerName,
            diffuseTexture  = tex,
            tileSize        = new Vector2(tileSize, tileSize),
            smoothness      = 0f,
            metallic        = 0f,
        };

        AssetDatabase.CreateAsset(layer, savePath);
        Debug.Log("[TerrainRebuild] Created " + layerName + " → " + savePath
                  + "  tex=" + tex.name + "  tile=" + tileSize + "m");
        return layer;
    }
}
