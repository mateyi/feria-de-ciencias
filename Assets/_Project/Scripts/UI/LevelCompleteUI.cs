using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de "Nivel Completado" que se muestra cuando el jugador convierte el penal.
///
/// Muestra:
///   - Puntaje final de la carrera.
///   - Monedas ganadas.
///   - Nivel desbloqueado.
///   - Botones: "Siguiente nivel" y "Menú principal".
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static LevelCompleteUI Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Textos")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text levelText;

    [Header("Botones")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button menuButton;

    // ─── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (nextLevelButton) nextLevelButton.onClick.AddListener(OnNextLevel);
        if (menuButton)      menuButton.onClick.AddListener(OnMenu);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>Rellena los datos y muestra el panel.</summary>
    public void Show(int score)
    {
        // Los textos no llevan emojis: la fuente LiberationSans SDF del proyecto
        // no tiene esos glifos y Unity los reemplaza por "□" (warning en consola).
        if (titleText)  titleText.text  = "¡Nivel Completado!";
        if (scoreText)  scoreText.text  = $"Puntaje: {score}";

        if (coinsText && GameData.Instance != null)
            coinsText.text = $"Monedas: {GameData.Instance.Coins}";

        if (levelText && GameData.Instance != null)
            levelText.text = $"Nivel {GameData.Instance.CurrentLevel} desbloqueado";
    }

    // ─── Botones ──────────────────────────────────────────────────────────────

    private void OnNextLevel()
    {
        // Reiniciar la partida con el nuevo nivel
        RunnerGameManager.Instance?.StartGame();
    }

    private void OnMenu()
    {
        UIManager.Instance?.ShowScreen(UIScreen.MainMenu);
    }
}
