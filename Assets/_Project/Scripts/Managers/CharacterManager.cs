using UnityEngine;

/// <summary>
/// Lleva la cuenta de que jugador esta equipado y le pasa sus bonus a las
/// estadisticas del equipo. El elegido se guarda en el archivo de partida
/// (SaveManager.Data.personajeEquipado) para que se recuerde entre sesiones.
///
/// Los jugadores hay que comprarlos con monedas. Mientras no se compre ninguno,
/// el equipo juega con el jugador generico (el modelo azul del principio), que
/// no da bonus. Los ids comprados se guardan en
/// SaveManager.Data.personajesComprados.
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

    [Header("Modelo 3D")]
    [Tooltip("Material del jugador generico. Es el que se usa mientras no haya nadie comprado.")]
    [SerializeField] private Material materialGenerico;

    [Tooltip("El jugador quieto que se ve atras del menu principal.")]
    [SerializeField] private GameObject modeloMenu;

    public CharacterData[] Catalogo => catalogo;

    /// <summary>
    /// Jugador equipado. Queda en null mientras el equipo use al jugador
    /// generico, que es el estado inicial de una partida nueva.
    /// </summary>
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

    /// <summary>Lee el jugador guardado. Si no hay ninguno comprado, queda el generico.</summary>
    public void CargarEquipado()
    {
        Equipado = PorId(SaveManager.Data.personajeEquipado);

        // Un id guardado sin haberlo comprado no vale: se vuelve al generico.
        if (Equipado != null && !Posee(Equipado)) Equipado = null;
    }

    // ─── Compra ───────────────────────────────────────────────────────────────

    /// <summary>True si el jugador ya fue comprado.</summary>
    public bool Posee(CharacterData personaje)
    {
        if (personaje == null) return false;
        return SaveManager.Data.personajesComprados.Contains(personaje.id);
    }

    /// <summary>True si todavia no lo tiene y le alcanzan las monedas.</summary>
    public bool PuedeComprar(CharacterData personaje)
    {
        if (personaje == null || Posee(personaje)) return false;
        return GameData.Instance != null && GameData.Instance.Coins >= personaje.precio;
    }

    /// <summary>
    /// Descuenta las monedas, marca al jugador como comprado y lo deja equipado.
    /// Devuelve false si no habia plata o si ya lo tenia.
    /// </summary>
    public bool Comprar(CharacterData personaje)
    {
        if (personaje == null || Posee(personaje)) return false;

        if (GameData.Instance == null || !GameData.Instance.SpendCoins(personaje.precio))
            return false;

        SaveManager.Data.personajesComprados.Add(personaje.id);
        SaveManager.Guardar();

        Equipar(personaje);

        Debug.Log($"[Personajes] comprado: {personaje.nombre} por {personaje.precio} monedas");
        return true;
    }

    // ─── Seleccion ────────────────────────────────────────────────────────────

    /// <summary>Equipa un jugador que ya este comprado, lo guarda y aplica sus bonus.</summary>
    public void Equipar(CharacterData personaje)
    {
        if (personaje == null || !Posee(personaje)) return;

        Equipado = personaje;
        SaveManager.Data.personajeEquipado = personaje.id;
        SaveManager.Guardar();

        AplicarAlJugador();
        AplicarModelo(GameObject.FindGameObjectWithTag("Player"));

        Debug.Log($"[Personajes] equipado: {personaje.nombre} ({personaje.pais})");
    }

    // ─── Efecto en el juego ───────────────────────────────────────────────────

    /// <summary>
    /// Deja las estadisticas del equipo en base + bonus del jugador equipado.
    /// Con el jugador generico (Equipado en null) quedan las base peladas.
    /// </summary>
    public void AplicarAlJugador()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return;

        float vel = Equipado != null ? Equipado.bonusVelocidad   : 0f;
        float ener = Equipado != null ? Equipado.bonusEnergia    : 0f;
        float hab = Equipado != null ? Equipado.bonusHabilidad   : 0f;
        float pop = Equipado != null ? Equipado.bonusPopularidad : 0f;

        stats.speed      = Mathf.Clamp(50f  + vel,  0f, 100f);
        stats.energy     = Mathf.Clamp(100f + ener, 0f, 100f);
        stats.skill      = Mathf.Clamp(50f  + hab,  0f, 100f);
        stats.popularity = Mathf.Clamp(50f  + pop,  0f, 100f);
    }

    /// <summary>
    /// Le pone al modelo del jugador el material de la camiseta del jugador
    /// equipado. Con el generico vuelve al material original. Tambien pinta al
    /// jugador quieto que se ve atras del menu.
    /// </summary>
    public void AplicarModelo(GameObject jugador)
    {
        PintarModelo(jugador);
        PintarModelo(modeloMenu);
    }

    private void PintarModelo(GameObject objetivo)
    {
        if (objetivo == null) return;

        var piel = objetivo.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (piel == null) return;

        Material material = (Equipado != null && Equipado.kit != null) ? Equipado.kit : materialGenerico;
        if (material != null) piel.sharedMaterial = material;
    }
}
