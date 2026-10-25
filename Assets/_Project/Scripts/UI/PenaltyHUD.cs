using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD del minijuego de penales.
///
/// Muestra tres cosas en pantalla:
///   1. Cuántos intentos le quedan al jugador ("Intentos: 3 / 3").
///   2. La instrucción de qué hacer ("¡Apuntá y hacé clic para patear!").
///   3. El resultado del tiro en grande ("¡GOOOL!" / "¡ATAJÓ!").
///
/// IMPORTANTE: los textos NO usan emojis. La fuente LiberationSans SDF del
/// proyecto no tiene esos glifos y Unity los reemplaza por "□", ensuciando la
/// consola con warnings. Por eso escribimos todo con caracteres latinos.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class PenaltyHUD : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static PenaltyHUD Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Panel raíz (se activa/desactiva)")]
    [Tooltip("GameObject que agrupa todo el HUD del penal.")]
    [SerializeField] private GameObject panelRoot;

    [Header("Textos")]
    [Tooltip("Texto con los intentos restantes.")]
    [SerializeField] private TMP_Text attemptsText;

    [Tooltip("Texto con la instrucción para el jugador.")]
    [SerializeField] private TMP_Text instructionText;

    [Tooltip("Texto grande con el resultado (GOL / ATAJADA).")]
    [SerializeField] private TMP_Text resultText;

    [Header("Colores del resultado")]
    [SerializeField] private Color goalColor = new Color(0.2f, 0.9f, 0.2f);
    [SerializeField] private Color saveColor = new Color(0.9f, 0.2f, 0.2f);

    // ─── Textos fijos (centralizados para que sean fáciles de cambiar) ────────
    private const string TextoInstruccion = "¡Apuntá y hacé clic para patear!";
    private const string TextoGol         = "¡GOOOOL!";
    private const string TextoAtajada     = "¡ATAJÓ!";

    /// <summary>
    /// Bandera de seguridad. El panel arranca DESACTIVADO en la escena, por lo que
    /// su Awake() todavía no corrió. Cuando el minijuego lo muestra por primera
    /// vez, SetActive(true) dispara Awake(), y si Awake() apagara el panel siempre
    /// lo dejaría invisible. Esta bandera le avisa a Awake() que el panel ya fue
    /// pedido y que NO tiene que ocultarlo.
    /// </summary>
    private bool _quiereEstarVisible;

    // ─── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Un solo HUD en la escena: si aparece un duplicado, se autodestruye.
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (resultText != null) resultText.gameObject.SetActive(false);

        // Ocultamos el panel SOLO si nadie pidió mostrarlo todavía.
        if (!_quiereEstarVisible && panelRoot != null)
            panelRoot.SetActive(false);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Muestra el HUD de penal con el número de intentos.
    /// Llamado por PenaltyMinigameManager al entrar al penal y en cada reintento.
    /// </summary>
    public void Show(int attemptsLeft, int maxAttempts)
    {
        // Marcamos la bandera ANTES de activar, para que Awake() (que se dispara
        // recién al activar el GameObject) no vuelva a ocultar el panel.
        _quiereEstarVisible = true;

        if (panelRoot != null) panelRoot.SetActive(true);

        UpdateAttempts(attemptsLeft, maxAttempts);

        if (instructionText != null)
            instructionText.text = TextoInstruccion;

        // El resultado arranca oculto hasta que haya un tiro.
        if (resultText != null) resultText.gameObject.SetActive(false);
    }

    /// <summary>Actualiza el contador de intentos ("Intentos: 2 / 3").</summary>
    public void UpdateAttempts(int attemptsLeft, int maxAttempts)
    {
        if (attemptsText != null)
            attemptsText.text = $"Intentos: {Mathf.Max(0, attemptsLeft)} / {maxAttempts}";
    }

    /// <summary>
    /// Muestra el resultado del tiro en grande y actualiza la instrucción.
    /// </summary>
    /// <param name="isGoal">true = gol, false = atajada.</param>
    /// <param name="attemptsLeft">Intentos restantes DESPUÉS del tiro.</param>
    public void ShowResult(bool isGoal, int attemptsLeft)
    {
        if (resultText == null) return;

        resultText.gameObject.SetActive(true);

        if (isGoal)
        {
            resultText.text  = TextoGol;
            resultText.color = goalColor;

            if (instructionText != null)
                instructionText.text = "¡Nivel completado!";
        }
        else
        {
            resultText.text  = TextoAtajada;
            resultText.color = saveColor;

            if (instructionText != null)
                instructionText.text = attemptsLeft > 0
                    ? $"Quedan {attemptsLeft} intento(s)"
                    : "Sin más intentos...";
        }
    }

    /// <summary>Oculta el HUD de penal por completo (fin del minijuego).</summary>
    public void Hide()
    {
        _quiereEstarVisible = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
