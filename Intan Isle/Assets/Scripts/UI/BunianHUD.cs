using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bunian HUD — minimal, translucent. Visible ONLY in Bunian form.
///
/// Layout:
///   Top-left:    GPS coordinates (live, lat/lon)
///   Below GPS:   Zone name + zone type
///   Top-right:   Altitude in metres above sea level
///   Centre-bottom: Veil Strain bar (thin, orchid purple)
///
/// Aesthetic: TeamLab light text on dark translucent panel.
/// No hard borders. Font: clean, warm white.
/// </summary>
public class BunianHUD : MonoBehaviour
{
    // ── Locked palette ─────────────────────────────────────────────
    private static readonly Color PanelColor    = new Color(0.0f, 0.05f, 0.05f, 0.55f);
    private static readonly Color TextColor      = new Color(0.98f, 0.98f, 0.96f, 1.0f); // warm white
    private static readonly Color VeilBarColor   = new Color(0.608f, 0.349f, 0.714f, 1.0f); // orchid #9B59B6
    private static readonly Color VeilBGColor    = new Color(0.1f, 0.05f, 0.12f, 0.6f);

    // ── References (auto-built if null) ──────────────────────────
    [Header("Linked Systems")]
    [SerializeField] private BarakahMeter       barakahMeter;
    [SerializeField] private BunianFlightController flightController;
    [SerializeField] private ZoneShaderLinker   zoneLinker;

    // ── UI elements (built at Awake) ──────────────────────────────
    private Canvas          _canvas;
    private CanvasGroup     _group;
    private TextMeshProUGUI _coordsText;
    private TextMeshProUGUI _zoneText;
    private TextMeshProUGUI _altText;
    private TextMeshProUGUI _timeText;
    private TextMeshProUGUI _tideText;
    private Image           _veilBarFill;
    private Image           _veilBarBG;
    private TextMeshProUGUI _formBadge;     // "✦ BUNIAN FORM" / "◦ PHYSICAL FORM"

    // ── Runtime ───────────────────────────────────────────────────
    private float _updateInterval = 0.25f;
    private float _timer          = 0f;
    private float _targetAlpha    = 0f;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        BuildHUD();

        if (barakahMeter    == null) barakahMeter    = FindObjectOfType<BarakahMeter>();
        if (flightController == null) flightController = FindObjectOfType<BunianFlightController>();
        if (zoneLinker       == null) zoneLinker       = FindObjectOfType<ZoneShaderLinker>();
    }

    void Update()
    {
        bool inVeiled = VeiledWorldManager.InVeiledWorld;
        bool inFlight = flightController != null && flightController.IsFlying;
        _targetAlpha  = (inVeiled || inFlight) ? 1f : 0f;
        _group.alpha  = Mathf.MoveTowards(_group.alpha, _targetAlpha, Time.deltaTime * 1.5f);
        _group.blocksRaycasts = inVeiled || inFlight;

        if (!inVeiled && !inFlight) return;

        _timer += Time.deltaTime;
        if (_timer < _updateInterval) return;
        _timer = 0f;

        RefreshHUD();
    }

    // ── Refresh data ──────────────────────────────────────────────

    private void RefreshHUD()
    {
        // GPS from CesiumGlobeAnchor on PlayerRig
        double lat = 0, lon = 0;
        var playerRig = GameObject.Find("PlayerRig");
        if (playerRig != null)
        {
            var anchor = playerRig.GetComponent<CesiumForUnity.CesiumGlobeAnchor>();
            if (anchor != null)
            {
                lon = anchor.longitudeLatitudeHeight.x;
                lat = anchor.longitudeLatitudeHeight.y;
            }
        }

        // Zone from ZoneData
        var data     = IntanIsleZoneData.Get();
        var zone     = data?.GetDominantZone(lat, lon);
        string zName = zone != null ? zone.zoneName : "Open Region";
        string zType = zone != null ? zone.zoneType.DisplayName() : "Forest";

        // Altitude from flight controller
        float alt = flightController != null ? (float)flightController.Altitude : 0f;

        // Veil Strain (inverse of Barakah — higher barakah = lower strain)
        float barakah    = barakahMeter != null ? barakahMeter.Value : 100f;
        float veilStrain = 1f - Mathf.Clamp01(barakah / 100f);

        // Update UI text
        if (_coordsText != null)
            _coordsText.text = $"{(lat >= 0 ? "N" : "S")}{Math.Abs(lat):F4}°  {(lon >= 0 ? "E" : "W")}{Math.Abs(lon):F4}°";

        if (_zoneText != null)
            _zoneText.text = $"{zName}\n<size=70%><color=#9B59B6>{zType}</color></size>";

        if (_altText != null)
            _altText.text = $"{alt:F0} m";

        if (_timeText != null)
        {
            if (TimeZoneService.Instance != null)
            {
                var lt  = TimeZoneService.Instance.LocalTime;
                var tz  = TimeZoneService.Instance;
                _timeText.text = $"{lt:HH:mm}  {tz.TimeZoneAbbr}\n" +
                                 $"<size=75%>{tz.UTCOffsetLabel}</size>";
            }
            else
            {
                _timeText.text = System.DateTime.UtcNow.ToString("HH:mm") + "  UTC";
            }
        }

        if (_tideText != null)
        {
            _tideText.text = TideService.Instance != null
                ? TideService.Instance.HUDString()
                : "TIDE  —";
        }

        if (_veilBarFill != null)
            _veilBarFill.fillAmount = veilStrain;

        if (_formBadge != null)
        {
            bool flying = flightController != null && flightController.IsFlying;
            _formBadge.text = flying ? "✦ BUNIAN FORM" : "◦ PHYSICAL FORM";
        }
    }

    // ── Build HUD UI at runtime ───────────────────────────────────

    private void BuildHUD()
    {
        // Canvas
        var canvasGO = new GameObject("BunianHUD_Canvas");
        canvasGO.transform.SetParent(transform);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        _group          = canvasGO.AddComponent<CanvasGroup>();
        _group.alpha    = 0f;
        _group.blocksRaycasts = false;

        // Top-left panel: GPS + Zone
        var tlPanel       = MakePanel(canvasGO.transform,
            new Vector2(0, 1), new Vector2(0, 1),     // anchor: top-left
            new Vector2(20, -20),                      // position
            new Vector2(320, 90));
        _coordsText        = MakeLabel(tlPanel.transform, "", new Vector2(0, 1), new Vector2(10, -8),  14f);
        _zoneText          = MakeLabel(tlPanel.transform, "", new Vector2(0, 1), new Vector2(10, -32), 13f);

        // Top-right panel: Altitude + Local Time
        var trPanel        = MakePanel(canvasGO.transform,
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20),
            new Vector2(180, 80));
        _altText           = MakeLabel(trPanel.transform, "0 m",  new Vector2(1, 1), new Vector2(-10, -10), 14f);
        _altText.alignment = TextAlignmentOptions.TopRight;
        _timeText          = MakeLabel(trPanel.transform, "--:--", new Vector2(1, 1), new Vector2(-10, -34), 13f);
        _timeText.alignment= TextAlignmentOptions.TopRight;

        // Bottom-left: Tide panel
        var tidePanel  = MakePanel(canvasGO.transform,
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(20, 55),
            new Vector2(200, 50));
        var tideLabel  = MakeLabel(tidePanel.transform, "TIDE", new Vector2(0, 1), new Vector2(10, -6), 9f);
        tideLabel.color = new Color(0.3f, 0.7f, 0.9f, 0.7f);
        _tideText       = MakeLabel(tidePanel.transform, "—", new Vector2(0, 1), new Vector2(10, -20), 12f);

        // Bottom-centre: Veil Strain bar
        BuildVeilBar(canvasGO.transform);

        // Top-centre: Form badge
        var badgeGO  = new GameObject("FormBadge");
        badgeGO.transform.SetParent(canvasGO.transform, false);
        var badgeRT  = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin = new Vector2(0.5f, 1f);
        badgeRT.anchorMax = new Vector2(0.5f, 1f);
        badgeRT.pivot     = new Vector2(0.5f, 1f);
        badgeRT.anchoredPosition = new Vector2(0, -12f);
        badgeRT.sizeDelta = new Vector2(240f, 24f);
        _formBadge           = badgeGO.AddComponent<TextMeshProUGUI>();
        _formBadge.fontSize  = 11f;
        _formBadge.color     = new Color(0.608f, 0.349f, 0.714f, 0.85f);
        _formBadge.alignment = TextAlignmentOptions.Center;
        _formBadge.text      = "◦ PHYSICAL FORM";
    }

    private RectTransform MakePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                     Vector2 anchoredPos, Vector2 size)
    {
        var go       = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img      = go.AddComponent<Image>();
        img.color    = PanelColor;
        // Soft rounded look via sprite (Unity default is fine here)

        return rt;
    }

    private TextMeshProUGUI MakeLabel(Transform parent, string text, Vector2 anchor,
                                       Vector2 pos, float fontSize)
    {
        var go       = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt       = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot     = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 40);

        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = TextColor;
        tmp.fontStyle = FontStyles.Normal;
        return tmp;
    }

    private void BuildVeilBar(Transform parent)
    {
        // Background
        var bgGO      = new GameObject("VeilBar_BG");
        bgGO.transform.SetParent(parent, false);
        var bgRT      = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0.3f, 0f);
        bgRT.anchorMax = new Vector2(0.7f, 0f);
        bgRT.pivot     = new Vector2(0.5f, 0f);
        bgRT.anchoredPosition = new Vector2(0, 30f);
        bgRT.sizeDelta = new Vector2(0, 6f);
        _veilBarBG            = bgGO.AddComponent<Image>();
        _veilBarBG.color      = VeilBGColor;

        // Fill
        var fillGO      = new GameObject("VeilBar_Fill");
        fillGO.transform.SetParent(bgGO.transform, false);
        var fillRT       = fillGO.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        _veilBarFill           = fillGO.AddComponent<Image>();
        _veilBarFill.color     = VeilBarColor;
        _veilBarFill.type      = Image.Type.Filled;
        _veilBarFill.fillMethod = Image.FillMethod.Horizontal;
        _veilBarFill.fillAmount = 0f;

        // Label
        var labelGO      = new GameObject("VeilBar_Label");
        labelGO.transform.SetParent(bgGO.transform, false);
        var labelRT       = labelGO.AddComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 1f);
        labelRT.anchorMax = new Vector2(0.5f, 1f);
        labelRT.pivot     = new Vector2(0.5f, 0f);
        labelRT.anchoredPosition = new Vector2(0, 4f);
        labelRT.sizeDelta = new Vector2(200f, 20f);
        var labelTMP      = labelGO.AddComponent<TextMeshProUGUI>();
        labelTMP.text     = "VEIL STRAIN";
        labelTMP.fontSize = 9f;
        labelTMP.color    = new Color(0.608f, 0.349f, 0.714f, 0.8f);
        labelTMP.alignment = TextAlignmentOptions.Center;
    }
}
