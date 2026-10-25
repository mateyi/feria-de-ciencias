using UnityEngine;

/// <summary>
/// Pickup de la pista (Fase C): objeto chico que flota y gira dentro de un carril
/// y, cuando el jugador lo toca, le da un efecto bueno.
///
/// Responsabilidades:
///   1. Animarse solo: giro sobre su eje y flotación suave.
///   2. Avisarle al PickupManager cuando el jugador lo toca (trigger contra el
///      tag "Player"). Igual que los obstáculos, el efecto NO se aplica acá: lo
///      resuelve el manager en un solo lugar.
///   3. Ser reutilizable por el pool: Inicializar() lo arma y Desactivar() lo
///      deja listo para volver a usarse, sin Instanciar/Destroy.
///
/// Carpeta: Assets/_Project/Scripts/Decisions/
/// (Va acá, junto a ObstacleBase, porque son los dos objetos de pista con los
///  que el jugador interactúa. No se creó una carpeta Pickups para no romper la
///  organización Scripts/{Player, Managers, Decisions, UI, Data}.)
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickupBase : MonoBehaviour
{
    /// <summary>Datos del pickup (tipo, valor, color, peso).</summary>
    public PickupData Datos { get; private set; }

    /// <summary>True mientras está en la pista y se puede recoger.</summary>
    public bool Activo { get; private set; }

    [Header("Animación")]
    [Tooltip("Grados por segundo que gira sobre su eje vertical.")]
    [SerializeField] private float velocidadGiro = 110f;

    [Tooltip("Cuánto sube y baja flotando, en metros.")]
    [SerializeField] private float amplitudFlote = 0.12f;

    [Tooltip("Velocidad de la flotación.")]
    [SerializeField] private float velocidadFlote = 2.2f;

    // Manager que nos prestó desde el pool.
    private PickupManager _manager;

    // Posición local de reposo (la Y es el centro de la flotación).
    private Vector3 _posicionBase;

    // Desfase para que no todos los pickups floten al mismo tiempo.
    private float _fase;

    // Colliders del pickup y de sus hijos: se apagan al recogerlo o al volver al pool.
    private Collider[] _colliders;

    // ─── Ciclo de vida del pool ───────────────────────────────────────────────

    private void Awake() => CachearColliders();

    /// <summary>
    /// Arma el pickup para salir a la pista. Lo llama el PickupManager justo
    /// después de sacarlo del pool (o de instanciarlo la primera vez).
    /// </summary>
    public void Inicializar(PickupData datos, PickupManager manager, Vector3 posicionLocal)
    {
        Datos      = datos;
        _manager   = manager;
        _posicionBase = posicionLocal;
        _fase      = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        transform.localPosition = posicionLocal;
        transform.localRotation = Quaternion.identity;

        CachearColliders();
        SetCollidersActivos(true);

        Activo = true;
    }

    /// <summary>Deja el pickup apagado y listo para volver al pool.</summary>
    public void Desactivar()
    {
        Activo = false;
        SetCollidersActivos(false);
    }

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!Activo) return;

        // Giro constante y flotación senoidal alrededor de la altura de reposo.
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.Self);

        float y = _posicionBase.y + Mathf.Sin(Time.time * velocidadFlote + _fase) * amplitudFlote;
        transform.localPosition = new Vector3(_posicionBase.x, y, _posicionBase.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Activo) return;
        if (other == null || !other.CompareTag("Player")) return;

        // El PlayerController puede estar en el objeto padre si el collider
        // pertenece a una parte del modelo.
        PlayerController jugador = other.GetComponent<PlayerController>();
        if (jugador == null) jugador = other.GetComponentInParent<PlayerController>();
        if (jugador == null) return;

        // Se apaga ANTES de avisar: el mismo pickup no se puede recoger dos veces.
        Activo = false;
        SetCollidersActivos(false);

        _manager?.Recoger(this);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void CachearColliders()
    {
        if (_colliders != null && _colliders.Length > 0) return;
        _colliders = GetComponentsInChildren<Collider>(true);
    }

    private void SetCollidersActivos(bool activos)
    {
        if (_colliders == null) return;
        foreach (var c in _colliders)
            if (c != null) c.enabled = activos;
    }
}
