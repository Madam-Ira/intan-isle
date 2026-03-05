using System;
using UnityEngine;

public enum BarakahSource
{
    Donation,
    HumaneCare,
    ZeroWaste,
    Consistency,
    Restraint
}

public class BarakahMeter : MonoBehaviour
{
    [SerializeField] private float barakahValue = 22f;

    public event Action<float, string> OnBarakahChanged;

    public float Value => barakahValue;

    public bool SadaqahModeActive => barakahValue >= 76f;

    public string CurrentLevelName => GetLevelName(barakahValue);

    private static string GetLevelName(float value)
    {
        if (value <= 30f) return "Seedling";
        if (value <= 55f) return "Cultivator";
        if (value <= 75f) return "Guardian";
        return "Shepherd";
    }

    public void AddBarakah(float amount, BarakahSource source)
    {
        barakahValue = Mathf.Clamp(barakahValue + amount, 0f, 100f);
        OnBarakahChanged?.Invoke(barakahValue, GetLevelName(barakahValue));
    }
}
