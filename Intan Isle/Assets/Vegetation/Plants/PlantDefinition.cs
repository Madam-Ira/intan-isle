using UnityEngine;
using System;

/// <summary>
/// Defines a single plant species with botanical accuracy, categorization, and biome placement data.
/// Preserves real-world recognisability while supporting URP emission materials for bioluminescence.
/// </summary>
[CreateAssetMenu(fileName = "Plant_", menuName = "Biome/Plant Definition")]
public class PlantDefinition : ScriptableObject
{
    [Header("Identity")]
    public string commonName;
    public string scientificName;
    [TextArea(2, 4)]
    public string botanicalDescription;

    [Header("Categorization")]
    public PlantLayer plantLayer = PlantLayer.Groundcover;
    public PlantCategory category = PlantCategory.Structural;
    public PlantToxicity toxicity = PlantToxicity.NonToxic;
    public bool isEdible;
    public bool isMedicinal;
    public bool isBioluminescent;

    [Header("Rabbit Ecology")]
    [Tooltip("Visible plant forage for rabbit dietary diversity")]
    public bool isRabbitForage;
    public RabbitForageType forageType = RabbitForageType.Browse;

    [Header("Biome Distribution")]
    [Tooltip("Which biome types this plant appears in")]
    public BiomeType[] nativeBiomes;

    [Header("Visual Assets")]
    public GameObject prefab;
    [Range(1, 3)]
    public int lodLevels = 2;
    
    [Header("Bioluminescence")]
    [Tooltip("Emission material for glow effects; supports partial regions")]
    public Material emissionMaterial;
    [Range(0f, 1f)]
    public float emissionIntensity;

    [Header("Pollution Zone Variant")]
    [Tooltip("Intensified toxic/corrupted appearance in polluted zones")]
    public PlantDefinition pollutionVariant;
}

/// <summary>
/// Rainforest stratification layers - mandatory presence in every biome
/// </summary>
public enum PlantLayer
{
    Emergent,
    Canopy,
    Understory,
    Groundcover,
    VinesEpiphytes,
    Aquatic
}

/// <summary>
/// Plant functional categories for ecosystem representation
/// </summary>
public enum PlantCategory
{
    Structural,           // Trees, major canopy elements
    Food,                 // Fruits, vegetables, herbs
    Medicinal,            // Culinary & healing herbs
    Toxic,                // Poisonous species (accurate morphology)
    Vines,                // Climbers, trellised varieties
    Epiphytes,            // Mosses, lichens, air plants
    AquaticSemiAquatic    // Wetland & water-dependent plants
}

/// <summary>
/// Toxicity levels for botanical accuracy
/// </summary>
public enum PlantToxicity
{
    NonToxic,
    Mildly,
    Moderately,
    Highly,
    DeadlyPoisonous
}

/// <summary>
/// Rabbit forage diversity - visible across biomes
/// </summary>
public enum RabbitForageType
{
    HayGrass,   // Structural grasses
    Browse,     // Shrub leaves, twigs
    Herb,       // Culinary & medicinal herbs
    Vine,       // Climbing forage
    Fruit,      // Fruit-bearing plants
    Root        // Root vegetables
}

/// <summary>
/// All biome zones in Intan Isle ecosystem
/// </summary>
public enum BiomeType
{
    HighlandMistyRainforest,
    LowlandTropicalRainforest,
    WaterfallSanctuary,
    Rivers,
    Lakes,
    Swamps,
    EnchantedVillage,
    PollutionZone
}
