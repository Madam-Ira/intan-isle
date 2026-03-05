#if USING_URP
using JBooth.MicroVerseCore.Browser;
using UnityEngine;

namespace Rowlan.Biomes_Presets_4
{
    [ExecuteInEditMode]
    public class Skybox_URP : MonoBehaviour, IContentBrowserDropAction
    {
        public Material skybox;
        public Color realtimeShadowColor;

        public UnityEngine.Rendering.AmbientMode ambientMode;
        public Color ambientEquatorColor;
        public Color ambientSkyColor;
        public Color ambientGroundColor;
        

        public bool fogEnabled;
        public Color fogColor;
        public FogMode fogMode;
        public float density;
        public float fogStartDistance;
        public float fogEndDistance;
                
        public void Execute(out bool destroyAfterExecute)
        {

            Debug.Log($"Adding skybox {skybox} and fog");

            if (skybox != null)
            {
                RenderSettings.skybox = skybox;
                RenderSettings.subtractiveShadowColor = realtimeShadowColor;

                RenderSettings.fog = fogEnabled;
                RenderSettings.fogColor = fogColor;
                RenderSettings.fogMode = fogMode;
                RenderSettings.fogDensity = density;
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;

                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientEquatorColor = ambientEquatorColor;
                RenderSettings.ambientSkyColor = ambientSkyColor;
                RenderSettings.ambientGroundColor = ambientGroundColor;

                DynamicGI.UpdateEnvironment();
            }

            destroyAfterExecute = true;
        }
    }
}
#endif