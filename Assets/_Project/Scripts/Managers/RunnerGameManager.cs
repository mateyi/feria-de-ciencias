using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Gestor principal del juego.
/// Controla el estado global (GameState), el puntaje, la velocidad progresiva
/// y la comunicación entre sistemas.
///
/// ESTADOS:
///   MainMenu        → pantalla de inicio, nada corre.
///   Running         → el runner está activo: movimiento, obstáculos, energía.
///   PenaltyMinigame → el runner se detuvo; el jugador patea el penal.
///   LevelComplete   → el penal fue gol; se muestra la pantalla de victoria.
///   GameOver        → el jugador murió o falló todos los intentos.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public class RunnerGameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static RunnerGameManager Instance { get; private set; }

    // ─── Estado público ───────────────────────────────────────────────────────
    public GameState State { get; private set; } = GameState.MainMenu;

    /// <summary>Retrocompatibilidad: otros scripts que chequeaban IsPlaying siguen funcionando.</summary>
    public bool IsPlaying => State == GameState.Running;

    /// <summary>True mientras el jugador tiene el juego en pausa (Fase E).</summary>
    public bool Pausado { get; private set; }

    public int   Score           { get; private set; }
    public float SpeedMultiplier { get; private set; } = 1f;

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Configuración de puntaje y velocidad")]
    [SerializeField] private float scorePerSecond      = 10f;
    [SerializeField] private float speedIncreaseRate   = 0.05f;
    [SerializeField] private float maxSpeedMultiplier  = 3f;
    [SerializeField] private float energyDrainPerSecond = 2f;

    [Header("Flujo META → túnel → penal")]
    [Tooltip("Z de la boca del túnel donde termina la carrera y arranca el penal. " +
             "El túnel va de z=309 a z=327 y la arena empieza ahí mismo.")]
    [SerializeField] private float puertaPenalZ = 326f;

    [Tooltip("Segundos máximos de carrera después de la META antes de forzar el penal. " +
             "Red de seguridad: si el jugador se traba en el túnel, el penal arranca igual.")]
    [SerializeField] private float tiempoMaximoHastaPenal = 8f;

    [Header("Spawn del jugador")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Prefab del jugador")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Pausa (Fase E)")]
    [Tooltip("Panel que se muestra mientras el jugador mantiene la pausa (Canvas/PausePanel).")]
    [SerializeField] private GameObject pausePanel;

    // ─── Privados ────────────────────────────────────────────────────────────
    private float _scoreAccum;
    private PlayerController _player;

    // Control del tramo META → túnel → penal.
    private bool      _metaCruzada;      // ya pasó el arco de META
    private bool      _penalIniciado;    // ya se cedió el control al minijuego
    private Coroutine _esperaPenal;

    // Etapa del torneo que se está jugando en ESTA partida. Se guarda al arrancar
    // porque al terminarla hay que saber si la que se superó era la FINAL (ahí el
    // jugador sale campeón del mundo y el torneo vuelve a empezar).
    private int _nivelJugado = 1;

    // ─── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        UIManager.Instance?.ShowScreen(UIScreen.MainMenu);
        SuscribirEventosPenal();
    }

    private void OnDestroy()
    {
        DesuscribirEventosPenal();
    }

    // ─── Conexión con el minijuego de penales ─────────────────────────────────

    /// <summary>
    /// El PenaltyMinigameManager expone OnLevelCompleted / OnLevelFailed como
    /// eventos. Acá nos suscribimos para enterarnos del resultado del penal y
    /// cambiar el GameState correspondiente.
    /// </summary>
    private void SuscribirEventosPenal()
    {
        var penal = PenaltyMinigameManager.Instance;
        if (penal == null)
        {
            Debug.LogWarning("[RunnerGameManager] No encontré PenaltyMinigameManager para suscribirme a sus eventos.");
            return;
        }

        // Quitamos primero por las dudas de una suscripción duplicada.
        penal.OnLevelCompleted -= HandlePenalGanado;
        penal.OnLevelFailed    -= HandlePenalPerdido;

        penal.OnLevelCompleted += HandlePenalGanado;
        penal.OnLevelFailed    += HandlePenalPerdido;
    }

    private void DesuscribirEventosPenal()
    {
        var penal = PenaltyMinigameManager.Instance;
        if (penal == null) return;

        penal.OnLevelCompleted -= HandlePenalGanado;
        penal.OnLevelFailed    -= HandlePenalPerdido;
    }

    /// <summary>El jugador convirtió el penal: nivel superado.</summary>
    private void HandlePenalGanado()  => OnLevelComplete();

    /// <summary>El jugador agotó los intentos: game over.</summary>
    private void HandlePenalPerdido() => OnPenaltyFailed();

    private void Update()
    {
        // Fase E: con Escape se pausa y se reanuda. Va antes del corte por estado
        // porque en pausa el estado sigue siendo Running.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Pausado) ResumeGame();
            else         PauseGame();
        }

        // Solo acumular puntaje y drenar energía mientras el runner está activo.
        if (State != GameState.Running) return;

        // Tramo META → túnel → penal: cuando el jugador llega a la boca del túnel
        // se le cede el control al minijuego de penales.
        if (_metaCruzada && !_penalIniciado) ControlarLlegadaAlPenal();

        // Puntaje por tiempo
        _scoreAccum += scorePerSecond * SpeedMultiplier * Time.deltaTime;
        if (_scoreAccum >= 1f)
        {
            int add = Mathf.FloorToInt(_scoreAccum);
            Score      += add;
            _scoreAccum -= add;
            UpdateHUD();
        }

        // Velocidad progresiva
        SpeedMultiplier = Mathf.Min(
            SpeedMultiplier + speedIncreaseRate * Time.deltaTime,
            maxSpeedMultiplier);

        // Drenar energía (solo mientras se corre de verdad: en el túnel previo
        // al penal no, porque moriría justo antes de patear).
        if (!_metaCruzada && PlayerStats.Instance != null)
        {
            // La mejora "Resistencia" reduce el gasto por segundo.
            float gasto = energyDrainPerSecond *
                (UpgradeManager.Instance != null ? UpgradeManager.Instance.MultiplicadorGastoEnergia : 1f);

            PlayerStats.Instance.DrainEnergy(gasto * Time.deltaTime);
            UpdateEnergyBar();

            if (PlayerStats.Instance.energy <= 0f)
                OnPlayerDied();
        }
    }

    // ─── Flujo de juego ───────────────────────────────────────────────────────

    /// <summary>Inicia o reinicia una partida completa desde el menú.</summary>
    public void StartGame()
    {
        // Si veníamos de una pausa, el tiempo tiene que volver a correr.
        Pausado = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);

        Score           = 0;
        SpeedMultiplier = 1f;
        _scoreAccum     = 0f;

        // Qué etapa del torneo se juega. Se recorta al rango válido para que un
        // guardado viejo (o un nivel de prueba) no deje al jugador fuera del
        // torneo ni lo mande más allá de la final.
        LevelConfig config = ObstacleManager.Configuracion;
        int guardado = GameData.Instance != null ? GameData.Instance.CurrentLevel : 1;
        _nivelJugado = Mathf.Clamp(guardado, 1, config != null ? config.NivelFinal : 1);

        // Reiniciar el tramo META → túnel → penal.
        _metaCruzada   = false;
        _penalIniciado = false;
        if (_esperaPenal != null) { StopCoroutine(_esperaPenal); _esperaPenal = null; }

        if (PlayerStats.Instance != null) PlayerStats.Instance.ResetStats();

        // Recargar los escudos comprados en la tienda para esta partida.
        UpgradeManager.Instance?.ReiniciarRun();

        // Instanciar o reposicionar jugador
        if (_player == null && playerPrefab != null)
        {
            Vector3 spawnPos = playerSpawnPoint != null
                ? playerSpawnPoint.position
                : new Vector3(0, 0.5f, 0);
            GameObject go = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            _player = go.GetComponent<PlayerController>();
        }
        else if (_player != null)
        {
            Vector3 spawnPos = playerSpawnPoint != null
                ? playerSpawnPoint.position
                : new Vector3(0, 0.5f, 0);
            _player.Respawn(spawnPos);
        }

        // El jugador equipado suma sus bonus y su camiseta. Va despues de crear al
        // jugador, porque PlayerStats.Instance recien existe una vez instanciado.
        if (CharacterManager.Instance != null && _player != null)
        {
            CharacterManager.Instance.AplicarAlJugador();
            CharacterManager.Instance.AplicarModelo(_player.gameObject);
        }

        TrackGenerator.Instance?.ResetTrack();

        // Devolver la cámara a su pose de carrera (por si venimos de un penal).
        PenaltyMinigameManager.Instance?.RestaurarCamaraDeCarrera();

        SetState(GameState.Running);
        UIManager.Instance?.ShowScreen(UIScreen.Gameplay);
        UpdateHUD();

        // Nombre de la etapa arriba al centro del HUD.
        GameplayHUD.Instance?.SetEtapa(NombreEtapa(_nivelJugado));
    }

    /// <summary>
    /// Nombre deportivo de la etapa pedida (Entrenamiento, Octavos de final, ...).
    /// Si todavía no hay LevelConfig devuelve un texto de respaldo.
    /// </summary>
    private static string NombreEtapa(int nivel)
    {
        LevelConfig config = ObstacleManager.Configuracion;
        return config != null ? config.NombreDeEtapa(nivel) : "Nivel " + nivel;
    }

    // ─── Pausa (Fase E) ───────────────────────────────────────────────────────

    /// <summary>
    /// Pausa pedida por el jugador: congela el tiempo y muestra el panel.
    /// El estado del juego NO cambia (sigue en Running): así todo lo que ya
    /// chequea Running sigue funcionando y, como Time.timeScale vale 0, nada
    /// avanza igual.
    /// </summary>
    public void PauseGame()
    {
        // Solo pausar si estamos corriendo (no interrumpir el penal).
        if (State != GameState.Running || Pausado) return;

        Pausado = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);

        Debug.Log("[RunnerGameManager] Juego en pausa.");
    }

    /// <summary>Saca la pausa: devuelve el tiempo a la normalidad y esconde el panel.</summary>
    public void ResumeGame()
    {
        if (!Pausado) return;

        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        Pausado = false;

        Debug.Log("[RunnerGameManager] Juego reanudado.");
    }

    /// <summary>Botón "MENU" del panel de pausa: cierra la partida y vuelve al menú.</summary>
    public void VolverAlMenuDesdePausa()
    {
        ResumeGame();   // devuelve el tiempo antes de cambiar de pantalla

        SetState(GameState.MainMenu);

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetRunnerEnabled(false);

        UIManager.Instance?.ShowScreen(UIScreen.MainMenu);
    }

    /// <summary>
    /// Congela el tiempo sin mostrar el panel. Lo usan otros sistemas que
    /// necesitan frenar el mundo (por ejemplo las decisiones) y no quieren que
    /// aparezca la pantalla de pausa del jugador.
    /// </summary>
    public void CongelarTiempo() => Time.timeScale = 0f;

    /// <summary>Vuelve a la velocidad normal sin tocar el panel de pausa.</summary>
    public void ReanudarTiempo() => Time.timeScale = 1f;

    /// <summary>
    /// Llamado por FinishArchTrigger cuando el jugador cruza la META.
    ///
    /// FLUJO REAL (correr → META → túnel → penal): al cruzar la META el jugador
    /// NO se detiene. Sigue corriendo por el túnel y el penal arranca recién al
    /// llegar a la boca del túnel (ver ControlarLlegadaAlPenal).
    /// </summary>
    public void OnFinishReached()
    {
        if (State != GameState.Running) return;
        if (_metaCruzada) return;   // idempotente: el arco puede disparar varias veces

        _metaCruzada = true;

        // El jugador sigue corriendo: NO se toca SetRunnerEnabled ni el estado.
        // Sólo se arranca la red de seguridad por si se traba en el túnel.
        if (_esperaPenal != null) StopCoroutine(_esperaPenal);
        _esperaPenal = StartCoroutine(RedDeSeguridadPenal());
    }

    /// <summary>
    /// Arranca el minijuego de penales. Lo llama ControlarLlegadaAlPenal() al
    /// entrar al túnel, o la red de seguridad si el jugador se quedó trabado.
    /// </summary>
    public void OnPenaltyArenaReached()
    {
        if (State != GameState.Running) return;
        if (_penalIniciado) return;
        _penalIniciado = true;

        if (_esperaPenal != null) { StopCoroutine(_esperaPenal); _esperaPenal = null; }

        SetState(GameState.PenaltyMinigame);

        // Recién acá se frena al runner.
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetRunnerEnabled(false);

        PenaltyMinigameManager.Instance?.BeginPenaltySequence();
    }

    /// <summary>Detecta que el jugador llegó a la boca del túnel (z >= puertaPenalZ).</summary>
    private void ControlarLlegadaAlPenal()
    {
        if (PlayerController.Instance == null) return;
        if (PlayerController.Instance.transform.position.z >= puertaPenalZ)
            OnPenaltyArenaReached();
    }

    /// <summary>Si nunca llega a la boca del túnel, el penal arranca igual.</summary>
    private IEnumerator RedDeSeguridadPenal()
    {
        yield return new WaitForSeconds(tiempoMaximoHastaPenal);

        if (!_penalIniciado && State == GameState.Running)
        {
            Debug.LogWarning("[RunnerGameManager] El jugador no llegó a la boca del túnel; " +
                             "se fuerza el inicio del penal.");
            OnPenaltyArenaReached();
        }

        _esperaPenal = null;
    }

    /// <summary>
    /// Llamado por PenaltyMinigameManager cuando el jugador convierte el penal.
    ///
    /// Si la etapa que se acaba de ganar era la FINAL, el jugador sale campeón
    /// del mundo: se muestra la frase de campeón y el torneo vuelve a empezar por
    /// la primera etapa. Si no, simplemente avanza a la etapa siguiente.
    /// </summary>
    public void OnLevelComplete()
    {
        if (State != GameState.PenaltyMinigame) return;

        SetState(GameState.LevelComplete);
        GameData.Instance?.TrySetBestScore(Score);
        PagarPartida();

        LevelConfig config = ObstacleManager.Configuracion;
        bool eraFinal = config != null && config.EsFinal(_nivelJugado);

        if (eraFinal)
        {
            // Campeón del mundo: el torneo arranca de nuevo desde la primera etapa.
            if (GameData.Instance != null) GameData.Instance.SetCurrentLevel(1);
            StartCoroutine(ShowCampeonDelay());
        }
        else
        {
            if (GameData.Instance != null) GameData.Instance.SetCurrentLevel(_nivelJugado + 1);
            StartCoroutine(ShowLevelCompleteDelay());
        }
    }

    /// <summary>Llamado por PenaltyMinigameManager cuando se agotan los intentos.</summary>
    public void OnPenaltyFailed()
    {
        if (State == GameState.PenaltyMinigame)
            OnPlayerDied();
    }

    public void OnPlayerDied()
    {
        if (State == GameState.GameOver) return;

        SetState(GameState.GameOver);
        GameData.Instance?.TrySetBestScore(Score);
        PagarPartida();
        StartCoroutine(ShowGameOverDelay());
    }

    // ─── Pago por partida ────────────────────────────────────────────────────

    /// <summary>Monedas fijas que deja cualquier partida, aunque salga mal.</summary>
    private const int PagoBase = 1000;

    /// <summary>Monedas extra por cada punto de puntaje.</summary>
    private const int PagoPorPunto = 4;

    /// <summary>
    /// Monedas que deja la partida al cerrarse. Una carrera de unos 660 puntos
    /// paga unas 3.640 monedas, asi que con tres buenas carreras se llega al
    /// jugador mas caro (Messi, 10.000).
    /// </summary>
    private void PagarPartida()
    {
        if (GameData.Instance == null) return;

        int pago = PagoBase + Mathf.Max(0, Score) * PagoPorPunto;
        GameData.Instance.AddCoins(pago);
        UpdateHUD();

        Debug.Log($"[RunnerGameManager] Pago de la partida: {pago} monedas (puntaje {Score}).");
    }

    // ─── Helpers privados ─────────────────────────────────────────────────────

    private void SetState(GameState newState)
    {
        State = newState;
        Debug.Log($"[RunnerGameManager] Estado → {newState}");
    }

    private IEnumerator ShowGameOverDelay()
    {
        yield return new WaitForSeconds(1.2f);
        UIManager.Instance?.ShowScreen(UIScreen.GameOver);
        GameOverUI.Instance?.Show(Score);
    }

    /// <summary>Pantalla de victoria normal: se superó una etapa y sigue el torneo.</summary>
    private IEnumerator ShowLevelCompleteDelay()
    {
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance?.ShowScreen(UIScreen.LevelComplete);
        LevelCompleteUI.Instance?.Show(Score, _nivelJugado, false);
    }

    /// <summary>Pantalla de campeón: se ganó la FINAL del torneo.</summary>
    private IEnumerator ShowCampeonDelay()
    {
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance?.ShowScreen(UIScreen.LevelComplete);
        LevelCompleteUI.Instance?.Show(Score, _nivelJugado, true);
    }

    // ─── HUD ─────────────────────────────────────────────────────────────────

    private void UpdateHUD()
    {
        if (GameplayHUD.Instance == null) return;
        GameplayHUD.Instance.SetScore(Score);
        if (GameData.Instance != null)
            GameplayHUD.Instance.SetCoins(GameData.Instance.Coins);
    }

    private void UpdateEnergyBar()
    {
        if (GameplayHUD.Instance != null && PlayerStats.Instance != null)
            GameplayHUD.Instance.SetEnergy(PlayerStats.Instance.EnergyNormalized);
    }

    public void AddCoin(int cantidad = 1)
    {
        GameData.Instance?.AddCoins(cantidad);
        UpdateHUD();
    }

    public void UpdateProgressBar(float value01)
    {
        GameplayHUD.Instance?.SetProgress(value01);
    }
}

/// <summary>
/// Estados posibles del juego.
/// Definido aquí para que todos los scripts lo vean sin namespace extra.
/// </summary>
public enum GameState
{
    MainMenu,
    Running,
    PenaltyMinigame,
    LevelComplete,
    GameOver
}