using System;
using UnityEngine;

public class AWFWaterSystem : MonoBehaviour
{
    public float waterCollected = 0f;
    public float maxCapacity = 20f;
    public int upgradeTier = 1;

    public event Action<float> OnWaterChanged;

    private static AWFWaterSystem _instance;
    public static AWFWaterSystem Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    private float CollectionAmount()
    {
        return upgradeTier switch
        {
            1 => 4f,
            2 => 8f,
            3 => 12f,
            _ => 4f
        };
    }

    public void CollectWater()
    {
        waterCollected = Mathf.Min(waterCollected + CollectionAmount(), maxCapacity);
        OnWaterChanged?.Invoke(waterCollected);
    }

    public bool UseWater(float amount)
    {
        if (waterCollected < amount) return false;
        waterCollected -= amount;
        OnWaterChanged?.Invoke(waterCollected);
        return true;
    }
}
