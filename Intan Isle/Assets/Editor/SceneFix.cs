using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tools > Intan Isle > Fix Pitch Black
///
/// Fixes the pitch-black game view caused by:
///   1. PlayerRig spawned at (0, 20, 0) — inside sculpted waterfall terrain geometry
///   2. VCam_FirstPerson with CinemachinePOV hijacking Mouse X/Y but no CinemachineBrain
///      to drive the actual camera (Cinemachine is inert without a Brain)
///   3. Camera potentially clipped underground
///
/// Actions:
///   1. Moves PlayerRig to (250, 50, 250) — centre of WaterfallSanctuary_Terrain, 50 m up
///      so gravity drops it onto whatever surface is visible
///   2. Disables VCam_FirstPerson to stop Cinemachine POV consuming mouse input
///   3. Verifies WaterfallSanctuary_Terrain is active
///   4. Disables CesiumWorldTerrain if present (avoid double-terrain conflict)
///   5. Ensures PlayerRig's MainCamera has no skybox override (uses scene skybox)
///   6. Marks scene dirty + saves
/// </summary>
public static class SceneFix
{
    [MenuItem("Tools/Intan Isle/Fix Pitch Black")]
    public static void FixPitchBlack()
    {
        bool anyChange = false;

        // ── 1. Teleport PlayerRig ─────────────────────────────────────────────
        var playerRig = GameObject.Find("PlayerRig");
        if (playerRig == null)
        {
            Debug.LogError("[SceneFix] PlayerRig not found in scene. Run Tools > Intan Isle > Create Player Rig first.");
            return;
        }

        Vector3 targetPos = new Vector3(250f, 50f, 250f);
        playerRig.transform.position = targetPos;
        EditorUtility.SetDirty(playerRig);
        anyChange = true;
        Debug.Log("[SceneFix] PlayerRig moved to " + targetPos);

        // ── 2. Disable VCam_FirstPerson ───────────────────────────────────────
        // The VCam has a CinemachinePOV child that reads Mouse X/Y.
        // Without a CinemachineBrain on the camera, Cinemachine cannot drive
        // the camera and instead only steals mouse input from PlayerLook.
        var vcam = playerRig.transform.Find("CameraRoot/VCam_FirstPerson");
        if (vcam == null)
        {
            // Also search the whole rig in case hierarchy differs
            foreach (Transform t in playerRig.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "VCam_FirstPerson") { vcam = t; break; }
            }
        }

        if (vcam != null)
        {
            if (vcam.gameObject.activeSelf)
            {
                vcam.gameObject.SetActive(false);
                EditorUtility.SetDirty(vcam.gameObject);
                anyChange = true;
                Debug.Log("[SceneFix] VCam_FirstPerson disabled — Cinemachine POV will no longer steal mouse input.");
            }
            else
            {
                Debug.Log("[SceneFix] VCam_FirstPerson already disabled.");
            }
        }
        else
        {
            Debug.LogWarning("[SceneFix] VCam_FirstPerson not found under PlayerRig — may already be removed.");
        }

        // ── 3. Verify WaterfallSanctuary_Terrain is active ───────────────────
        var terrainGO = GameObject.Find("WaterfallSanctuary_Terrain");
        if (terrainGO == null)
        {
            Debug.LogWarning("[SceneFix] WaterfallSanctuary_Terrain not found.");
        }
        else if (!terrainGO.activeSelf)
        {
            terrainGO.SetActive(true);
            EditorUtility.SetDirty(terrainGO);
            anyChange = true;
            Debug.Log("[SceneFix] WaterfallSanctuary_Terrain activated.");
        }
        else
        {
            Debug.Log("[SceneFix] WaterfallSanctuary_Terrain is active. OK.");
        }

        // ── 4. Disable CesiumWorldTerrain if present ─────────────────────────
        var cesiumTerrain = GameObject.Find("CesiumWorldTerrain");
        if (cesiumTerrain != null && cesiumTerrain.activeSelf)
        {
            cesiumTerrain.SetActive(false);
            EditorUtility.SetDirty(cesiumTerrain);
            anyChange = true;
            Debug.Log("[SceneFix] CesiumWorldTerrain disabled — using WaterfallSanctuary_Terrain instead.");
        }

        // ── 5. Fix camera clear flags ─────────────────────────────────────────
        // If the camera is using Depth-only or Nothing, it will show black.
        var mainCam = FindMainCameraUnder(playerRig);
        if (mainCam != null)
        {
            if (mainCam.clearFlags == CameraClearFlags.Depth || mainCam.clearFlags == CameraClearFlags.Nothing)
            {
                mainCam.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(mainCam);
                anyChange = true;
                Debug.Log("[SceneFix] MainCamera clearFlags set to Skybox.");
            }
            else
            {
                Debug.Log("[SceneFix] MainCamera clearFlags = " + mainCam.clearFlags + ". OK.");
            }

            // Ensure the camera is active
            if (!mainCam.gameObject.activeSelf)
            {
                mainCam.gameObject.SetActive(true);
                EditorUtility.SetDirty(mainCam.gameObject);
                anyChange = true;
                Debug.Log("[SceneFix] MainCamera GameObject activated.");
            }

            Debug.Log("[SceneFix] MainCamera world position: " + mainCam.transform.position
                      + "  rotation: " + mainCam.transform.eulerAngles);
        }
        else
        {
            Debug.LogWarning("[SceneFix] No Camera component found under PlayerRig.");
        }

        // ── 6. Report the scene-level original Main Camera ────────────────────
        // If there's a scene-level Main Camera that is disabled, warn.
        var allCameras = Object.FindObjectsOfType<Camera>(true); // includeInactive = true
        foreach (var cam in allCameras)
        {
            if (!cam.gameObject.activeSelf || !cam.enabled)
                Debug.LogWarning("[SceneFix] Disabled camera found: " + cam.gameObject.name
                                 + " (active=" + cam.gameObject.activeSelf + ", enabled=" + cam.enabled + ")");
        }

        // ── Save ──────────────────────────────────────────────────────────────
        if (anyChange)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("[SceneFix] Scene saved.");
        }

        Debug.Log("[SceneFix] Done.\n"
                  + "  PlayerRig: " + playerRig.transform.position + "\n"
                  + "  Press Play — the rig starts 50 m above terrain and gravity drops it down.\n"
                  + "  WASD to move, mouse to look. If still black, check URP Renderer in Project Settings.");
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static Camera FindMainCameraUnder(GameObject root)
    {
        foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            return cam;   // first one found
        return null;
    }
}
