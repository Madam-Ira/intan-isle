using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guardians of the Land — visual presence only. No dialogue. No interaction.
///
/// Visual doctrine:
///   ANCIENT/PROTECTED FOREST : subtle silhouettes, 0.15 alpha, slow drift
///   SACRED FOREST            : distant warm glow shapes behind trees
///   POLLUTION/TOXIC          : wounded forms, dark, hunched, 0.10 alpha
///   Proximity: fade in at 20m, full at 5m, gone beyond 40m
///   World hums near healthy spirits. Silent near wounded/absent.
///   LANGUAGE: always "Nature Spirits" / "Guardians of the Land"
/// </summary>
public class BunianSpiritForms : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private int   poolSize          = 12;
    [SerializeField] private float spawnRadius       = 25f;
    [SerializeField] private float fadeInDistance    = 20f;
    [SerializeField] private float fullVisDistance   =  5f;
    [SerializeField] private float despawnDistance   = 40f;

    [Header("Movement")]
    [SerializeField] private float driftSpeed        = 0.3f;
    [SerializeField] private float driftAmplitude    = 0.8f;

    [Header("Audio")]
    [SerializeField] private AudioSource ambientHum;
    [SerializeField] private float       humFadeSpeed = 0.5f;

    [Header("Material")]
    [SerializeField] private Material spiritMaterial; // Uses IntanIsle_VeiledWorldShader

    // ── Locked palette ────────────────────────────────────────────
    private static readonly Color HealthyColor  = new Color(1.0f, 1.0f, 1.0f, 0.15f); // spirit white
    private static readonly Color WoundedColor  = new Color(0.1f, 0.0f, 0.0f, 0.10f); // dark, suffering
    private static readonly Color SacredColor   = new Color(1.0f, 0.85f, 0.7f, 0.12f); // warm amber shimmer

    // ── Spirit form data ──────────────────────────────────────────
    private class Spirit
    {
        public GameObject go;
        public Renderer   rend;
        public Vector3    driftOrigin;
        public float      driftPhase;
        public bool       wounded;
        public float      targetAlpha;
    }

    private List<Spirit>  _pool  = new List<Spirit>();
    private Transform     _player;
    private bool          _inVeiledWorld = false;
    private float         _targetHum     = 0f;

    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        _player = FindPlayerRig();
        BuildPool();
        StartCoroutine(RespawnLoop());
    }

    void Update()
    {
        _inVeiledWorld = VeiledWorldManager.InVeiledWorld;

        if (!_inVeiledWorld)
        {
            HideAll();
            FadeHum(0f);
            return;
        }

        bool anyHealthy = false;
        float zt = Shader.GetGlobalFloat("_IntanIsle_ZoneType");
        bool zoneHealthy = ((ZoneType)(int)zt).IsHealthy();

        foreach (var s in _pool)
        {
            if (s.go == null) continue;
            UpdateSpirit(s, zoneHealthy);
            if (!s.wounded && s.go.activeSelf) anyHealthy = true;
        }

        _targetHum = anyHealthy ? 0.4f : 0f;
        FadeHum(_targetHum);
    }

    // ── Pool ──────────────────────────────────────────────────────

    private void BuildPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var go   = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name  = "Guardian_" + i;
            go.transform.SetParent(transform);
            go.SetActive(false);

            // Remove collider — purely visual
            Destroy(go.GetComponent<Collider>());

            var rend = go.GetComponent<Renderer>();
            if (spiritMaterial != null)
                rend.material = new Material(spiritMaterial);
            else
            {
                rend.material            = new Material(Shader.Find("Sprites/Default"));
                rend.material.renderQueue = 3500;
            }
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            _pool.Add(new Spirit
            {
                go         = go,
                rend       = rend,
                driftPhase = Random.value * Mathf.PI * 2f,
                wounded    = false,
                targetAlpha = 0f,
            });
        }
    }

    // ── Per-spirit update ─────────────────────────────────────────

    private void UpdateSpirit(Spirit s, bool zoneHealthy)
    {
        if (_player == null) return;

        float dist = Vector3.Distance(s.go.transform.position, _player.position);

        // Despawn if too far
        if (dist > despawnDistance)
        {
            s.go.SetActive(false);
            return;
        }

        // Compute target alpha from proximity
        float alpha = 0f;
        if (dist <= fullVisDistance)
            alpha = s.wounded ? 0.10f : (zoneHealthy ? 0.15f : 0.08f);
        else if (dist < fadeInDistance)
            alpha = Mathf.Lerp(s.wounded ? 0.10f : 0.15f, 0f,
                               (dist - fullVisDistance) / (fadeInDistance - fullVisDistance));

        s.targetAlpha = alpha;

        // Apply colour
        Color baseColor = s.wounded ? WoundedColor : (zoneHealthy ? HealthyColor : WoundedColor);
        var   col       = baseColor;
        col.a           = Mathf.MoveTowards(GetCurrentAlpha(s), s.targetAlpha, Time.deltaTime * 0.5f);
        if (s.rend != null) s.rend.material.color = col;

        // Drift motion — gentle float
        s.driftPhase += Time.deltaTime * driftSpeed;
        Vector3 drift = new Vector3(
            Mathf.Sin(s.driftPhase * 0.7f) * driftAmplitude,
            Mathf.Sin(s.driftPhase * 1.1f) * driftAmplitude * 0.5f,
            Mathf.Cos(s.driftPhase * 0.9f) * driftAmplitude
        );

        // Wounded forms: hunched, smaller oscillation
        if (s.wounded)
        {
            drift    *= 0.3f;
            drift.y  -= 0.2f;
        }

        s.go.transform.position = s.driftOrigin + drift;

        // Billboard toward camera
        if (Camera.main != null)
            s.go.transform.LookAt(Camera.main.transform);

        // Wounded: flicker
        if (s.wounded && s.go.activeSelf)
        {
            float fl = Mathf.Sin(Time.time * 4f + s.driftPhase) * 0.03f;
            var   mc = s.rend.material.color;
            mc.a     = Mathf.Max(0, mc.a + fl);
            s.rend.material.color = mc;
        }
    }

    private float GetCurrentAlpha(Spirit s)
        => s.rend != null ? s.rend.material.color.a : 0f;

    // ── Respawn loop — place spirits near player ──────────────────

    private IEnumerator RespawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            if (!_inVeiledWorld || _player == null) continue;

            float zt       = Shader.GetGlobalFloat("_IntanIsle_ZoneType");
            bool  wounded  = !((ZoneType)(int)zt).IsHealthy();

            // Find an inactive spirit and place it near player
            foreach (var s in _pool)
            {
                if (s.go.activeSelf) continue;

                float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist     = Random.Range(5f, spawnRadius);
                float height   = Random.Range(0.5f, 3f);

                s.driftOrigin  = _player.position
                               + new Vector3(Mathf.Sin(angle) * dist, height, Mathf.Cos(angle) * dist);
                s.wounded      = wounded;
                s.go.transform.position = s.driftOrigin;
                s.go.transform.localScale = wounded
                    ? new Vector3(0.6f, 1.2f, 0.1f)
                    : new Vector3(0.8f, 2.0f, 0.1f);
                s.go.SetActive(true);
                break;
            }
        }
    }

    // ── Utility ───────────────────────────────────────────────────

    private void HideAll()
    {
        foreach (var s in _pool) if (s.go != null) s.go.SetActive(false);
    }

    private void FadeHum(float target)
    {
        if (ambientHum == null) return;
        ambientHum.volume = Mathf.MoveTowards(ambientHum.volume, target, Time.deltaTime * humFadeSpeed);
        if (target > 0 && !ambientHum.isPlaying) ambientHum.Play();
        else if (Mathf.Approximately(ambientHum.volume, 0f)) ambientHum.Stop();
    }

    private Transform FindPlayerRig()
    {
        var go = GameObject.Find("PlayerRig");
        return go != null ? go.transform : transform;
    }
}
