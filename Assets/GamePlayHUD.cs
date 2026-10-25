using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD durante la partida: monedas, puntaje, etapa del torneo, barra de progreso
/// y barra de energia.
/// Llama a estos metodos desde tu logica de juego cuando cambie cada valor.
/// Va en el GameplayPanel.
/// </summary>
public class GameplayHUD : MonoBehaviour
{
    public static GameplayHUD Instance { get; private set; }

    [Header("Textos")]
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text scoreText;

    [Tooltip("Nombre de la etapa que se esta jugando (Entrenamiento, Octavos de final, ...).")]
    [SerializeField] private TMP_Text etapaText;

    [Header("Barras (en el Slider: Min Value 0, Max Value 1)")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Slider energyBar;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetCoins(int coins)
    {
        if (coinsText) coinsText.text = coins.ToString();
    }

    public void SetScore(int score)
    {
        if (scoreText) scoreText.text = score.ToString();
    }

    // Muestra el nombre de la etapa del torneo arriba al centro del HUD.
    public void SetEtapa(string nombre)
    {
        if (etapaText) etapaText.text = nombre;
    }

    // value01 va de 0 a 1 (0% a 100%)
    public void SetProgress(float value01)
    {
        if (progressBar) progressBar.value = Mathf.Clamp01(value01);
    }

    public void SetEnergy(float value01)
    {
        if (energyBar) energyBar.value = Mathf.Clamp01(value01);
    }
}
