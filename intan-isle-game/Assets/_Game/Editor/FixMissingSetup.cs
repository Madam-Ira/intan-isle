#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;

namespace IntanIsle.Core
{
    public static class FixMissingSetup
    {
        [MenuItem("Tools/Intan Isle/Fix Missing Setup")]
        public static void Fix()
        {
            int fixed_ = 0;

            // ═══════════════════════════════════════════════════════════
            // 1. PLAYER RIG (MEDIUM severity)
            // ═══════════════════════════════════════════════════════════

            GameObject existingRig = GameObject.Find("PlayerRig");
            if (existingRig != null)
            {
                Debug.Log("[FixSetup] PlayerRig already exists, skipping creation.");
            }
            else
            {
                // Create rig hierarchy
                GameObject rig = new GameObject("PlayerRig");
                rig.tag = "Player";
                Undo.RegisterCreatedObjectUndo(rig, "Create PlayerRig");

                // CharacterController
                CharacterController cc = rig.AddComponent<CharacterController>();
                cc.height = 1.75f;
                cc.radius = 0.3f;
                cc.center = new Vector3(0f, 0.875f, 0f);

                // Body (organisational child)
                GameObject body = new GameObject("Body");
                body.transform.SetParent(rig.transform, false);

                // CameraRoot at eye height
                GameObject camRoot = new GameObject("CameraRoot");
                camRoot.transform.SetParent(rig.transform, false);
                camRoot.transform.localPosition = new Vector3(0f, 1.65f, 0f);

                // MainCamera under CameraRoot
                GameObject camGO = new GameObject("MainCamera");
                camGO.tag = "MainCamera";
                camGO.transform.SetParent(camRoot.transform, false);
                Camera cam = camGO.AddComponent<Camera>();
                cam.fieldOfView = 60f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 10000f;
                camGO.AddComponent<AudioListener>();

                // Disable AudioListener on the old Main Camera
                Camera[] allCams = UnityEngine.Object.FindObjectsOfType<Camera>();
                foreach (Camera c in allCams)
                {
                    if (c.gameObject == camGO) continue;
                    AudioListener al = c.GetComponent<AudioListener>();
                    if (al != null)
                    {
                        al.enabled = false;
                        Debug.Log($"[FixSetup] Disabled AudioListener on old camera '{c.gameObject.name}'.");
                    }
                }

                // Add FirstPersonController via reflection
                Type fpcType = FindType("IntanIsle.Core.FirstPersonController");
                if (fpcType != null)
                {
                    Component fpc = rig.AddComponent(fpcType);

                    // Wire cameraRoot reference
                    SetSerializedField(fpc, "cameraRoot", camRoot.transform);

                    // Wire InputActionAsset
                    InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                        "Assets/_Game/Input/PlayerInput.inputactions");
                    if (inputAsset != null)
                    {
                        SetSerializedField(fpc, "inputActions", inputAsset);
                        Debug.Log("[FixSetup] InputActionAsset assigned to FirstPersonController.");
                    }
                    else
                    {
                        Debug.LogWarning("[FixSetup] PlayerInput.inputactions not found.");
                    }

                    Debug.Log("[FixSetup] FirstPersonController added to PlayerRig.");
                }
                else
                {
                    Debug.LogWarning("[FixSetup] FirstPersonController type not found.");
                }

                // Add BunianFlightController via reflection
                Type bfcType = FindType("IntanIsle.Core.BunianFlightController");
                if (bfcType != null)
                {
                    Component bfc = rig.AddComponent(bfcType);

                    // Wire mainCamera reference
                    SetSerializedField(bfc, "mainCamera", cam);
                    Debug.Log("[FixSetup] BunianFlightController added, mainCamera wired.");
                }
                else
                {
                    Debug.LogWarning("[FixSetup] BunianFlightController type not found.");
                }

                // Position at Singapore origin, slight elevation
                rig.transform.position = new Vector3(0f, 50f, 0f);

                Debug.Log("[FixSetup] PlayerRig created with full hierarchy.");
                fixed_++;
            }

            // Wire ProceduralVegetationPlacer.player to PlayerRig
            GameObject playerRig = GameObject.Find("PlayerRig");
            if (playerRig != null)
            {
                Type pvpType = FindType("IntanIsle.Core.ProceduralVegetationPlacer");
                if (pvpType != null)
                {
                    UnityEngine.Object pvp = UnityEngine.Object.FindObjectOfType(pvpType);
                    if (pvp != null)
                    {
                        SetSerializedField(pvp as Component, "player", playerRig.transform);
                        EditorUtility.SetDirty(pvp);
                        Debug.Log("[FixSetup] ProceduralVegetationPlacer.player wired to PlayerRig.");
                        fixed_++;
                    }
                }
            }

            // ═══════════════════════════════════════════════════════════
            // 2. EXTRA MANGROVE ZONES (LOW severity — only 4 defined)
            // ═══════════════════════════════════════════════════════════

            IntanIsleZoneData zoneData = AssetDatabase.LoadAssetAtPath<IntanIsleZoneData>(
                "Assets/_Game/Resources/IntanIsleZoneData.asset");

            if (zoneData != null)
            {
                Undo.RecordObject(zoneData, "Add Missing Zones");

                int before = zoneData.ZoneCount;

                // Extra mangroves (real-world locations)
                TryAddZone(zoneData, "Matang Mangroves Malaysia", 4.84f, 100.62f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Can Gio Mangrove Vietnam", 10.41f, 106.89f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Bhitarkanika Mangroves India", 20.73f, 86.87f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Sungei Buloh Singapore", 1.45f, 103.73f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Pasir Ris Mangroves Singapore", 1.38f, 103.95f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Mangrove Bay Abu Dhabi", 24.45f, 54.62f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Ranong Mangroves Thailand", 9.95f, 98.63f, ZoneType.MANGROVE);
                TryAddZone(zoneData, "Sabah Mangroves Borneo", 5.97f, 118.07f, ZoneType.MANGROVE);

                // Extra transboundary haze corridors
                TryAddZone(zoneData, "Kalimantan Haze Zone", -1.5f, 116.0f, ZoneType.TRANSBOUNDARY_HAZE);
                TryAddZone(zoneData, "Sumatra Peat Fires", 1.0f, 102.5f, ZoneType.TRANSBOUNDARY_HAZE);
                TryAddZone(zoneData, "Mekong Burn Zone", 18.0f, 103.0f, ZoneType.TRANSBOUNDARY_HAZE);
                TryAddZone(zoneData, "Punjab Stubble Burning", 30.5f, 75.5f, ZoneType.TRANSBOUNDARY_HAZE);

                int added = zoneData.ZoneCount - before;
                if (added > 0)
                {
                    EditorUtility.SetDirty(zoneData);
                    Debug.Log($"[FixSetup] Added {added} new zone(s) to IntanIsleZoneData. Total: {zoneData.ZoneCount}.");
                    fixed_++;
                }
                else
                {
                    Debug.Log("[FixSetup] All extra zones already present.");
                }
            }

            // ═══════════════════════════════════════════════════════════
            // SAVE
            // ═══════════════════════════════════════════════════════════

            AssetDatabase.SaveAssets();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("══════════════════════════════════════════════");
            Debug.Log($"[FixSetup] Complete — {fixed_} fix(es) applied.");
            Debug.Log("══════════════════════════════════════════════");
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = assembly.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        private static void SetSerializedField(Component comp, string fieldName, UnityEngine.Object value)
        {
            SerializedObject so = new SerializedObject(comp);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[FixSetup] Field '{fieldName}' not found on {comp.GetType().Name}.");
            }
        }

        private static void TryAddZone(IntanIsleZoneData data, string name, float lat, float lon, ZoneType type)
        {
            // Check if zone already exists (by name)
            foreach (var z in data.Zones)
            {
                if (z.name == name) return;
            }

            // Use reflection to call AddZone if it exists, otherwise add directly
            var method = data.GetType().GetMethod("AddZone",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(string), typeof(float), typeof(float), typeof(ZoneType) },
                null);

            if (method != null)
            {
                method.Invoke(data, new object[] { name, lat, lon, type });
            }
            else
            {
                Debug.LogWarning($"[FixSetup] AddZone method not found on IntanIsleZoneData. Zone '{name}' not added.");
            }
        }
    }
}
#endif
