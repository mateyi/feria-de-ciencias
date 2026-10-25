using UnityEngine;

/// <summary>
/// Lee el nivel de cada mejora desde el guardado y expone el efecto real de
/// cada una como un valor listo para usar. Los sistemas de juego (velocidad,
/// energía, monedas, disparo) preguntan acá en lugar de saber de la tienda.
///
/// Va en el GameObject "GameData" de la escena.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Catálogo (mismo orden que la tienda)")]
    [SerializeField] private UpgradeData[] catalogo;

    public UpgradeData[] Catalogo => catalogo;

    // Escudos disponibles en la partida actual; se recargan en cada StartGame.
    private int _escudosRestantes;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ─── Niveles y compra ─────────────────────────────────────────────────────

    public int GetNivel(UpgradeId id) => SaveManager.GetNivel(id);

    public UpgradeData GetData(UpgradeId id)
    {
        if (catalogo == null) return null;
        foreach (var u in catalogo)
            if (u != null && u.id == id) return u;
        return null;
    }

    /// <summary>
    /// Intenta comprar el siguiente nivel de una mejora. Devuelve true si la
    /// compra se hizo (había monedas y no estaba al máximo).
    /// </summary>
    public bool Comprar(UpgradeData mejora)
    {
        if (mejora == null) return false;
        if (GameData.Instance == null) return false;

        int nivel = GetNivel(mejora.id);
        if (nivel >= mejora.nivelMaximo) return false;

        int costo = mejora.CostoParaNivel(nivel + 1);
        if (!GameData.Instance.SpendCoins(costo)) return false;

        SaveManager.SetNivel(mejora.id, nivel + 1);
        Debug.Log($"[Tienda] {mejora.nombre} → nivel {nivel + 1} (costó {costo} monedas)");
        return true;
    }

    // ─── Efectos reales de cada mejora ────────────────────────────────────────

    /// <summary>+6% de velocidad de carrera por nivel.</summary>
    public float BonusVelocidad => 0.06f * GetNivel(UpgradeId.Velocidad);

    /// <summary>Gasto de energía multiplicado por esto (−10% por nivel).</summary>
    public float MultiplicadorGastoEnergia =>
        Mathf.Clamp(1f - 0.10f * GetNivel(UpgradeId.Resistencia), 0.3f, 1f);

    /// <summary>+12% de velocidad al cambiar de carril por nivel.</summary>
    public float MultiplicadorCambioCarril => 1f + 0.12f * GetNivel(UpgradeId.Control);

    /// <summary>Potencia de disparo extra (se suma a skill/100 en el penal).</summary>
    public float BonusPotencia => 0.08f * GetNivel(UpgradeId.Potencia);

    /// <summary>Cuánto se le resta a la precisión del arquero (−6% por nivel).</summary>
    public float ReduccionPrecisionArquero => 0.06f * GetNivel(UpgradeId.Definicion);

    /// <summary>Monedas extra por cada lata recogida (1 extra cada 2 niveles).</summary>
    public int MonedasExtra => GetNivel(UpgradeId.Suerte) / 2;

    /// <summary>Radio extra del imán de monedas, en metros (+0,5 por nivel).</summary>
    public float RadioImanExtra => 0.5f * GetNivel(UpgradeId.Iman);

    // ─── Escudo ───────────────────────────────────────────────────────────────

    /// <summary>Recarga los escudos al empezar una partida.</summary>
    public void ReiniciarRun() => _escudosRestantes = GetNivel(UpgradeId.Escudo);

    public int EscudosRestantes => _escudosRestantes;

    /// <summary>Consume un escudo. Devuelve true si había uno disponible.</summary>
    public bool ConsumirEscudo()
    {
        if (_escudosRestantes <= 0) return false;
        _escudosRestantes--;
        Debug.Log($"[Escudo] Choque absorbido. Quedan {_escudosRestantes}.");
        return true;
    }

    /// <summary>
    /// Suma escudos durante la partida. Lo usa el pickup de chaleco (Fase C):
    /// se suma a este mismo contador para no tener dos lógicas de escudo.
    /// </summary>
    public void AgregarEscudo(int cantidad)
    {
        if (cantidad <= 0) return;

        _escudosRestantes += cantidad;
        Debug.Log($"[Escudo] +{cantidad} del pickup. Total {_escudosRestantes}.");
    }
}
