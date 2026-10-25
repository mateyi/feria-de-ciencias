using UnityEngine;

/// <summary>
/// Clase base de TODOS los obstáculos del juego.
///
/// Responsabilidades:
///   1. Avisarle al ObstacleManager cuando el jugador lo toca (detección por
///      trigger contra el tag "Player").
///   2. Dejar que cada obstáculo concreto (estático, patrulla, deslizante,
///      rodante) implemente su Comportamiento() sin repetir el código de choque.
///   3. Ser reutilizable por el pool: Inicializar() arma el obstáculo y
///      Desactivar() lo deja listo para volver a usarse, sin Instanciar/Destroy.
///
/// El daño NO se aplica acá: se lo pide al ObstacleManager, que es el único que
/// sabe de invulnerabilidad, tarjetas y feedback (partículas, sonido, sacudida).
///
/// Carpeta: Assets/_Project/Scripts/Decisions/
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class ObstacleBase : MonoBehaviour
{
    /// <summary>Datos del obstáculo (daño, efecto, peso, etc).</summary>
    public ObstacleData Datos { get; private set; }

    /// <summary>True mientras el obstáculo está en la pista y puede lastimar.</summary>
    public bool Activo { get; private set; }

    /// <summary>Segundos desde que apareció. Lo usan los comportamientos con tiempos.</summary>
    protected float TiempoVida { get; private set; }

    /// <summary>Manager que nos prestó desde el pool.</summary>
    protected ObstacleManager Manager { get; private set; }

    // Evita que un mismo obstáculo descuente energía dos veces si el trigger
    // se dispara varias veces en el mismo choque.
    private bool _yaGolpeo;

    // Colliders del obstáculo y de sus hijos: se apagan al volver al pool.
    private Collider[] _colliders;

    // ─── Ciclo de vida del pool ───────────────────────────────────────────────

    /// <summary>
    /// Arma el obstáculo para salir a la pista. Lo llama el ObstacleManager
    /// justo después de sacarlo del pool (o de instanciarlo la primera vez).
    /// </summary>
    public virtual void Inicializar(ObstacleData datos, ObstacleManager manager)
    {
        Datos    = datos;
        Manager  = manager;
        _yaGolpeo = false;
        TiempoVida = 0f;
        Activo     = true;

        CachearColliders();
        SetCollidersActivos(true);

        Preparar();
    }

    /// <summary>Deja el obstáculo apagado y listo para volver al pool.</summary>
    public virtual void Desactivar()
    {
        Activo = false;
        SetCollidersActivos(false);
    }

    /// <summary>
    /// Giro de tuerca que quiera hacer cada obstáculo al aparecer (resetear
    /// posiciones, apagar el aviso, etc). Por defecto no hace nada.
    /// </summary>
    protected virtual void Preparar() { }

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake() => CachearColliders();

    protected virtual void Update()
    {
        if (!Activo) return;

        TiempoVida += Time.deltaTime;
        Comportamiento(Time.deltaTime);
    }

    /// <summary>
    /// Movimiento propio de cada obstáculo. Por defecto es un obstáculo quieto.
    /// </summary>
    protected virtual void Comportamiento(float deltaTime) { }

    // ─── Choque contra el jugador ─────────────────────────────────────────────

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (!Activo || _yaGolpeo) return;
        if (other == null || !other.CompareTag("Player")) return;

        // El PlayerController puede estar en el objeto padre si el collider
        // pertenece a una parte del modelo.
        PlayerController jugador = other.GetComponent<PlayerController>();
        if (jugador == null) jugador = other.GetComponentInParent<PlayerController>();
        if (jugador == null) return;

        // Los obstáculos bajos se pueden saltar: si el jugador viene en el aire
        // por encima del alto del obstáculo, lo pasa sin daño.
        if (PasaPorArriba(jugador)) return;

        _yaGolpeo = true;
        Manager?.RegistrarChoque(this, jugador);
    }

    /// <summary>
    /// True si el jugador está saltando y ya pasó por arriba del obstáculo.
    /// Sólo aplica a los obstáculos marcados como "sePuedeSaltar".
    /// </summary>
    protected bool PasaPorArriba(PlayerController jugador)
    {
        if (Datos == null || !Datos.sePuedeSaltar) return false;
        if (jugador == null) return false;

        // El alto del obstáculo menos un margen: si los pies ya están por encima
        // del borde superior, se considera que lo pasó limpio.
        float altoNecesario = Mathf.Max(0.2f, Datos.altura * 0.7f);
        return jugador.AlturaSobrePiso > altoNecesario;
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

    /// <summary>
    /// Velocidad lateral del obstáculo: la propia si está configurada, si no la
    /// del nivel actual.
    /// </summary>
    protected float VelocidadLateral()
    {
        if (Datos != null && Datos.velocidadLateral > 0.01f) return Datos.velocidadLateral;
        if (Manager != null) return Manager.VelocidadLateralPorDefecto;
        return 7f;
    }
}
