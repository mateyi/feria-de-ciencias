using UnityEngine;

public enum UIScreen
{
    MainMenu,
    Gameplay,
    Shop,
    Characters,
    Settings,
    GameOver,
    LevelComplete   // Pantalla de victoria tras convertir el penal
}

/// <summary>
/// Controla qué pantalla (panel) está visible. Prende una y apaga el resto.
/// Los botones llaman a los métodos Open... desde el evento OnClick del Inspector.
/// Va en el Canvas.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Paneles (arrastrar desde la jerarquía)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject charactersPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject levelCompletePanel;  // Nuevo panel de victoria

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
        if (mainMenuPanel)      mainMenuPanel.SetActive(screen == UIScreen.MainMenu);
        if (gameplayPanel)      gameplayPanel.SetActive(screen == UIScreen.Gameplay);
        if (shopPanel)          shopPanel.SetActive(screen == UIScreen.Shop);
        if (charactersPanel)    charactersPanel.SetActive(screen == UIScreen.Characters);
        if (settingsPanel)      settingsPanel.SetActive(screen == UIScreen.Settings);
        if (gameOverPanel)      gameOverPanel.SetActive(screen == UIScreen.GameOver);
        if (levelCompletePanel) levelCompletePanel.SetActive(screen == UIScreen.LevelComplete);
    }

    // Estos métodos se asignan en el OnClick de cada botón
    public void OpenMainMenu()     => ShowScreen(UIScreen.MainMenu);
    public void OpenGameplay()     => ShowScreen(UIScreen.Gameplay);
    public void OpenShop()         => ShowScreen(UIScreen.Shop);
    public void OpenCharacters()   => ShowScreen(UIScreen.Characters);
    public void OpenSettings()     => ShowScreen(UIScreen.Settings);
    public void OpenGameOver()     => ShowScreen(UIScreen.GameOver);
    public void OpenLevelComplete()=> ShowScreen(UIScreen.LevelComplete);
}
