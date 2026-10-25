using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CEREBRO DE LOS PICKUPS (Fase C).
///
/// Hace cuatro cosas:
///
///  1. POOL: guarda los pickups ya usados en pilas reutilizables, uno por tipo
///     (PickupData). No se Instancia ni se Destroy en plena carrera.
///
///  2. REPARTO: cuando el ObstacleSpawner arma un segmento de pista le pide que
///     reparta unos pocos pickups en carriles al azar. Cuántos caen por segmento
///     lo define la etapa que se está jugando: frecuencia baja y configurable.
///
///  3. EFECTOS: lleva los segundos que le quedan al turbo y al imán y expone el
///     valor que consultan los demás sistemas:
///        - PlayerController pregunta MultiplicadorVelocidad.
///        - Coin pregunta ImanActivo y RadioImanActual.
///     El ESCUDO no se lleva acá: se suma al contador de UpgradeManager, que ya
///     es el único que sabe consumirlo cuando el jugador choca. Así no se
///     duplica la lógica de la tienda.
///
///  4. FEEDBACK: partículas del color del pickup al recogerlo.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public class PickupManager : MonoBehaviour
{
    public static PickupManager Instance { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════
    //  INSPECTOR
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Catálogo (todos los pickups del juego)")]
    [SerializeField] private PickupData[] catalogo;

    [Header("Frecuencia por etapa")]
    [Tooltip("Pickups que se reparten en CADA segmento de pista, uno por etapa " +
             "(posición 0 = Entrenamiento, posición 5 = Final). Los valores bajos " +
             "son a propósito: un pickup cada dos o tres segmentos, no en todos.")]
    [SerializeField] private float[] pickupsPorNivel = { 0.30f, 0.35f, 0.40f, 0.45f, 0.50f, 0.55f };

    [Tooltip("Se usa cuando la etapa jugada queda fuera de la lista de arriba.")]
    [Range(0f, 4f)]
    [SerializeField] private float pickupsPorDefecto = 0.40f;

    [Header("Reparto")]
    [Tooltip("Altura sobre el piso a la que flota el pickup, en metros.")]
    [SerializeField] private float alturaPickup = 1.1f;

    [Tooltip("Padre de los pickups reciclados. Si queda vacío se crea solo.")]
    [SerializeField] private Transform contenedorPickups;

    [Tooltip("Distancia, en metros, que un pickup tiene que quedar detrás del " +
             "jugador para volver al pool.")]
    [SerializeField] private float distanciaReciclado = 14f;

    [Header("Feedback")]
    [Tooltip("Cantidad de partículas que salen al recoger un pickup (0 = ninguna).")]
    [Range(0, 60)]
    [SerializeField] private int particulasAlRecoger = 12;

    // ═════════════════════════════════════════════════════════════════════════
    //  ESTADO INTERNO
    // ═════════════════════════════════════════════════════════════════════════

    // Una pila de objetos reciclables por cada tipo de pickup.
    private readonly Dictionary<PickupData, Stack<PickupBase>> _pools =
        new Dictionary<PickupData, Stack<PickupBase>>();

    // Pickups que están ahora mismo en la pista.
    private readonly List<PickupBase> _activos = new List<PickupBase>();

    // Efectos temporales.
    private float _turboRestante;
    private float _turboDuracion;
    private float _turboFactor = 1f;

    private float _imanRestante;
    private float _imanDuracion;
    private float _imanRadio;

    private bool _estabaCorriendo;

    private ParticleSystem _particulas;

    // ═════════════════════════════════════════════════════════════════════════
    //  PROPIEDADES QUE USA EL RESTO DEL JUEGO
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Catálogo de pickups. El HUD lo usa para sacar los colores.</summary>
    public PickupData[] Catalogo => catalogo;

    /// <summary>Multiplicador de velocidad del turbo. 1 = sin turbo.</summary>
    public float MultiplicadorVelocidad => _turboRestante > 0f ? Mathf.Max(1f, _turboFactor) : 1f;

    /// <summary>True mientras dura el imán.</summary>
    public bool ImanActivo => _imanRestante > 0f;

    /// <summary>
    /// Radio de atracción de las latas, en metros. Suma la mejora "Imán" de la
    /// tienda en vez de reemplazarla.
    /// </summary>
    public float RadioImanActual
    {
        get
        {
            if (!ImanActivo) return 0f;

            float extra = UpgradeManager.Instance != null ? UpgradeManager.Instance.RadioImanExtra : 0f;
            return Mathf.Max(0f, _imanRadio + extra);
        }
    }

    /// <summary>Cuánto le queda al turbo, de 0 a 1 (para la barra del HUD).</summary>
    public float TurboRestante01 =>
        _turboDuracion > 0f ? Mathf.Clamp01(_turboRestante / _turboDuracion) : 0f;

    /// <summary>Cuánto le queda al imán, de 0 a 1 (para la barra del HUD).</summary>
    public float ImanRestante01 =>
        _imanDuracion > 0f ? Mathf.Clamp01(_imanRestante / _imanDuracion) : 0f;

    /// <summary>Escudos disponibles. Sale del contador de la tienda: no se duplica.</summary>
    public int EscudosActivos =>
        UpgradeManager.Instance != null ? UpgradeManager.Instance.EscudosRestantes : 0;

    // ═════════════════════════════════════════════════════════════════════════
    //  UNITY
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (contenedorPickups == null)
        {
            var go = new GameObject("Pickups_Pool");
            go.transform.SetParent(transform, false);
            contenedorPickups = go.transform;
        }
    }

    private void Update()
    {
        // Igual que los obstáculos: si no hay GameManager (escena de prueba) se
        // considera que la carrera está corriendo.
        var gm = RunnerGameManager.Instance;
        bool corriendo = gm == null || gm.State == GameState.Running;

        if (corriendo && !_estabaCorriendo) ReiniciarEfectos();
        _estabaCorriendo = corriendo;

        // Los efectos temporales sólo corren mientras el jugador corre.
        if (corriendo)
        {
            if (_turboRestante > 0f) _turboRestante = Mathf.Max(0f, _turboRestante - Time.deltaTime);
            if (_imanRestante  > 0f) _imanRestante  = Mathf.Max(0f, _imanRestante  - Time.deltaTime);
        }

        ReciclarLosQueQuedaronAtras();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API: REPARTO DE PICKUPS EN UN SEGMENTO
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Reparte los pickups de un segmento de pista. Lo llama ObstacleSpawner,
    /// que es el mismo que le pide los obstáculos al ObstacleManager.
    /// </summary>
    /// <param name="segmento">El segmento que se acaba de instanciar.</param>
    /// <param name="zInicioLocal">Z local donde empieza la zona de spawn.</param>
    /// <param name="zFinLocal">Z local donde termina.</param>
    /// <param name="zMinimaMundo">Z del mundo antes de la cual no se pone nada.</param>
    public void PoblarSegmento(Transform segmento, float zInicioLocal, float zFinLocal, float zMinimaMundo)
    {
        if (segmento == null) return;

        float rango = Mathf.Max(0f, zFinLocal - zInicioLocal);
        if (rango < 4f) return;

        // Cuántos pickups caen en este segmento según la etapa.
        float cantidad = PickupsDeLaEtapa();
        int cuantos = Mathf.FloorToInt(cantidad);
        if (UnityEngine.Random.value < cantidad - cuantos) cuantos++;
        if (cuantos <= 0) return;

        for (int i = 0; i < cuantos; i++)
        {
            // Z al azar dentro de la zona útil, evitando los bordes del segmento.
            float zLocal = zInicioLocal + rango * UnityEngine.Random.Range(0.15f, 0.90f);

            // Zona segura de arranque: los primeros metros quedan limpios.
            if (segmento.position.z + zLocal < zMinimaMundo) continue;

            PickupData dato = SortearPickup();
            if (dato == null || dato.prefab == null) continue;

            int carril = UnityEngine.Random.Range(0, LaneSystem.LaneCount);
            Colocar(dato, segmento, LaneSystem.GetLaneX(carril), zLocal);
        }
    }

    /// <summary>
    /// Pickups que corresponden a la etapa que se está jugando. Es el único
    /// número que hay que tocar para que salgan más o menos seguido.
    /// </summary>
    private float PickupsDeLaEtapa()
    {
        if (pickupsPorNivel == null || pickupsPorNivel.Length == 0) return pickupsPorDefecto;

        int indice = Mathf.Clamp(NivelDelJuego() - 1, 0, pickupsPorNivel.Length - 1);
        return Mathf.Max(0f, pickupsPorNivel[indice]);
    }

    /// <summary>Etapa que se está jugando (la misma que leen los obstáculos).</summary>
    private static int NivelDelJuego()
    {
        return SaveManager.Data != null ? Mathf.Max(1, SaveManager.Data.currentLevel) : 1;
    }

    /// <summary>Sortea qué pickup sale, filtrando por etapa y respetando los pesos.</summary>
    private PickupData SortearPickup()
    {
        if (catalogo == null || catalogo.Length == 0) return null;

        int nivel = NivelDelJuego();

        float total = 0f;
        foreach (var dato in catalogo)
        {
            if (dato == null || dato.prefab == null) continue;
            if (dato.nivelMinimo > nivel) continue;
            total += Mathf.Max(0f, dato.peso);
        }

        if (total <= 0f) return null;

        float sorteo = UnityEngine.Random.Range(0f, total);
        foreach (var dato in catalogo)
        {
            if (dato == null || dato.prefab == null) continue;
            if (dato.nivelMinimo > nivel) continue;

            sorteo -= Mathf.Max(0f, dato.peso);
            if (sorteo <= 0f) return dato;
        }

        return null;
    }

    /// <summary>Saca un pickup del pool y lo pone en la pista.</summary>
    private PickupBase Colocar(PickupData dato, Transform segmento, float x, float zLocal)
    {
        PickupBase pickup = ObtenerDelPool(dato);
        if (pickup == null) return null;

        // El prefab está armado para flotar a la altura del jugador: el
        // Inicializar le pasa la posición local de reposo.
        pickup.transform.SetParent(segmento, false);
        pickup.Inicializar(dato, this, new Vector3(x, alturaPickup, zLocal));

        _activos.Add(pickup);
        return pickup;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  API: RECOGER
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lo llama PickupBase cuando el jugador lo toca. Acá se aplica el efecto y
    /// el pickup vuelve al pool.
    /// </summary>
    public void Recoger(PickupBase pickup)
    {
        if (pickup == null) return;

        PickupData datos = pickup.Datos;
        if (datos == null) { Devolver(pickup); return; }

        AplicarEfecto(datos);

        EfectoDeRecogida(pickup.transform.position, datos.color);
        PickupHUD.InstanceOrCreate()?.Destello(datos.tipo);

        Devolver(pickup);
    }

    /// <summary>Aplica el efecto de cada tipo, reutilizando los sistemas que ya existen.</summary>
    private void AplicarEfecto(PickupData datos)
    {
        switch (datos.tipo)
        {
            // ── Bebida isotónica: recupera energía ────────────────────────────
            case TipoPickup.Energia:
                PlayerStats.Instance?.AddEnergy(datos.valor);

                // Refrescamos la barra del HUD en el acto.
                if (GameplayHUD.Instance != null && PlayerStats.Instance != null)
                    GameplayHUD.Instance.SetEnergy(PlayerStats.Instance.EnergyNormalized);
                break;

            // ── Botines turbo: más velocidad por unos segundos ───────────────
            case TipoPickup.Turbo:
                _turboDuracion = Mathf.Max(0.1f, datos.duracion);
                _turboRestante = _turboDuracion;
                _turboFactor   = Mathf.Max(1f, datos.valor);
                break;

            // ── Imán: atrae las latas por unos segundos ───────────────────────
            case TipoPickup.Iman:
                _imanDuracion = Mathf.Max(0.1f, datos.duracion);
                _imanRestante = _imanDuracion;
                _imanRadio    = Mathf.Max(1f, datos.valor);
                break;

            // ── Chaleco: suma escudos al contador de la tienda ───────────────
            case TipoPickup.Escudo:
                UpgradeManager.Instance?.AgregarEscudo(Mathf.Max(1, Mathf.RoundToInt(datos.valor)));
                break;
        }

        Debug.Log($"[Pickup] {datos.nombre} recogido ({datos.tipo}).");
    }

    /// <summary>Apaga los efectos temporales (se llama al empezar cada partida).</summary>
    public void ReiniciarEfectos()
    {
        _turboRestante = 0f;
        _turboDuracion = 0f;
        _turboFactor   = 1f;

        _imanRestante = 0f;
        _imanDuracion = 0f;
        _imanRadio    = 0f;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  POOL
    // ═════════════════════════════════════════════════════════════════════════

    private PickupBase ObtenerDelPool(PickupData dato)
    {
        if (dato == null) return null;

        if (!_pools.TryGetValue(dato, out Stack<PickupBase> pila))
        {
            pila = new Stack<PickupBase>();
            _pools[dato] = pila;
        }

        // Reutilizamos el primero que sirva.
        while (pila.Count > 0)
        {
            PickupBase guardado = pila.Pop();
            if (guardado == null) continue;
            guardado.gameObject.SetActive(true);
            return guardado;
        }

        // No había ninguno: recién acá se instancia.
        if (dato.prefab == null) return null;

        GameObject go = Instantiate(dato.prefab, contenedorPickups);
        PickupBase nuevo = go.GetComponent<PickupBase>();
        if (nuevo == null) nuevo = go.GetComponentInChildren<PickupBase>(true);

        if (nuevo == null)
        {
            Debug.LogError($"[PickupManager] El prefab '{dato.prefab.name}' no tiene un " +
                           $"componente PickupBase.");
            Destroy(go);
            return null;
        }

        go.SetActive(true);
        return nuevo;
    }

    /// <summary>Devuelve un pickup al pool (no lo destruye).</summary>
    private void Devolver(PickupBase pickup)
    {
        if (pickup == null) return;

        _activos.Remove(pickup);
        pickup.Desactivar();

        // Lo sacamos al contenedor del pool para que no quede colgado del
        // segmento de pista que se va a destruir.
        pickup.transform.SetParent(contenedorPickups, false);
        pickup.gameObject.SetActive(false);

        if (pickup.Datos != null)
        {
            if (!_pools.TryGetValue(pickup.Datos, out Stack<PickupBase> pila))
            {
                pila = new Stack<PickupBase>();
                _pools[pickup.Datos] = pila;
            }
            pila.Push(pickup);
        }
    }

    /// <summary>Devuelve al pool los pickups que ya quedaron detrás del jugador.</summary>
    private void ReciclarLosQueQuedaronAtras()
    {
        if (_activos.Count == 0) return;
        if (PlayerController.Instance == null) return;

        float zJugador = PlayerController.Instance.transform.position.z;

        for (int i = _activos.Count - 1; i >= 0; i--)
        {
            PickupBase pickup = _activos[i];

            if (pickup == null) { _activos.RemoveAt(i); continue; }

            if (pickup.transform.position.z < zJugador - distanciaReciclado)
                Devolver(pickup);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  FEEDBACK
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Un puñado de partículas del color del pickup al recogerlo.</summary>
    private void EfectoDeRecogida(Vector3 posicion, Color color)
    {
        if (particulasAlRecoger <= 0) return;

        if (_particulas == null) _particulas = CrearParticulas();
        if (_particulas == null) return;

        var main = _particulas.main;
        main.startColor = color;

        _particulas.transform.position = posicion;
        _particulas.Emit(particulasAlRecoger);
    }

    /// <summary>Crea (una sola vez) el sistema de partículas del pickup.</summary>
    private ParticleSystem CrearParticulas()
    {
        var go = new GameObject("Particulas_Pickups");
        go.transform.SetParent(transform, false);

        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.duration        = 0.5f;
        main.loop            = false;
        main.playOnAwake     = false;
        main.startLifetime   = 0.4f;
        main.startSpeed      = 3.2f;
        main.startSize       = 0.16f;
        main.startColor      = Color.white;
        main.gravityModifier = -0.3f;   // suben un poquito, se ven alegres
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles    = 120;

        // Sólo emitimos por código con Emit(): no queremos un chorro continuo.
        var emision = ps.emission;
        emision.enabled      = true;
        emision.rateOverTime = 0f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius    = 0.2f;

        // Material: el del pipeline URP si está, si no el de sprites.
        var render = go.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) render.material = new Material(shader);

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        return ps;
    }
}
