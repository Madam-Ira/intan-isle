#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace IntanIsle.Core
{
    /// <summary>
    /// Editor-only script that bootstraps the Intan Isle scene by creating
    /// the _GameManager GameObject, adding required components, and wiring
    /// scene references. Run via <c>Tools &gt; Intan Isle &gt; Bootstrap Scene</c>.
    /// </summary>
    public static class GameBootstrap
    {
        private const string GameManagerName = "_GameManager";

        [MenuItem("Tools/Intan Isle/Bootstrap Scene")]
        public static void BootstrapScene()
        {
            int assigned = 0;
            int missing = 0;

            // ── 1. Find or create _GameManager ─────────────────────────

            GameObject gmGO = GameObject.Find(GameManagerName);

            if (gmGO == null)
            {
                gmGO = new GameObject(GameManagerName);
                Undo.RegisterCreatedObjectUndo(gmGO, "Create _GameManager");
                Debug.Log($"[Bootstrap] Created {GameManagerName}.");
            }
            else
            {
                Debug.Log($"[Bootstrap] Found existing {GameManagerName}.");
            }

            // ── 2. Ensure core components on _GameManager ──────────────

            GameManager gm = EnsureComponent<GameManager>(gmGO);
            EnsureComponent<NurAINConnector>(gmGO);
            EnsureComponent<ArkShieldLayer>(gmGO);

            // ── 3. Ensure scene manager components exist ─────────────────
            //
            // The EnvironmentSetup script creates named GameObjects under
            // _Managers but does NOT add MonoBehaviour components. We add
            // them here so they can be found and assigned.

            EnsureSceneManagerComponent<ZoneShaderLinker>("ZoneShaderLinker");
            EnsureSceneManagerComponent<EmotionalDayNightCycle>("EmotionalDayNightCycle");
            EnsureSceneManagerComponent<BlessingMeterController>("BlessingMeterController");
            EnsureSceneManagerComponent<VeiledWorldManager>("VeiledWorldManager");

            // ── 4. Find and assign references to GameManager ────────────

            SerializedObject so = new SerializedObject(gm);

            assigned += AssignRef<ZoneShaderLinker>(so, "zoneShaderLinkerRef", ref missing);
            assigned += AssignRef<EmotionalDayNightCycle>(so, "emotionalDayNightCycleRef", ref missing);
            assigned += AssignRef<BlessingMeterController>(so, "blessingMeterControllerRef", ref missing);
            assigned += AssignRef<VeiledWorldManager>(so, "veiledWorldManagerRef", ref missing);

            so.ApplyModifiedProperties();

            // ── 5. Save scene ──────────────────────────────────────────

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // ── 6. Report ──────────────────────────────────────────────

            Debug.Log("══════════════════════════════════════════════");
            Debug.Log("[Bootstrap] Scene bootstrap complete.");
            Debug.Log($"[Bootstrap] Assigned: {assigned} / 4 references.");
            if (missing > 0)
                Debug.LogWarning($"[Bootstrap] Missing: {missing} reference(s) — see warnings above.");
            else
                Debug.Log("[Bootstrap] All references wired successfully.");
            Debug.Log("══════════════════════════════════════════════");
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            if (existing != null) return existing;

            T added = Undo.AddComponent<T>(go);
            Debug.Log($"[Bootstrap] Added {typeof(T).Name} to {go.name}.");
            return added;
        }

        /// <summary>
        /// Ensures a MonoBehaviour component exists somewhere in the scene.
        /// First checks via FindObjectOfType. If not found, looks for a
        /// GameObject with a matching name (e.g. under _Managers) and adds
        /// the component to it.
        /// </summary>
        private static void EnsureSceneManagerComponent<T>(string goName) where T : MonoBehaviour
        {
            if (Object.FindObjectOfType<T>() != null) return;

            GameObject go = GameObject.Find(goName);
            if (go == null)
                go = GameObject.Find("_Managers/" + goName);

            if (go != null)
            {
                Undo.AddComponent<T>(go);
                Debug.Log($"[Bootstrap] Added {typeof(T).Name} component to existing {go.name}.");
            }
            else
            {
                go = new GameObject(goName);
                Undo.RegisterCreatedObjectUndo(go, $"Create {goName}");
                Undo.AddComponent<T>(go);
                Debug.Log($"[Bootstrap] Created {goName} with {typeof(T).Name} component.");
            }
        }

        private static int AssignRef<T>(SerializedObject so, string propName, ref int missing) where T : MonoBehaviour
        {
            T found = Object.FindObjectOfType<T>();
            if (found != null)
            {
                so.FindProperty(propName).objectReferenceValue = found;
                Debug.Log($"[Bootstrap] {typeof(T).Name} assigned (on {found.gameObject.name}).");
                return 1;
            }

            Debug.LogWarning($"[Bootstrap] {typeof(T).Name} NOT FOUND after ensure step.");
            missing++;
            return 0;
        }
    }
}
#endif
