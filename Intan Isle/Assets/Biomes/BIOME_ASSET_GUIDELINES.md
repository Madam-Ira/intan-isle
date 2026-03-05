# Intan Isle Biome System - Asset Documentation

## Overview
This document defines the botanical accuracy standards, plant categorization, and visual language for all rainforest biomes in Intan Isle. All scenes must include full rainforest stratification (canopy, understory, groundcover, vines, epiphytes, aquatic plants) with no sparse or cleared areas.

## Mandatory Rainforest Stratification

Every biome must contain plants from all six layers:

| Layer | Role | Examples |
|-------|------|----------|
| **Emergent** | Tallest trees breaking above canopy | Fig, Dipterocarp, Hardwoods |
| **Canopy** | Dense upper foliage layer | Canopy trees, mossy trunks |
| **Understory** | Mid-height shrubs and young trees | Ginger, banana, fruit trees |
| **Groundcover** | Low flora and leaf litter | Grasses, moss, ferns, leaf litter |
| **Vines/Epiphytes** | Climbing plants and air-dependent flora | Flowering vines, mosses, lichens, epiphytes |
| **Aquatic** | Water-dependent vegetation | Water lilies, aquatic herbs, semi-aquatic plants |

**Validation Rule**: Before declaring a scene complete, run `BiomeManager.IsValid()` to confirm all layers are present.

---

## Biome Specifications

### Highland Misty Rainforest (Cameron Highlands Inspired)

**Visual Language**: Cool greens, mist layers, emerald saturation
- **Canopy**: Mossy trunks, dense epiphyte coverage
- **Understory**: Edible ferns, medicinal vines, young fruit trees
- **Groundcover**: Moss, leaf litter, mountain grasses
- **Vines**: Flowering & medicinal varieties
- **Food/Toxic**: Edible ferns, culinary herbs; toxic mushrooms
- **Fauna**: Insects, birds, small mammals
- **Environmental**: High humidity (0.85), cool temperature (15-20°C)

**Key Plant Families**: Araceae (arums), Orchidaceae, Pteridophyta (ferns), Zingiberaceae (ginger family)

---

### Lowland Tropical Rainforest

**Visual Language**: Vibrant greens, bright fruits, warm saturation
- **Canopy**: Dipterocarps, figs, large hardwoods
- **Understory**: Banana, ginger shrubs, fruit-bearing bushes
- **Groundcover**: Tropical grasses, ferns, dense litter
- **Vines**: Fruiting vines, poisonous lookalikes
- **Food/Toxic**: Fruits, spices, toxic plant mimics
- **Fauna**: Reptiles, amphibians, insects
- **Environmental**: Very high humidity (0.90), warm (25-30°C)

**Key Plant Families**: Dipterocarpaceae, Moraceae (figs), Musaceae (bananas), Zingiberaceae

---

### Waterfall Sanctuary (Top & Bottom Zones)

**Visual Language**: Aquatic blues, mist effects, flowing water
- **Canopy**: Cliff-edge trees, overhanging branches
- **Understory**: Riverbank bushes, wetland shrubs
- **Groundcover**: Wet grasses, moss, riverside herbaceous plants
- **Vines**: Hanging vines, water-tolerant climbers
- **Food/Toxic**: Aquatic herbs, edible water plants; unsafe algae and water parasites
- **Fauna**: Fish, frogs, aquatic insects, birds
- **Environmental**: Very high humidity (0.95), flowing water presence

**Key Plant Families**: Nymphaeaceae (water lilies), Araceae, Equisetaceae (horsetails)

---

### Rivers, Lakes & Swamps

**Visual Language**: Water-dominant, wet banks, algae-rich
- **Aquatic Plants**: Water lilies, pond plants, submerged herbs
- **Bankside**: Semi-aquatic rushes, cattails, wetland grasses
- **Swamp Trees**: Specialized flood-tolerant species
- **Toxic Elements**: Unsafe algae blooms, parasitic plants
- **Fauna**: Fish, amphibians, water birds, insects
- **Environmental**: Saturated ground (1.0 humidity), variable temperature

**Botanical Note**: All water plants must show realistic growth patterns (emergent, floating, submerged).

---

### Enchanted Village (Treepods, Caves, Gardens)

**Visual Language**: Cultivated greenery, garden organization, warm earth tones
- **Canopy**: Integrated canopy structure for treepo integration
- **Understory**: Deliberate gardens, culinary herbs, medicinal plots
- **Groundcover**: Hay grasses, cultivated lawn areas
- **Vines**: Trellised vines, trained climbers
- **Food/Toxic**: Vegetables, culinary herbs; prep-dependent toxic foods (e.g., cassava requires processing)
- **Fauna**: Rabbits, birds, beneficial insects
- **Environmental**: Managed moisture (0.65), moderate temperature (20-25°C)

**Key Plant Families**: Brassicaceae (cabbages), Apiaceae (herbs), Cucurbitaceae (squashes), Solanaceae (peppers)

---

### Pollution Zones

**Visual Language**: **NO GREEN**. Use deep purples, crimson reds, ash grey, oil black
- **Canopy**: Wilted, leafless or sickly branches
- **Understory**: Sick shrubs, stunted growth, discolored foliage
- **Groundcover**: Dead grasses, ash, barren patches
- **Vines**: Corrupted vines, twisted forms, unnatural colors
- **Food/Toxic**: Intensified toxic variants, hyperaccumulated toxins
- **Fauna**: Dramatically reduced fauna; sickly animals where present
- **Environmental**: Degraded soil (0.3 humidity), extreme temperatures (too hot or cold)

**Restoration Rule**: When pollution is cleansed, zones return to their true green biome appearance with fauna and clear water.

**Color Palette**:
- Deep Purple: RGB(102, 0, 153)
- Crimson Red: RGB(204, 0, 0)
- Ash Grey: RGB(150, 150, 150)
- Oil Black: RGB(20, 20, 20)

---

## Botanical Accuracy Doctrine

### Recognition & Stylization
- **Design preserves real-world recognisability**: Shape, color, and growth context must match actual species
- **Stylization enhances saturation** but must not mislead about species identity
- **Glow and hum are secondary signals only**: Bioluminescence is for atmosphere/communication, not morphological definition

### Morphological Accuracy Examples

| Plant | Must Show | Cannot Omit |
|-------|-----------|-------------|
| **Ginger** | Rhizome node, compound leaves, parallel venation | True candle-like inflorescence |
| **Banana** | Large compound leaves, tree-like herbaceous stem | Hanging fruit bunches in growth stages |
| **Ferns** | Pinnate/pinnately-compound fronds, fiddlehead spiral buds | Sporangia on frond undersides (educational value) |
| **Vines** | Climbing mechanism (tendrils, aerial rootlets, twining stems) | Realistic growth path following support |
| **Epiphytes** | Air-dependent growth on host branches | No soil contact (except orchids with pseudo-bulbs) |
| **Poisonous Species** | Distinctive shape/color/pattern (e.g., bright berries, umbrella-like caps) | Cannot resemble safe lookalikes exactly |

---

## Food, Medicinal & Poisonous Plants

### Edible Food Plants (Must Be Embedded Across Scenes)

**Vegetables**:
- Cassava, taro, yam, ginger, turmeric, lemongrass
- Leafy greens: morning glory, amaranth, wild spinach
- Cruciferous: mustard greens, bok choy

**Herbs (Culinary & Medicinal)**:
- Basil, mint, coriander, cilantro, parsley
- Lemongrass, galangal, turmeric
- Thyme, oregano, sage

**Fruits**:
- Tree fruits: mango, papaya, coconut, calamansi
- Bush fruits: passion fruit, guava, raspberry
- Vine fruits: pumpkin, melon, passion fruit

**Mushrooms**:
- Oyster mushroom, shiitake, enoki
- Medicinal: reishi, ganoderma

---

### Medicinal Plants (With Real Active Compounds)

- **Turmeric** (*Curcuma longa*): Curcumin for inflammation
- **Ginger** (*Zingiber officinale*): Gingerol for digestion
- **Mint** (*Mentha* spp.): Menthol for respiratory function
- **Lemongrass** (*Cymbopogon*): Citral for antimicrobial effects
- **Gotu Kola** (*Centella asiatica*): Triterpenoids for cognition
- **Basil** (*Ocimum sanctum*): Essential oils for antioxidant activity

---

### Poisonous & Toxic Plants (Accurate Morphology)

**Highly Toxic (Deadly)**:
- **Ricinus communis** (Castor bean): Large palmately-lobed leaves, spiky seed pods
- **Strychnos nux-vomica** (Strychnine tree): Small leaves, orange berries
- **Nicotiana** species: Funnel-shaped flowers, sticky leaves

**Moderately Toxic**:
- **Datura species** (Angel's trumpet): Trumpet-shaped flowers, spiky seed pods
- **Aconitum** (Monkshood): Purple hood-shaped flowers
- **Dieffenbachia**: Variegated leaves, calcium oxalate crystals

**Mildly Toxic/Preparation-Dependent**:
- **Cassava** (*Manihot esculenta*): Must process to remove cyanogenic glucosides
- **Kidney beans** (*Phaseolus vulgaris*): Raw seeds contain phytohaemagglutinin; must cook
- **Green potatoes**: Contain solanine; must remove green portions

**Visual Cue Guidelines**:
- Poisonous plants should have distinctive visual markers (bright colors, unusual shapes)
- Must NOT perfectly resemble safe lookalikes
- Poisonous mushrooms often show: bright colors, unusual caps, deformed stems

---

## Rabbit Ecology & Breed-Specific Forage

### Breeds to Model Accurately

| Breed | Origin | Characteristics | Forage Preference |
|-------|--------|-----------------|-------------------|
| **New Zealand White** | New Zealand | Large, white, pink eyes | Hay, browse, herbs |
| **Champagne d'Argent** | France | Silver-blue coat, large frame | Varied greens, grasses |
| **Argent Brun** | France | Brown-gold coat, medium size | Tender herbs, fruits |
| **TAMUK** | Texas A&M | Heat-tolerant, varied colors | Hot-climate browse, succulents |

### Visible Forage Diversity (Must Appear Across All Biomes)

**Hay Grasses**:
- Timothy grass, orchard grass, fescue
- Visual: Green tufts, seed heads, dried golden varieties

**Browse (Shrub Leaves & Twigs)**:
- Willow, hazel, apple tree branches
- Visual: Leafy branches at rabbit-reachable height, bark variety

**Herbs**:
- Parsley, cilantro, basil, mint, clover
- Visual: Leafy plants in gardens and wild patches

**Vines**:
- Climbing beans, pumpkin vines, grape vines
- Visual: Trailing growth with edible leaves and tendrils

**Fruits**:
- Apple, pear, papaya, guava, berries
- Visual: Fruits on trees/vines at various ripeness stages

**Roots**:
- Carrot, parsnip, beet, turnip
- Visual: Green tops emerging from soil

---

## Bioluminescent Compatibility

### URP Emission Material Support

All bioluminescent plants must:
1. Use URP-compatible emission materials
2. Support partial emission regions (not full-plant glow)
3. Have configurable `_EmissionIntensity` parameter (0.0 to 1.0)

### Implementation Example

```csharp
// In Material:
// - Set Emission Color to desired glow color
// - Enable Emission in URP Forward Renderer
// - Use emission as a SECONDARY SIGNAL only

// In PlantDefinition:
public Material emissionMaterial;
[Range(0f, 1f)]
public float emissionIntensity = 0.5f;

// Called by BiomeManager:
emissionMaterial.SetFloat("_EmissionIntensity", emissionIntensity);
```

### Allowed Glow Plants
- Bioluminescent fungi (faintly glowing caps)
- Glowing mosses and lichens
- Firefly-attracting flowers (subtle glow)
- Magical/mythical flora (enchanted regions only)

---

## Asset Naming Conventions

All plant prefabs and materials must follow this naming scheme:

```
Plant_[CommonName]_[BiomeType]_[Layer]_[LOD]

Examples:
- Plant_Ginger_HighlandMisty_Understory_LOD0.prefab
- Plant_Cassava_EnchantedVillage_Groundcover_LOD1.prefab
- Plant_Willow_WaterfallSanctuary_VinesEpiphytes_LOD0.prefab
- Plant_WiltedTree_PollutionZone_Canopy_LOD1.prefab
```

### Folder Structure
```
Assets/
├── Vegetation/
│   ├── Plants/           # PlantDefinition ScriptableObjects
│   │   ├── HighlandMisty/
│   │   ├── Lowland/
│   │   ├── Waterfall/
│   │   └── Village/
│   └── Prefabs/          # 3D Models & LOD variants
│       ├── Canopy/
│       ├── Understory/
│       ├── Groundcover/
│       └── VinesEpiphytes/
└── Biomes/
    ├── BiomeDefinition.cs
    ├── BiomeManager.cs
    ├── HighlandMistyRainforest/
    │   ├── BiomeDef_HighlandMisty.asset
    │   └── PlantRegistry_HighlandMisty.asset
    └── [Other biomes...]
```

---

## Quality Checklist for Each Biome

- [ ] All 6 stratification layers present with multiple plant varieties
- [ ] Food plants (vegetables, herbs, fruits) naturally embedded
- [ ] Medicinal plants visible and identifiable
- [ ] Poisonous plants marked with distinctive morphology
- [ ] Rabbit forage diversity visible (hay, browse, herbs, vines, fruits, roots)
- [ ] Plant prefabs optimized with LOD levels (max 3)
- [ ] Bioluminescent plants assigned emission materials (URP-compatible)
- [ ] Biome color palette applied (or pollution palette for corrupted zones)
- [ ] `BiomeManager.IsValid()` returns true
- [ ] No green in pollution zones (purple/red/grey/black only)
- [ ] Mobile optimization: prefabs under target poly count, textures atlassed

---

## References & Further Reading

- **FAO Edible Plants Database**: Real-world edible plant identification
- **Tropicos.org**: Botanical taxonomy and distribution
- **IPNI (International Plant Names Index)**: Scientific naming standards
- **Poison Control Databases**: Accurate toxicity data and visual markers
