using UnityEngine;
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

    // ─── Privados ────────────────────────────────────────────────────────────
    private float _scoreAccum;
    private PlayerController _player;

    // Control del tramo META → túnel → penal.
    private bool      _metaCruzada;      // ya pasó el arco de META
    private bool      _penalIniciado;    // ya se cedió el control al minijuego
    private Coroutine _esperaPenal;

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
        Score           = 0;
        SpeedMultiplier = 1f;
        _scoreAccum     = 0f;

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
    }

    public void PauseGame()
    {
        // Solo pausar si estamos corriendo (no interrumpir el penal)
        if (State == GameState.Running)
            Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (State == GameState.Running)
            Time.timeScale = 1f;
    }

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

    /// <summary>Llamado por PenaltyMinigameManager cuando el jugador convierte el penal.</summary>
    public void OnLevelComplete()
    {
        if (State == GameState.PenaltyMinigame)
        {
            SetState(GameState.LevelComplete);
            GameData.Instance?.TrySetBestScore(Score);
            PagarPartida();
            // Avanzar nivel
            if (GameData.Instance != null)
                GameData.Instance.SetCurrentLevel(GameData.Instance.CurrentLevel + 1);
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

    private IEnumerator ShowLevelCompleteDelay()
    {
        yield return new WaitForSeconds(1.5f);
        UIManager.Instance?.ShowScreen(UIScreen.LevelComplete);
        LevelCompleteUI.Instance?.Show(Score);
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