using UnityEngine;

namespace IntanIsle.Core
{
    /// <summary>
    /// Registers the Day 1 vertical slice polluted zone in the
    /// <see cref="PollutionZoneManager"/> on Start. ONE polluted zone,
    /// ONE healing action — as specified in the locked Day 1 scope.
    /// </summary>
    public class DefaultZoneBootstrap : MonoBehaviour
    {
        [Header("Day 1 Polluted Zone")]
        [SerializeField] private string zoneId = "jurong_island";
        [SerializeField] private string biomeName = "wetland";
        [SerializeField] private float initialPollution = 0.75f;

        [Header("Farming Zone")]
        [SerializeField] private string farmZoneId = "lim_chu_kang";
        [SerializeField] private string farmBiome = "forest";

        private void Start()
        {
            PollutionZoneManager pollution = PollutionZoneManager.Instance;
            if (pollution == null)
            {
                Debug.LogWarning("[DefaultZoneBootstrap] PollutionZoneManager not found.");
                return;
            }

            // Register the one polluted zone for the vertical slice
            pollution.RegisterZone(zoneId, biomeName, initialPollution);
            Debug.Log($"[DefaultZoneBootstrap] Registered polluted zone: {zoneId} ({biomeName}) at {initialPollution:P0}.");

            // Register the farming zone (clean)
            pollution.RegisterZone(farmZoneId, farmBiome, 0f);
            pollution.SetFarmingZone(farmZoneId);
            Debug.Log($"[DefaultZoneBootstrap] Registered farming zone: {farmZoneId} (clean).");

            // Set player to the polluted zone initially
            pollution.SetPlayerZone(zoneId);
        }
    }
}
