using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Biome definition containing full stratification, plant registry, visual language, and optimization settings.
/// Serves as a data container for biome initialization and scene setup.
/// </summary>
[CreateAssetMenu(fileName = "BiomeDef_", menuName = "Biome/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Header("Biome Identity")]
    public BiomeType biomeType;
    [TextArea(2, 4)]
    public string description;

    [Header("Plant Registry")]
    [Tooltip("All plants organized by stratification and category")]
    public PlantRegistry plantRegistry;

    [Header("Visual Language")]
    [Tooltip("Primary color palette for this biome")]
    public Color primaryColor = Color.green;
    public Color secondaryColor = Color.white;
    [Range(0.5f, 2f)]
    public float saturationMultiplier = 1f;

    [Header("Pollution Zone Override")]
    [Tooltip("For polluted zones: deep purples, crimson reds, ash grey, oil black")]
    public bool isPollutionZone;
    public Color pollutionPrimaryColor = new Color(0.4f, 0f, 0.6f);   // Deep purple
    public Color pollutionSecondaryColor = new Color(0.8f, 0.1f, 0.1f); // Crimson

    [Header("Performance & LOD")]
    [Range(100, 5000)]
    public int maxVisiblePlants = 500;
    public float cullDistance = 200f;
    [Tooltip("LOD distance thresholds (in meters)")]
    public Vector2 lodDistances = new Vector2(50f, 150f);

    [Header("Environmental Conditions")]
    [Range(0f, 1f)]
    public float humidity;
    [Range(-5f, 40f)]
    public float temperature;
    [Tooltip("Wet, moist, or dry groundcover")]
    public GroundType groundType = GroundType.Moist;

    /// <summary>
    /// Validate biome has all mandatory stratification components
    /// </summary>
    public bool IsValid()
    {
        if (plantRegistry == null)
        {
            Debug.LogWarning($"Biome {name} has no plant registry!", this);
            return false;
        }
        return plantRegistry.ValidateStratification();
    }

    /// <summary>
    /// Get visual color based on biome state (normal or polluted)
    /// </summary>
    public Color GetPrimaryColor()
    {
        return isPollutionZone ? pollutionPrimaryColor : primaryColor;
    }

    public Color GetSecondaryColor()
    {
        return isPollutionZone ? pollutionSecondaryColor : secondaryColor;
    }
}

/// <summary>
/// Ground conditions affecting plant visibility and growth
/// </summary>
public enum GroundType
{
    Wet,      // Swamps, waterfall zones
    Moist,    // Highland misty, lowland rainforest
    Dry,      // Pollution zones, degraded areas
    Rocky     // Cliff environments
}
