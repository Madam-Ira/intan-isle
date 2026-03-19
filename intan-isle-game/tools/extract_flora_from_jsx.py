#!/usr/bin/env python3
"""
Extract FLORA_DATA from the JSX component and save as master JSON.
Usage: python tools/extract_flora_from_jsx.py
"""
import json, re, os, sys

JSX_PATH = os.path.join(os.path.dirname(__file__), "..",
    "dashboard", "src", "components", "IntanIsle_FloraAssetBrowser_AllAsia.jsx")
OUTPUT = os.path.join(os.path.dirname(__file__), "..",
    "Assets", "_Game", "Resources", "Flora", "MASTER_AllAsia_480species.json")

def main():
    if not os.path.exists(JSX_PATH):
        print(f"ERROR: JSX not found at {JSX_PATH}")
        print("Save the Flora Browser JSX component first.")
        sys.exit(1)

    with open(JSX_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    # Find JSON.parse('...')
    match = re.search(r"JSON\.parse\('(\[.*?\])'\)", content, re.DOTALL)
    if not match:
        print("ERROR: Could not find JSON.parse('[...]') in JSX")
        sys.exit(1)

    raw = match.group(1)
    # Unescape the JSON string (it's inside single quotes in JS)
    data = json.loads(raw)

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)

    print(f"Extracted {len(data)} species to {OUTPUT}")
    print("Now run: python tools/flora_json_splitter.py")

if __name__ == "__main__":
    main()
