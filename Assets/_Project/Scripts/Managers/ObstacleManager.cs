using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// CEREBRO DEL SISTEMA DE OBSTÁCULOS (Fase B).
///
/// Hace cuatro cosas:
///
///  1. POOL: guarda los obstáculos ya usados en pilas reutilizables, uno por
///     tipo (ObstacleData). Nunca se Instancia ni se Destroy en plena carrera:
///     se pide, se usa y se devuelve. Eso evita los tirones de memoria.
///
///  2. GENERACIÓN: cuando TrackGenerator instancia un segmento, el
///     ObstacleSpawner del segmento le pide a este manager que lo llene.
///     Se arman "filas": una fila nunca tapa los 3 carriles, siempre deja al
///     menos uno libre, y se respeta una distancia mínima entre filas para
///     que el jugador tenga tiempo de reaccionar.
///
///  3. DIFICULTAD: cuántas filas, cuánto margen y qué obstáculos pueden salir
///     lo define el LevelConfig del nivel actual (GameData.CurrentLevel).
///
///  4. CHOQUE: es el único que aplica daño. Descuenta energía, enciende la
///     tarjeta amarilla si corresponde, dispara partículas, sacude la cámara,
///     hace sonar el impacto y da un segundo de invulnerabilidad.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Dificultad")]
    [Tooltip("Configuración de los 6 niveles (velocidad, densidad, obstáculos permitidos).")]
    [SerializeField] private LevelConfig levelConfig;

    [Header("Pool de obstáculos")]
    [Tooltip("Padre de los obstáculos reciclados. Si queda vacío se crea solo.")]
    [SerializeField] private Transform contenedorObstaculos;

    [Tooltip("Cuántas copias de cada obstáculo se preparan al empezar el nivel.")]
    [Range(0, 8)]
    [SerializeField] private int precalentarPorTipo = 2;

    [Tooltip("Distancia, en metros, que un obstáculo tiene que quedar detrás del " +
             "jugador para volver al pool.")]
    [SerializeField] private float distanciaReciclado = 14f;

    [Header("Tarjetas del árbitro")]
    [Tooltip("Si está prendido, juntar 3 tarjetas amarillas en un nivel es tarjeta " +
             "roja y derrota.")]
    [SerializeField] private bool usarTarjetas = true;

    [Tooltip("Cuántas amarillas hacen falta para la roja.")]
    [Range(1, 5)]
    [SerializeField] private int tarjetasParaRoja = 3;

    [Header("Feedback del choque")]
    [Tooltip("Intensidad de la sacudida de cámara (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float fuerzaSacudida = 0.7f;

    [Tooltip("Duración de la sacudida de cámara, en segundos.")]
    [SerializeField] private float duracionSacudida = 0.22f;

    [Tooltip("Cantidad de partículas del golpe.")]
    [Range(0, 60)]
    [SerializeField] private int particulasPorChoque = 18;

    [Tooltip("Sonido del impacto. Si queda vacío no suena nada (queda el hook listo " +
             "para cuando exista el SoundManager de la Fase F).")]
    [SerializeField] private AudioClip sonidoImpacto;

    // ═════════════════════════════════════════════════════════════════════════
    //  EVENTOS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Se dispara cuando el jugador choca con un obstáculo de efecto
    /// "pelota robada" (el tackle). La Fase D lo usa para hacer que la pelota
    /// se escape y vuelva al pie.
    /// </summary>
    public event Action OnPelotaRobada;

    // ═════════════════════════════════════════════════════════════════════════
    //  ESTADO INTERNO
    // ═════════════════════════════════════════════════════════════════════════

    // Una pila de objetos reciclables por cada tipo de obstáculo.
    private readonly Dictionary<ObstacleData, Stack<ObstacleBase>> _pools =
        new Dictionary<ObstacleData, Stack<ObstacleBase>>();

    // Obstáculos que están ahora mismo en la pista.
    private readonly List<ObstacleBase> _activos = new List<ObstacleBase>();

    private NivelObstaculos _nivelActual;
    private int   _nivelCacheado = -1;
    private int   _tarjetas;
    private bool  _rojaMostrada;
    private bool  _estabaCorriendo;

    private CameraFollow _camara;
    private AudioSource  _audio;
    private ParticleSystem _particulas;

    // ═════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES QUE USA EL RESTO DEL JUEGO
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Velocidad extra del nivel actual (la multiplica PlayerController).</summary>
    public float MultiplicadorVelocidadNivel =>
        _nivelActual != null ? Mathf.Max(0.1f, _nivelActual.multiplicadorVelocidad) : 1f;

    /// <summary>
    /// Configuración de las seis etapas. La usan el HUD, el menú y la pantalla de
    /// victoria para mostrar el nombre de la etapa (Entrenamiento, Octavos de
    /// final, ...) en vez de un número de nivel.
    /// </summary>
    public LevelConfig Config => levelConfig;

    /// <summary>Atajo estático: null si el manager todavía no existe en la escena.</summary>
    public static LevelConfig Configuracion => Instance != null ? Instance.Config : null;

    /// <summary>Nombre de la etapa del torneo que se está jugando ahora mismo.</summary>
    public string NombreEtapaActual =>
        levelConfig != null ? levelConfig.NombreDeEtapa(NivelDelJuego()) : "";

    /// <summary>True si la partida en curso es la FINAL del torneo.</summary>
    public bool EnLaFinal => levelConfig != null && levelConfig.EsFinal(NivelDelJuego());

    /// <summary>Segundos de gracia después de un choque.</summary>
    public float TiempoInvulnerabilidad =>
        levelConfig != null ? Mathf.Max(0f, levelConfig.tiempoInvulnerabilidad) : 1f;

    /// <summary>Velocidad lateral por defecto de los obstáculos que se mueven de costado.</summary>
    public float VelocidadLateralPorDefecto =>
        levelConfig != null ? Mathf.Max(0.5f, levelConfig.velocidadLateralPorDefecto) : 7f;

    /// <summary>Configuración del nivel que se está jugando (puede ser null).</summary>
    public NivelObstaculos NivelActual => _nivelActual;

    // ═════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (contenedorObstaculos == null)
        {
            var go = new GameObject("Obstaculos_Pool");
            go.transform.SetParent(transform, false);
            contenedorObstaculos = go.transform;
        }

        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;

        AsegurarNivel(force: true);
    }

    private void Update()
    {
        DetectarNivelNuevo();
        ReciclarLosQueQuedaronAtras();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API: GENERACIÓN DE OBSTÁCULOS
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llena un segmento de pista con filas de obstáculos.
    /// Lo llama ObstacleSpawner (que vive dentro de cada Segment_Normal).
    /// </summary>
    /// <param name="segmento">El segmento que se acaba de instanciar.</param>
    /// <param name="zInicioLocal">Z local donde empieza la zona de spawn.</param>
    /// <param name="zFinLocal">Z local donde termina.</param>
    /// <param name="zMinimaMundo">Z del mundo antes de la cual no se pone nada (zona segura de arranque).</param>
    public void PoblarSegmento(Transform segmento, float zInicioLocal, float zFinLocal, float zMinimaMundo)
    {
        if (segmento == null) return;

        NivelObstaculos nivel = AsegurarNivel();
        if (nivel == null) return;

        float rango = Mathf.Max(0f, zFinLocal - zInicioLocal);
        if (rango < 4f) return;

        // ── 1. Cuántas filas entran en este segmento ─────────────────────────
        int filas = Mathf.FloorToInt(nivel.filasPorSegmento);
        if (UnityEngine.Random.value < nivel.filasPorSegmento - filas) filas++;
        if (filas <= 0) return;

        // El margen manda: si no entran tantas filas sin amontonarse, se recortan.
        float margen = Mathf.Max(4f, nivel.margenEntreFilas);
        int maxFilas = Mathf.Max(1, Mathf.FloorToInt(rango / margen) + 1);
        filas = Mathf.Min(filas, maxFilas);

        float paso = rango / filas;
        float baseZ = segmento.position.z;

        // ── 2. Colocar cada fila ─────────────────────────────────────────────
        for (int i = 0; i < filas; i++)
        {
            // Centro de la franja + un pequeño desorden para que no se vea en serie.
            float zLocal = zInicioLocal + paso * (i + 0.5f);
            zLocal += UnityEngine.Random.Range(-paso * 0.15f, paso * 0.15f);

            // Zona segura: los primeros metros de la pista quedan limpios.
            if (baseZ + zLocal < zMinimaMundo) continue;

            CrearFila(segmento, nivel, zLocal);
        }
    }

    /// <summary>
    /// Arma UNA fila: elige el obstáculo, decide si ocupa uno o dos carriles y
    /// coloca las instancias. Nunca ocupa los tres carriles.
    /// </summary>
    private void CrearFila(Transform segmento, NivelObstaculos nivel, float zLocal)
    {
        ObstacleData dato = SortearObstaculo(nivel);
        if (dato == null || dato.prefab == null) return;

        // ¿Esta fila va a tapar dos carriles?
        bool dosCarriles = dato.carrilesQueOcupa >= 2
                           || UnityEngine.Random.value < nivel.probabilidadDosCarriles;

        // Un prefab ancho (la barrera) tiene que ir sobre dos carriles ADYACENTES.
        // Centrado sobre los carriles 0 y 2 (los de los costados) su ancho cubriría
        // también el del medio y la fila taparía los tres, sin salida posible.
        int[] carriles = dato.carrilesQueOcupa >= 2
                         ? ElegirParAdyacente()
                         : ElegirCarriles(dosCarriles);

        if (dato.carrilesQueOcupa >= 2)
        {
            // Prefab ancho (barrera): una sola pieza centrada entre los dos carriles.
            float x = (LaneSystem.GetLaneX(carriles[0]) + LaneSystem.GetLaneX(carriles[1])) * 0.5f;
            Colocar(dato, segmento, x, zLocal);
        }
        else
        {
            // Los obstáculos que se mueven DE COSTADO ya recorren varios carriles
            // por su cuenta (el deslizante cruza la cancha, el árbitro móvil va y
            // vuelve). Dos en la misma fila podrían tapar los tres carriles en el
            // momento en que se cruzan, así que van de a uno.
            bool seMueveDeCostado = dato.tipo == TipoObstaculo.Deslizante
                                    || dato.tipo == TipoObstaculo.Patrulla;

            // Un obstáculo por cada carril elegido (1 o 2 como máximo).
            int cuantos = (dosCarriles && !seMueveDeCostado) ? 2 : 1;
            for (int i = 0; i < cuantos && i < carriles.Length; i++)
                Colocar(dato, segmento, LaneSystem.GetLaneX(carriles[i]), zLocal);
        }
    }

    /// <summary>
    /// Elige 1 o 2 carriles al azar de los tres. Como máximo se ocupan 2, así
    /// SIEMPRE queda al menos un carril libre y alcanzable.
    /// </summary>
    private static int[] ElegirCarriles(bool dos)
    {
        int[] todos = { 0, 1, 2 };

        // Desordenamos (Fisher-Yates) para que no salga siempre el mismo carril.
        for (int i = todos.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (todos[i], todos[j]) = (todos[j], todos[i]);
        }

        return dos ? new[] { todos[0], todos[1] } : new[] { todos[0] };
    }

    /// <summary>
    /// Devuelve un par de carriles PEGADOS (0-1 o 1-2), al azar.
    ///
    /// Los prefabs anchos se centran entre los dos carriles elegidos, así que el
    /// par nunca puede ser 0-2: centrado ahí el obstáculo taparía también el
    /// carril del medio y la fila quedaría sin ninguna salida.
    /// </summary>
    private static int[] ElegirParAdyacente()
    {
        return UnityEngine.Random.value < 0.5f ? new[] { 0, 1 } : new[] { 1, 2 };
    }

    /// <summary>
    /// Sortea qué obstáculo aparece, respetando el nivel mínimo y los pesos de
    /// la lista de permitidos del nivel.
    /// </summary>
    private static ObstacleData SortearObstaculo(NivelObstaculos nivel)
    {
        if (nivel == null || nivel.permitidos == null) return null;

        float total = 0f;

        // Primera pasada: suma de pesos de los que realmente pueden salir.
        foreach (var entrada in nivel.permitidos)
        {
            if (entrada == null || entrada.dato == null || entrada.dato.prefab == null) continue;
            if (entrada.dato.nivelMinimo > nivel.nivel) continue;
            total += Mathf.Max(0f, entrada.peso * entrada.dato.peso);
        }

        if (total <= 0f) return null;

        // Segunda pasada: elegimos por peso.
        float sorteo = UnityEngine.Random.Range(0f, total);
        foreach (var entrada in nivel.permitidos)
        {
            if (entrada == null || entrada.dato == null || entrada.dato.prefab == null) continue;
            if (entrada.dato.nivelMinimo > nivel.nivel) continue;

            sorteo -= Mathf.Max(0f, entrada.peso * entrada.dato.peso);
            if (sorteo <= 0f) return entrada.dato;
        }

        // Respaldo: el último válido.
        for (int i = nivel.permitidos.Count - 1; i >= 0; i--)
        {
            var e = nivel.permitidos[i];
            if (e != null && e.dato != null && e.dato.prefab != null) return e.dato;
        }

        return null;
    }

    /// <summary>
    /// Saca un obstáculo del pool y lo pone en la pista en la posición pedida.
    /// </summary>
    private ObstacleBase Colocar(ObstacleData dato, Transform segmento, float x, float zLocal)
    {
        ObstacleBase obs = ObtenerDelPool(dato);
        if (obs == null) return null;

        Transform t = obs.transform;
        t.SetParent(segmento, false);

        float xFinal = x;

        // El deslizante arranca AFUERA de la cancha y cruza: su propio script
        // decide el recorrido a partir de dónde nace.
        if (dato.tipo == TipoObstaculo.Deslizante)
        {
            float lado = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            xFinal = lado * (LaneSystem.LaneWidth * 1.5f + 3.2f);
        }

        t.localPosition = new Vector3(xFinal, 0f, zLocal);
        t.localRotation = Quaternion.identity;

        obs.Inicializar(dato, this);
        _activos.Add(obs);
        return obs;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API: CHOQUE
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lo llama ObstacleBase cuando el jugador toca un obstáculo. Acá se decide
    /// TODO el daño y el feedback, en un solo lugar.
    /// </summary>
    public void RegistrarChoque(ObstacleBase obstaculo, PlayerController jugador)
    {
        if (jugador == null || !jugador.IsAlive) return;
        if (jugador.Invulnerable) return;   // todavía está en el segundo de gracia

        ObstacleData datos = obstaculo != null ? obstaculo.Datos : null;

        float danio    = datos != null ? datos.danioEnergia : 12f;
        float duracion = datos != null ? datos.duracionRalentizacion : 0f;

        // La tarjeta amarilla del árbitro siempre ralentiza un segundo.
        if (datos != null && datos.efecto == EfectoObstaculo.TarjetaAmarilla && duracion <= 0f)
            duracion = 1f;

        // 1. Energía, parpadeo e invulnerabilidad (lo hace el jugador).
        jugador.RecibirChoque(danio, duracion);

        // 2. Feedback físico y sonoro.
        Vector3 posicion = obstaculo != null ? obstaculo.transform.position : jugador.transform.position;
        EfectoDeImpacto(posicion);

        // 3. Efectos propios de cada tipo de obstáculo.
        if (datos != null)
        {
            switch (datos.efecto)
            {
                case EfectoObstaculo.TarjetaAmarilla:
                    MostrarTarjetaAmarilla();
                    break;

                case EfectoObstaculo.PelotaRobada:
                    // La Fase D engancha acá el BallDribble.
                    OnPelotaRobada?.Invoke();
                    break;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  POOL
    // ═════════════════════════════════════════════════════════════════════════

    private ObstacleBase ObtenerDelPool(ObstacleData dato)
    {
        if (dato == null) return null;

        if (!_pools.TryGetValue(dato, out Stack<ObstacleBase> pila))
        {
            pila = new Stack<ObstacleBase>();
            _pools[dato] = pila;
        }

        // Reutilizamos el primero que sirva.
        while (pila.Count > 0)
        {
            ObstacleBase guardado = pila.Pop();
            if (guardado == null) continue;
            guardado.gameObject.SetActive(true);
            return guardado;
        }

        // No había ninguno: recién acá se instancia.
        if (dato.prefab == null) return null;

        GameObject go = Instantiate(dato.prefab, contenedorObstaculos);
        ObstacleBase nuevo = go.GetComponent<ObstacleBase>();
        if (nuevo == null) nuevo = go.GetComponentInChildren<ObstacleBase>(true);

        if (nuevo == null)
        {
            Debug.LogError($"[ObstacleManager] El prefab '{dato.prefab.name}' no tiene ningún " +
                           $"componente que herede de ObstacleBase.");
            Destroy(go);
            return null;
        }

        go.SetActive(true);
        return nuevo;
    }

    /// <summary>Devuelve un obstáculo al pool (no lo destruye).</summary>
    private void Devolver(ObstacleBase obstaculo)
    {
        if (obstaculo == null) return;

        _activos.Remove(obstaculo);
        obstaculo.Desactivar();

        // Lo sacamos al contenedor del pool para que no quede colgado del
        // segmento de pista que se va a destruir.
        obstaculo.transform.SetParent(contenedorObstaculos, false);
        obstaculo.gameObject.SetActive(false);

        if (obstaculo.Datos != null)
        {
            if (!_pools.TryGetValue(obstaculo.Datos, out Stack<ObstacleBase> pila))
            {
                pila = new Stack<ObstacleBase>();
                _pools[obstaculo.Datos] = pila;
            }
            pila.Push(obstaculo);
        }
    }

    /// <summary>Devuelve al pool los obstáculos que ya quedaron detrás del jugador.</summary>
    private void ReciclarLosQueQuedaronAtras()
    {
        if (_activos.Count == 0) return;
        if (PlayerController.Instance == null) return;

        float zJugador = PlayerController.Instance.transform.position.z;

        for (int i = _activos.Count - 1; i >= 0; i--)
        {
            ObstacleBase obs = _activos[i];

            if (obs == null) { _activos.RemoveAt(i); continue; }

            if (obs.transform.position.z < zJugador - distanciaReciclado)
                Devolver(obs);
        }
    }

    /// <summary>Prepara unas copias de cada obstáculo del nivel para no instanciar en carrera.</summary>
    private void Precalentar(NivelObstaculos nivel)
    {
        if (nivel == null || nivel.permitidos == null || precalentarPorTipo <= 0) return;

        foreach (var entrada in nivel.permitidos)
        {
            ObstacleData dato = entrada != null ? entrada.dato : null;
            if (dato == null || dato.prefab == null) continue;

            if (!_pools.TryGetValue(dato, out Stack<ObstacleBase> pila))
            {
                pila = new Stack<ObstacleBase>();
                _pools[dato] = pila;
            }

            while (pila.Count < precalentarPorTipo)
            {
                GameObject go = Instantiate(dato.prefab, contenedorObstaculos);
                ObstacleBase obs = go.GetComponent<ObstacleBase>();
                if (obs == null) obs = go.GetComponentInChildren<ObstacleBase>(true);

                if (obs == null) { Destroy(go); break; }

                obs.Desactivar();
                go.SetActive(false);
                pila.Push(obs);
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  NIVEL ACTUAL
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Nivel que está jugando el jugador (mínimo 1).</summary>
    private static int NivelDelJuego()
    {
        if (GameData.Instance != null) return Mathf.Max(1, GameData.Instance.CurrentLevel);
        return 1;
    }

    /// <summary>
    /// Devuelve la configuración del nivel actual, recargándola si cambió.
    /// </summary>
    private NivelObstaculos AsegurarNivel(bool force = false)
    {
        int nivel = NivelDelJuego();

        if (!force && _nivelActual != null && nivel == _nivelCacheado) return _nivelActual;

        _nivelCacheado = nivel;
        _nivelActual = levelConfig != null ? levelConfig.Obtener(nivel) : null;

        return _nivelActual;
    }

    /// <summary>
    /// Detecta el arranque de una partida para recargar el nivel y limpiar las
    /// tarjetas acumuladas.
    /// </summary>
    private void DetectarNivelNuevo()
    {
        var gm = RunnerGameManager.Instance;
        bool corriendo = gm != null && gm.State == GameState.Running;

        if (corriendo && !_estabaCorriendo)
        {
            AsegurarNivel(force: true);
            Precalentar(_nivelActual);

            _tarjetas = 0;
            _rojaMostrada = false;
            AvisosHUD.InstanceOrCreate()?.LimpiarTarjetas();
        }

        _estabaCorriendo = corriendo;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  TARJETAS
    // ═════════════════════════════════════════════════════════════════════════

    private void MostrarTarjetaAmarilla()
    {
        if (_rojaMostrada) return;   // la partida ya está perdida: no se sigue contando

        _tarjetas++;

        AvisosHUD avisos = AvisosHUD.InstanceOrCreate();
        avisos?.MostrarTarjetaAmarilla(usarTarjetas ? _tarjetas : 0);

        Debug.Log($"[ObstacleManager] Tarjeta amarilla {_tarjetas}/{tarjetasParaRoja}.");

        if (usarTarjetas && !_rojaMostrada && _tarjetas >= tarjetasParaRoja)
        {
            // Una sola roja por partida: la derrota ya está pedida y los choques
            // que siguen (mientras se muestra el fin de nivel) no vuelven a contar.
            _rojaMostrada = true;

            avisos?.MostrarTarjetaRoja();
            Debug.Log("[ObstacleManager] Tres amarillas: tarjeta ROJA, derrota de nivel.");
            RunnerGameManager.Instance?.OnPlayerDied();
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  FEEDBACK
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Partículas + sacudida de cámara + sonido, todo en un solo lugar.</summary>
    private void EfectoDeImpacto(Vector3 posicion)
    {
        // ── Partículas ───────────────────────────────────────────────────────
        if (particulasPorChoque > 0)
        {
            if (_particulas == null) _particulas = CrearParticulas();
            if (_particulas != null)
            {
                _particulas.transform.position = posicion;
                _particulas.Emit(particulasPorChoque);
            }
        }

        // ── Sacudida de cámara ───────────────────────────────────────────────
        if (_camara == null && Camera.main != null)
            _camara = Camera.main.GetComponent<CameraFollow>();

        _camara?.Sacudir(fuerzaSacudida, duracionSacudida);

        // ── Sonido ───────────────────────────────────────────────────────────
        // Si todavía no hay clip asignado no pasa nada: queda el hook listo.
        if (_audio != null && sonidoImpacto != null)
            _audio.PlayOneShot(sonidoImpacto);
    }

    /// <summary>Crea (una sola vez) el sistema de partículas del impacto.</summary>
    private ParticleSystem CrearParticulas()
    {
        var go = new GameObject("Particulas_Impacto");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = 0.45f;
        main.startSpeed      = 5f;
        main.startSize       = 0.18f;
        main.startColor      = new Color(1f, 0.95f, 0.6f, 1f);
        main.gravityModifier = 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 200;

        // Sólo emitimos por código con Emit(): no queremos un chorro continuo.
        var emision = ps.emission;
        emision.enabled = true;
        emision.rateOverTime = 0f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.15f;

        // Material: el del pipeline URP si está, si no el de sprites.
        var render = go.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) render.material = new Material(shader);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        return ps;
    }
}
