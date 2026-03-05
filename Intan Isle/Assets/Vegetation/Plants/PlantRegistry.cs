using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Registry of all plants in a biome. Organizes plants by stratification layer and category.
/// Ensures every scene maintains full rainforest stratification (mandatory).
/// </summary>
[CreateAssetMenu(fileName = "BiomeRegistry_", menuName = "Biome/Plant Registry")]
public class PlantRegistry : ScriptableObject
{
    public BiomeType biomeType;
    
    [Header("Stratification Completeness")]
    [Tooltip("Full rainforest mandatory: Emergent, Canopy, Understory, Groundcover, Vines/Epiphytes, Aquatic")]
    public List<PlantDefinition> emergentLayer = new();
    public List<PlantDefinition> canopyLayer = new();
    public List<PlantDefinition> understoryLayer = new();
    public List<PlantDefinition> groundcoverLayer = new();
    public List<PlantDefinition> vinesEpiphytesLayer = new();
    public List<PlantDefinition> aquaticLayer = new();

    [Header("Food & Botanical Accuracy")]
    [Tooltip("Vegetables, herbs (culinary & medicinal), fruits - naturally embedded")]
    public List<PlantDefinition> foodPlants = new();
    public List<PlantDefinition> medicinalPlants = new();
    public List<PlantDefinition> poisonousPlants = new();

    [Header("Rabbit Forage Diversity")]
    public List<PlantDefinition> rabbitForage = new();

    [Header("Bioluminescent Assets")]
    [Tooltip("Plants supporting URP emission materials for in-house glow & hum effects")]
    public List<PlantDefinition> bioluminescentPlants = new();

    /// <summary>
    /// Validates that biome contains all mandatory stratification layers
    /// </summary>
    public bool ValidateStratification()
    {
        return emergentLayer.Count > 0 &&
               canopyLayer.Count > 0 &&
               understoryLayer.Count > 0 &&
               groundcoverLayer.Count > 0 &&
               vinesEpiphytesLayer.Count > 0 &&
               aquaticLayer.Count > 0;
    }

    /// <summary>
    /// Get all plants for a specific layer
    /// </summary>
    public List<PlantDefinition> GetPlantsByLayer(PlantLayer layer)
    {
        return layer switch
        {
            PlantLayer.Emergent => emergentLayer,
            PlantLayer.Canopy => canopyLayer,
            PlantLayer.Understory => understoryLayer,
            PlantLayer.Groundcover => groundcoverLayer,
            PlantLayer.VinesEpiphytes => vinesEpiphytesLayer,
            PlantLayer.Aquatic => aquaticLayer,
            _ => new List<PlantDefinition>()
        };
    }

    /// <summary>
    /// Get all plants for a specific category
    /// </summary>
    public List<PlantDefinition> GetPlantsByCategory(PlantCategory category)
    {
        return category switch
        {
            PlantCategory.Food => foodPlants,
            PlantCategory.Medicinal => medicinalPlants,
            PlantCategory.Toxic => poisonousPlants,
            _ => new List<PlantDefinition>()
        };
    }

    /// <summary>
    /// Get random plant from layer (for placement variation)
    /// </summary>
    public PlantDefinition GetRandomPlantFromLayer(PlantLayer layer)
    {
        var plants = GetPlantsByLayer(layer);
        if (plants.Count == 0) return null;
        return plants[Random.Range(0, plants.Count)];
    }
}
