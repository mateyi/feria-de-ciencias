using UnityEngine;
using System;
using System.Collections;

// ─────────────────────────────────────────────────────────────────────────────
// INPUT: el proyecto está configurado con "Active Input Handling = Input System
// Package (New)" (ProjectSettings → activeInputHandler: 1). Por eso leer la API
// vieja (Input.mousePosition) lanzaría una excepción en runtime.
// Usamos directivas del compilador para que el script funcione tanto con el
// Input System nuevo como con el Input Manager viejo, sin romper en ninguno.
// ─────────────────────────────────────────────────────────────────────────────
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Minijuego de penales completo, implementado como una MÁQUINA DE ESTADOS interna.
///
/// FLUJO DE ESTADOS:
///   Inactivo → Intro → Apuntando → Pateando → ResolviendoResultado
///                       ↑                            │
///                       │                            ├─→ Gol        → FinDeNivel (nivel superado)
///                       └──── Reintento ─────────────┴─→ Atajada    → FinDeNivel (game over)
///
/// RESPONSABILIDADES:
///   1. TRANSICIÓN: congelar al runner y llevar jugador, cámara y pelota al penal.
///   2. APUNTADO: una mira sigue al mouse proyectada sobre el PLANO DEL ARCO;
///      el arco está dividido en 6 zonas y la zona apuntada se resalta.
///   3. PATEADA: al hacer clic, la pelota vuela con trayectoria parabólica
///      (isKinematic + Lerp) hacia la zona elegida. La Potencia del jugador
///      acorta la duración del tiro.
///   4. ARQUERO: elige una zona con probabilidad ajustable y se estira hacia
///      ella (animación si hay Animator Controller; si no, estirada por código).
///   5. RESULTADO: misma columna = ATAJADA, distinta = GOL. Si es gol se avisa
///      por OnLevelCompleted; si se agotan los intentos, por OnLevelFailed.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
[DisallowMultipleComponent]
public class PenaltyMinigameManager : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════════
    //  MÁQUINA DE ESTADOS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Estados posibles del minijuego de penales.</summary>
    public enum PenaltyState
    {
        Inactivo,             // el penal no empezó
        Intro,                // transición: jugador, cámara y pelota tomando posición
        Apuntando,            // el jugador puede mover la mira y hacer clic
        Pateando,             // la pelota está volando hacia el arco
        ResolviendoResultado, // se está mostrando GOL / ATAJADA
        Gol,                  // resultado: gol
        Atajada,              // resultado: atajada
        FinDeNivel            // terminó: se avisó al GameManager
    }

    /// <summary>Estado actual (solo lectura desde afuera, útil para el HUD y el debug).</summary>
    public PenaltyState Estado { get; private set; } = PenaltyState.Inactivo;

    /// <summary>Intentos que le quedan al jugador.</summary>
    public int IntentosRestantes => _intentosRestantes;

    // ═════════════════════════════════════════════════════════════════════════
    //  EVENTOS (el RunnerGameManager se suscribe a estos)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Se dispara cuando el jugador convierte el penal.</summary>
    public event Action OnLevelCompleted;

    /// <summary>Se dispara cuando el jugador agota los intentos.</summary>
    public event Action OnLevelFailed;

    // ═════════════════════════════════════════════════════════════════════════
    //  SINGLETON
    // ═════════════════════════════════════════════════════════════════════════

    public static PenaltyMinigameManager Instance { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Referencias de la arena")]
    [Tooltip("Transform donde se coloca el jugador para patear (punto de penal).")]
    [SerializeField] private Transform penaltySpot;

    [Tooltip("La pelota de penal (debe tener Rigidbody).")]
    [SerializeField] private Rigidbody ball;

    [Tooltip("Posición inicial de la pelota. Si coincide con la pelota, se usa la " +
             "posición que tenía al empezar el penal.")]
    [SerializeField] private Transform ballSpawnPoint;

    [Tooltip("El arquero rival (debe tener GoalkeeperAI).")]
    [SerializeField] private GoalkeeperAI goalkeeper;

    [Tooltip("Cámara principal (se reposiciona para el penal).")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Posición y rotación de la cámara durante el penal.")]
    [SerializeField] private Transform penaltyCameraAnchor;

    [Tooltip("Campo de visión (FOV) que usa la cámara durante el penal. Más chico = " +
             "el arco se ve más grande y claro. Se restaura al valor original al salir.")]
    [SerializeField] private float fovPenal = 45f;

    [Header("Arco y zonas")]
    [Tooltip("Collider que representa el PLANO DEL ARCO. La mira se proyecta " +
             "sobre él y se limita a sus bordes. Se crea solo si falta.")]
    [SerializeField] private BoxCollider aimPlane;

    [Tooltip("Las 6 zonas del arco. Orden: TopLeft, TopCenter, TopRight, " +
             "BottomLeft, BottomCenter, BottomRight.")]
    [SerializeField] private Transform[] targetZones = new Transform[6];

    [Header("UI")]
    [Tooltip("Mira que sigue al mouse.")]
    [SerializeField] private RectTransform aimReticle;

    [Tooltip("HUD del penal (intentos, instrucción, resultado).")]
    [SerializeField] private PenaltyHUD penaltyHUD;

    [Tooltip("Cámara del Canvas (vacío si el Canvas es Screen Space - Overlay).")]
    [SerializeField] private Camera uiCamera;

    [Header("Configuración del tiro")]
    [Tooltip("Duración base del vuelo de la pelota en segundos.")]
    [SerializeField] private float kickDuration = 0.6f;

    [Tooltip("Altura máxima del arco parabólico de la pelota.")]
    [SerializeField] private float kickArcHeight = 2f;

    [Tooltip("Máximo de intentos antes de Game Over.")]
    [SerializeField] private int maxAttempts = 3;

    [Tooltip("Cuánto acorta la Potencia del jugador la duración del tiro " +
             "(0.35 = hasta 35% más rápido con Potencia 100).")]
    [Range(0f, 0.8f)]
    [SerializeField] private float reduccionDuracionMaxima = 0.35f;

    [Header("Dificultad del arquero")]
    [Tooltip("Probabilidad de que el arquero ATAJE el penal. 0.5 = mitad y mitad " +
             "(la mitad de los tiros entran, la mitad los saca el arquero).")]
    [Range(0f, 1f)]
    [SerializeField] private float goalkeeperAccuracy = 0.5f;

    [Tooltip("Cuánto sube la probabilidad de atajada por cada nivel jugado. " +
             "En 0 el arquero se queda siempre en la probabilidad base.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float accuracyPorNivel = 0f;

    [Tooltip("Tope duro de la probabilidad de atajada. La probabilidad nunca pasa " +
             "de acá aunque se jueguen muchos niveles seguidos. 0.5 = nunca ataja " +
             "más de la mitad de los tiros.")]
    [Range(0f, 1f)]
    [SerializeField] private float techoProbabilidadAtajada = 0.5f;

    [Header("Tiempos de resultado (segundos)")]
    [SerializeField] private float tiempoMensajeGol = 2f;
    [SerializeField] private float tiempoMensajeAtajada = 1.5f;

    [Header("Debug")]
    [Tooltip("Dibuja Gizmos de las zonas y de la trayectoria, y loguea cada cambio de estado.")]
    [SerializeField] private bool modoDebug = true;

    [Tooltip("Tecla para saltar directo al penal sin recorrer el nivel (solo en el Editor).")]
    [SerializeField] private KeyCode teclaAtajoDebug = KeyCode.P;

    // ═════════════════════════════════════════════════════════════════════════
    //  VARIABLES INTERNAS
    // ═════════════════════════════════════════════════════════════════════════

    private int  _intentosRestantes;
    private int  _zonaApuntada = 4;          // zona bajo la mira (0..5)
    private int  _zonaPateada  = 4;          // zona hacia la que salió el tiro

    // Posición "de fábrica" de la pelota, capturada antes de tocarla nunca.
    private Vector3    _pelotaPosicionInicial;
    private Quaternion _pelotaRotacionInicial;

    // Pose de la cámara de carrera, para devolverla tal cual al salir del penal.
    private Vector3    _camPosOriginal;
    private Quaternion _camRotOriginal;
    private float      _fovOriginal;

    // Estado del cursor antes de entrar al penal, para restaurarlo al salir.
    private bool             _cursorVisibleOriginal;
    private CursorLockMode   _cursorLockOriginal;

    // Última trayectoria calculada (solo para dibujar Gizmos en modo debug).
    private Vector3 _trazaInicio, _trazaFin;
    private bool    _tieneTraza;

    // Corrutina de la secuencia en curso, para poder cortarla.
    private Coroutine _secuencia;

    // ═════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Autoconfiguración silenciosa: si una referencia quedó vacía en el
        // Inspector, la buscamos por nombre/tipo para que el penal nunca falle.
        AutoConfigurar(silencioso: true);

        // Guardamos la pose original de la pelota ANTES de moverla nunca.
        if (ball != null)
        {
            _pelotaPosicionInicial = ball.transform.position;
            _pelotaRotacionInicial = ball.transform.rotation;
        }
    }

    private void Update()
    {
        // ── Atajo de prueba: salta directo al penal sin recorrer el nivel ────
#if UNITY_EDITOR
        if (InputTecla(teclaAtajoDebug))
            IniciarModoPenalDebug();
#endif

        // ── Apuntado: solo mientras el jugador puede patear ──────────────────
        if (Estado == PenaltyState.Apuntando)
        {
            ActualizarMira();

            if (InputClickIzquierdo())
                ConfirmarPateada();
        }
    }

    private void OnDrawGizmos()
    {
        if (!modoDebug) return;

        // ── Las 6 zonas del arco como cajas de colores ───────────────────────
        if (targetZones != null)
        {
            for (int i = 0; i < targetZones.Length; i++)
            {
                if (targetZones[i] == null) continue;

                // La zona apuntada se dibuja en verde; el resto en amarillo.
                Gizmos.color = (i == _zonaApuntada && Estado == PenaltyState.Apuntando)
                    ? new Color(0.2f, 1f, 0.3f, 0.9f)
                    : new Color(1f, 0.85f, 0.1f, 0.5f);

                Gizmos.DrawWireCube(targetZones[i].position,
                                    new Vector3(2.2f, 1.1f, 0.25f));
            }
        }

        // ── Plano del arco ───────────────────────────────────────────────────
        if (aimPlane != null)
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.6f);
            Bounds b = aimPlane.bounds;
            Gizmos.DrawWireCube(b.center, b.size);
        }

        // ── Última trayectoria de la pelota ──────────────────────────────────
        if (_tieneTraza)
        {
            Gizmos.color = Color.white;
            Vector3 previo = _trazaInicio;
            const int pasos = 20;
            for (int i = 1; i <= pasos; i++)
            {
                float t = i / (float)pasos;
                Vector3 p = Vector3.Lerp(_trazaInicio, _trazaFin, t)
                          + Vector3.up * (kickArcHeight * Mathf.Sin(t * Mathf.PI));
                Gizmos.DrawLine(previo, p);
                previo = p;
            }
        }

        // ── Punto de penal ───────────────────────────────────────────────────
        if (penaltySpot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(penaltySpot.position, 0.25f);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por RunnerGameManager.OnFinishReached() cuando el jugador cruza la META.
    /// Arranca la secuencia de transición hacia el penal.
    /// </summary>
    public void BeginPenaltySequence()
    {
        if (_secuencia != null) StopCoroutine(_secuencia);
        _secuencia = StartCoroutine(SecuenciaPenal());
    }

    /// <summary>
    /// Atajo de prueba (tecla P en el Editor): activa el modo penal sin tener que
    /// recorrer todo el nivel. Si el juego está en el menú, arranca la partida y
    /// salta directo al penal.
    /// </summary>
    public void IniciarModoPenalDebug()
    {
        var gm = RunnerGameManager.Instance;

        if (gm == null)
        {
            // Sin GameManager (escena de prueba) arrancamos el penal directo.
            BeginPenaltySequence();
            return;
        }

        // Si no estamos corriendo, arrancamos una partida y saltamos al penal.
        // Se llama a OnPenaltyArenaReached (no a OnFinishReached) porque ahora la
        // META ya no frena al jugador: el penal arranca al llegar al túnel.
        if (gm.State != GameState.Running)
        {
            gm.StartGame();
            gm.OnPenaltyArenaReached();
        }
        else
        {
            gm.OnPenaltyArenaReached();
        }
    }

    /// <summary>
    /// Atajo de prueba: patea directo hacia una zona (0..5) sin necesidad de mouse.
    /// Sirve para el piloto automático del Editor y para probar el minijuego sin
    /// depender del input real. Solo tiene efecto en estado Apuntando.
    /// </summary>
    public void PatearDebug(int zona)
    {
        if (Estado != PenaltyState.Apuntando) return;

        _zonaApuntada = Mathf.Clamp(zona, 0, 5);
        ConfirmarPateada();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SECUENCIA PRINCIPAL
    // ═════════════════════════════════════════════════════════════════════════

    private IEnumerator SecuenciaPenal()
    {
        CambiarEstado(PenaltyState.Intro);

        // Guardamos el estado del cursor y lo liberamos para poder apuntar.
        _cursorVisibleOriginal = Cursor.visible;
        _cursorLockOriginal    = Cursor.lockState;
        Cursor.visible   = true;
        Cursor.lockState = CursorLockMode.None;

        // Esperamos un frame para que el CharacterController procese el frenado.
        yield return null;

        // ── 1. Jugador en el punto penal, mirando al arco ────────────────────
        if (PlayerController.Instance != null && penaltySpot != null)
        {
            // El jugador se ubica DETRÁS y a un costado de la pelota (no encima
            // de ella): así la cámara del penal lo muestra en el encuadre sin
            // que tape el arco ni la pelota.
            Vector3 posicionJugador = penaltySpot.position + new Vector3(-0.9f, 0f, -1.2f);

            PlayerController.Instance.Respawn(posicionJugador);
            // Respawn vuelve a habilitar el runner: lo apagamos otra vez.
            PlayerController.Instance.SetRunnerEnabled(false);
            PlayerController.Instance.transform.rotation = penaltySpot.rotation;
        }

        // ── 2. Cámara al ancla del penal ─────────────────────────────────────
        MoverCamaraAlPenal();

        // ── 3. Pelota y arquero a su estado inicial ──────────────────────────
        ResetearPelota();
        goalkeeper?.ResetToIdle();
        ResetearZonas();

        // ── 4. HUD de penal ──────────────────────────────────────────────────
        _intentosRestantes = Mathf.Max(1, maxAttempts);
        HUD?.Show(_intentosRestantes, maxAttempts);

        // Pausa dramática antes de habilitar el tiro.
        yield return new WaitForSeconds(0.9f);

        // ── 5. Habilitar el apuntado ─────────────────────────────────────────
        CentrarMiraEnArco();
        MostrarMira(true);
        CambiarEstado(PenaltyState.Apuntando);
        _secuencia = null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  APUNTADO
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Proyecta el mouse sobre el PLANO DEL ARCO, limita el punto a los bordes
    /// del arco, mueve la mira y resalta la zona correspondiente.
    ///
    /// Usamos un plano matemático (Plane.Raycast) en vez de un raycast físico:
    /// así la pelota, el arquero o cualquier collider del arco no pueden
    /// interceptar el rayo ni romper el apuntado.
    /// </summary>
    private void ActualizarMira()
    {
        if (mainCamera == null || aimPlane == null) return;

        Vector2 mouse = PosicionMouse();
        Ray rayo = mainCamera.ScreenPointToRay(mouse);

        // Plano del arco: pasa por el centro del AimPlane, mirando hacia la cámara.
        Bounds b = aimPlane.bounds;
        Plane plano = new Plane(Vector3.back, b.center);

        if (!plano.Raycast(rayo, out float distancia)) return;

        Vector3 punto = rayo.GetPoint(distancia);

        // ── Limitar la mira a los bordes del arco ────────────────────────────
        punto.x = Mathf.Clamp(punto.x, b.min.x, b.max.x);
        punto.y = Mathf.Clamp(punto.y, b.min.y, b.max.y);
        punto.z = b.center.z;

        // ── Calcular qué zona del arco está debajo de la mira ────────────────
        int zonaAnterior = _zonaApuntada;
        _zonaApuntada = ZonaDesdePunto(punto);

        // Resaltar solo la zona apuntada.
        if (zonaAnterior != _zonaApuntada || _primeraActualizacionMira)
        {
            ResaltarZona(_zonaApuntada);
            _primeraActualizacionMira = false;
        }

        // ── Mover la mira en pantalla ────────────────────────────────────────
        if (aimReticle != null)
        {
            Vector3 pantalla = mainCamera.WorldToScreenPoint(punto);
            pantalla.z = 0f;
            aimReticle.position = pantalla;
        }
    }

    // Bandera para forzar el primer resaltado al entrar en Apuntando.
    private bool _primeraActualizacionMira = true;

    /// <summary>
    /// Convierte un punto del plano del arco en un índice de zona 0..5.
    /// Reparto: 3 columnas (izq/centro/der) × 2 filas (arriba/abajo).
    /// Índice = fila * 3 + columna  →  0..5 (mismo orden que targetZones).
    /// </summary>
    private int ZonaDesdePunto(Vector3 punto)
    {
        Bounds b = aimPlane.bounds;

        // u = 0 en el borde izquierdo, 1 en el derecho.
        float u = Mathf.InverseLerp(b.min.x, b.max.x, punto.x);
        // v = 0 en el borde inferior, 1 en el superior.
        float v = Mathf.InverseLerp(b.min.y, b.max.y, punto.y);

        int columna = Mathf.Clamp(Mathf.FloorToInt(u * 3f), 0, 2);   // 0,1,2
        int fila    = v >= 0.5f ? 0 : 1;                             // 0=arriba, 1=abajo

        return fila * 3 + columna;
    }

    /// <summary>Resalta la zona indicada y apaga el resto.</summary>
    private void ResaltarZona(int zona)
    {
        if (targetZones == null) return;

        for (int i = 0; i < targetZones.Length; i++)
        {
            if (targetZones[i] == null) continue;

            var pz = targetZones[i].GetComponent<PenaltyZone>();
            if (pz != null) pz.SetHighlighted(i == zona);
        }
    }

    /// <summary>Apaga el resaltado de todas las zonas.</summary>
    private void ResetearZonas() => ResaltarZona(-1);

    // ═════════════════════════════════════════════════════════════════════════
    //  PATEADA
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>El jugador hizo clic: se bloquea el apuntado y arranca el tiro.</summary>
    private void ConfirmarPateada()
    {
        if (goalkeeper == null && ball == null) return;

        _zonaPateada = _zonaApuntada;

        if (modoDebug)
            Debug.Log($"[Penal] Click recibido → zona {_zonaPateada} ({NombreZona(_zonaPateada)})");

        CambiarEstado(PenaltyState.Pateando);
        MostrarMira(false);
        ResetearZonas();

        _secuencia = StartCoroutine(SecuenciaDeTiro());
    }

    private IEnumerator SecuenciaDeTiro()
    {
        // ── 1. El arquero elige su zona ──────────────────────────────────────
        GoalkeeperAI.SaveZone zonaArquero = ElegirZonaArquero(_zonaPateada);

        // ── 2. La pelota vuela en parábola hacia la zona pateada ─────────────
        Vector3 destino = PosicionObjetivoDeZona(_zonaPateada);
        Vector3 origen  = ball != null ? ball.transform.position : destino;

        _trazaInicio = origen;
        _trazaFin    = destino;
        _tieneTraza  = true;

        yield return StartCoroutine(AnimarPelota(origen, destino, DuracionTiroEfectiva()));

        // ── 3. Evaluar el resultado ──────────────────────────────────────────
        bool esGol = EvaluarGol(_zonaPateada, zonaArquero);

        if (modoDebug)
            Debug.Log($"[Penal] Resultado: {(esGol ? "GOL" : "ATAJADA")} " +
                      $"(pateó {NombreZona(_zonaPateada)}, arquero cubrió {zonaArquero})");

        CambiarEstado(PenaltyState.ResolviendoResultado);
        yield return StartCoroutine(ResolverResultado(esGol));

        _secuencia = null;
    }

    /// <summary>
    /// Duración del tiro ajustada por la POTENCIA del jugador.
    /// Usamos la stat 'skill' (habilidad) de PlayerStats como potencia de tiro:
    /// a mayor habilidad, menor duración → la pelota llega antes.
    /// </summary>
    private float DuracionTiroEfectiva()
    {
        float potencia01 = 0f;
        if (PlayerStats.Instance != null)
            potencia01 = Mathf.Clamp01(PlayerStats.Instance.skill / 100f);

        // Mejora "Potencia" de la tienda: suma pegada al disparo.
        if (UpgradeManager.Instance != null)
            potencia01 = Mathf.Clamp01(potencia01 + UpgradeManager.Instance.BonusPotencia);

        // Con potencia 0 → duración base. Con potencia 1 → se resta el máximo.
        float factor = 1f - (reduccionDuracionMaxima * potencia01);
        return Mathf.Max(0.15f, kickDuration * factor);
    }

    /// <summary>
    /// Mueve la pelota de 'origen' a 'destino' con una parábola.
    /// Durante el vuelo la ponemos isKinematic para que la física no la desvíe,
    /// y al finalizar le devolvemos el control a la física.
    /// </summary>
    private IEnumerator AnimarPelota(Vector3 origen, Vector3 destino, float duracion)
    {
        if (ball == null) yield break;

        ball.isKinematic = true;
        ball.linearVelocity  = Vector3.zero;
        ball.angularVelocity = Vector3.zero;

        float transcurrido = 0f;
        while (transcurrido < duracion)
        {
            transcurrido += Time.deltaTime;
            float t = Mathf.Clamp01(transcurrido / duracion);

            // Interpolación lineal + arco vertical (sin) para la parábola.
            Vector3 lineal = Vector3.Lerp(origen, destino, t);
            float   arco   = kickArcHeight * Mathf.Sin(t * Mathf.PI);

            ball.transform.position = lineal + Vector3.up * arco;

            // Rotación de la pelota mientras vuela (efecto visual).
            ball.transform.Rotate(Vector3.right, 720f * Time.deltaTime, Space.Self);

            yield return null;
        }

        // Posición final exacta.
        ball.transform.position = destino;
    }

    /// <summary>Punto del espacio al que vuela la pelota para una zona dada.</summary>
    private Vector3 PosicionObjetivoDeZona(int zona)
    {
        if (targetZones != null && zona >= 0 && zona < targetZones.Length && targetZones[zona] != null)
        {
            // Si la zona tiene PenaltyZone usamos su TargetPosition (punto fino).
            var pz = targetZones[zona].GetComponent<PenaltyZone>();
            if (pz != null) return pz.TargetPosition;
            return targetZones[zona].position;
        }

        return aimPlane != null ? aimPlane.bounds.center : Vector3.zero;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ARQUERO
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hace elegir al arquero y decide, de una sola vez, si el penal termina en
    /// gol o en atajada.
    ///
    /// CÓMO FUNCIONA (mitad y mitad):
    ///   1. Se sortea UNA vez con la probabilidad de atajada (0.5 por defecto).
    ///   2. Si le toca atajar, el arquero se tira a la MISMA columna del tiro
    ///      (se ve que la saca bien).
    ///   3. Si le toca que sea gol, se tira a una de las otras dos columnas
    ///      (se ve que voló para el otro lado).
    ///
    /// Así la animación SIEMPRE coincide con el resultado: no queda el arquero
    /// clavado en el medio mientras la pelota entra por el costado.
    /// Antes se sorteaba la columna y después se comparaba, y eso duplicaba las
    /// chances de atajada (adivinaba + acertaba de casualidad), por eso atajaba
    /// casi siempre.
    /// </summary>
    private GoalkeeperAI.SaveZone ElegirZonaArquero(int zonaDelTiro)
    {
        float probabilidad = ProbabilidadAtajadaEfectiva();
        bool  ataja        = UnityEngine.Random.value < probabilidad;

        GoalkeeperAI.SaveZone columnaDelTiro = ColumnaDelTiro(zonaDelTiro);
        GoalkeeperAI.SaveZone elegida;

        if (ataja)
        {
            // Se tira bien: cubre justo la columna del tiro.
            elegida = columnaDelTiro;
        }
        else
        {
            // Se tira mal: va a una de las otras dos columnas, así es gol.
            elegida = ColumnaContraria(columnaDelTiro);
        }

        // Le avisamos al arquero para que anime la estirada hacia esa columna.
        goalkeeper?.React(elegida);

        if (modoDebug)
            Debug.Log($"[Penal] Arquero: {elegida} | probabilidad de atajada {probabilidad:P0} " +
                      $"| resultado {(ataja ? "ATAJADA" : "GOL")}");

        return elegida;
    }

    /// <summary>
    /// Probabilidad real de atajada = base del Inspector + suba por nivel,
    /// menos lo que reste la mejora "Definición" de la tienda.
    /// Siempre queda recortada por el techo configurado (0.5 por defecto), así
    /// el arquero nunca vuelve a atajar casi todo por más niveles que se jueguen.
    /// </summary>
    private float ProbabilidadAtajadaEfectiva()
    {
        int nivel = 1;
        if (GameData.Instance != null) nivel = Mathf.Max(1, GameData.Instance.CurrentLevel);

        // Mejora "Definición" de la tienda: el arquero ataja menos.
        float reduccion = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.ReduccionPrecisionArquero : 0f;

        float probabilidad = goalkeeperAccuracy + accuracyPorNivel * (nivel - 1) - reduccion;

        // El techo se recorta a 0..1 por las dudas de que en el Inspector quede
        // un valor raro, y después se aplica como tope superior.
        float techo = Mathf.Clamp01(techoProbabilidadAtajada);
        return Mathf.Clamp(Mathf.Clamp01(probabilidad), 0f, techo);
    }

    /// <summary>
    /// Devuelve una de las DOS columnas que no son la del tiro, sorteada al azar.
    /// Sirve para que, cuando el arquero se tira mal, no se lo vea siempre
    /// volando para el mismo lado.
    /// </summary>
    private static GoalkeeperAI.SaveZone ColumnaContraria(GoalkeeperAI.SaveZone columnaDelTiro)
    {
        int indiceTiro = (int)columnaDelTiro;                 // 0, 1 o 2
        int salto      = UnityEngine.Random.value < 0.5f ? 1 : 2;
        return (GoalkeeperAI.SaveZone)((indiceTiro + salto) % 3);
    }

    /// <summary>
    /// Convierte el índice de zona (0..5) a la columna que cubre el arquero.
    /// Las zonas 0,3 son izquierda; 1,4 centro; 2,5 derecha.
    /// </summary>
    private GoalkeeperAI.SaveZone ColumnaDelTiro(int zona)
    {
        int columna = zona % 3;
        return columna switch
        {
            0 => GoalkeeperAI.SaveZone.Left,
            2 => GoalkeeperAI.SaveZone.Right,
            _ => GoalkeeperAI.SaveZone.Center,
        };
    }

    /// <summary>
    /// Hay GOL cuando el arquero no cubre la columna del tiro.
    /// Comparamos solo la columna porque el arquero tiene 3 animaciones
    /// (izquierda / centro / derecha), no 6.
    /// </summary>
    private bool EvaluarGol(int zona, GoalkeeperAI.SaveZone zonaArquero)
        => ColumnaDelTiro(zona) != zonaArquero;

    // ═════════════════════════════════════════════════════════════════════════
    //  RESULTADO
    // ═════════════════════════════════════════════════════════════════════════

    private IEnumerator ResolverResultado(bool esGol)
    {
        if (esGol)
        {
            // ── GOL ──────────────────────────────────────────────────────────
            CambiarEstado(PenaltyState.Gol);
            HUD?.ShowResult(true, _intentosRestantes);

            // Bonus de monedas por convertir el penal.
            for (int i = 0; i < 5; i++) RunnerGameManager.Instance?.AddCoin();

            yield return new WaitForSeconds(tiempoMensajeGol);

            FinalizarNivel(gano: true);
        }
        else
        {
            // ── ATAJADA ──────────────────────────────────────────────────────
            _intentosRestantes--;
            CambiarEstado(PenaltyState.Atajada);
            HUD?.ShowResult(false, _intentosRestantes);
            HUD?.UpdateAttempts(_intentosRestantes, maxAttempts);

            if (modoDebug)
                Debug.Log($"[Penal] Atajada. Intentos restantes: {_intentosRestantes}");

            yield return new WaitForSeconds(tiempoMensajeAtajada);

            if (_intentosRestantes <= 0)
            {
                // Se agotaron los intentos → fin de nivel con derrota.
                FinalizarNivel(gano: false);
            }
            else
            {
                // ── REINTENTO: restaurar pelota y arquero ────────────────────
                ResetearPelota();
                goalkeeper?.ResetToIdle();
                ResetearZonas();

                HUD?.Show(_intentosRestantes, maxAttempts);

                yield return new WaitForSeconds(0.4f);

                CentrarMiraEnArco();
                MostrarMira(true);
                _primeraActualizacionMira = true;
                CambiarEstado(PenaltyState.Apuntando);
            }
        }
    }

    /// <summary>
    /// Cierra el minijuego: limpia cursor/mira/cámara y avisa al RunnerGameManager.
    /// </summary>
    private void FinalizarNivel(bool gano)
    {
        CambiarEstado(PenaltyState.FinDeNivel);

        MostrarMira(false);
        HUD?.Hide();
        ResetearZonas();
        RestaurarCursor();

        if (gano)
        {
            // Disparamos el evento. Si nadie está suscrito (por orden de Awake),
            // avisamos directo al GameManager para no dejar el nivel colgado.
            if (OnLevelCompleted != null) OnLevelCompleted.Invoke();
            else RunnerGameManager.Instance?.OnLevelComplete();
        }
        else
        {
            if (OnLevelFailed != null) OnLevelFailed.Invoke();
            else RunnerGameManager.Instance?.OnPenaltyFailed();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CÁMARA, PELOTA, CURSOR, MIRA
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mueve la cámara al ancla del penal y desactiva el CameraFollow para que
    /// no la devuelva al carril. Guarda la pose original para restaurarla después.
    /// </summary>
    private void MoverCamaraAlPenal()
    {
        if (mainCamera == null || penaltyCameraAnchor == null) return;

        _camPosOriginal = mainCamera.transform.position;
        _camRotOriginal = mainCamera.transform.rotation;
        _fovOriginal    = mainCamera.fieldOfView;

        var follow = mainCamera.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = false;

        mainCamera.transform.position = penaltyCameraAnchor.position;
        mainCamera.transform.rotation = penaltyCameraAnchor.rotation;

        // Achicamos el FOV: con el de carrera (72°) el arco se veía chico y lejano.
        if (fovPenal > 0f) mainCamera.fieldOfView = fovPenal;
    }

    /// <summary>
    /// Devuelve la cámara a la pose de carrera y reactiva el CameraFollow.
    /// Se llama al reiniciar la partida (lo usa RunnerGameManager) y al salir del penal.
    /// </summary>
    public void RestaurarCamaraDeCarrera()
    {
        if (mainCamera == null) return;

        var follow = mainCamera.GetComponent<CameraFollow>();
        if (follow != null) follow.enabled = true;

        // Solo restauramos si salimos del ancla (evita saltos raros al inicio).
        if (Estado == PenaltyState.Inactivo) return;

        mainCamera.transform.position = _camPosOriginal;
        mainCamera.transform.rotation = _camRotOriginal;

        // Devolvemos el campo de visión de carrera.
        if (_fovOriginal > 0f) mainCamera.fieldOfView = _fovOriginal;
    }

    /// <summary>Devuelve la pelota a su posición inicial y la deja quieta.</summary>
    private void ResetearPelota()
    {
        if (ball == null) return;

        // Si el 'ballSpawnPoint' es la propia pelota (pasa en esta escena),
        // usar su posición sería un error: la pelota quedaría donde quedó.
        // Por eso usamos siempre la pose capturada al arrancar el juego.
        bool spawnValido = ballSpawnPoint != null && ballSpawnPoint != ball.transform;

        Vector3    pos = spawnValido ? ballSpawnPoint.position : _pelotaPosicionInicial;
        Quaternion rot = _pelotaRotacionInicial;

        ball.isKinematic     = true;
        ball.linearVelocity  = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.transform.SetPositionAndRotation(pos, rot);

        _tieneTraza = false;
    }

    /// <summary>
    /// Coloca la mira en el centro del arco. Se usa al entrar en la fase de
    /// apuntado para que no aparezca en la esquina de la pantalla el primer frame.
    /// </summary>
    private void CentrarMiraEnArco()
    {
        if (mainCamera == null || aimReticle == null || aimPlane == null) return;

        Vector3 pantalla = mainCamera.WorldToScreenPoint(aimPlane.bounds.center);
        pantalla.z = 0f;
        aimReticle.position = pantalla;
    }

    /// <summary>Muestra u oculta la mira.</summary>
    private void MostrarMira(bool visible)
    {
        if (aimReticle == null) return;

        // Si la mira no tiene sprite, no se dibuja nunca: le generamos uno.
        var img = aimReticle.GetComponent<UnityEngine.UI.Image>();
        if (img != null && img.sprite == null)
            img.sprite = CrearSpriteMiraPorDefecto();

        aimReticle.gameObject.SetActive(visible);
    }

    /// <summary>Restaura el estado del cursor que había antes de entrar al penal.</summary>
    private void RestaurarCursor()
    {
        Cursor.visible   = _cursorVisibleOriginal;
        Cursor.lockState = _cursorLockOriginal;
    }

    /// <summary>
    /// Genera un sprite de mira (círculo + cruz) por código.
    /// Se usa como respaldo si la Image del reticle no tiene sprite asignado,
    /// porque una Image sin sprite no dibuja nada.
    /// </summary>
    private static Sprite CrearSpriteMiraPorDefecto()
    {
        const int T = 64;
        var tex = new Texture2D(T, T, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            name = "MiraGenerada"
        };

        var pixeles = new Color32[T * T];
        Vector2 centro = new Vector2(T / 2f - 0.5f, T / 2f - 0.5f);

        for (int y = 0; y < T; y++)
        {
            for (int x = 0; x < T; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), centro);

                bool anillo   = Mathf.Abs(d - 22f) < 2.5f;             // círculo
                bool punto    = d < 3.5f;                               // centro
                bool cruzH    = Mathf.Abs(y - centro.y) < 1.5f && Mathf.Abs(x - centro.x) < 13f && d > 9f;
                bool cruzV    = Mathf.Abs(x - centro.x) < 1.5f && Mathf.Abs(y - centro.y) < 13f && d > 9f;

                pixeles[y * T + x] = (anillo || punto || cruzH || cruzV)
                    ? new Color32(255, 240, 60, 255)
                    : new Color32(0, 0, 0, 0);
            }
        }

        tex.SetPixels32(pixeles);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, T, T), new Vector2(0.5f, 0.5f), 100f);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  INPUT (compatible con Input System nuevo y con Input Manager viejo)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Posición del mouse en píxeles de pantalla.</summary>
    private static Vector2 PosicionMouse()
    {
#if ENABLE_INPUT_SYSTEM
        // Input System nuevo (el que usa este proyecto).
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        // Input Manager viejo.
        return Input.mousePosition;
#endif
    }

    /// <summary>¿Se hizo clic izquierdo en este frame?</summary>
    private static bool InputClickIzquierdo()
    {
#if ENABLE_INPUT_SYSTEM
        // Mouse.current puede ser null si no hay mouse enchufado.
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    /// <summary>¿Se presionó esta tecla en este frame? (solo se usa en el Editor).</summary>
    private static bool InputTecla(KeyCode tecla)
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;

        // Traducimos la KeyCode a la tecla equivalente del Input System nuevo.
        return tecla switch
        {
            KeyCode.P => Keyboard.current.pKey.wasPressedThisFrame,
            KeyCode.O => Keyboard.current.oKey.wasPressedThisFrame,
            KeyCode.K => Keyboard.current.kKey.wasPressedThisFrame,
            KeyCode.L => Keyboard.current.lKey.wasPressedThisFrame,
            _         => false,
        };
#else
        return Input.GetKeyDown(tecla);
#endif
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  DEBUG
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Cambia de estado y (en modo debug) lo informa por consola.</summary>
    private void CambiarEstado(PenaltyState nuevo)
    {
        Estado = nuevo;
        if (modoDebug) Debug.Log($"[Penal] Estado: {nuevo}");
    }

    /// <summary>Nombre legible de una zona, para los logs de debug.</summary>
    private static string NombreZona(int zona) => zona switch
    {
        0 => "Arriba-Izquierda",
        1 => "Arriba-Centro",
        2 => "Arriba-Derecha",
        3 => "Abajo-Izquierda",
        4 => "Abajo-Centro",
        5 => "Abajo-Derecha",
        _ => "?"
    };

    /// <summary>Acceso corto al HUD (usa la referencia o el singleton).</summary>
    private PenaltyHUD HUD => penaltyHUD != null ? penaltyHUD : PenaltyHUD.Instance;

    // ═════════════════════════════════════════════════════════════════════════
    //  AUTOCONFIGURACIÓN
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Menú contextual del Inspector: busca y crea todo lo que falte.
    /// Uso: clic derecho sobre el componente → "Autoconfigurar".
    /// </summary>
    [ContextMenu("Autoconfigurar")]
    public void AutoconfigurarDesdeMenu()
    {
        AutoConfigurar(silencioso: false);

#if UNITY_EDITOR
        // En el Editor guardamos los cambios y marcamos la escena como sucia.
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif

        Debug.Log("[Penal] Autoconfiguración terminada.");
    }

    /// <summary>
    /// Busca por nombre/tipo todas las referencias vacías y crea lo que falte.
    /// </summary>
    /// <param name="silencioso">Si es true no loguea nada cuando todo está bien.</param>
    private void AutoConfigurar(bool silencioso)
    {
        int encontrados = 0;
        int creados    = 0;

        // ── Punto penal ──────────────────────────────────────────────────────
        if (penaltySpot == null)
        {
            penaltySpot = BuscarTransform("PenaltySpotMark");
            if (penaltySpot != null) encontrados++;
        }

        // ── Pelota ───────────────────────────────────────────────────────────
        if (ball == null)
        {
            var arena = GameObject.Find("PenaltyArena_Instance");
            if (arena != null)
            {
                var t = arena.transform.Find("Ball");
                if (t != null) { ball = t.GetComponent<Rigidbody>(); if (ball != null) encontrados++; }
            }
        }

        // ── Arquero ──────────────────────────────────────────────────────────
        if (goalkeeper == null)
        {
            goalkeeper = FindFirstObjectByType<GoalkeeperAI>(FindObjectsInactive.Include);
            if (goalkeeper != null) encontrados++;
        }

        // ── Cámara principal ─────────────────────────────────────────────────
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null) encontrados++;
        }

        // ── Ancla de cámara de penal ─────────────────────────────────────────
        if (penaltyCameraAnchor == null)
        {
            penaltyCameraAnchor = BuscarTransform("PenaltyCameraAnchor");

            if (penaltyCameraAnchor == null)
            {
                // La creamos detrás del punto penal y elevada, mirando al arco.
                var go = new GameObject("PenaltyCameraAnchor");
                Vector3 basePos = penaltySpot != null ? penaltySpot.position : new Vector3(0f, 0f, 329f);
                go.transform.position = basePos + new Vector3(0f, 2.6f, -5f);
                go.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
                penaltyCameraAnchor = go.transform;
                creados++;
            }
            else encontrados++;
        }

        // ── Zonas del arco (6) ───────────────────────────────────────────────
        var zonasEncontradas = FindObjectsByType<PenaltyZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (zonasEncontradas.Length >= 6)
        {
            if (targetZones == null || targetZones.Length != 6) targetZones = new Transform[6];

            // Ordenamos por IdIndex para que el índice del array coincida con la zona.
            var ordenadas = new PenaltyZone[6];
            foreach (var z in zonasEncontradas)
                if (z.IdIndex >= 0 && z.IdIndex < 6) ordenadas[z.IdIndex] = z;

            for (int i = 0; i < 6; i++)
                targetZones[i] = ordenadas[i] != null ? ordenadas[i].transform : targetZones[i];

            encontrados++;
        }
        else if (!silencioso)
        {
            Debug.LogWarning($"[Penal] No encontré las 6 zonas del arco (encontré {zonasEncontradas.Length}). " +
                             "Revisá PenaltyArena_Instance/Goal/TargetZones.");
        }

        // ── Plano del arco (AimPlane) ────────────────────────────────────────
        if (aimPlane == null)
        {
            aimPlane = BuscarComponente<BoxCollider>("PenaltyAimPlane");

            if (aimPlane == null)
            {
                aimPlane = CrearAimPlane();
                creados++;
            }
            else encontrados++;
        }

        // ── Mira ─────────────────────────────────────────────────────────────
        if (aimReticle == null)
        {
            var t = BuscarTransform("AimReticle");
            if (t != null) { aimReticle = t as RectTransform; if (aimReticle != null) encontrados++; }
        }

        // ── HUD de penal ─────────────────────────────────────────────────────
        if (penaltyHUD == null)
        {
            penaltyHUD = FindFirstObjectByType<PenaltyHUD>(FindObjectsInactive.Include);
            if (penaltyHUD != null) encontrados++;
        }

        // ── Sprite de la mira ────────────────────────────────────────────────
        if (aimReticle != null)
        {
            var img = aimReticle.GetComponent<UnityEngine.UI.Image>();
            if (img != null && img.sprite == null)
            {
                img.sprite = CrearSpriteMiraPorDefecto();
                creados++;
            }
        }

        // ── Animator Controller del arquero ──────────────────────────────────
        if (goalkeeper != null)
        {
            var anim = goalkeeper.GetComponent<Animator>();
            if (anim != null && anim.runtimeAnimatorController == null)
            {
                var ctrl = Resources.Load<RuntimeAnimatorController>("GoalkeeperController");
#if UNITY_EDITOR
                if (ctrl == null)
                    ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        "Assets/_Project/Animations/GoalkeeperController.controller");
#endif
                if (ctrl != null) { anim.runtimeAnimatorController = ctrl; creados++; }
                else if (!silencioso)
                    Debug.LogWarning("[Penal] El arquero no tiene Animator Controller y no pude cargar " +
                                     "GoalkeeperController. Se usará la estirada por código (fallback).");
            }
        }

        if (!silencioso)
            Debug.Log($"[Penal] Autoconfigurar: {encontrados} referencias encontradas, {creados} creadas.");
    }

    /// <summary>
    /// Crea un BoxCollider invisible con el tamaño exacto del arco, apoyado sobre
    /// el plano de las 6 zonas. Sirve para limitar la mira a los bordes del arco.
    /// Es un TRIGGER para no interferir con la física de la pelota.
    /// </summary>
    private BoxCollider CrearAimPlane()
    {
        // Calculamos el rectángulo del arco a partir de las posiciones de las zonas.
        Vector3 centro = new Vector3(0f, 1.22f, 339f);
        Vector3 tamano = new Vector3(7.32f, 2.44f, 0.2f);

        if (targetZones != null)
        {
            bool hayZonas = false;
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            float z = 0f;

            foreach (var t in targetZones)
            {
                if (t == null) continue;
                hayZonas = true;
                minX = Mathf.Min(minX, t.position.x);
                maxX = Mathf.Max(maxX, t.position.x);
                minY = Mathf.Min(minY, t.position.y);
                maxY = Mathf.Max(maxY, t.position.y);
                z    = t.position.z;
            }

            if (hayZonas)
            {
                // Con 3 columnas y 2 filas: el paso entre centros define la celda.
                float pasoX = (maxX - minX) / 2f;
                float pasoY = (maxY - minY);

                centro = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, z);
                tamano = new Vector3(pasoX * 3f, pasoY * 2f, 0.2f);
            }
        }

        var go = new GameObject("PenaltyAimPlane");
        go.transform.position = centro;
        go.transform.rotation = Quaternion.identity;

        var col = go.AddComponent<BoxCollider>();
        col.size      = tamano;
        col.isTrigger = true;   // no colisiona: solo se usa para el apuntado

        if (modoDebug)
            Debug.Log($"[Penal] AimPlane creado en {centro} con tamaño {tamano}");

        return col;
    }

    /// <summary>Busca un Transform por nombre en toda la escena (incluye inactivos).</summary>
    private static Transform BuscarTransform(string nombre)
    {
        var todos = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in todos)
            if (t.name == nombre) return t;
        return null;
    }

    /// <summary>Busca un componente de tipo T en un GameObject con ese nombre.</summary>
    private static T BuscarComponente<T>(string nombre) where T : Component
    {
        var go = GameObject.Find(nombre);
        return go != null ? go.GetComponent<T>() : null;
    }
}
