using UnityEngine;

/// <summary>
/// Datos persistentes del jugador: monedas, mejor puntaje y nivel actual.
///
/// FASE 16: el guardado pasó a SaveManager, que escribe un archivo JSON en
/// Application.persistentDataPath. Esta clase queda como fachada para que el
/// resto del juego (HUD, menú, tienda) siga usando GameData.Instance sin
/// enterarse del cambio.
///
/// Va en un GameObject vacío llamado "GameData".
/// </summary>
public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public int Coins        => SaveManager.Data.coins;
    public int BestScore    => SaveManager.Data.bestScore;
    public int CurrentLevel => SaveManager.Data.currentLevel;

    // La UI se suscribe a esto para refrescarse sola cuando cambian los datos
    public event System.Action OnDataChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // sobrevive al cambiar de escena

        SaveManager.Cargar();
    }

    public void AddCoins(int amount)
    {
        SaveManager.Data.coins += amount;
        SaveManager.Guardar();
        OnDataChanged?.Invoke();
    }

    public bool SpendCoins(int amount)
    {
        if (SaveManager.Data.coins < amount) return false;
        SaveManager.Data.coins -= amount;
        SaveManager.Guardar();
        OnDataChanged?.Invoke();
        return true;
    }

    public void TrySetBestScore(int score)
    {
        if (score <= SaveManager.Data.bestScore) return;
        SaveManager.Data.bestScore = score;
        SaveManager.Guardar();
        OnDataChanged?.Invoke();
    }

    public void SetCurrentLevel(int level)
    {
        SaveManager.Data.currentLevel = Mathf.Max(1, level);
        SaveManager.Guardar();
        OnDataChanged?.Invoke();
    }
}
