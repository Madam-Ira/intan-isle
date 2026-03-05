using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Attach to a trigger volume (BoxCollider, isTrigger=true) to create a pollution zone.
///
/// While the player is inside:
///   - Drains BarakahMeter at veilStrainRate per second
///   - Activates the zone's post-processing Volume
///
/// When BarakahMeter.Value drops below barakahHazeThreshold:
///   - Activates the haze ParticleSystem on this zone
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Volume))]
public class VeilStrainZone : MonoBehaviour
{
    [Header("Zone Identity")]
    public string zoneName = "PollutionZone";

    [Header("Veil Strain")]
    [Tooltip("Barakah drained per second while player is inside")]
    public float veilStrainRate = 15f;

    [Header("Haze Threshold")]
    [Tooltip("Haze particle system activates when BarakahMeter.Value is below this")]
    public float barakahHazeThreshold = 25f;

    [Header("References (auto-found if left blank)")]
    public BarakahMeter barakahMeter;
    public ParticleSystem hazeParticles;

    // ── Private state ─────────────────────────────────────────────────────
    private Volume    _volume;
    private BoxCollider _collider;
    private bool      _playerInside;

    private void Awake()
    {
        _volume   = GetComponent<Volume>();
        _collider = GetComponent<BoxCollider>();
        _collider.isTrigger = true;

        // Post-processing volume is a local volume; weight driven by proximity
        _volume.isGlobal = false;
        _volume.blendDistance = 20f;
        _volume.weight = 0f;           // start fully off
        _volume.priority = 10f;

        if (barakahMeter == null)
            barakahMeter = FindObjectOfType<BarakahMeter>();

        if (hazeParticles == null)
            hazeParticles = GetComponentInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (barakahMeter == null) return;

        // ── Drain Barakah while player is inside ─────────────────────────
        if (_playerInside)
        {
            barakahMeter.AddBarakah(-veilStrainRate * Time.deltaTime, BarakahSource.Restraint);
        }

        // ── Activate / deactivate volume weight ──────────────────────────
        float targetWeight = _playerInside ? 1f : 0f;
        _volume.weight = Mathf.MoveTowards(_volume.weight, targetWeight, Time.deltaTime * 2f);

        // ── Haze particle system: on when Barakah is critically low ──────
        if (hazeParticles != null)
        {
            bool shouldHaze = barakahMeter.Value < barakahHazeThreshold;
            if (shouldHaze && !hazeParticles.isPlaying)
                hazeParticles.Play();
            else if (!shouldHaze && hazeParticles.isPlaying)
                hazeParticles.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            _playerInside = true;
            Debug.Log("[VeilStrainZone] Entered: " + zoneName);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            _playerInside = false;
            Debug.Log("[VeilStrainZone] Exited: " + zoneName);
        }
    }

    // ── Editor helper ─────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0.5f, 0f, 0.8f, 0.25f); // deep violet
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = new Color(0.5f, 0f, 0.8f, 0.9f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
}
