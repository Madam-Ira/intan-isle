using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// GPS Home Base — the player's real-world location becomes their in-game home.
///
/// First launch: reads device GPS (or editor fallback = Singapore).
/// Saves to PlayerPrefs. Places HomeAnchor at the GPS-resolved position.
/// Auto-tags home zone type from IntanIsleZoneData.
///
/// HomeAnchor glows with warm amber in VeiledWorld.
/// </summary>
public class PlayerHomeBase : MonoBehaviour
{
    // ── PlayerPrefs keys ──────────────────────────────────────────
    private const string PREF_LAT  = "HomeLatitude";
    private const string PREF_LON  = "HomeLongitude";
    private const string PREF_ZONE = "HomeZoneType";

    // ── Editor / device fallback ─────────────────────────────────
    private const double DEFAULT_LAT = 1.3521;   // Singapore
    private const double DEFAULT_LON = 103.8198;
    private const double SG_LAT      = 1.3521;
    private const double SG_LON      = 103.8198;

    [Header("Anchor Visuals")]
    [SerializeField] private GameObject homeAnchorPrefab;
    [SerializeField] private Light      homeAnchorLight;      // warm amber point light
    [SerializeField] private float      anchorGlowIntensity = 2.5f;

    // ── Locked palette — warm amber lantern #FF8C00 ───────────────
    private static readonly Color AmberGlow  = new Color(1.0f, 0.549f, 0.0f, 1f); // #FF8C00

    // ── Runtime state ─────────────────────────────────────────────
    public double    HomeLat     { get; private set; }
    public double    HomeLon     { get; private set; }
    public ZoneType  HomeZone    { get; private set; }
    public string    HomeZoneName{ get; private set; }
    public bool      IsSet       { get; private set; }

    private GameObject _anchorGO;

    // ─────────────────────────────────────────────────────────────

    IEnumerator Start()
    {
        yield return StartCoroutine(ResolveHomeLocation());
        PlaceAnchor();
    }

    void Update()
    {
        if (_anchorGO == null) return;

        // Glow in VeiledWorld, dim in PhysicalWorld
        bool veiled = VeiledWorldManager.InVeiledWorld;
        if (homeAnchorLight != null)
        {
            homeAnchorLight.color     = AmberGlow;
            homeAnchorLight.intensity = Mathf.MoveTowards(
                homeAnchorLight.intensity,
                veiled ? anchorGlowIntensity : 0.2f,
                Time.deltaTime * 2f);
        }
    }

    // ── GPS resolution ────────────────────────────────────────────

    private IEnumerator ResolveHomeLocation()
    {
        // If previously saved, use that
        if (PlayerPrefs.HasKey(PREF_LAT))
        {
            HomeLat = PlayerPrefs.GetFloat(PREF_LAT);
            HomeLon = PlayerPrefs.GetFloat(PREF_LON);
            HomeZone = (ZoneType)PlayerPrefs.GetInt(PREF_ZONE, 0);
            IsSet   = true;
            Debug.Log("[PlayerHomeBase] Loaded saved home: " + HomeLat + "°N, " + HomeLon + "°E  Zone: " + HomeZone);
            yield break;
        }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        // Request device GPS
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[PlayerHomeBase] GPS not enabled. Falling back to Singapore.");
            SetHome(DEFAULT_LAT, DEFAULT_LON);
            yield break;
        }

        Input.location.Start(10f, 5f);
        int timeout = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0)
        {
            yield return new WaitForSeconds(1f);
            timeout--;
        }

        if (Input.location.status == LocationServiceStatus.Running)
        {
            double lat = Input.location.lastData.latitude;
            double lon = Input.location.lastData.longitude;
            Input.location.Stop();
            SetHome(lat, lon);
        }
        else
        {
            Input.location.Stop();
            Debug.LogWarning("[PlayerHomeBase] GPS failed. Falling back to Singapore.");
            SetHome(DEFAULT_LAT, DEFAULT_LON);
        }
#else
        // Editor fallback
        SetHome(DEFAULT_LAT, DEFAULT_LON);
        yield break;
#endif
    }

    private void SetHome(double lat, double lon)
    {
        HomeLat  = lat;
        HomeLon  = lon;

        // Resolve zone
        var data = IntanIsleZoneData.Get();
        var zone = data?.GetDominantZone(lat, lon);
        HomeZone     = zone?.zoneType ?? ZoneType.FOREST;
        HomeZoneName = zone?.zoneName ?? "Open Region";

        // Save to PlayerPrefs
        PlayerPrefs.SetFloat(PREF_LAT,  (float)lat);
        PlayerPrefs.SetFloat(PREF_LON,  (float)lon);
        PlayerPrefs.SetInt(PREF_ZONE,   (int)HomeZone);
        PlayerPrefs.Save();

        IsSet = true;

        string ecologyMsg = HomeZone.IsToxic()
            ? "You carry the ecological stakes of a wounded land."
            : "You carry the memory of living land.";

        Debug.Log("[PlayerHomeBase] Home set: " + lat.ToString("F4") + "°N, " + lon.ToString("F4") + "°E"
            + "\n  Zone: " + HomeZone.DisplayName() + " — " + HomeZoneName
            + "\n  " + ecologyMsg);
    }

    // ── Place the HomeAnchor in world space ───────────────────────

    private void PlaceAnchor()
    {
        if (!IsSet) return;

        // World-space position: offset from Singapore origin
        const double M_PER_LAT = 111000.0;
        double mPerLon = M_PER_LAT * Math.Cos(SG_LAT * Math.PI / 180.0);
        float  wx      = (float)((HomeLon - SG_LON) * mPerLon);
        float  wz      = (float)((HomeLat - SG_LAT) * M_PER_LAT);
        Vector3 pos    = new Vector3(wx, 0f, wz);

        if (homeAnchorPrefab != null)
        {
            _anchorGO = Instantiate(homeAnchorPrefab, pos, Quaternion.identity);
            _anchorGO.name = "HomeAnchor";
        }
        else
        {
            // Fallback: simple glowing sphere
            _anchorGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _anchorGO.name = "HomeAnchor";
            _anchorGO.transform.position = pos;
            _anchorGO.transform.localScale = Vector3.one * 3f;
            var rend = _anchorGO.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_EmissionColor", AmberGlow * 2f);
                mat.EnableKeyword("_EMISSION");
                rend.material = mat;
            }

            // Point light — warm amber
            var lightGO    = new GameObject("HomeAnchorLight");
            lightGO.transform.SetParent(_anchorGO.transform);
            lightGO.transform.localPosition = Vector3.up * 2f;
            homeAnchorLight       = lightGO.AddComponent<Light>();
            homeAnchorLight.type  = LightType.Point;
            homeAnchorLight.color = AmberGlow;
            homeAnchorLight.range = 25f;
            homeAnchorLight.intensity = 0.2f;
        }

        Debug.Log("[PlayerHomeBase] HomeAnchor placed at world: " + pos);
    }
}
