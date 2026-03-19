#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IntanIsle.Core
{
    public static class FixCesiumMaterial
    {
        [MenuItem("Tools/Intan Isle/Fix Cesium Magenta")]
        public static void Fix()
        {
            // Try Cesium Unlit first (simplest, no shadow variants to break)
            // then Cesium Default, then custom HLSL fallback
            Material mat = Resources.Load<Material>("CesiumUnlitTilesetMaterial");
            string source = "CesiumUnlitTilesetMaterial";

            if (mat == null)
            {
                mat = Resources.Load<Material>("CesiumDefaultTilesetMaterial");
                source = "CesiumDefaultTilesetMaterial";
            }

            if (mat == null)
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/_Game/Materials/CesiumTerrain_URP.mat");
                source = "CesiumTerrain_URP (custom fallback)";
            }

            if (mat == null)
            {
                Debug.LogError("[FixCesium] No suitable material found.");
                return;
            }

            Debug.Log($"[FixCesium] Using: {source}");
            Debug.Log($"[FixCesium] Shader: {mat.shader.name}");
            Debug.Log($"[FixCesium] Shader OK: {mat.shader.isSupported}");

            // Find Cesium3DTileset via reflection
            Type tilesetType = Type.GetType("CesiumForUnity.Cesium3DTileset, CesiumForUnity");
            if (tilesetType == null)
            {
                Debug.LogError("[FixCesium] Cesium3DTileset type not found.");
                return;
            }

            PropertyInfo opaqueProp = tilesetType.GetProperty("opaqueMaterial");
            PropertyInfo suspendProp = tilesetType.GetProperty("suspendUpdate");
            if (opaqueProp == null)
            {
                Debug.LogError("[FixCesium] opaqueMaterial property not found.");
                return;
            }

            UnityEngine.Object[] tilesets = UnityEngine.Object.FindObjectsOfType(tilesetType, true);
            Debug.Log($"[FixCesium] Found {tilesets.Length} tileset(s).");

            // Phase 1: suspend + assign material
            foreach (UnityEngine.Object tileset in tilesets)
            {
                Component comp = tileset as Component;
                if (comp == null) continue;

                Undo.RecordObject(tileset, "Fix Cesium Material");

                if (suspendProp != null)
                    suspendProp.SetValue(tileset, true);

                opaqueProp.SetValue(tileset, mat);
                EditorUtility.SetDirty(tileset);

                Material verify = opaqueProp.GetValue(tileset) as Material;
                Debug.Log($"[FixCesium] '{comp.gameObject.name}': assigned={verify != null}, mat={verify?.name}");
            }

            // Phase 2: unsuspend to reload
            foreach (UnityEngine.Object tileset in tilesets)
            {
                if (suspendProp != null)
                    suspendProp.SetValue(tileset, false);
                EditorUtility.SetDirty(tileset);
            }

            // Save
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[FixCesium] Done — tiles reloading.");
        }
    }
}
#endif
