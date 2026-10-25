using UnityEngine;

/// <summary>
/// Datos persistentes del jugador: monedas, mejor puntaje, nivel actual y los
/// ajustes de sonido y brillo (Fase F).
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

    // Ajustes de sonido y brillo (Fase F).
    public float Volumen    => SaveManager.Data.volumen;
    public float Brillo     => SaveManager.Data.brillo;
    public bool  Silenciado => SaveManager.Data.silenciado;

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

    // ─── Ajustes de sonido y brillo (Fase F) ─────────────────────────────────
    // Los cambios se aplican al instante en AudioListener.volume y en el velo
    // del brillo; acá sólo se dejan anotados. El disco se escribe después, con
    // GuardarAjustes(), para no guardar en cada frame mientras se arrastra un
    // slider.

    public void SetVolumen(float valor)
    {
        SaveManager.Data.volumen = Mathf.Clamp01(valor);
        SaveManager.Data.ajustesGuardados = true;
    }

    public void SetBrillo(float valor)
    {
        SaveManager.Data.brillo = Mathf.Clamp01(valor);
        SaveManager.Data.ajustesGuardados = true;
    }

    public void SetSilenciado(bool valor)
    {
        SaveManager.Data.silenciado = valor;
        SaveManager.Data.ajustesGuardados = true;
    }

    /// <summary>Escribe los ajustes en el archivo de guardado.</summary>
    public void GuardarAjustes() => SaveManager.Guardar();
}
