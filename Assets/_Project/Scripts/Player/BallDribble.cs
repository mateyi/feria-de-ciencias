using UnityEngine;

/// <summary>
/// Pelota que el jugador lleva pegada al pie mientras corre (Fase D).
///
/// Qué hace:
///   1. Sigue al jugador: va adelante y un poco sobre el pie derecho, y rueda
///      con la velocidad real a la que se mueve (gira según el radio).
///   2. Cuando el jugador choca con un obstáculo de tipo "pelota robada"
///      (el tackle), la pelota se escapa de costado y vuelve sola al pie.
///
/// Se engancha sola al evento ObstacleManager.OnPelotaRobada, así que no hay
/// que cablear nada a mano ni tocar el generador de obstáculos.
///
/// Estructura en la escena: un objeto raíz "BallDribble" con este componente y
/// un hijo "Pelota" (esfera con el material de la pelota, SIN collider ni
/// Rigidbody: se mueve por código y no debe chocar con nada).
///
/// Carpeta: Assets/_Project/Scripts/Player/
/// </summary>
public class BallDribble : MonoBehaviour
{
    public static BallDribble Instance { get; private set; }

    /// <summary>True mientras la pelota está en el pie (no se la robaron).</summary>
    public bool TienePelota => _estado == EstadoPelota.EnElPie;

    // Los tres momentos posibles de la pelota.
    private enum EstadoPelota
    {
        EnElPie,     // pegada al jugador
        Robada,      // salió despedida y está en el aire / en el piso
        Volviendo    // vuelve rodando al pie
    }

    // ─── Inspector: acomodo en el pie ─────────────────────────────────────────
    [Header("Acomodo en el pie")]
    [Tooltip("Metros hacia adelante desde el jugador.")]
    [SerializeField] private float adelante = 1.05f;

    [Tooltip("Metros hacia la derecha: la pelota va sobre el pie derecho.")]
    [SerializeField] private float costado = 0.28f;

    [Tooltip("Radio de la pelota en metros. También define su tamaño en pantalla.")]
    [SerializeField] private float radio = 0.22f;

    // ─── Inspector: suavizado del seguimiento ─────────────────────────────────
    [Header("Suavizado del seguimiento")]
    [Tooltip("Qué tan rápido copia el cambio de carril. Bajo = la pelota se atrasa.")]
    [SerializeField] private float suavizadoLateral = 11f;

    [Tooltip("Qué tan rápido copia la altura (para que no flote si el jugador salta).")]
    [SerializeField] private float suavizadoVertical = 9f;

    // ─── Inspector: cuando la roban ───────────────────────────────────────────
    [Header("Cuando la roban (tackle)")]
    [Tooltip("Velocidad con la que sale despedida de costado.")]
    [SerializeField] private float fuerzaRobo = 4.5f;

    [Tooltip("Impulso hacia arriba del rebote.")]
    [SerializeField] private float impulsoArriba = 3.2f;

    [Tooltip("Gravedad que se le aplica mientras vuela.")]
    [SerializeField] private float gravedadRobo = -18f;

    [Tooltip("Segundos que tarda en empezar a volver al pie.")]
    [SerializeField] private float duracionRobo = 0.9f;

    [Tooltip("Velocidad base de vuelta al pie. Siempre se le suma la del jugador.")]
    [SerializeField] private float velocidadVuelta = 9f;

    // ─── Inspector: referencias ───────────────────────────────────────────────
    [Header("Referencias")]
    [Tooltip("Transform de la pelota. Si queda vacío se busca un hijo \"Pelota\".")]
    [SerializeField] private Transform pelota;

    // ─── Estado interno ───────────────────────────────────────────────────────
    private EstadoPelota _estado = EstadoPelota.EnElPie;
    private Vector3      _velocidadRobo;
    private float        _tiempoRobada;
    private bool         _suscripto;        // ya escuchamos al ObstacleManager
    private bool         _corriendoAntes;   // para detectar el arranque de carrera

    // Para saber a qué velocidad se mueve realmente el jugador.
    private Vector3 _posJugadorAnterior;
    private float   _velocidadJugador;

    // ═════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Si no la asignaron a mano, buscamos el hijo "Pelota".
        if (pelota == null)
        {
            Transform hija = transform.Find("Pelota");
            if (hija != null) pelota = hija;
        }

        if (pelota == null)
        {
            Debug.LogWarning("[BallDribble] No encontré la pelota: el componente se apaga.");
            enabled = false;
            return;
        }

        // El tamaño visual sale del radio, así la esfera rueda sin patinar.
        pelota.localScale = Vector3.one * (radio * 2f);
    }

    private void OnEnable()  { IntentarSuscribir(); }
    private void OnDisable() { Desuscribir(); }

    // Se actualiza en LateUpdate para leer al jugador ya movido en este frame:
    // si corriera en Update la pelota quedaría siempre un frame atrás.
    private void LateUpdate()
    {
        if (!_suscripto) IntentarSuscribir();

        PlayerController jugador = PlayerController.Instance;
        RunnerGameManager gm = RunnerGameManager.Instance;

        // La pelota sólo existe durante la carrera: en el menú, en el game over,
        // en la pantalla de victoria y en el penal se esconde.
        bool corriendo = jugador != null && jugador.IsAlive &&
                         (gm == null || gm.State == GameState.Running);

        if (!corriendo)
        {
            _estado = EstadoPelota.EnElPie;
            _corriendoAntes = false;
            if (pelota.gameObject.activeSelf) pelota.gameObject.SetActive(false);
            return;
        }

        // Primer frame de la carrera (o después de un respawn): la pegamos al pie.
        if (!_corriendoAntes)
        {
            _corriendoAntes = true;
            _estado = EstadoPelota.EnElPie;
            if (!pelota.gameObject.activeSelf) pelota.gameObject.SetActive(true);
            pelota.position = Destino(jugador);
            _posJugadorAnterior = jugador.transform.position;
            _velocidadJugador   = 0f;
        }

        MedirVelocidadDelJugador(jugador);

        switch (_estado)
        {
            case EstadoPelota.EnElPie:   SeguirAlPie(jugador);  break;
            case EstadoPelota.Robada:    VolarSuelta();         break;
            case EstadoPelota.Volviendo: VolverAlPie(jugador);  break;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// El jugador recibió un tackle: la pelota sale despedida y después vuelve.
    /// La llama ObstacleManager a través de OnPelotaRobada.
    /// </summary>
    public void Robar()
    {
        if (!enabled || pelota == null) return;
        if (_estado == EstadoPelota.Robada) return;   // ya está volando

        // Se escapa hacia un costado, medido desde la orientación del jugador.
        Vector3 derecha = Vector3.right;
        PlayerController jugador = PlayerController.Instance;
        if (jugador != null)
        {
            derecha = jugador.transform.right;
            derecha.y = 0f;
            if (derecha.sqrMagnitude < 0.0001f) derecha = Vector3.right;
            derecha.Normalize();
        }

        float lado = Random.value < 0.5f ? -1f : 1f;

        _estado = EstadoPelota.Robada;
        _tiempoRobada = 0f;

        // Se escapa de costado, pero conserva el avance del jugador: si saliera
        // sólo de costado se quedaría muy atrás y tardaría en volver.
        Vector3 avance = Vector3.forward;
        float velocidadAvance = 0f;
        if (jugador != null)
        {
            avance = jugador.transform.forward;
            avance.y = 0f;
            if (avance.sqrMagnitude < 0.0001f) avance = Vector3.forward;
            avance.Normalize();
            velocidadAvance = jugador.CurrentSpeed;
        }

        _velocidadRobo = avance   * velocidadAvance
                       + derecha * (lado * fuerzaRobo)
                       + Vector3.up * impulsoArriba;

        // Si estaba escondida (por ejemplo al arrancar) la mostramos igual.
        if (!pelota.gameObject.activeSelf) pelota.gameObject.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  MOVIMIENTO
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Punto exacto del pie derecho: adelante y al costado, medido en el espacio
    /// del jugador (así sigue funcionando si el jugador gira).
    /// </summary>
    private Vector3 Destino(PlayerController jugador)
        => jugador.transform.TransformPoint(new Vector3(costado, radio, adelante));

    /// <summary>La pelota persigue el pie y rueda lo que se movió.</summary>
    private void SeguirAlPie(PlayerController jugador)
    {
        Vector3 anterior = pelota.position;

        // Trabajamos en el espacio del jugador: X es el costado, Z el avance.
        // El avance se copia exacto (si se suavizara, la pelota quedaría atrás y
        // se metería dentro de las piernas); el costado sí se suaviza para que el
        // cambio de carril se vea como un arrastre y no como un salto.
        Vector3 local = jugador.transform.InverseTransformPoint(anterior);

        local.x = Amortiguar(local.x, costado, suavizadoLateral);
        local.y = Amortiguar(local.y, radio,   suavizadoVertical);
        local.z = adelante;

        Vector3 nueva = jugador.transform.TransformPoint(local);
        pelota.position = nueva;

        Rodar(nueva - anterior);
    }

    /// <summary>Vuela con gravedad hasta el piso; después arranca la vuelta.</summary>
    private void VolarSuelta()
    {
        _tiempoRobada += Time.deltaTime;
        _velocidadRobo.y += gravedadRobo * Time.deltaTime;

        Vector3 anterior = pelota.position;
        Vector3 nueva = anterior + _velocidadRobo * Time.deltaTime;

        // Si toca el piso se frena y queda quieta hasta que empieza a volver.
        if (nueva.y <= radio)
        {
            nueva.y = radio;
            _velocidadRobo = Vector3.zero;
        }

        pelota.position = nueva;
        Rodar(nueva - anterior);

        if (_tiempoRobada >= duracionRobo) _estado = EstadoPelota.Volviendo;
    }

    /// <summary>Vuelve rodando al pie. Siempre corre un poco más rápido que el jugador.</summary>
    private void VolverAlPie(PlayerController jugador)
    {
        Vector3 objetivo = Destino(jugador);
        Vector3 anterior = pelota.position;

        // Tiene que ir siempre más rápido que el jugador: si igualara su
        // velocidad quedaría persiguiéndolo para siempre sin alcanzarlo.
        float distancia = Vector3.Distance(anterior, objetivo);
        float velocidad = Mathf.Max(velocidadVuelta, _velocidadJugador + 8f + distancia * 2f);

        Vector3 nueva = Vector3.MoveTowards(anterior, objetivo, velocidad * Time.deltaTime);
        pelota.position = nueva;
        Rodar(nueva - anterior);

        if ((nueva - objetivo).sqrMagnitude < 0.0025f)   // a menos de 5 cm
            _estado = EstadoPelota.EnElPie;
    }

    /// <summary>
    /// Mide cuánto avanzó el jugador en el piso durante este frame. PlayerController
    /// informa la velocidad base, pero el nivel y los pickups la modifican, así que
    /// para perseguirlo hay que mirar el desplazamiento real.
    /// </summary>
    private void MedirVelocidadDelJugador(PlayerController jugador)
    {
        Vector3 delta = jugador.transform.position - _posJugadorAnterior;
        delta.y = 0f;

        if (Time.deltaTime > 0.0001f)
            _velocidadJugador = Mathf.Clamp(delta.magnitude / Time.deltaTime, 0f, 40f);

        _posJugadorAnterior = jugador.transform.position;
    }

    /// <summary>Gira la pelota como si rodara sobre el piso.</summary>
    private void Rodar(Vector3 desplazamiento)
    {
        float distancia = desplazamiento.magnitude;
        if (distancia < 0.0001f) return;

        Vector3 eje = Vector3.Cross(Vector3.up, desplazamiento.normalized);
        if (eje.sqrMagnitude < 0.0001f) return;

        pelota.Rotate(eje.normalized, distancia / Mathf.Max(0.01f, radio) * Mathf.Rad2Deg,
                      Space.World);
    }

    /// <summary>Interpolación independiente del framerate.</summary>
    private static float Amortiguar(float actual, float objetivo, float velocidad)
        => Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-velocidad * Time.deltaTime));

    // ═════════════════════════════════════════════════════════════════════════
    //  ENGANCHE CON EL OBSTACLE MANAGER
    // ═════════════════════════════════════════════════════════════════════════

    private void IntentarSuscribir()
    {
        ObstacleManager obstaculos = ObstacleManager.Instance;
        if (obstaculos == null) return;

        // Primero sacamos y después ponemos: así nunca queda enganchado dos veces.
        obstaculos.OnPelotaRobada -= Robar;
        obstaculos.OnPelotaRobada += Robar;
        _suscripto = true;
    }

    private void Desuscribir()
    {
        if (_suscripto && ObstacleManager.Instance != null)
            ObstacleManager.Instance.OnPelotaRobada -= Robar;

        _suscripto = false;
    }
}
