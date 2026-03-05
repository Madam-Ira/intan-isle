using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public static class PlayerRigSetup
{
    [MenuItem("Tools/Intan Isle/Create Player Rig")]
    public static void CreatePlayerRig()
    {
        // ── Guard: replace any existing PlayerRig ────────────────────
        var existing = GameObject.Find("PlayerRig");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "PlayerRig exists",
                "A PlayerRig already exists in the scene. Replace it?",
                "Replace", "Cancel");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // ── 1. PlayerRig root ─────────────────────────────────────────
        // CharacterController MUST be on the root so CC.Move() moves
        // every child (CameraRoot, MainCamera) together.
        var rigGO = new GameObject("PlayerRig");
        Undo.RegisterCreatedObjectUndo(rigGO, "Create PlayerRig");
        rigGO.transform.position = Vector3.zero;

        var cc = Undo.AddComponent<CharacterController>(rigGO);
        cc.height = 1.75f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.875f, 0f);

        Undo.AddComponent<PlayerMovement>(rigGO);

        // ── 2. Body — empty child for visual reference only ───────────
        var bodyGO = new GameObject("Body");
        Undo.RegisterCreatedObjectUndo(bodyGO, "Create Body");
        bodyGO.transform.SetParent(rigGO.transform, false);

        // ── 3. CameraRoot (eye height) ────────────────────────────────
        var camRootGO = new GameObject("CameraRoot");
        Undo.RegisterCreatedObjectUndo(camRootGO, "Create CameraRoot");
        camRootGO.transform.SetParent(rigGO.transform, false);
        camRootGO.transform.localPosition = new Vector3(0f, 1.65f, 0f);

        // ── 4. MainCamera ─────────────────────────────────────────────
        var camGO = new GameObject("MainCamera");
        Undo.RegisterCreatedObjectUndo(camGO, "Create MainCamera");
        camGO.transform.SetParent(camRootGO.transform, false);
        camGO.tag = "MainCamera";

        var cam = Undo.AddComponent<Camera>(camGO);
        cam.nearClipPlane = 0.05f;
        cam.fieldOfView   = 75f;
        Undo.AddComponent<AudioListener>(camGO);

        // Disable the scene's original Main Camera to avoid conflicts
        foreach (var other in Object.FindObjectsOfType<Camera>())
        {
            if (other == cam) continue;
            Undo.RecordObject(other.gameObject, "Disable old camera");
            other.gameObject.SetActive(false);
            Debug.LogWarning("[PlayerRigSetup] Disabled camera: " + other.gameObject.name);
        }

        // ── 5. PlayerLook ─────────────────────────────────────────────
        // Yaw  → rotates PlayerRig (transform.rotation in PlayerLook)
        // Pitch → rotates CameraRoot (cameraRoot.localRotation)
        var look = Undo.AddComponent<PlayerLook>(rigGO);
        look.cameraRoot = camRootGO.transform;

        // ── 6. Cinemachine VCam ───────────────────────────────────────
        var vcamGO = new GameObject("VCam_FirstPerson");
        Undo.RegisterCreatedObjectUndo(vcamGO, "Create VCam");
        vcamGO.transform.SetParent(rigGO.transform, false);

        var vcam = Undo.AddComponent<CinemachineVirtualCamera>(vcamGO);
        vcam.Follow               = camRootGO.transform;
        vcam.m_Lens.FieldOfView   = 75f;
        vcam.m_Lens.NearClipPlane = 0.05f;
        vcam.AddCinemachineComponent<CinemachinePOV>();

        // ── 7. Report component list ──────────────────────────────────
        Debug.Log(
            "[PlayerRigSetup] ✓ PlayerRig created at world (0,0,0).\n" +
            "Components on PlayerRig:\n" +
            "  • CharacterController  height=1.75  radius=0.3  center=(0,0.875,0)\n" +
            "  • PlayerMovement       walkSpeed=2.2\n" +
            "  • PlayerLook           cameraRoot=" + camRootGO.name + "\n" +
            "Children:\n" +
            "  • Body          (empty)\n" +
            "  • CameraRoot    localPos=(0,1.65,0)\n" +
            "      └ MainCamera  tag=MainCamera  near=0.05  FOV=75\n" +
            "  • VCam_FirstPerson  Follow=CameraRoot\n\n" +
            "ACTION REQUIRED: Move PlayerRig near WaterfallSanctuary_Terrain, then press Ctrl+S to save the scene."
        );

        // ── 8. Force-mark scene dirty and save ────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = rigGO;
        EditorGUIUtility.PingObject(rigGO);
    }

    [MenuItem("Tools/Intan Isle/Diagnose Player Rig")]
    public static void DiagnosePlayerRig()
    {
        var rig = GameObject.Find("PlayerRig");
        if (rig == null)
        {
            Debug.LogError("[Diagnose] NO PlayerRig found in scene. Run 'Create Player Rig' first, then press Ctrl+S to save.");
            return;
        }

        var cc  = rig.GetComponent<CharacterController>();
        var pm  = rig.GetComponent<PlayerMovement>();
        var pl  = rig.GetComponent<PlayerLook>();
        var cam = rig.GetComponentInChildren<Camera>();

        Debug.Log(
            "[Diagnose] PlayerRig found.\n" +
            "  Active:             " + rig.activeInHierarchy + "\n" +
            "  Position:           " + rig.transform.position + "\n" +
            "  CharacterController: " + (cc != null ? "✓ enabled=" + cc.enabled : "MISSING") + "\n" +
            "  PlayerMovement:      " + (pm != null ? "✓ enabled=" + pm.enabled : "MISSING") + "\n" +
            "  PlayerLook:          " + (pl != null ? "✓ enabled=" + pl.enabled + " cameraRoot=" + (pl.cameraRoot != null ? pl.cameraRoot.name : "NULL") : "MISSING") + "\n" +
            "  Camera (child):      " + (cam != null ? "✓ " + cam.gameObject.name : "MISSING")
        );

        // Check for duplicates
        var allRigs = Object.FindObjectsOfType<PlayerMovement>();
        if (allRigs.Length > 1)
            Debug.LogWarning("[Diagnose] " + allRigs.Length + " PlayerMovement components found in scene — DELETE duplicates!");
        else
            Debug.Log("[Diagnose] No duplicate PlayerMovement components.");
    }
}
