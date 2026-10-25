using UnityEngine;

/// <summary>
/// Lleva la cuenta de que jugador esta equipado y le pasa sus bonus a las
/// estadisticas del equipo. El elegido se guarda en el archivo de partida
/// (SaveManager.Data.personajeEquipado) para que se recuerde entre sesiones.
///
/// Va en el GameObject "GameData", al lado de UpgradeManager.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance { get; private set; }

    [Header("Catalogo (el orden es el que se ve en pantalla)")]
    [SerializeField] private CharacterData[] catalogo;

    public CharacterData[] Catalogo => catalogo;

    /// <summary>Jugador equipado. Nunca queda en null si el catalogo tiene datos.</summary>
    public CharacterData Equipado { get; private set; }

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        CargarEquipado();
    }

    // ─── Catalogo ─────────────────────────────────────────────────────────────

    /// <summary>Busca un jugador por su id interno.</summary>
    public CharacterData PorId(string id)
    {
        if (string.IsNullOrEmpty(id) || catalogo == null) return null;

        foreach (var c in catalogo)
            if (c != null && c.id == id) return c;

        return null;
    }

    /// <summary>Lee el jugador guardado; si no hay, usa el primero del catalogo.</summary>
    public void CargarEquipado()
    {
        if (catalogo == null || catalogo.Length == 0) return;

        Equipado = PorId(SaveManager.Data.personajeEquipado);
        if (Equipado == null) Equipado = catalogo[0];
    }

    /// <summary>Equipa un jugador, lo guarda y aplica sus bonus.</summary>
    public void Equipar(CharacterData personaje)
    {
        if (personaje == null) return;

        Equipado = personaje;
        SaveManager.Data.personajeEquipado = personaje.id;
        SaveManager.Guardar();

        AplicarAlJugador();

        Debug.Log($"[Personajes] equipado: {personaje.nombre} ({personaje.pais})");
    }

    // ─── Efecto en el juego ───────────────────────────────────────────────────

    /// <summary>
    /// Deja las estadisticas del equipo en base + bonus del jugador equipado
    /// (mas lo que hayan sumado las decisiones de la carrera).
    /// </summary>
    public void AplicarAlJugador()
    {
        var stats = PlayerStats.Instance;
        if (stats == null || Equipado == null) return;

        stats.speed      = Mathf.Clamp(50f  + Equipado.bonusVelocidad,   0f, 100f);
        stats.energy     = Mathf.Clamp(100f + Equipado.bonusEnergia,     0f, 100f);
        stats.skill      = Mathf.Clamp(50f  + Equipado.bonusHabilidad,   0f, 100f);
        stats.popularity = Mathf.Clamp(50f  + Equipado.bonusPopularidad, 0f, 100f);
    }
}
