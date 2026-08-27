using UnityEngine;
using System;

/// <summary>
/// Estadísticas del futbolista que se modifican con las decisiones del juego.
/// Velocidad, energía, habilidad y popularidad.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Estadísticas (0-100)")]
    [Range(0, 100)] public float speed = 50f;
    [Range(0, 100)] public float energy = 100f;
    [Range(0, 100)] public float skill = 50f;
    [Range(0, 100)] public float popularity = 50f;

    public event Action OnStatsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ApplyConsequence(StatConsequence consequence)
    {
        speed     = Mathf.Clamp(speed     + consequence.speedDelta,      0, 100);
        energy    = Mathf.Clamp(energy    + consequence.energyDelta,     0, 100);
        skill     = Mathf.Clamp(skill     + consequence.skillDelta,      0, 100);
        popularity= Mathf.Clamp(popularity+ consequence.popularityDelta, 0, 100);
        OnStatsChanged?.Invoke();
    }

    public void DrainEnergy(float amount)
    {
        energy = Mathf.Clamp(energy - amount, 0, 100);
        OnStatsChanged?.Invoke();
    }

    /// <summary>Suma energía (p. ej. al recoger una lata de Red Bull).</summary>
    public void AddEnergy(float amount)
    {
        energy = Mathf.Clamp(energy + amount, 0, 100);
        OnStatsChanged?.Invoke();
    }

    public void ResetStats()
    {
        speed = 50f; energy = 100f; skill = 50f; popularity = 50f;
        OnStatsChanged?.Invoke();
    }

    /// <summary>Energía normalizada 0-1 para la barra de UI.</summary>
    public float EnergyNormalized => energy / 100f;
}

[System.Serializable]
public struct StatConsequence
{
    public float speedDelta;
    public float energyDelta;
    public float skillDelta;
    public float popularityDelta;
    [TextArea(2, 4)] public string description;
}
