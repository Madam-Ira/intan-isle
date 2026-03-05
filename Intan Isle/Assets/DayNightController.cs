using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [Header("Time")]
    [Range(0f, 1f)] public float timeOfDay = 0f; // 0 = day, 1 = night
    public bool autoCycle = false;
    public float cycleDurationSeconds = 600f; // 10 minutes

    [Header("Sun")]
    public Light sunLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Fog")]
    public Gradient fogColor;
    public AnimationCurve fogDensity;

    void Update()
    {
        if (autoCycle && cycleDurationSeconds > 0.1f)
        {
            timeOfDay += Time.deltaTime / cycleDurationSeconds;
            if (timeOfDay > 1f) timeOfDay = 0f;
        }

        ApplyLighting(timeOfDay);
    }

    void ApplyLighting(float t)
    {
        if (!sunLight) return;

        // Rotate sun from day (higher) to night (lower)
        sunLight.transform.rotation =
            Quaternion.Euler(Mathf.Lerp(50f, -30f, t), -30f, 0f);

        // Sun light
        sunLight.color = sunColor.Evaluate(t);
        sunLight.intensity = sunIntensity.Evaluate(t);

        // Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = fogColor.Evaluate(t);
        RenderSettings.fogDensity = fogDensity.Evaluate(t);
    }
}
