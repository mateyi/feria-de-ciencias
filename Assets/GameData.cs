using UnityEngine;

/// <summary>
/// Guarda los datos persistentes del jugador: monedas, mejor puntaje y nivel actual.
/// Usa PlayerPrefs para guardar entre sesiones y un evento para avisar a la UI cuando algo cambia.
/// Va en un GameObject vacio llamado "GameData".
/// </summary>
public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public int Coins { get; private set; }
    public int BestScore { get; private set; }
    public int CurrentLevel { get; private set; }

    // La UI se suscribe a esto para refrescarse sola cuando cambian los datos
    public event System.Action OnDataChanged;

    private const string KEY_COINS = "coins";
    private const string KEY_BEST = "bestScore";
    private const string KEY_LEVEL = "currentLevel";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // sobrevive al cambiar de escena
        Load();
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        Save();
        OnDataChanged?.Invoke();
    }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        Save();
        OnDataChanged?.Invoke();
        return true;
    }

    public void TrySetBestScore(int score)
    {
        if (score <= BestScore) return;
        BestScore = score;
        Save();
        OnDataChanged?.Invoke();
    }

    public void SetCurrentLevel(int level)
    {
        CurrentLevel = level;
        Save();
        OnDataChanged?.Invoke();
    }

    private void Load()
    {
        Coins = PlayerPrefs.GetInt(KEY_COINS, 0);
        BestScore = PlayerPrefs.GetInt(KEY_BEST, 0);
        CurrentLevel = PlayerPrefs.GetInt(KEY_LEVEL, 1);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_COINS, Coins);
        PlayerPrefs.SetInt(KEY_BEST, BestScore);
        PlayerPrefs.SetInt(KEY_LEVEL, CurrentLevel);
        PlayerPrefs.Save();
    }
}