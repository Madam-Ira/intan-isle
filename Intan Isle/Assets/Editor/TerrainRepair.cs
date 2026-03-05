using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Intan Isle > Full Terrain Repair
///
/// One-click repair for the magenta terrain caused by MicroSplat (Built-in pipeline).
///
/// Actions performed:
///   1. Print full terrain state to Console (diagnosis)
///   2. Disable the MicroSplatTerrain MonoBehaviour so it cannot override materialTemplate at runtime
///   3. Assign URPTerrain.mat (Universal Render Pipeline/Terrain/Lit) as materialTemplate
///   4. Clear MicroSplat texture-array layers, assign standard GrassLayer/DirtLayer/RockLayer
///   5. Call terrain.Flush() to regenerate splatmap
///   6. Save scene
/// </summary>
public static class TerrainRepair
{
    private const string URP_MAT   = "Assets/Scripts/TerrainMaterials/URPTerrain.mat";
    private const string DBG_MAT   = "Assets/Scripts/TerrainMaterials/TerrainDebug.mat";
    private const string GRASS     = "Assets/Scripts/TerrainLayers/GrassLayer.asset";
    private const string DIRT      = "Assets/Scripts/TerrainLayers/DirtLayer.asset";
    private const string ROCK      = "Assets/Scripts/TerrainLayers/RockLayer.asset";
    private const string GRASS_TEX = "Assets/Fantasy Forest Environment Free Sample/Textures/grass01.tga";
    private const string URP_SHADER = "Universal Render Pipeline/Terrain/Lit";

    [MenuItem("Tools/Intan Isle/Full Terrain Repair")]
    public static void FullRepair()
    {
        // ── 1. Find terrain ───────────────────────────────────────────────────
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null)
        {
            Debug.LogError("[TerrainRepair] WaterfallSanctuary_Terrain not found.");
            return;
        }

        var terrain = terrainGO.GetComponent<Terrain>();
        var td      = terrain.terrainData;

        // ── 2. Diagnose current state ─────────────────────────────────────────
        var msBehaviour = terrainGO.GetComponent("MicroSplatTerrain") as MonoBehaviour
                       ?? FindMicroSplatComponent(terrainGO);

        Debug.Log("[TerrainRepair] ══════════ DIAGNOSIS ══════════\n"
            + "  terrain GO active     : " + terrainGO.activeInHierarchy + "\n"
            + "  terrain enabled       : " + terrain.enabled + "\n"
            + "  materialTemplate      : " + MatDesc(terrain) + "\n"
            + "  terrainLayers count   : " + td.terrainLayers.Length + "\n"
            + "  MicroSplatTerrain     : " + (msBehaviour == null ? "not found" : "found, enabled=" + msBehaviour.enabled) + "\n"
            + "  URP shader available  : " + (Shader.Find(URP_SHADER) != null ? "YES" : "NO — URP not configured") + "\n"
            + "  terrain position      : " + terrainGO.transform.position + "\n"
            + "  terrainData size      : " + td.size);

        for (int i = 0; i < td.terrainLayers.Length; i++)
        {
            var l = td.terrainLayers[i];
            Debug.Log("  layer[" + i + "] " + (l == null ? "NULL" : l.name)
                      + "  diffuse=" + (l?.diffuseTexture == null ? "NULL" : l.diffuseTexture.name));
        }

        // ── 3. Disable MicroSplatTerrain component ────────────────────────────
        if (msBehaviour != null && msBehaviour.enabled)
        {
            msBehaviour.enabled = false;
            EditorUtility.SetDirty(msBehaviour);
            Debug.Log("[TerrainRepair] Disabled MicroSplatTerrain component.");
        }

        // ── 4. Set materialTemplate = null (let URP handle terrain automatically) ──
        terrain.materialTemplate = null;
        EditorUtility.SetDirty(terrain);
        Debug.Log("[TerrainRepair] materialTemplate set to NULL — URP will use its default terrain shader.");

        // ── 5. Assign terrain layers ──────────────────────────────────────────
        var grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GRASS);
        if (grassLayer == null)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(GRASS_TEX);
            if (tex != null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scripts/TerrainLayers"))
                    AssetDatabase.CreateFolder("Assets/Scripts", "TerrainLayers");
                grassLayer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(6f, 6f) };
                AssetDatabase.CreateAsset(grassLayer, GRASS);
                AssetDatabase.SaveAssets();
                Debug.Log("[TerrainRepair] Created GrassLayer from grass01.tga.");
            }
        }

        if (grassLayer != null)
        {
            var layers = new List<TerrainLayer> { grassLayer };
            var dirt = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DIRT);
            var rock = AssetDatabase.LoadAssetAtPath<TerrainLayer>(ROCK);
            if (dirt != null) layers.Add(dirt);
            if (rock != null) layers.Add(rock);

            td.terrainLayers = layers.ToArray();
            EditorUtility.SetDirty(td);
            Debug.Log("[TerrainRepair] Assigned " + layers.Count + " terrain layers. Layer[0]="
                      + grassLayer.name + " tex=" + (grassLayer.diffuseTexture?.name ?? "null"));
        }
        else
        {
            Debug.LogWarning("[TerrainRepair] No grass texture found — terrain layers not set.");
        }

        // ── 6. Flush + save ───────────────────────────────────────────────────
        terrain.Flush();
        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log("[TerrainRepair] ══════════ DONE ══════════\n"
            + "  materialTemplate : " + MatDesc(terrain) + "\n"
            + "  terrainLayers    : " + td.terrainLayers.Length + "\n"
            + "  Press Play to test. If still magenta, share the full Console log above.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MonoBehaviour FindMicroSplatComponent(GameObject go)
    {
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;
            var typeName = mb.GetType().Name;
            if (typeName.Contains("MicroSplat") || typeName.Contains("Splat"))
                return mb;
        }
        return null;
    }

    private static string MatDesc(Terrain t)
    {
        if (t.materialTemplate == null) return "(null)";
        var s = t.materialTemplate.shader;
        return t.materialTemplate.name + " / shader=" + (s == null ? "MISSING" : s.name);
    }
}
