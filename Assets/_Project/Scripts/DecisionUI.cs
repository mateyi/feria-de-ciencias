using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de UI para las decisiones. Muestra la pregunta, dos opciones con colores,
/// un temporizador y el feedback de la consecuencia elegida.
/// </summary>
public class DecisionUI : MonoBehaviour
{
    [Header("Panel principal")]
    [SerializeField] private GameObject decisionPanel;

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

    [Header("Feedback")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text   feedbackTitleText;
    [SerializeField] private TMP_Text   feedbackDescText;
    [SerializeField] private TMP_Text   feedbackStatsText;

    private void Awake()
    {
        if (leftButton)  leftButton.onClick.AddListener(() => DecisionManager.Instance?.Choose(true));
        if (rightButton) rightButton.onClick.AddListener(() => DecisionManager.Instance?.Choose(false));
        Hide();
        HideFeedback();
    }

    public void Show(DecisionData data)
    {
        if (decisionPanel) decisionPanel.SetActive(true);

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
        if (decisionPanel) decisionPanel.SetActive(false);
    }

    public void UpdateTimer(float normalized)
    {
        if (timerSlider) timerSlider.value = Mathf.Clamp01(normalized);
    }

    public void ShowFeedback(string title, string desc, StatConsequence c)
    {
        if (feedbackPanel) feedbackPanel.SetActive(true);
        if (feedbackTitleText) feedbackTitleText.text = title;
        if (feedbackDescText)  feedbackDescText.text  = desc;
        if (feedbackStatsText)
        {
            string stats = "";
            if (c.speedDelta      != 0) stats += $"Velocidad {(c.speedDelta > 0 ? "+" : "")}{c.speedDelta}\n";
            if (c.energyDelta     != 0) stats += $"Energía {(c.energyDelta > 0 ? "+" : "")}{c.energyDelta}\n";
            if (c.skillDelta      != 0) stats += $"Habilidad {(c.skillDelta > 0 ? "+" : "")}{c.skillDelta}\n";
            if (c.popularityDelta != 0) stats += $"Popularidad {(c.popularityDelta > 0 ? "+" : "")}{c.popularityDelta}";
            feedbackStatsText.text = stats;
        }
    }

    public void HideFeedback()
    {
        if (feedbackPanel) feedbackPanel.SetActive(false);
    }
}
