using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de Game Over. Muestra el puntaje final, el mejor puntaje
/// y botones para reintentar o volver al menú.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("Textos")]
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text statsText;

    [Header("Botones")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (retryButton) retryButton.onClick.AddListener(OnRetry);
        if (menuButton)  menuButton.onClick.AddListener(OnMenu);
    }

    public void Show(int score)
    {
        if (finalScoreText) finalScoreText.text = $"Puntaje: {score}";
        if (bestScoreText && GameData.Instance != null)
            bestScoreText.text = $"Mejor: {GameData.Instance.BestScore}";

        if (statsText && PlayerStats.Instance != null)
        {
            var s = PlayerStats.Instance;
            statsText.text = $"⚡ Velocidad: {s.speed:F0}\n" +
                             $"💪 Energía: {s.energy:F0}\n" +
                             $"⚽ Habilidad: {s.skill:F0}\n" +
                             $"🌟 Popularidad: {s.popularity:F0}";
        }
    }

    private void OnRetry()
    {
        RunnerGameManager.Instance?.StartGame();
    }

    private void OnMenu()
    {
        UIManager.Instance?.ShowScreen(UIScreen.MainMenu);
    }
}
