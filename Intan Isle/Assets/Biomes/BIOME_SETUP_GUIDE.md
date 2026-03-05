# Biome Setup Guide - Highland Misty Rainforest Example

This guide walks through creating a complete biome scene from scratch using the biome framework.

## Step 1: Organize Plant Assets

Create `PlantDefinition` ScriptableObjects for all plants in your biome. Example structure:

```
Assets/Vegetation/Plants/
├── HighlandMisty/
│   ├── Plant_Ginger_HighlandMisty_Understory.asset
│   ├── Plant_Fern_HighlandMisty_Groundcover.asset
│   ├── Plant_MossyTree_HighlandMisty_Canopy.asset
│   ├── Plant_Mint_HighlandMisty_Groundcover.asset
│   └── [6+ more plants per stratification layer]
├── Lowland/
│   ├── Plant_Fig_LowlandTropical_Canopy.asset
│   └── [...]
└── [...other biomes...]
```

**Rule**: Minimum 6 plants (one per stratification layer), but aim for 10-15 per biome for visual diversity.

## Step 2: Create PlantRegistry

1. Right-click in `Assets/Biomes/HighlandMistyRainforest/`
2. Create > Biome > Plant Registry
3. Name: `BiomeRegistry_HighlandMisty`
4. Assign plants to their layers:
   - **Emergent**: [Large canopy trees]
   - **Canopy**: Mossy trees, epiphyte-covered specimens
   - **Understory**: Ginger, banana, fruit trees
   - **Groundcover**: Ferns, moss, grasses
   - **Vines/Epiphytes**: Climbing vines, mosses, orchids
   - **Aquatic**: Water lilies, streamside plants

5. Also categorize by function:
   - **Food Plants**: Ginger, mint, edible ferns
   - **Medicinal**: Ginger, mint, gotu kola
   - **Toxic**: (None in Highland Misty; focus on poison-free zone)
   - **Rabbit Forage**: Mint, grasses, herbs

6. Hit save. If editor script validates, you should see success feedback.

## Step 3: Create BiomeDefinition

1. Right-click in same folder
2. Create > Biome > Biome Definition
3. Name: `BiomeDef_HighlandMisty`
4. Configure:
   - **Biome Type**: HighlandMistyRainforest
   - **Plant Registry**: Drag in your BiomeRegistry_HighlandMisty
   - **Primary Color**: Cool emerald green `RGB(51, 128, 77)`
   - **Secondary Color**: Misty grey-green `RGB(179, 204, 179)`
   - **Saturation**: 1.1 (slightly enhanced)
   - **Pollution Zone**: OFF
   - **Environmental**:
     - Humidity: 0.85
     - Temperature: 18°C
     - Ground Type: Moist

5. Save.

## Step 4: Create Scene & Attach BiomeManager

1. Create new Unity scene: `Assets/Scenes/HighlandMistyRainforest.unity`
2. Create empty GameObject at root: `HighlandMisty_Biome`
3. Add Component > BiomeManager
4. Drag `BiomeDef_HighlandMisty` into the inspector field
5. Enable "Validate On Start" for debugging
6. Save scene

## Step 5: Verify Stratification

In the scene, select the BiomeManager GameObject and click Play. Check console:
- Should log: `"Biome initialized: HighlandMistyRainforest (Polluted: False)"`
- If any layers missing, you'll see: `"Biome HighlandMisty is missing stratification layers!"`

**Fix**: Go back to PlantRegistry and ensure all 6 layer lists have at least one plant.

## Step 6: Populate Scene with Plants

You have two options:

### Option A: Manual Placement (Simple, Editor-friendly)
1. Create child GameObjects under `HighlandMisty_Biome`
2. Drag plant prefabs into the scene at various heights
3. Group by layer in GameObject hierarchy for organization

### Option B: Procedural Placement (Requires Scripting)
Create a simple `BiomePlacementScript` that:
```csharp
void PlaceRandomPlants()
{
    var biomeManager = GetComponent<BiomeManager>();
    var registry = biomeManager.Registry;
    
    for (int i = 0; i < 50; i++)
    {
        var layer = (PlantLayer)(i % 6);  // Cycle through layers
        var plant = registry.GetRandomPlantFromLayer(layer);
        if (plant.prefab != null)
        {
            Instantiate(plant.prefab, RandomPosition(), Quaternion.identity);
        }
    }
}
```

## Step 7: Integrate with Cesium Terrain

If using Cesium geospatial terrain:
1. Add a `CesiumGeoreference` component to your scene root
2. Position the biome's origin at desired coordinates (lat/lon/height)
3. Place plant GameObjects relative to the georeference
4. Cesium will render terrain + your vegetation together

Example:
```csharp
// In a placement script
var geoRef = GetComponent<CesiumGeoreference>();
var worldPos = geoRef.ConvertGeographicToEcef(new double3(lat, lon, height));
Instantiate(prefab, worldPos, Quaternion.identity);
```

## Step 8: Test & Optimize

- [ ] Play scene; verify all 6 layers visible
- [ ] Check console for stratification warnings
- [ ] Profile performance (target: 60 FPS on iPhone 11)
- [ ] Adjust LOD distances if needed: `BiomeDef.lodDistances`
- [ ] Enable bioluminescence if desired: `biomeManager.EnableBioluminescence(true)`

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Missing stratification layers" | Add plants to ALL 6 layer lists in PlantRegistry |
| Plants invisible | Check prefab assignment in PlantDefinition; verify LOD 0 exists |
| Performance poor | Reduce `maxVisiblePlants` or increase `cullDistance` in BiomeDefinition |
| Wrong colors | Verify `primaryColor` / `secondaryColor` in BiomeDefinition match biome palette |
| Bioluminescence doesn't work | Ensure `emissionMaterial` is URP-compatible with `_EmissionIntensity` parameter |

## Next: Pollution Zone Variant

To create a pollution zone version of this biome:
1. Duplicate `BiomeDef_HighlandMisty` → `BiomeDef_HighlandMisty_Polluted`
2. Toggle **Pollution Zone**: ON
3. Colors auto-switch to purples/crimsons
4. Assign `pollutionVariant` plants in each PlantDefinition
5. When "cleansed," restore `isPollutionZone: false`
