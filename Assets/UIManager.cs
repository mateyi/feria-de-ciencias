using UnityEngine;

public enum UIScreen
{
    MainMenu,
    Gameplay,
    Shop,
    Characters,
    Settings,
    GameOver
}

/// <summary>
/// Controla que pantalla (panel) esta visible. Prende una y apaga el resto.
/// Los botones llaman a los metodos Open... desde el evento OnClick del Inspector.
/// Va en el Canvas.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles (arrastrar desde la jerarquia)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject charactersPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ShowScreen(UIScreen.MainMenu);
    }

    public void ShowScreen(UIScreen screen)
    {
        if (mainMenuPanel) mainMenuPanel.SetActive(screen == UIScreen.MainMenu);
        if (gameplayPanel) gameplayPanel.SetActive(screen == UIScreen.Gameplay);
        if (shopPanel) shopPanel.SetActive(screen == UIScreen.Shop);
        if (charactersPanel) charactersPanel.SetActive(screen == UIScreen.Characters);
        if (settingsPanel) settingsPanel.SetActive(screen == UIScreen.Settings);
        if (gameOverPanel) gameOverPanel.SetActive(screen == UIScreen.GameOver);
    }

    // Estos metodos se asignan en el OnClick de cada boton
    public void OpenMainMenu() => ShowScreen(UIScreen.MainMenu);
    public void OpenGameplay() => ShowScreen(UIScreen.Gameplay);
    public void OpenShop() => ShowScreen(UIScreen.Shop);
    public void OpenCharacters() => ShowScreen(UIScreen.Characters);
    public void OpenSettings() => ShowScreen(UIScreen.Settings);
    public void OpenGameOver() => ShowScreen(UIScreen.GameOver);
}