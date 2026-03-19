#!/usr/bin/env python3
"""
Flora JSON Pack Splitter
========================
Reads MASTER_AllAsia_480species.json and generates:
- 10 category JSON files (Orchids, Trees, Flowers, etc.)
- Summary report

Usage:
  python flora_json_splitter.py

Input:  Assets/_Game/Resources/Flora/MASTER_AllAsia_480species.json
Output: Assets/_Game/Resources/Flora/IntanIsle_Flora_{Category}_AllRegions.json
"""

import json
import os
import sys

FLORA_DIR = os.path.join(os.path.dirname(__file__), "..",
                         "Assets", "_Game", "Resources", "Flora")

MASTER_FILE = os.path.join(FLORA_DIR, "MASTER_AllAsia_480species.json")

CATEGORIES = [
    "Orchids", "Trees", "Flowers", "Aquatic & Mangrove", "Fruits",
    "Shrubs", "Herbs", "Mushrooms", "Vegetables", "Carnivorous & Specialist"
]

REGIONS = ["SEA", "East Asia", "South Asia", "Central Asia", "Western Asia", "North Asia"]


def main():
    if not os.path.exists(MASTER_FILE):
        print(f"ERROR: Master file not found at {MASTER_FILE}")
        print("Place MASTER_AllAsia_480species.json in Assets/_Game/Resources/Flora/")
        sys.exit(1)

    with open(MASTER_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    print(f"Loaded {len(data)} species from master file.")
    print()

    # Category splits
    for cat in CATEGORIES:
        subset = [sp for sp in data if sp.get("gc") == cat]
        safe_name = cat.replace(" & ", "_and_").replace(" ", "_")
        filename = f"IntanIsle_Flora_{safe_name}_AllRegions.json"
        filepath = os.path.join(FLORA_DIR, filename)

        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(subset, f, indent=2, ensure_ascii=False)

        print(f"  {cat}: {len(subset)} species -> {filename}")

    print()

    # Region summary
    print("Region breakdown:")
    for region in REGIONS:
        count = len([sp for sp in data if sp.get("r") == region])
        print(f"  {region}: {count} species")

    print()

    # Conservation summary
    print("Conservation breakdown:")
    conservation_counts = {}
    for sp in data:
        cv = sp.get("cv", "Unknown")
        conservation_counts[cv] = conservation_counts.get(cv, 0) + 1
    for cv, count in sorted(conservation_counts.items(), key=lambda x: -x[1]):
        print(f"  {cv}: {count}")

    print()
    print(f"Done. {len(CATEGORIES)} category files written to {FLORA_DIR}")


if __name__ == "__main__":
    main()
