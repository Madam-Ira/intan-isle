using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rowlan.Biomes_Presets_4
{
    /// <summary>
    /// List all directional lights in the inspector. Show a warning if more than 1 is active.
    /// Used eg for dragging in directional lights from the content browser. The user should instantly see a warning that eg the default directional light is still active.
    /// </summary>
    [CustomEditor(typeof(LightValidation))]
    public class LightValidationEditor : Editor
    {
        private LightValidation editorTarget;

        public void OnEnable()
        {
            editorTarget = target as LightValidation;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            base.OnInspectorGUI();

            ProcessInspectorLights();

            serializedObject.ApplyModifiedProperties();
        }

        private void ProcessInspectorLights()
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (lights.Length == 0)
                return;

            EditorGUILayout.LabelField("Directional Lights in Scene", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            {

                // filter directional lights
                lights = lights.Where(x => x.type == LightType.Directional).ToArray();

                // filter by current and others
                List<Light> presetLights = lights.Where(x => x.transform.IsChildOf(editorTarget.transform)).ToList();
                List<Light> otherLights = lights.Where(x => !x.transform.IsChildOf(editorTarget.transform)).ToList();

                // inspector
                DrawLightInspectorGroup("Preset", presetLights);
                DrawLightInspectorGroup("Other", otherLights);

                // warning if there are more than 1 active directional lights
                int activeCount = presetLights.Where(x => x.transform.gameObject.activeInHierarchy).Count() + otherLights.Where(x => x.transform.gameObject.activeInHierarchy).Count();
                MessageType mt = activeCount > 1 ? MessageType.Warning : MessageType.None;
                EditorGUILayout.HelpBox("There's only 1 directional light allowed in Unity at a time for eg shadow casting", mt);

            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draw group of lights in inspector
        /// </summary>
        /// <param name="header"></param>
        /// <param name="lights"></param>
        private void DrawLightInspectorGroup(string header, List<Light> lights)
        {
            if (lights.Count == 0)
                return;

            EditorGUILayout.LabelField(header, EditorStyles.miniBoldLabel);

            EditorGUI.indentLevel++;
            {
                lights.ToList().ForEach(x => DrawLightInspector(x));
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Draw single light in inspector
        /// </summary>
        /// <param name="light"></param>
        private void DrawLightInspector(Light light)
        {
            bool active = light.gameObject.activeInHierarchy;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                active = EditorGUILayout.Toggle(light.transform.name, active);

                if (check.changed)
                {
                    light.gameObject.SetActive(active);
                }
            }
        }
    }
}