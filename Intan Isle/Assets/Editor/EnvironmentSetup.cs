using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnvironmentSetup
{
    [MenuItem("Tools/Intan Isle/Setup Environment Visuals")]
    public static void SetupEnvironment()
    {
        var log = new StringBuilder();
        log.AppendLine("=== Intan Isle Environment Setup ===");

        // ── Find terrain ─────────────────────────────────────────────
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null)
        {
            Debug.LogError("[EnvironmentSetup] WaterfallSanctuary_Terrain not found in scene.");
            return;
        }

        var terrain = terrainGO.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("[EnvironmentSetup] WaterfallSanctuary_Terrain has no Terrain component.");
            return;
        }

        var td  = terrain.terrainData;
        var pos = terrainGO.transform.position;
        log.AppendLine("Terrain: " + terrainGO.name + "  pos=" + pos + "  size=" + td.size);

        // ── Terrain layers ────────────────────────────────────────────
        AddTerrainLayers(td, log);

        // ── Tree scatter ──────────────────────────────────────────────
        ScatterTrees(terrain, pos, td, log);

        // ── Save ──────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        log.AppendLine("\nScene saved.");
        Debug.Log(log.ToString());
    }

    // ─────────────────────────────────────────────────────────────────
    // Terrain layers
    // ─────────────────────────────────────────────────────────────────

    private static void AddTerrainLayers(TerrainData td, StringBuilder log)
    {
        log.AppendLine("\n── Terrain Texture Layers ──");

        string[] names     = { "Grass",  "Dirt",   "Rock"            };
        string[] paths     = {
            "Assets/Fantasy Forest Environment Free Sample/Textures/grass01.tga",
            "Assets/Fantasy Forest Environment Free Sample/Textures/dirt01.tga",
            "Assets/Fantasy Forest Environment Free Sample/Textures/bark01_bottom.tga"
        };
        float[]  tileSizeX = { 6f, 4f, 3f };
        float[]  tileSizeZ = { 6f, 4f, 3f };

        string saveDir = "Assets/Scripts/TerrainLayers/";

        // Ensure save folder exists
        if (!AssetDatabase.IsValidFolder(saveDir.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Scripts", "TerrainLayers");

        var existingLayers = new List<TerrainLayer>(td.terrainLayers);

        for (int i = 0; i < names.Length; i++)
        {
            string name     = names[i];
            string texPath  = paths[i];
            string savePath = saveDir + name + "Layer.asset";

            // Skip if already added
            var already = AssetDatabase.LoadAssetAtPath<TerrainLayer>(savePath);
            if (already != null)
            {
                log.AppendLine("  [SKIP] " + name + " layer already exists.");
                if (!existingLayers.Contains(already))
                    existingLayers.Add(already);
                continue;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                log.AppendLine("  [WARN] Texture not found: " + texPath);
                continue;
            }

            var layer = new TerrainLayer();
            layer.diffuseTexture = tex;
            layer.tileSize       = new Vector2(tileSizeX[i], tileSizeZ[i]);

            AssetDatabase.CreateAsset(layer, savePath);
            existingLayers.Add(layer);

            log.AppendLine("  [OK]  " + name + "  tex=" + tex.name
                           + "  tile=(" + tileSizeX[i] + "," + tileSizeZ[i] + ")"
                           + "  saved=" + savePath);
        }

        td.terrainLayers = existingLayers.ToArray();
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssets();

        log.AppendLine("  Total layers: " + td.terrainLayers.Length);
    }

    // ─────────────────────────────────────────────────────────────────
    // Tree scatter
    // ─────────────────────────────────────────────────────────────────

    private static void ScatterTrees(Terrain terrain, Vector3 terrainPos,
                                     TerrainData td, StringBuilder log)
    {
        log.AppendLine("\n── Tree Scatter ──");

        const string prefabPath = "Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/tree_1.prefab";
        var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (treePrefab == null)
        {
            log.AppendLine("  [ERROR] Prefab not found: " + prefabPath);
            return;
        }
        log.AppendLine("  Prefab: " + treePrefab.name);

        // Remove previous group
        var old = GameObject.Find("FantasyForest_Trees");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            log.AppendLine("  Removed previous FantasyForest_Trees.");
        }

        // Parent under WorldRoot if it exists, otherwise scene root
        Transform parentTransform = null;
        var worldRoot = GameObject.Find("WorldRoot");
        if (worldRoot != null)
            parentTransform = worldRoot.transform;

        var parentGO = new GameObject("FantasyForest_Trees");
        Undo.RegisterCreatedObjectUndo(parentGO, "Create FantasyForest_Trees");
        if (parentTransform != null)
            parentGO.transform.SetParent(parentTransform, false);

        // Scatter settings
        const int   count      = 30;
        const float radius     = 80f;
        const float scaleMin   = 0.8f;
        const float scaleMax   = 1.2f;
        const int   seed       = 42;

        // Centre at (250, 0, 250) as requested, clamped to terrain bounds
        float cx = 250f;
        float cz = 250f;

        var rng = new System.Random(seed);
        log.AppendLine("  Center=(" + cx + ",0," + cz + ")  radius=" + radius
                       + "  count=" + count + "  seed=" + seed);
        log.AppendLine("  # | Position                   | RotY  | Scale");

        int placed = 0;

        while (placed < count)
        {
            double angle = rng.NextDouble() * System.Math.PI * 2.0;
            double dist  = System.Math.Sqrt(rng.NextDouble()) * radius;

            float x = cx + (float)(System.Math.Cos(angle) * dist);
            float z = cz + (float)(System.Math.Sin(angle) * dist);

            // Clamp to terrain bounds
            x = Mathf.Clamp(x, terrainPos.x + 2f, terrainPos.x + td.size.x - 2f);
            z = Mathf.Clamp(z, terrainPos.z + 2f, terrainPos.z + td.size.z - 2f);

            float y     = terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainPos.y;
            float rotY  = (float)(rng.NextDouble() * 360.0);
            float scale = scaleMin + (float)(rng.NextDouble() * (scaleMax - scaleMin));

            var go = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, parentGO.transform);
            go.name                = "tree_" + (placed + 1);
            go.transform.position  = new Vector3(x, y, z);
            go.transform.rotation  = Quaternion.Euler(0f, rotY, 0f);
            go.transform.localScale = Vector3.one * scale;
            Undo.RegisterCreatedObjectUndo(go, "Place tree");

            log.AppendLine("  " + (placed + 1)
                           + " | (" + x.ToString("F1") + ", " + y.ToString("F1") + ", " + z.ToString("F1") + ")"
                           + " | " + rotY.ToString("F0") + "deg"
                           + " | " + scale.ToString("F2") + "x");
            placed++;
        }

        log.AppendLine("\n  Placed " + placed + "/" + count + " trees.");
        log.AppendLine("  Parent: " + (parentTransform != null ? parentTransform.name + "/FantasyForest_Trees" : "FantasyForest_Trees (scene root)"));
    }
}
