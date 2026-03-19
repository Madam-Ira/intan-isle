# Flora Asset Data — Intan Isle: Clandestine
## 480 Species · 6 Regions · 10 Game Categories

### File Structure
```
Flora/
├── MASTER_AllAsia_480species.json          ← Single source of truth (place here)
├── IntanIsle_Flora_Orchids_AllRegions.json
├── IntanIsle_Flora_Trees_AllRegions.json
├── IntanIsle_Flora_Flowers_AllRegions.json
├── IntanIsle_Flora_Aquatic_and_Mangrove_AllRegions.json
├── IntanIsle_Flora_Fruits_AllRegions.json
├── IntanIsle_Flora_Shrubs_AllRegions.json
├── IntanIsle_Flora_Herbs_AllRegions.json
├── IntanIsle_Flora_Mushrooms_AllRegions.json
├── IntanIsle_Flora_Vegetables_AllRegions.json
└── IntanIsle_Flora_Carnivorous_and_Specialist_AllRegions.json
```

### How to generate category splits
1. Place `MASTER_AllAsia_480species.json` in this folder
2. Run: `python tools/flora_json_splitter.py`

### JSON Field Reference
| Key | Description | Example |
|-----|-------------|---------|
| id  | Asset ID (per region) | 1 |
| n   | Common name | "Dipterocarp / Meranti" |
| s   | Scientific name | "Shorea spp." |
| gc  | Game category | "Trees" |
| sc  | Sub-category | "Canopy Tree" |
| r   | Region | "SEA" |
| co  | Countries | "Malaysia, Indonesia" |
| h   | Habitat | "Lowland Rainforest" |
| cv  | Conservation status | "Vulnerable" |
| er  | Ecological role | "Canopy keystone" |
| sg  | Cultural/educational significance | "Primary timber" |
| ed  | Edible/medicinal/toxic | "None (timber)" |
| at  | Asset type (Static/Animated) | "Static" |
| sz  | Size range | "25-60m" |
| gl  | Veiled World glow description | "Deep emerald pulse" |
| tm  | TeamLab mood surface | "Entire trunk bark" |
| un  | Unity scene mode | "Quiet Forest" |

### Regions
- SEA (South-East Asia) — 190 species
- East Asia — 117 species  
- South Asia — 115 species
- Central Asia — 103 species
- Western Asia — 120 species
- North Asia — 107 species

### Categories
- Orchids: 21 · Trees: 90 · Flowers: 154 · Aquatic: 13
- Fruits: 36 · Shrubs: 20 · Herbs: 71 · Mushrooms: 15
- Vegetables: 53 · Carnivorous: 7
