using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Panel de UI para las decisiones.
///
/// DOS MOMENTOS:
///  1. La decisión: pregunta, dos opciones con su color, temporizador.
///  2. El resultado: un cartel minimalista que aparece después de elegir y
///     muestra SIEMPRE tres atributos, todos con signo + si la opción era
///     buena, o todos con signo - si era mala (Velocidad, Energía, Habilidad,
///     Popularidad).
///
/// Estructura en la escena (Canvas/DecisionPanel):
///   DecisionPanel  (velo oscuro a pantalla completa, este script va acá)
///     CenterPanel  (la pregunta y las dos opciones)
///     FeedbackPanel (el cartel minimalista del resultado)
///
/// Carpeta: Assets/_Project/Scripts/
///
/// DESACTIVADO: reemplazado por el sistema de obstáculos.
/// El panel se mantiene armado en la escena pero nadie lo abre: DecisionManager
/// está apagado y no quedan DecisionPoint en la pista.
/// </summary>
public class DecisionUI : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject decisionPanel;
    [Tooltip("Contenedor de la pregunta y las dos opciones.")]
    [SerializeField] private GameObject centerPanel;
    [Tooltip("Velo oscuro de fondo. Se aclara cuando se muestra el resultado.")]
    [SerializeField] private Image      dimOverlay;

    [Header("Pregunta")]
    [SerializeField] private TMP_Text questionText;

    [Header("Opción Izquierda")]
    [SerializeField] private GameObject leftOptionPanel;
    [SerializeField] private TMP_Text   leftLabelText;
    [SerializeField] private TMP_Text   leftDescText;
    [SerializeField] private Image      leftBg;
    [SerializeField] private Button     leftButton;

    [Header("Opción Derecha")]
    [SerializeField] private GameObject rightOptionPanel;
    [SerializeField] private TMP_Text   rightLabelText;
    [SerializeField] private TMP_Text   rightDescText;
    [SerializeField] private Image      rightBg;
    [SerializeField] private Button     rightButton;

    [Header("Temporizador")]
    [SerializeField] private Slider timerSlider;

    [Header("Cartel de resultado (minimalista)")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text   feedbackTitleText;
    [SerializeField] private TMP_Text   feedbackDescText;
    [SerializeField] private TMP_Text   feedbackStatsText;
    [Tooltip("Palabra grande de veredicto: VENTAJA / DESVENTAJA.")]
    [SerializeField] private TMP_Text   feedbackVerdictText;
    [Tooltip("Franja de color arriba del cartel.")]
    [SerializeField] private Image      feedbackAccent;

    // ─── Paleta ───────────────────────────────────────────────────────────────

    private static readonly Color VerdeUi = new Color(0.30f, 0.83f, 0.48f, 1f);
    private static readonly Color RojoUi  = new Color(1.00f, 0.42f, 0.37f, 1f);

    private const string VerdeHex = "#4CD37A";
    private const string RojoHex  = "#FF6A5E";

    private const float VeloDecision  = 0.70f;
    private const float VeloResultado = 0.20f;

    /// <summary>Cantidad de atributos que muestra el cartel.</summary>
    private const int AtributosVisibles = 3;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (leftButton)  leftButton.onClick.AddListener(() => DecisionManager.Instance?.Choose(true));
        if (rightButton) rightButton.onClick.AddListener(() => DecisionManager.Instance?.Choose(false));

        if (dimOverlay == null) dimOverlay = GetComponent<Image>();

        Hide();
        HideFeedback();
    }

    // ─── Decisión ─────────────────────────────────────────────────────────────

    public void Show(DecisionData data)
    {
        if (decisionPanel) decisionPanel.SetActive(true);
        if (centerPanel)   centerPanel.SetActive(true);
        if (feedbackPanel) feedbackPanel.SetActive(false);
        PonerVelo(VeloDecision);

        if (questionText)   questionText.text   = data.question;
        if (leftLabelText)  leftLabelText.text  = data.leftLabel;
        if (leftDescText)   leftDescText.text   = data.leftDescription;
        if (rightLabelText) rightLabelText.text = data.rightLabel;
        if (rightDescText)  rightDescText.text  = data.rightDescription;

        if (leftBg)  leftBg.color  = data.leftColor;
        if (rightBg) rightBg.color = data.rightColor;

        if (timerSlider) timerSlider.value = 1f;
    }

    public void Hide()
    {
        if (centerPanel)   centerPanel.SetActive(false);
        if (feedbackPanel) feedbackPanel.SetActive(false);
        if (decisionPanel) decisionPanel.SetActive(false);
    }

    public void UpdateTimer(float normalized)
    {
        if (timerSlider) timerSlider.value = Mathf.Clamp01(normalized);
    }

    // ─── Cartel de resultado ──────────────────────────────────────────────────

    /// <summary>
    /// Muestra el cartel minimalista con los tres atributos que movió la
    /// opción elegida. <paramref name="positivo"/> decide el color del marco y
    /// la palabra de veredicto.
    /// </summary>
    public void ShowFeedback(string title, string desc, StatConsequence c, bool positivo)
    {
        // El cartel vive adentro de DecisionPanel: hay que mantener el panel
        // encendido o el resultado no se ve nunca.
        if (decisionPanel) decisionPanel.SetActive(true);
        if (centerPanel)   centerPanel.SetActive(false);
        if (feedbackPanel) feedbackPanel.SetActive(true);
        PonerVelo(VeloResultado);

        Color acento = positivo ? VerdeUi : RojoUi;

        if (feedbackTitleText)   feedbackTitleText.text = title;
        if (feedbackDescText)    feedbackDescText.text  = desc;

        if (feedbackVerdictText)
        {
            feedbackVerdictText.text  = positivo ? "VENTAJA" : "DESVENTAJA";
            feedbackVerdictText.color = acento;
        }

        if (feedbackAccent) feedbackAccent.color = acento;

        if (feedbackStatsText) feedbackStatsText.text = ConstruirAtributos(c);
    }

    public void HideFeedback()
    {
        if (feedbackPanel) feedbackPanel.SetActive(false);
        if (decisionPanel) decisionPanel.SetActive(false);
    }

    /// <summary>
    /// Arma el texto del cartel: los tres atributos que más cambiaron, todos
    /// con su signo y su color. Un atributo en cero no se muestra.
    /// </summary>
    private static string ConstruirAtributos(StatConsequence c)
    {
        var filas = new List<(string nombre, float valor)>();

        void Agregar(string nombre, float valor)
        {
            if (Mathf.Approximately(valor, 0f)) return;
            filas.Add((nombre, valor));
        }

        Agregar("Velocidad",   c.speedDelta);
        Agregar("Energía",     c.energyDelta);
        Agregar("Habilidad",   c.skillDelta);
        Agregar("Popularidad", c.popularityDelta);

        // Orden por impacto: las que más mueven la balanza van arriba.
        filas.Sort((a, b) => Mathf.Abs(b.valor).CompareTo(Mathf.Abs(a.valor)));

        var sb = new StringBuilder();
        for (int i = 0; i < filas.Count && i < AtributosVisibles; i++)
        {
            var f = filas[i];
            string color = f.valor > 0f ? VerdeHex : RojoHex;
            string signo = f.valor > 0f ? "+" : "-";
            sb.Append($"<color={color}><b>{signo}{Mathf.Abs(f.valor):0}</b>   {f.nombre}</color>");
            if (i < Mathf.Min(filas.Count, AtributosVisibles) - 1) sb.Append('\n');
        }

        return sb.ToString();
    }

    private void PonerVelo(float alpha)
    {
        if (dimOverlay == null) return;
        var c = dimOverlay.color;
        c.a = alpha;
        dimOverlay.color = c;
    }
}
