#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using IntanIsle.Core;

public static class BlessingWeightTablePopulator
{
    [MenuItem("Tools/Intan Isle/Create Blessing Weight Table")]
    public static void Create()
    {
        string folder = "Assets/_Game/Resources";
        string path = folder + "/BlessingWeightTable.asset";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/_Game", "Resources");

        BlessingWeightTable existing = AssetDatabase.LoadAssetAtPath<BlessingWeightTable>(path);
        if (existing != null)
        {
            Debug.Log("[BlessingWeights] Asset already exists, repopulating defaults.");
            Undo.RecordObject(existing, "Repopulate Blessing Weights");
            existing.PopulateDefaults();
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return;
        }

        BlessingWeightTable table = ScriptableObject.CreateInstance<BlessingWeightTable>();
        table.PopulateDefaults();
        AssetDatabase.CreateAsset(table, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"[BlessingWeights] Created at {path} with {table.Weights.Count} entries.");
    }
}
#endif
