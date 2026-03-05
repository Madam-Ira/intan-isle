// ════════════════════════════════════════════════════════════════
// INTAN ISLE — SCENE WIRING
// Menu: Tools > Intan Isle > Wire All Scripts
//
// Attaches all 13 core scripts to the correct GameObjects and
// wires every Inspector reference that can be resolved automatically.
//
// Safe to run multiple times — checks for existing components first.
// ════════════════════════════════════════════════════════════════

using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class IntanIsleSceneWiring
{
    [MenuItem("Tools/Intan Isle/Wire All Scripts")]
    public static void WireAll()
    {
        int attached = 0;
        int linked   = 0;

        // ── 1. GameSystems GO ─────────────────────────────────────
        var gameSystems = FindOrCreate("GameSystems", null);

        var dayNight = GetOrAdd<EmotionalDayNightCycle>(gameSystems, ref attached);
        var zoneLinker = GetOrAdd<ZoneShaderLinker>(gameSystems, ref attached);
        var veiledMgr = GetOrAdd<VeiledWorldManager>(gameSystems, ref attached);
        var homeBase  = GetOrAdd<PlayerHomeBase>(gameSystems, ref attached);
        var vegPlacer = GetOrAdd<ProceduralVegetationPlacer>(gameSystems, ref attached);
        var bunianHUD = GetOrAdd<BunianHUD>(gameSystems, ref attached);

        // ── 2. VeiledWorldRoot — child of GameSystems ─────────────
        var veiledRoot = FindOrCreate("VeiledWorldRoot", gameSystems.transform);
        veiledRoot.SetActive(false); // off by default (physical world is default)

        var webVFX     = GetOrAdd<WoodWideWebVFX>(veiledRoot, ref attached);
        var spirits    = GetOrAdd<BunianSpiritForms>(veiledRoot, ref attached);

        // ── 3. BlessingMeterSystem — ensure BarakahMeter is on it ─
        var blessingGO  = GameObject.Find("BlessingMeterSystem");
        BarakahMeter barakah = null;
        if (blessingGO != null)
        {
            barakah = GetOrAdd<BarakahMeter>(blessingGO, ref attached);
        }
        else
        {
            Debug.LogWarning("[Wiring] BlessingMeterSystem not found — BarakahMeter not wired.");
        }

        // ── 4. PlayerRig — BunianFlightController + CesiumGlobeAnchor
        var playerRig = GameObject.Find("PlayerRig");
        BunianFlightController flightCtrl = null;
        if (playerRig != null)
        {
            flightCtrl = GetOrAdd<BunianFlightController>(playerRig, ref attached);

        }
        else
        {
            Debug.LogWarning("[Wiring] PlayerRig not found — BunianFlightController not attached.");
        }

        // ── 5. Link references ────────────────────────────────────

        // Directional Light → EmotionalDayNightCycle
        var dirLight = FindFirstOfType<Light>(l => l.type == LightType.Directional);
        if (dirLight != null)
        {
            var so = new SerializedObject(dayNight);
            var prop = so.FindProperty("directionalLight");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = dirLight;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // Global Volume → EmotionalDayNightCycle
        var globalVol = FindFirstOfType<Volume>(v => v.isGlobal);
        if (globalVol != null)
        {
            var so = new SerializedObject(dayNight);
            var prop = so.FindProperty("postProcessVolume");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = globalVol;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // ZoneShaderLinker → EmotionalDayNightCycle
        {
            var so = new SerializedObject(dayNight);
            var prop = so.FindProperty("shaderLinker");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = zoneLinker;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // BarakahMeter → EmotionalDayNightCycle
        if (barakah != null)
        {
            var so = new SerializedObject(dayNight);
            var prop = so.FindProperty("barakahMeter");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = barakah;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // VeiledWorldRoot → VeiledWorldManager
        {
            var so = new SerializedObject(veiledMgr);
            var prop = so.FindProperty("veiledWorldRoot");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = veiledRoot;
                so.ApplyModifiedProperties();
                linked++;
            }
            // Also wire the global Volume as veiledVolume if present
            if (globalVol != null)
            {
                var vProp = so.FindProperty("veiledVolume");
                if (vProp != null && vProp.objectReferenceValue == null)
                {
                    vProp.objectReferenceValue = globalVol;
                    linked++;
                }
            }
            so.ApplyModifiedProperties();
        }

        // PlayerRig → ZoneShaderLinker
        if (playerRig != null)
        {
            var so = new SerializedObject(zoneLinker);
            var prop = so.FindProperty("playerRig");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = playerRig.transform;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // ZoneShaderLinker → VeiledWorldManager
        {
            var so = new SerializedObject(veiledMgr);
            var prop = so.FindProperty("shaderLinker");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = zoneLinker;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // MainCamera → BunianFlightController
        var mainCam = Camera.main;
        if (mainCam != null && flightCtrl != null)
        {
            var so = new SerializedObject(flightCtrl);
            var prop = so.FindProperty("mainCamera");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = mainCam;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // BarakahMeter → BunianHUD
        if (barakah != null)
        {
            var so = new SerializedObject(bunianHUD);
            var prop = so.FindProperty("barakahMeter");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = barakah;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // BunianFlightController → BunianHUD
        if (flightCtrl != null)
        {
            var so = new SerializedObject(bunianHUD);
            var prop = so.FindProperty("flightController");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = flightCtrl;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // ZoneShaderLinker → BunianHUD
        {
            var so = new SerializedObject(bunianHUD);
            var prop = so.FindProperty("zoneLinker");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = zoneLinker;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // IntanIsleZoneData → ZoneShaderLinker
        var zoneDataAsset = AssetDatabase.LoadAssetAtPath<IntanIsleZoneData>(
            "Assets/Resources/Zones/IntanIsleZones.asset");
        if (zoneDataAsset != null)
        {
            var so = new SerializedObject(zoneLinker);
            var prop = so.FindProperty("zoneData");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = zoneDataAsset;
                so.ApplyModifiedProperties();
                linked++;
            }
        }
        else
        {
            Debug.LogWarning("[Wiring] IntanIsleZones.asset not found — run 'Tools > Intan Isle > Create Zone Data Asset' first.");
        }

        // WoodWideWeb existing GO → assign treesParent if "FantasyForest_Trees" exists
        var treesParent = GameObject.Find("FantasyForest_Trees")
                        ?? GameObject.Find("WorldRoot");
        if (treesParent != null)
        {
            var so = new SerializedObject(webVFX);
            var prop = so.FindProperty("treesParent");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = treesParent.transform;
                so.ApplyModifiedProperties();
                linked++;
            }
        }

        // ── Auto-populate ProceduralVegetationPlacer prefab arrays ──
        linked += AssignVegetationPrefabs(vegPlacer);

        // ── 6. Migrate existing WoodWideWeb GO ────────────────────
        // If there's a legacy "WoodWideWeb" GO in scene (from old setup), log it
        var legacyWeb = GameObject.Find("WoodWideWeb");
        if (legacyWeb != null && legacyWeb != veiledRoot)
        {
            Debug.Log("[Wiring] Legacy 'WoodWideWeb' GO found — WoodWideWebVFX is now on VeiledWorldRoot. " +
                      "You may delete the old empty 'WoodWideWeb' GO.");
        }

        // ── 7. Save ───────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(
            EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        Debug.Log($"[Intan Isle Wiring] Complete — {attached} components attached, {linked} references linked.");
        EditorUtility.DisplayDialog(
            "Intan Isle — Scene Wiring Complete",
            $"Components attached: {attached}\n" +
            $"References linked:   {linked}\n\n" +
            "Remaining manual steps:\n" +
            "• Assign canopy/understory/groundcover prefab arrays on ProceduralVegetationPlacer\n" +
            "• Set Cesium Ion token (Window > Cesium)\n" +
            "• Run Tools > Intan Isle > Setup Cesium World if not yet done\n" +
            "• Create Resources/Zones/IntanIsleZones.asset (right-click > Create > Intan Isle > Zone Data)",
            "OK");
    }

    // ── Create Zone Data asset in Resources/Zones/ ────────────────

    [MenuItem("Tools/Intan Isle/Create Zone Data Asset")]
    public static void CreateZoneDataAsset()
    {
        const string dir  = "Assets/Resources/Zones";
        const string path = dir + "/IntanIsleZones.asset";

        if (AssetDatabase.LoadAssetAtPath<IntanIsleZoneData>(path) != null)
        {
            Debug.Log("[Wiring] IntanIsleZones.asset already exists at " + path);
            EditorUtility.DisplayDialog("Zone Data", "IntanIsleZones.asset already exists at\n" + path, "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Resources", "Zones");

        var asset = ScriptableObject.CreateInstance<IntanIsleZoneData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        Debug.Log("[Wiring] Created IntanIsleZones.asset at " + path);
        EditorUtility.DisplayDialog("Zone Data Created",
            "IntanIsleZones.asset created at:\n" + path +
            "\n\nNow assign it to ZoneShaderLinker.zoneData in the Inspector.", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static GameObject FindOrCreate(string name, Transform parent)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return existing;

        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go, ref int counter) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c == null)
        {
            c = go.AddComponent<T>();
            counter++;
            Debug.Log($"[Wiring] Added {typeof(T).Name} to '{go.name}'");
        }
        return c;
    }

    private static T FindFirstOfType<T>(System.Func<T, bool> predicate) where T : Component
    {
        return Object.FindObjectsOfType<T>().FirstOrDefault(predicate);
    }

    // ── Auto-assign URP tree prefabs to ProceduralVegetationPlacer ─

    private static int AssignVegetationPrefabs(ProceduralVegetationPlacer placer)
    {
        int linked = 0;
        const string URP = "Assets/Realistic Tree/Prefabs/URP";

        // Canopy — largest trees (Ash, Chestnut, Weeping Willow, Spruce)
        var canopyPaths = new[]
        {
            URP + "/Ash/Ash 1.prefab",
            URP + "/Ash/Ash 3.prefab",
            URP + "/Ash/Ash 5.prefab",
            URP + "/Chestnut/Chestnut 1.prefab",
            URP + "/Chestnut/Chestnut 3.prefab",
            URP + "/Spruce/Spruce 1.prefab",
            URP + "/Spruce/Spruce 4.prefab",
            URP + "/Weeping Willow/Weeping_Willow 1.prefab",
            URP + "/Weeping Willow/Weeping_Willow 3.prefab",
        };

        // Understory — smaller/mid Birch and Ash
        var understoryPaths = new[]
        {
            URP + "/Birch/Birch 1.prefab",
            URP + "/Birch/Birch 3.prefab",
            URP + "/Birch/Birch 5.prefab",
            URP + "/Birch/Birch 7.prefab",
            URP + "/Ash/Ash 2.prefab",
            URP + "/Ash/Ash 4.prefab",
            URP + "/Chestnut/Chestnut 2.prefab",
            URP + "/Chestnut/Chestnut 4.prefab",
        };

        // Groundcover — Birch groups + Spruce small + Weeping Willow small
        var groundcoverPaths = new[]
        {
            URP + "/Birch/Birch Group 1.prefab",
            URP + "/Birch/Birch Group 2.prefab",
            URP + "/Spruce/Spruce Group 1.prefab",
            URP + "/Spruce/Spruce 9.prefab",
            URP + "/Birch/Birch 9.prefab",
            URP + "/Birch/Birch 10.prefab",
        };

        linked += SetPrefabArray(placer, "canopyPrefabs",      canopyPaths);
        linked += SetPrefabArray(placer, "understoryPrefabs",  understoryPaths);
        linked += SetPrefabArray(placer, "groundcoverPrefabs", groundcoverPaths);

        // Kampung — Weeping Willow feels appropriate
        var kampungPaths = new[]
        {
            URP + "/Weeping Willow/Weeping_Willow 2.prefab",
            URP + "/Weeping Willow/Weeping_Willow 4.prefab",
        };
        linked += SetPrefabArray(placer, "kampungPrefabs", kampungPaths);

        return linked;
    }

    private static int SetPrefabArray(Component comp, string fieldName, string[] paths)
    {
        var so   = new SerializedObject(comp);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning("[Wiring] Field not found: " + fieldName); return 0; }
        if (prop.arraySize > 0) return 0; // already assigned

        var prefabs = paths
            .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
            .Where(p => p != null)
            .ToArray();

        if (prefabs.Length == 0)
        {
            Debug.LogWarning("[Wiring] No prefabs found for " + fieldName + " — check Realistic Tree/Prefabs/URP/");
            return 0;
        }

        prop.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];

        so.ApplyModifiedProperties();
        Debug.Log($"[Wiring] {fieldName} — assigned {prefabs.Length} prefabs.");
        return 1;
    }
}
