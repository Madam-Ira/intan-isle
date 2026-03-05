using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Wood Wide Web — glowing root-line network between trees.
///
/// Visual doctrine:
///   Lines: deep green #1A5C3A at 0.15 alpha
///   Nodes: bright emerald #00FF88
///   Pulse dot travels at 2 m/sec along each line
///   POLLUTION zones: lines broken, dark, flickering
///   Active ONLY in VeiledWorld layer
///   Connections update every 5 seconds
/// </summary>
public class WoodWideWebVFX : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] private float connectionRadius  = 30f;   // metres
    [SerializeField] private float updateInterval    = 5f;    // seconds
    [SerializeField] private float pulseSpeed        = 2f;    // m/sec
    [SerializeField] private float lineAlpha         = 0.15f;

    [Header("Tree Roots — assign tags or a parent GO")]
    [SerializeField] private string treeTag          = "Tree";
    [SerializeField] private Transform treesParent;            // FantasyForest_Trees

    [Header("Materials")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Material nodeMaterial;

    // ── Locked palette colours ────────────────────────────────────
    private static readonly Color LineColor     = new Color(0.102f, 0.361f, 0.227f, 0.15f); // #1A5C3A
    private static readonly Color NodeColor     = new Color(0.0f,   1.0f,   0.533f, 1.0f);  // #00FF88
    private static readonly Color LineColorToxic = new Color(0.2f,  0.0f,  0.0f,   0.08f); // dark, broken
    private static readonly Color NodeColorToxic = new Color(0.4f,  0.0f,  0.0f,   0.5f);

    // ── Runtime ───────────────────────────────────────────────────
    private List<Transform>    _trees       = new List<Transform>();
    private List<LineRenderer> _lines       = new List<LineRenderer>();
    private List<float>        _pulsePosNorm = new List<float>();  // 0–1 along each line
    private GameObject         _lineParent;
    private bool               _zoneIsToxic = false;

    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _lineParent = new GameObject("WoodWideWeb_Lines");
        _lineParent.transform.SetParent(transform);
    }

    void OnEnable()
    {
        StartCoroutine(UpdateLoopCoroutine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (!VeiledWorldManager.InVeiledWorld) return;
        AnimatePulses();
    }

    // ── Tree collection ───────────────────────────────────────────

    private void GatherTrees()
    {
        _trees.Clear();

        if (treesParent != null)
        {
            foreach (Transform t in treesParent)
                _trees.Add(t);
        }
        else
        {
            var tagged = GameObject.FindGameObjectsWithTag(treeTag);
            foreach (var go in tagged)
                _trees.Add(go.transform);
        }
    }

    // ── Build connection network ──────────────────────────────────

    private IEnumerator UpdateLoopCoroutine()
    {
        while (true)
        {
            if (VeiledWorldManager.InVeiledWorld)
            {
                GatherTrees();
                BuildConnections();
            }
            yield return new WaitForSeconds(updateInterval);
        }
    }

    private void BuildConnections()
    {
        // Determine zone toxicity from current zone type
        float zt = Shader.GetGlobalFloat("_IntanIsle_ZoneType");
        _zoneIsToxic = (zt >= 7f); // DEFORESTATION and above = degraded

        // Clear old lines
        foreach (var lr in _lines) if (lr != null) Destroy(lr.gameObject);
        _lines.Clear();
        _pulsePosNorm.Clear();

        int connectionCount = 0;
        for (int i = 0; i < _trees.Count; i++)
        {
            for (int j = i + 1; j < _trees.Count; j++)
            {
                float dist = Vector3.Distance(_trees[i].position, _trees[j].position);
                if (dist > connectionRadius) continue;

                var lr = CreateLineRenderer(_trees[i].position, _trees[j].position, dist);
                _lines.Add(lr);
                _pulsePosNorm.Add(Random.value); // stagger pulse starts
                connectionCount++;
            }
        }

        Debug.Log("[WoodWideWeb] Connections: " + connectionCount + "  Trees: " + _trees.Count + "  Toxic: " + _zoneIsToxic);
    }

    private LineRenderer CreateLineRenderer(Vector3 a, Vector3 b, float dist)
    {
        var go = new GameObject("WebLine");
        go.transform.SetParent(_lineParent.transform);

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] { a, b });
        lr.useWorldSpace   = true;
        lr.startWidth      = 0.15f;
        lr.endWidth        = 0.08f;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows  = false;
        lr.allowOcclusionWhenDynamic = false;

        if (lineMaterial != null)
            lr.material = lineMaterial;
        else
        {
            lr.material            = new Material(Shader.Find("Sprites/Default"));
            lr.material.renderQueue = 3000;
        }

        Color lineCol = _zoneIsToxic ? LineColorToxic : LineColor;
        lr.startColor = lr.endColor = lineCol;

        return lr;
    }

    // ── Pulse dot animation ───────────────────────────────────────

    private void AnimatePulses()
    {
        if (_lines.Count == 0) return;
        float speed = pulseSpeed * Time.deltaTime;

        for (int i = 0; i < _lines.Count; i++)
        {
            var lr = _lines[i];
            if (lr == null) continue;

            _pulsePosNorm[i] = (_pulsePosNorm[i] + speed / lr.GetPosition(0).magnitude) % 1f;
            float t = _pulsePosNorm[i];

            // Flicker in toxic zones
            if (_zoneIsToxic)
            {
                float flicker = Mathf.Sin(Time.time * 8f + i) * 0.5f + 0.5f;
                Color c = Color.Lerp(LineColorToxic, Color.black, flicker * 0.7f);
                lr.startColor = lr.endColor = c;
            }

            // Draw pulse dot at position t along the line
            // (Using a small node sphere moved each frame would be more accurate
            //  but LineRenderer allows 3-point "bead" as a workaround)
        }
    }

    // ── Public status ─────────────────────────────────────────────
    public int ConnectionCount => _lines.Count;
}
