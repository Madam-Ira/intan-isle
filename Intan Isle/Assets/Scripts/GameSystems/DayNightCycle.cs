using System;
using UnityEngine;

public enum GamePhase
{
    Dawn,
    Morning,
    Midday,
    Afternoon,
    Dusk,
    Night
}

public class DayNightCycle : MonoBehaviour
{
    public float phaseDuration = 300f;

    private GamePhase _currentPhase = GamePhase.Dawn;
    private float _phaseTimer = 0f;

    public event Action<GamePhase> OnPhaseChanged;

    public GamePhase CurrentPhase => _currentPhase;

    private void Start()
    {
        TriggerPhaseEffects(_currentPhase);
    }

    private void Update()
    {
        _phaseTimer += Time.deltaTime;
        if (_phaseTimer >= phaseDuration)
        {
            _phaseTimer -= phaseDuration;
            AdvancePhase();
        }
    }

    private void AdvancePhase()
    {
        int next = ((int)_currentPhase + 1) % 6;
        _currentPhase = (GamePhase)next;
        OnPhaseChanged?.Invoke(_currentPhase);
        TriggerPhaseEffects(_currentPhase);
    }

    private void TriggerPhaseEffects(GamePhase phase)
    {
        if (phase == GamePhase.Dawn && AWFWaterSystem.Instance != null)
            AWFWaterSystem.Instance.CollectWater();
    }
}
