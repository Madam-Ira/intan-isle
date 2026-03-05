using JBooth.MicroVerseCore.Browser;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rowlan.Biomes_Presets_4
{
    /// <summary>
    /// Instance type handling of presets. A category can exist only once in the microverse environment
    /// </summary>
    public class PresetInstance : MonoBehaviour, IContentBrowserDropAction
    {
        public enum Category
        {
            None,
            Sky,
            Fog,
            Water,
            SkyAndLight,
            Volume
        }

        /// <summary>
        /// What should happen when a duplicate instance is found?
        /// </summary>
        public enum DuplicateFoundAction
        {
            Hide,
            Destroy,
        }

        public Category category = Category.None;

        public DuplicateFoundAction duplicateFoundAction = DuplicateFoundAction.Destroy;

        /// <summary>
        /// List of gameobjects which should be activated after the preset was dragged into the scene.
        /// Example: Directional Light would briefly show an error in the console stating that only 1 directional light is allowed in a scene.
        /// </summary>
        [Tooltip("List of gameobjects which should be activated after the preset was dragged into the scene. In the preset they are deactivated like e. g. Directional Light")]
        public List<GameObject> delayedActivation = new List<GameObject>();

        #region ContentBrowser
        /// <summary>
        /// Execute an action after this prefab was dropped into the scene from the content browser
        /// </summary>
        /// <param name="destroyAfterExecute"></param>
        public void Execute(out bool destroyAfterExecute)
        {
            // assuming this is the only gameobject of the type in the hierarchy
            destroyAfterExecute = false;

            // find all of type
            PresetInstance[] instances = GameObject.FindObjectsByType<PresetInstance>(FindObjectsSortMode.None);

            // filter self
            PresetInstance[] self = instances
                .Where(x => x.transform == this.transform) // only self
                .ToArray();

            // filter others
            PresetInstance[] others = instances
                .Where(x => x.category == category) // same category
                .Where(x => x.isActiveAndEnabled) // exclude dactivated ones
                .Where(x => x.transform != this.transform) // exclude self
                .ToArray();

            // perform delayed activation (before objects get destroyed)
            foreach (PresetInstance instance in self)
            {
                foreach (GameObject go in instance.delayedActivation)
                {
                    go.gameObject.SetActive(true);
                }
            }

            foreach (PresetInstance instance in others)
            {
                // Debug.Log($"Prefab {timeOfDay.name} exists, applying settings to it");
                switch(duplicateFoundAction)
                {
                    case DuplicateFoundAction.Hide:

                        // Undo.RegisterCompleteObjectUndo(instance.gameObject, "Hide duplicate");

                        instance.transform.gameObject.SetActive(false);

                        break;

                    case DuplicateFoundAction.Destroy:
                        
                        // Undo.RegisterCompleteObjectUndo(instance.gameObject, "Delete duplicate");

                        DestroyImmediate(instance.gameObject);

                        break;

                    default: 
                        Debug.LogError($"Unsupported duplicate found action {duplicateFoundAction}");
                        break;

                }
                // the gameobject already existed, destroy this one
                destroyAfterExecute = false;
            }
        }
        #endregion ContentBrowser
    }
}
