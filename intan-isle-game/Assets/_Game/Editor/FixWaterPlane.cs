#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IntanIsle.Core
{
    public static class FixWaterPlane
    {
        [MenuItem("Tools/Intan Isle/Fix Water Plane")]
        public static void Fix()
        {
            // Delete old SUIMONO objects (including inactive)
            DeleteByName("River_WaterPlane");
            DeleteByName("SUIMONO_Module");

            // Create URP water plane under RealWorldRoot
            GameObject realWorld = GameObject.Find("RealWorldRoot");
            if (realWorld == null)
            {
                Debug.LogError("[FixWater] RealWorldRoot not found.");
                return;
            }

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "River_WaterPlane";
            water.transform.SetParent(realWorld.transform);
            water.transform.localPosition = new Vector3(0f, -2f, -200f);
            water.transform.localScale = new Vector3(10f, 1f, 40f);
            Undo.RegisterCreatedObjectUndo(water, "Create URP Water Plane");

            // Assign URP water material
            Material waterMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Game/Materials/River_Water_URP.mat");

            if (waterMat != null)
            {
                MeshRenderer mr = water.GetComponent<MeshRenderer>();
                mr.sharedMaterial = waterMat;
                Debug.Log("[FixWater] URP water material assigned.");
            }
            else
            {
                Debug.LogWarning("[FixWater] River_Water_URP.mat not found.");
            }

            // Remove collider (water plane doesn't need physics)
            MeshCollider col = water.GetComponent<MeshCollider>();
            if (col != null)
                Object.DestroyImmediate(col);

            // Save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[FixWater] Done — SUIMONO removed, URP water plane created.");
        }

        private static void DeleteByName(string name)
        {
            // Find all objects including inactive
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in all)
            {
                if (t.name == name && t.gameObject.scene.isLoaded)
                {
                    Debug.Log($"[FixWater] Deleting '{name}'.");
                    Undo.DestroyObjectImmediate(t.gameObject);
                }
            }
        }
    }
}
#endif
