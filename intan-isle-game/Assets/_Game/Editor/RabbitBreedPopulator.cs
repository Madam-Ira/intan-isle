#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using IntanIsle.Core;

public static class RabbitBreedPopulator
{
    [MenuItem("Tools/Intan Isle/Create Rabbit Breed Assets")]
    public static void CreateBreeds()
    {
        string folder = "Assets/_Game/Resources/Rabbits";

        if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/Rabbits"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources"))
                AssetDatabase.CreateFolder("Assets/_Game", "Resources");
            AssetDatabase.CreateFolder("Assets/_Game/Resources", "Rabbits");
        }

        int created = 0;

        created += CreateBreed(folder, "NZWhite", RabbitBreed.NZWhite,
            "New Zealand White — the standard commercial meat rabbit. Hardy, docile, and efficient.",
            0.04f, 0.06f, 0.3f, 15f, 25f,
            0.80f, 0.15f, 0.05f, 2.5f, 0.4f);

        created += CreateBreed(folder, "ChampagneDArgent", RabbitBreed.ChampagneDArgent,
            "Champagne d'Argent — heritage breed with silver fur. Known for rich, flavourful meat.",
            0.045f, 0.055f, 0.35f, 12f, 22f,
            0.78f, 0.17f, 0.05f, 2.8f, 0.85f);

        created += CreateBreed(folder, "Californian", RabbitBreed.Californian,
            "Californian — fast-growing dual-purpose breed. White body with dark points.",
            0.05f, 0.065f, 0.4f, 14f, 26f,
            0.82f, 0.13f, 0.05f, 2.3f, 0.5f);

        created += CreateBreed(folder, "Rex", RabbitBreed.Rex,
            "Rex — prized for velvety fur. Gentle temperament. Lower meat yield but exceptional pelts.",
            0.035f, 0.05f, 0.25f, 10f, 20f,
            0.75f, 0.18f, 0.07f, 1.8f, 0.95f);

        created += CreateBreed(folder, "TAMUK", RabbitBreed.TAMUK,
            "Texas A&M University-Kingsville composite — heat-resistant breed developed for tropical climates. Critical for equatorial farming.",
            0.04f, 0.07f, 0.9f, 20f, 38f,
            0.80f, 0.15f, 0.05f, 2.2f, 0.3f);

        AssetDatabase.SaveAssets();
        Debug.Log($"[RabbitBreeds] Created {created} breed asset(s) at {folder}/.");
    }

    private static int CreateBreed(string folder, string name, RabbitBreed breed,
        string desc, float feedDrain, float waterDrain, float heatRes,
        float tempMin, float tempMax, float hay, float pellets, float greens,
        float meatKg, float furQ)
    {
        string path = $"{folder}/{name}.asset";
        if (AssetDatabase.LoadAssetAtPath<RabbitScriptableObject>(path) != null)
        {
            Debug.Log($"[RabbitBreeds] {name} already exists, skipping.");
            return 0;
        }

        RabbitScriptableObject so = ScriptableObject.CreateInstance<RabbitScriptableObject>();

        // Use SerializedObject to set private [SerializeField] fields
        SerializedObject sso = new SerializedObject(so);
        sso.FindProperty("breedName").stringValue = name;
        sso.FindProperty("breedEnum").intValue = (int)breed;
        sso.FindProperty("description").stringValue = desc;
        sso.FindProperty("baseFeedDrainRate").floatValue = feedDrain;
        sso.FindProperty("baseWaterDrainRate").floatValue = waterDrain;
        sso.FindProperty("heatResistance").floatValue = heatRes;
        sso.FindProperty("optimalTempMin").floatValue = tempMin;
        sso.FindProperty("optimalTempMax").floatValue = tempMax;
        sso.FindProperty("feedRatioHay").floatValue = hay;
        sso.FindProperty("feedRatioPellets").floatValue = pellets;
        sso.FindProperty("feedRatioGreens").floatValue = greens;
        sso.FindProperty("meatYieldKg").floatValue = meatKg;
        sso.FindProperty("furQuality").floatValue = furQ;
        sso.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(so, path);
        Debug.Log($"[RabbitBreeds] Created {name} at {path}.");
        return 1;
    }
}
#endif
