using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages biome initialization, plant registry queries, and visual state (normal/polluted).
/// Attached to biome root GameObject; coordinates stratification validation and placement systems.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    [SerializeField] private BiomeDefinition biomeDefinition;
    [SerializeField] private bool validateOnStart = true;

    private PlantRegistry registry;
    private bool isInitialized;

    public BiomeType BiomeType => biomeDefinition.biomeType;
    public PlantRegistry Registry => registry;
    public bool IsPolluted => biomeDefinition.isPollutionZone;

    void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Initialize biome and validate stratification (mandatory presence of all layers)
    /// </summary>
    public void Initialize()
    {
        if (biomeDefinition == null)
        {
            Debug.LogError("BiomeManager: No BiomeDefinition assigned!", gameObject);
            return;
        }

        registry = biomeDefinition.plantRegistry;

        if (validateOnStart && !biomeDefinition.IsValid())
        {
            Debug.LogWarning($"Biome {biomeDefinition.name} is missing stratification layers!", gameObject);
        }

        isInitialized = true;
        Debug.Log($"Biome initialized: {BiomeType} (Polluted: {IsPolluted})", gameObject);
    }

    /// <summary>
    /// Get random plant from specific layer (for procedural placement)
    /// </summary>
    public PlantDefinition GetRandomPlantFromLayer(PlantLayer layer)
    {
        if (!isInitialized) Initialize();
        return registry?.GetRandomPlantFromLayer(layer);
    }

    /// <summary>
    /// Get all plants of a category (food, medicinal, toxic)
    /// </summary>
    public List<PlantDefinition> GetPlantsByCategory(PlantCategory category)
    {
        if (!isInitialized) Initialize();
        return registry?.GetPlantsByCategory(category) ?? new List<PlantDefinition>();
    }

    /// <summary>
    /// Get visual color for this biome (respects pollution state)
    /// </summary>
    public Color GetPrimaryColor() => biomeDefinition.GetPrimaryColor();
    public Color GetSecondaryColor() => biomeDefinition.GetSecondaryColor();

    /// <summary>
    /// Apply bioluminescence effect (glow & hum support via emission materials)
    /// </summary>
    public void EnableBioluminescence(bool enable)
    {
        if (registry?.bioluminescentPlants == null) return;

        foreach (var plant in registry.bioluminescentPlants)
        {
            if (plant.emissionMaterial != null)
            {
                plant.emissionMaterial.SetFloat("_EmissionIntensity",
                    enable ? plant.emissionIntensity : 0f);
            }
        }
    }

    public BiomeDefinition GetBiomeDefinition() => biomeDefinition;
}
