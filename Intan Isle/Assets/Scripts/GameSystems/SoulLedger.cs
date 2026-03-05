using System;
using System.Collections.Generic;
using UnityEngine;

public class SoulLedger : MonoBehaviour
{
    public struct SoulEntry
    {
        public DateTime timestamp;
        public string actionName;
        public float barakahDelta;
        public BarakahSource source;
    }

    private const int MaxEntries = 100;
    private List<SoulEntry> entries = new List<SoulEntry>();

    private BarakahMeter _barakahMeter;

    private void Start()
    {
        _barakahMeter = FindObjectOfType<BarakahMeter>();
        if (_barakahMeter != null)
            _barakahMeter.OnBarakahChanged += OnBarakahChanged;
    }

    private void OnDestroy()
    {
        if (_barakahMeter != null)
            _barakahMeter.OnBarakahChanged -= OnBarakahChanged;
    }

    private void OnBarakahChanged(float value, string levelName)
    {
        RecordEntry($"Barakah -> {levelName}", 0f, BarakahSource.Consistency);
    }

    public void RecordEntry(string actionName, float delta, BarakahSource source)
    {
        if (entries.Count >= MaxEntries)
            entries.RemoveAt(0);

        entries.Add(new SoulEntry
        {
            timestamp = DateTime.UtcNow,
            actionName = actionName,
            barakahDelta = delta,
            source = source
        });
    }

    public List<SoulEntry> GetRecentEntries(int count)
    {
        int start = Mathf.Max(0, entries.Count - count);
        return entries.GetRange(start, entries.Count - start);
    }
}
