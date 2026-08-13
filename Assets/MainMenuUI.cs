using UnityEngine;
using TMPro;

/// <summary>
/// Muestra monedas, mejor puntaje y nivel en el menu principal.
/// Se refresca solo cuando GameData avisa que algo cambio.
/// Va en el MainMenuPanel.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text levelText;

    private void Start()
    {
        if (GameData.Instance != null)
        {
            GameData.Instance.OnDataChanged += Refresh;
            Refresh();
        }
    }

    private void OnEnable()
    {
        // al volver al menu desde otra pantalla, actualiza los numeros
        if (GameData.Instance != null) Refresh();
    }

    private void OnDestroy()
    {
        if (GameData.Instance != null)
            GameData.Instance.OnDataChanged -= Refresh;
    }

    private void Refresh()
    {
        if (coinsText) coinsText.text = GameData.Instance.Coins.ToString();
        if (bestScoreText) bestScoreText.text = "Mejor: " + GameData.Instance.BestScore;
        if (levelText) levelText.text = "Nivel " + GameData.Instance.CurrentLevel;
    }
}