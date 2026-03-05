using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Intan Isle > Repair Alphamap
///
/// Clears the corrupted MicroSplat splatmap by repainting the full terrain
/// with 100% layer 0 (Grass), zeroing all other layers.
/// </summary>
public static class AlphamapRepair
{
    [MenuItem("Tools/Intan Isle/Repair Alphamap")]
    public static void RepairAlphamap()
    {
        // ── 1. Find terrain ───────────────────────────────────────────────────
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null)
        {
            Debug.LogError("[AlphamapRepair] WaterfallSanctuary_Terrain not found.");
            return;
        }

        var terrain = terrainGO.GetComponent<Terrain>();
        var td      = terrain.terrainData;

        // ── 2. Report current state ───────────────────────────────────────────
        int res     = td.alphamapResolution;
        int layers  = td.alphamapLayers;

        Debug.Log("[AlphamapRepair] Terrain: " + terrainGO.name
            + "\n  heightmapResolution : " + td.heightmapResolution
            + "\n  alphamapResolution  : " + res
            + "\n  alphamapLayers      : " + layers
            + "\n  terrainLayers.Length: " + td.terrainLayers.Length);

        for (int i = 0; i < td.terrainLayers.Length; i++)
        {
            var l = td.terrainLayers[i];
            Debug.Log("  layer[" + i + "] "
                + (l == null ? "NULL" : l.name)
                + "  diffuse=" + (l?.diffuseTexture == null ? "NULL" : l.diffuseTexture.name));
        }

        // ── 3. Ensure we have at least 1 layer before writing alphamaps ───────
        if (td.terrainLayers.Length == 0)
        {
            Debug.LogError("[AlphamapRepair] No terrain layers assigned. "
                + "Run Tools > Intan Isle > Full Terrain Repair first.");
            return;
        }

        // ── 4. Build full-terrain alphamap: layer 0 = 1.0, rest = 0.0 ────────
        // SetAlphamaps expects float[height, width, layerCount]
        int w = res;
        int h = res;
        int numLayers = td.alphamapLayers; // reflects actual layer count after assignment

        var maps = new float[h, w, numLayers];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                maps[y, x, 0] = 1f; // layer 0 = Grass, full coverage
                for (int l = 1; l < numLayers; l++)
                    maps[y, x, l] = 0f;
            }
        }

        // ── 5. Apply and save ─────────────────────────────────────────────────
        td.SetAlphamaps(0, 0, maps);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log("[AlphamapRepair] Done."
            + "\n  alphamapResolution : " + res + " × " + res
            + "\n  alphamapLayers     : " + numLayers
            + "\n  Painted " + (w * h) + " pixels — layer 0 = 1.0 across entire terrain."
            + "\n  Press Play — terrain should render fully grass-green.");
    }
}
