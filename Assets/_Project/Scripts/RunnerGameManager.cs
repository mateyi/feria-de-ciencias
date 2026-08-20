using UnityEngine;
using System.Collections;

/// <summary>
/// Gestor principal del juego. Controla el estado (menu, jugando, game over),
/// el puntaje, la velocidad progresiva y la comunicación entre sistemas.
/// </summary>
public class RunnerGameManager : MonoBehaviour
{
    public static RunnerGameManager Instance { get; private set; }

    [Header("Configuración")]
    [SerializeField] private float scorePerSecond      = 10f;
    [SerializeField] private float speedIncreaseRate   = 0.05f;   // por segundo
    [SerializeField] private float maxSpeedMultiplier  = 3f;
    [SerializeField] private float energyDrainPerSecond = 2f;

    [Header("Spawn del jugador")]
    [SerializeField] private Transform playerSpawnPoint;

    [Header("Prefab del jugador")]
    [SerializeField] private GameObject playerPrefab;

    // Estado
    public bool IsPlaying { get; private set; }
    public int  Score     { get; private set; }
    public float SpeedMultiplier { get; private set; } = 1f;

    private float _scoreAccum;
    private PlayerController _player;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Asegura que empiece en el menú principal
        UIManager.Instance?.ShowScreen(UIScreen.MainMenu);
    }

    private void Update()
    {
        if (!IsPlaying) return;

        // Puntaje
        _scoreAccum += scorePerSecond * SpeedMultiplier * Time.deltaTime;
        if (_scoreAccum >= 1f)
        {
            int add = Mathf.FloorToInt(_scoreAccum);
            Score += add;
            _scoreAccum -= add;
            UpdateHUD();
        }

        // Velocidad progresiva
        SpeedMultiplier = Mathf.Min(SpeedMultiplier + speedIncreaseRate * Time.deltaTime, maxSpeedMultiplier);

        // Drenar energía
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.DrainEnergy(energyDrainPerSecond * Time.deltaTime);
            UpdateEnergyBar();

            if (PlayerStats.Instance.energy <= 0f)
                OnPlayerDied();
        }
    }

    // ─── Flujo de juego ───────────────────────────────────────────────────────

    public void StartGame()
    {
        Score           = 0;
        SpeedMultiplier = 1f;
        _scoreAccum     = 0f;

        // Resetear stats
        if (PlayerStats.Instance != null) PlayerStats.Instance.ResetStats();

        // Instanciar o reposicionar jugador
        if (_player == null && playerPrefab != null)
        {
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : new Vector3(0, 0.5f, 0);
            GameObject go = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            _player = go.GetComponent<PlayerController>();
        }
        else if (_player != null)
        {
            Vector3 spawnPos = playerSpawnPoint != null ? playerSpawnPoint.position : new Vector3(0, 0.5f, 0);
            _player.Respawn(spawnPos);
        }

        // Reiniciar generador de pista
        TrackGenerator.Instance?.ResetTrack();

        IsPlaying = true;
        UIManager.Instance?.ShowScreen(UIScreen.Gameplay);
        UpdateHUD();
    }

    public void PauseGame()
    {
        IsPlaying = false;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        IsPlaying = true;
        Time.timeScale = 1f;
    }

    public void OnPlayerDied()
    {
        if (!IsPlaying) return;
        IsPlaying = false;

        // Guardar mejor puntaje
        if (GameData.Instance != null)
        {
            GameData.Instance.TrySetBestScore(Score);
        }

        StartCoroutine(ShowGameOverDelay());
    }

    private IEnumerator ShowGameOverDelay()
    {
        yield return new WaitForSeconds(1.2f);
        UIManager.Instance?.ShowScreen(UIScreen.GameOver);
        GameOverUI.Instance?.Show(Score);
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

    public void AddCoin()
    {
        GameData.Instance?.AddCoins(1);
        UpdateHUD();
    }

    public void UpdateProgressBar(float value01)
    {
        GameplayHUD.Instance?.SetProgress(value01);
    }
}
