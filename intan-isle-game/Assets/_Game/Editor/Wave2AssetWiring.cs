#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IntanIsle.Core
{
    public static class Wave2AssetWiring
    {
        [MenuItem("Tools/Intan Isle/Wire Wave 2 Assets")]
        public static void WireWave2()
        {
            int wired = 0;
            int missing = 0;

            // ── 1. ProceduralVegetationPlacer ───────────────────────────

            ProceduralVegetationPlacer pvp = Object.FindObjectOfType<ProceduralVegetationPlacer>();
            GameObject pvpGO;

            if (pvp == null)
            {
                // Find or create a GO under RealWorldRoot
                GameObject realWorld = GameObject.Find("RealWorldRoot");
                if (realWorld == null)
                {
                    realWorld = new GameObject("RealWorldRoot");
                    Undo.RegisterCreatedObjectUndo(realWorld, "Create RealWorldRoot");
                }

                pvpGO = new GameObject("ProceduralVegetation");
                pvpGO.transform.SetParent(realWorld.transform);
                Undo.RegisterCreatedObjectUndo(pvpGO, "Create ProceduralVegetation");
                pvp = Undo.AddComponent<ProceduralVegetationPlacer>(pvpGO);
                Debug.Log("[Wave2] Created ProceduralVegetation GO with ProceduralVegetationPlacer.");
            }
            else
            {
                pvpGO = pvp.gameObject;
                Debug.Log($"[Wave2] Found ProceduralVegetationPlacer on {pvpGO.name}.");
            }

            SerializedObject so = new SerializedObject(pvp);

            // ── Canopy: Birch trees + Tree9 variants ────────────────────

            string[] canopyPaths = {
                "Assets/Next_Spring/Tree_Bundle/Prefabs/White_Birch/Mobile/M_Realistic21_1.prefab",
                "Assets/Next_Spring/Tree_Bundle/Prefabs/White_Birch/Mobile/M_Realistic21_3.prefab",
                "Assets/Next_Spring/Tree_Bundle/Prefabs/White_Birch/Mobile/M_Realistic21_5.prefab",
                "Assets/Next_Spring/Tree_Bundle/Prefabs/White_Birch/Mobile/M_Realistic21_8.prefab",
                "Assets/Next_Spring/Tree_Bundle/Prefabs/White_Birch/Mobile/M_Realistic21_12.prefab",
                "Assets/Tree9/Tree9_2.prefab",
                "Assets/Tree9/Tree9_3.prefab",
                "Assets/Tree9/Tree9_4.prefab",
                "Assets/Tree9/Tree9_5.prefab",
                "Assets/NatureStarterKit2/Nature/tree01.prefab",
                "Assets/NatureStarterKit2/Nature/tree02.prefab",
                "Assets/NatureStarterKit2/Nature/tree03.prefab",
            };
            wired += AssignPrefabArray(so, "canopyPrefabs", canopyPaths, ref missing);

            // ── Understory: bushes ──────────────────────────────────────

            string[] understoryPaths = {
                "Assets/NatureStarterKit2/Nature/bush01.prefab",
                "Assets/NatureStarterKit2/Nature/bush02.prefab",
                "Assets/NatureStarterKit2/Nature/bush03.prefab",
                "Assets/NatureStarterKit2/Nature/bush04.prefab",
                "Assets/NatureStarterKit2/Nature/bush05.prefab",
                "Assets/NatureStarterKit2/Nature/bush06.prefab",
            };
            wired += AssignPrefabArray(so, "understoryPrefabs", understoryPaths, ref missing);

            // ── Groundcover: succulents + grass ─────────────────────────

            string[] groundcoverPaths = {
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Succulent_EcheveriaRosalinda_var1.prefab",
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Succulent_EcheveriaRosalinda_var2.prefab",
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Succulent_ghost_plant_var1.prefab",
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Succulent_ghost_plant_var2.prefab",
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Succulent_houseleek_var1.prefab",
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Succulent_HensAndChicks_var1.prefab",
                "Assets/SeedMesh/Succulents/Succulents/Prefabs/Zebra_Cactus_Var1.prefab",
                "Assets/Fantasy Forest Environment Free Sample/Meshes/Prefabs/grass01.prefab",
            };
            wired += AssignPrefabArray(so, "groundcoverPrefabs", groundcoverPaths, ref missing);

            // ── Mangrove: palm trees ────────────────────────────────────

            string[] mangrovePaths = {
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Palm Trees/Prefabs/PalmTreeSingle.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Palm Trees/Prefabs/PalmTreeSingleBent.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Palm Trees/Prefabs/PalmTreesDual.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Palm Trees/Prefabs/PalmTreesDualBent.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Palm Trees/Prefabs/PalmTreeTrio.prefab",
            };
            wired += AssignPrefabArray(so, "mangrovePrefabs", mangrovePaths, ref missing);

            // ── Dead ground: rocks ──────────────────────────────────────

            string[] deadPaths = {
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Rocks/Prefabs/rock01.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Rocks/Prefabs/rock02.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Rocks/Prefabs/rock03.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Rocks/Prefabs/rock04.prefab",
                "Assets/SUIMONO - WATER SYSTEM 2/_DEMO/TERRAIN/Objects/Rocks/Prefabs/rock05.prefab",
            };
            wired += AssignPrefabArray(so, "deadGroundPrefabs", deadPaths, ref missing);

            // ── Wire ZoneShaderLinker reference ─────────────────────────

            ZoneShaderLinker zsl = Object.FindObjectOfType<ZoneShaderLinker>();
            if (zsl != null)
            {
                so.FindProperty("zoneLinker").objectReferenceValue = zsl;
                Debug.Log($"[Wave2] ZoneShaderLinker assigned to vegetation placer.");
                wired++;
            }
            else
            {
                Debug.LogWarning("[Wave2] ZoneShaderLinker not found — vegetation won't adapt to zones.");
                missing++;
            }

            so.ApplyModifiedProperties();

            // ── 2. SUIMONO Water — place in RealWorldRoot ───────────────

            PlaceSuimonoWater(ref wired, ref missing);

            // ── 3. Save scene ───────────────────────────────────────────

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // ── 4. Report ───────────────────────────────────────────────

            Debug.Log("══════════════════════════════════════════════");
            Debug.Log("[Wave2] Asset wiring complete.");
            Debug.Log($"[Wave2] Wired: {wired} assignments.");
            if (missing > 0)
                Debug.LogWarning($"[Wave2] Missing: {missing} item(s) — see warnings above.");
            else
                Debug.Log("[Wave2] All Wave 2 assets wired successfully.");
            Debug.Log("══════════════════════════════════════════════");
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static int AssignPrefabArray(SerializedObject so, string propertyName, string[] paths, ref int missing)
        {
            SerializedProperty prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[Wave2] Property '{propertyName}' not found on component.");
                missing++;
                return 0;
            }

            int loaded = 0;
            prop.arraySize = paths.Length;

            for (int i = 0; i < paths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                if (prefab != null)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
                    loaded++;
                }
                else
                {
                    Debug.LogWarning($"[Wave2] Prefab not found: {paths[i]}");
                    missing++;
                }
            }

            Debug.Log($"[Wave2] {propertyName}: {loaded}/{paths.Length} prefabs assigned.");
            return 1;
        }

        private static void PlaceSuimonoWater(ref int wired, ref int missing)
        {
            // Check if SUIMONO module already in scene
            if (GameObject.Find("SUIMONO_Module") != null)
            {
                Debug.Log("[Wave2] SUIMONO_Module already in scene, skipping.");
                wired++;
                return;
            }

            GameObject realWorld = GameObject.Find("RealWorldRoot");
            if (realWorld == null)
            {
                Debug.LogWarning("[Wave2] RealWorldRoot not found — cannot place water.");
                missing++;
                return;
            }

            // Instantiate SUIMONO module (manages the water system)
            GameObject modulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SUIMONO - WATER SYSTEM 2/PREFABS/SUIMONO_Module.prefab");

            if (modulePrefab != null)
            {
                GameObject module = (GameObject)PrefabUtility.InstantiatePrefab(modulePrefab, realWorld.transform);
                module.name = "SUIMONO_Module";
                module.transform.localPosition = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(module, "Create SUIMONO Module");
                Debug.Log("[Wave2] SUIMONO_Module placed under RealWorldRoot.");
                wired++;
            }
            else
            {
                Debug.LogWarning("[Wave2] SUIMONO_Module.prefab not found.");
                missing++;
            }

            // Instantiate water surface at Singapore river zone (lat 1.35, lon 103.82)
            // In Unity coords relative to CesiumGeoreference origin, this is near (0, 0, 0)
            GameObject surfacePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/SUIMONO - WATER SYSTEM 2/PREFABS/SUIMONO_Surface.prefab");

            if (surfacePrefab != null)
            {
                GameObject surface = (GameObject)PrefabUtility.InstantiatePrefab(surfacePrefab, realWorld.transform);
                surface.name = "River_WaterPlane";
                // Position at approximate Singapore River zone — slightly south of origin
                surface.transform.localPosition = new Vector3(0f, -2f, -200f);
                surface.transform.localScale = new Vector3(50f, 1f, 200f);
                Undo.RegisterCreatedObjectUndo(surface, "Create River Water Plane");
                Debug.Log("[Wave2] River_WaterPlane placed at waterway zone coordinates (0, -2, -200).");
                wired++;
            }
            else
            {
                Debug.LogWarning("[Wave2] SUIMONO_Surface.prefab not found.");
                missing++;
            }
        }
    }
}
#endif
