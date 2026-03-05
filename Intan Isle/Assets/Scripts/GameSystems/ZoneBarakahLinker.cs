using UnityEngine;

/// <summary>
/// Wires ZoneType + VeiledWorld state into BarakahMeter every frame.
///
/// PASSIVE VEILED DRAIN   — spiritual cost of crossing over: -0.5/s
///
/// HEALTHY ZONE RECOVERY  — always active, Physical or Veiled:
///   Ancient / Sacred Forest      +3.0/s
///   Protected Forest             +2.25/s
///   Forest / Mangrove            +1.5/s
///   Waterway                     +1.2/s
///   Kampung Heritage             +1.2/s
///   Food Security                +0.8/s
///
/// DEGRADED ZONE EXTRA DRAIN — Veiled World only (spiritual wounds):
///   Toxic                        -3.0/s
///   Pollution                    -2.0/s
///   River Pollution              -1.0/s
///   Transboundary Haze           -1.0/s
///   Coral Degradation            -0.8/s
///   Deforestation                -0.5/s
///
/// Attach to: PlayerRig or a persistent GameSystems object.
/// </summary>
public class ZoneBarakahLinker : MonoBehaviour
{
    [Header("Passive Veiled World Drain (per second)")]
    [SerializeField] private float veiledPassiveDrain = 0.5f;

    [Header("Healthy Zone Recovery (per second)")]
    [SerializeField] private float recoveryAncient   = 3.0f;
    [SerializeField] private float recoveryForest    = 1.5f;
    [SerializeField] private float recoveryKampung   = 1.2f;
    [SerializeField] private float recoveryFood      = 0.8f;

    [Header("Degraded Zone Extra Drain — Veiled Only (per second)")]
    [SerializeField] private float drainToxic        = 3.0f;
    [SerializeField] private float drainPollution    = 2.0f;
    [SerializeField] private float drainRiver        = 1.0f;
    [SerializeField] private float drainHaze         = 1.0f;
    [SerializeField] private float drainCoral        = 0.8f;
    [SerializeField] private float drainDeforest     = 0.5f;

    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter barakahMeter;

    // Zone is re-read from the shader global every 0.5 s (cheap)
    private float    _zoneTimer   = 0f;
    private ZoneType _currentZone = ZoneType.FOREST;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (barakahMeter == null) barakahMeter = FindObjectOfType<BarakahMeter>();
    }

    void Update()
    {
        if (barakahMeter == null) return;

        // Refresh zone type from shader global every 0.5 s
        _zoneTimer += Time.deltaTime;
        if (_zoneTimer >= 0.5f)
        {
            _zoneTimer    = 0f;
            _currentZone  = (ZoneType)(int)Shader.GetGlobalFloat("_IntanIsle_ZoneType");
        }

        float dt     = Time.deltaTime;
        bool  veiled = VeiledWorldManager.InVeiledWorld;

        // ── 1. Passive Veiled drain ───────────────────────────────
        if (veiled)
            barakahMeter.AddBarakah(-veiledPassiveDrain * dt, BarakahSource.Restraint);

        // ── 2. Healthy zone recovery (Physical or Veiled) ─────────
        float recovery = RecoveryRate(_currentZone);
        if (recovery > 0f)
            barakahMeter.AddBarakah(recovery * dt, BarakahSource.HumaneCare);

        // ── 3. Degraded zone extra drain (Veiled only) ────────────
        if (veiled)
        {
            float extra = DegradedDrain(_currentZone);
            if (extra > 0f)
                barakahMeter.AddBarakah(-extra * dt, BarakahSource.Restraint);
        }
    }

    // ─────────────────────────────────────────────────────────────

    private float RecoveryRate(ZoneType z) => z switch
    {
        ZoneType.ANCIENT_FOREST    => recoveryAncient,
        ZoneType.SACRED_FOREST     => recoveryAncient,
        ZoneType.PROTECTED_FOREST  => recoveryAncient * 0.75f,
        ZoneType.FOREST            => recoveryForest,
        ZoneType.MANGROVE          => recoveryForest,
        ZoneType.WATERWAY          => recoveryKampung,
        ZoneType.KAMPUNG_HERITAGE  => recoveryKampung,
        ZoneType.FOOD_SECURITY     => recoveryFood,
        _                          => 0f,
    };

    private float DegradedDrain(ZoneType z) => z switch
    {
        ZoneType.TOXIC              => drainToxic,
        ZoneType.POLLUTION          => drainPollution,
        ZoneType.RIVER_POLLUTION    => drainRiver,
        ZoneType.TRANSBOUNDARY_HAZE => drainHaze,
        ZoneType.CORAL_DEGRADATION  => drainCoral,
        ZoneType.DEFORESTATION      => drainDeforest,
        _                           => 0f,
    };

    // ── Editor preview ────────────────────────────────────────────
#if UNITY_EDITOR
    void OnValidate()
    {
        veiledPassiveDrain = Mathf.Max(0f, veiledPassiveDrain);
        recoveryAncient    = Mathf.Max(0f, recoveryAncient);
        recoveryForest     = Mathf.Max(0f, recoveryForest);
        recoveryKampung    = Mathf.Max(0f, recoveryKampung);
        recoveryFood       = Mathf.Max(0f, recoveryFood);
    }
#endif
}
