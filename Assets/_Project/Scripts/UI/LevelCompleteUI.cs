using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de victoria que se muestra cuando el jugador convierte el penal.
///
/// Tiene DOS modos:
///
///   1. ETAPA SUPERADA (modo normal): se ganó una etapa del torneo y sigue la
///      próxima. Muestra "Ahora: Cuartos de final", el puntaje y las monedas.
///
///   2. CAMPEON DEL MUNDO: la etapa ganada era la FINAL. Muestra la frase
///      "SALISTE CAMPEON DEL MUNDO CON &lt;jugador&gt;. FELICIDADES." con el nombre
///      del jugador equipado, y el torneo vuelve a empezar por la primera etapa.
///
/// Los textos no llevan emojis: la fuente LiberationSans SDF del proyecto no
/// tiene esos glifos y Unity los reemplaza por un cuadrado.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static LevelCompleteUI Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Textos")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text levelText;

    [Header("Botones")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button menuButton;

    // ─── Privados ─────────────────────────────────────────────────────────────

    // Tamaños que vienen del Inspector. El modo campeón usa el título un poco
    // más chico porque "SALISTE CAMPEON DEL MUNDO" es más largo que
    // "ETAPA SUPERADA" y tiene que entrar en la caja de 600 de ancho.
    private float _tituloTamanoBase = 52f;
    private float _nivelTamanoBase  = 34f;

    // Etiqueta de texto del botón de seguir, para cambiar "SIGUIENTE" por
    // "JUGAR DE NUEVO" cuando el jugador sale campeón.
    private TMP_Text _etiquetaBotonSeguir;

    // ─── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (titleText) _tituloTamanoBase = titleText.fontSize;
        if (levelText) _nivelTamanoBase  = levelText.fontSize;

        // No hace falta arrastrar la etiqueta en el Inspector: es el texto que
        // está adentro del propio botón.
        if (nextLevelButton)
            _etiquetaBotonSeguir = nextLevelButton.GetComponentInChildren<TMP_Text>(true);

        if (nextLevelButton) nextLevelButton.onClick.AddListener(OnNextLevel);
        if (menuButton)      menuButton.onClick.AddListener(OnMenu);
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Rellena y muestra el panel.
    ///
    /// <paramref name="etapaCompletada"/> es el nivel del torneo que se acaba de
    /// superar y <paramref name="esCampeon"/> indica que esa etapa era la FINAL.
    /// </summary>
    public void Show(int score, int etapaCompletada, bool esCampeon)
    {
        LevelConfig config = ObstacleManager.Configuracion;

        if (esCampeon)
        {
            MostrarCampeon(score);
            return;
        }

        string siguiente = config != null ? config.EtapaSiguiente(etapaCompletada) : "la siguiente etapa";

        if (titleText)
        {
            titleText.fontSize = _tituloTamanoBase;
            titleText.text     = "ETAPA SUPERADA";
        }

        if (levelText)
        {
            levelText.fontSize = _nivelTamanoBase;
            levelText.text     = "Ahora: " + siguiente;
        }

        if (_etiquetaBotonSeguir) _etiquetaBotonSeguir.text = "SIGUIENTE";

        RellenarPuntajes(score);

        string superada = config != null ? config.NombreDeEtapa(etapaCompletada) : etapaCompletada.ToString();
        Debug.Log($"[LevelComplete] etapa superada: {superada} -> sigue: {siguiente}");
    }

    // ─── Modo campeón ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pantalla final del torneo. Arma la frase
    /// "SALISTE CAMPEON DEL MUNDO" + "CON &lt;jugador&gt;. FELICIDADES."
    /// en dos renglones para que se lea grande y no se salga del panel.
    /// </summary>
    private void MostrarCampeon(int score)
    {
        string jugador = NombreDelJugadorEquipado();

        if (titleText)
        {
            titleText.fontSize = _tituloTamanoBase * 0.72f;
            titleText.text     = "SALISTE CAMPEON DEL MUNDO";
        }

        if (levelText)
        {
            levelText.fontSize = _nivelTamanoBase;
            levelText.text     = string.IsNullOrEmpty(jugador)
                ? "FELICIDADES."
                : $"CON {jugador}. FELICIDADES.";
        }

        if (_etiquetaBotonSeguir) _etiquetaBotonSeguir.text = "JUGAR DE NUEVO";

        RellenarPuntajes(score);

        Debug.Log($"[LevelComplete] CAMPEON DEL MUNDO. Jugador: " +
                  (string.IsNullOrEmpty(jugador) ? "(generico)" : jugador));
    }

    /// <summary>
    /// Nombre del jugador equipado para la frase de campeón: usa el apodo si
    /// está cargado (es como se lo conoce en la cancha) y si no el nombre.
    /// Devuelve vacío cuando todavía se juega con el modelo genérico.
    /// </summary>
    private static string NombreDelJugadorEquipado()
    {
        CharacterManager cm = CharacterManager.Instance;
        if (cm == null || cm.Equipado == null) return "";

        string nombre = !string.IsNullOrEmpty(cm.Equipado.apodo)
            ? cm.Equipado.apodo
            : cm.Equipado.nombre;

        return string.IsNullOrEmpty(nombre) ? "" : nombre.ToUpperInvariant();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void RellenarPuntajes(int score)
    {
        if (scoreText) scoreText.text = $"Puntaje: {score}";

        if (coinsText && GameData.Instance != null)
            coinsText.text = $"Monedas: {GameData.Instance.Coins}";
    }

    // ─── Botones ──────────────────────────────────────────────────────────────

    private void OnNextLevel()
    {
        // Con el torneo avanzado (o reiniciado si salió campeón), arranca la
        // próxima etapa. StartGame lee el nivel guardado.
        RunnerGameManager.Instance?.StartGame();
    }

    private void OnMenu()
    {
        UIManager.Instance?.ShowScreen(UIScreen.MainMenu);
    }
}
