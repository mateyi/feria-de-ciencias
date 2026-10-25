using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de ajustes (Fase F): volumen general, silencio y brillo de pantalla.
///
/// Cómo funciona cada control:
///   - VOLUMEN se aplica sobre AudioListener.volume, que afecta a todos los
///     sonidos del juego sin tener que tocar cada AudioSource.
///   - SILENCIO es un interruptor aparte: guarda el volumen elegido y lo aplica
///     como 0. Al volver a prenderlo reaparece el mismo volumen, no un valor
///     nuevo.
///   - BRILLO no se puede cambiar desde un script en un juego normal, así que se
///     simula con un velo negro (Canvas/BrightnessOverlay) dibujado encima de la
///     escena y debajo de los paneles: cuanto más bajo el brillo, más opaco.
///
/// Los tres valores se guardan en el archivo de guardado al salir del panel
/// (botón VOLVER) y al cerrar el juego, no en cada frame: así arrastrar un
/// slider mientras se juega no escribe el disco sesenta veces por segundo.
///
/// Va en el GameObject Canvas (siempre activo), para que el volumen y el brillo
/// guardados se apliquen apenas arranca la partida, aunque el panel esté cerrado.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Sonido")]
    [SerializeField] private Slider volumenSlider;
    [SerializeField] private Button botonSilencio;
    [SerializeField] private TMP_Text etiquetaSilencio;

    [Header("Brillo")]
    [SerializeField] private Slider brilloSlider;
    [Tooltip("Velo negro que se dibuja encima de la escena para bajar el brillo.")]
    [SerializeField] private Image veloBrillo;

    [Header("Navegación")]
    [SerializeField] private Button botonVolver;

    [Tooltip("Cuánto oscurece el velo cuando el brillo está al mínimo.")]
    [SerializeField] private float oscurecimientoMaximo = 0.8f;

    private void Awake()
    {
        if (volumenSlider)  volumenSlider.onValueChanged.AddListener(AlCambiarVolumen);
        if (brilloSlider)   brilloSlider.onValueChanged.AddListener(AlCambiarBrillo);
        if (botonSilencio)  botonSilencio.onClick.AddListener(AlternarSilencio);
        if (botonVolver)    botonVolver.onClick.AddListener(VolverAlMenu);
    }

    private void Start()
    {
        // Los ajustes guardados se aplican al arrancar, sin abrir el panel.
        CargarAjustes();
    }

    private void OnApplicationQuit()
    {
        GuardarAjustes();
    }

    // ─── Cargar y guardar ─────────────────────────────────────────────────────

    /// <summary>Trae los valores guardados y los aplica a la pantalla y al sonido.</summary>
    public void CargarAjustes()
    {
        float volumen  = GameData.Instance != null ? GameData.Instance.Volumen : SaveManager.VolumenPorDefecto;
        float brillo   = GameData.Instance != null ? GameData.Instance.Brillo  : SaveManager.BrilloPorDefecto;
        bool  silencio = GameData.Instance != null && GameData.Instance.Silenciado;

        // SetValueWithoutNotify mueve la perilla sin disparar onValueChanged,
        // así no se reescribe el guardado mientras se carga.
        if (volumenSlider) volumenSlider.SetValueWithoutNotify(volumen);
        if (brilloSlider)  brilloSlider.SetValueWithoutNotify(brillo);

        AplicarVolumen(volumen, silencio);
        AplicarBrillo(brillo);
        ActualizarEtiquetaSilencio(silencio);
    }

    /// <summary>Escribe los ajustes en el archivo de guardado.</summary>
    public void GuardarAjustes()
    {
        if (GameData.Instance == null) return;

        if (volumenSlider) GameData.Instance.SetVolumen(volumenSlider.value);
        if (brilloSlider)  GameData.Instance.SetBrillo(brilloSlider.value);

        GameData.Instance.GuardarAjustes();
    }

    // ─── Controles ────────────────────────────────────────────────────────────

    private void AlCambiarVolumen(float valor)
    {
        AplicarVolumen(valor, GameData.Instance != null && GameData.Instance.Silenciado);
    }

    private void AlCambiarBrillo(float valor)
    {
        AplicarBrillo(valor);
    }

    /// <summary>Prende o apaga el sonido sin perder el volumen elegido.</summary>
    private void AlternarSilencio()
    {
        bool silencio = !(GameData.Instance != null && GameData.Instance.Silenciado);

        if (GameData.Instance != null)
        {
            GameData.Instance.SetSilenciado(silencio);
            GameData.Instance.GuardarAjustes();   // el interruptor es raro: se guarda al toque
        }

        float volumen = volumenSlider != null ? volumenSlider.value : SaveManager.VolumenPorDefecto;

        AplicarVolumen(volumen, silencio);
        ActualizarEtiquetaSilencio(silencio);
    }

    private void VolverAlMenu()
    {
        GuardarAjustes();
        UIManager.Instance?.OpenMainMenu();
    }

    // ─── Efectos ──────────────────────────────────────────────────────────────

    /// <summary>AudioListener.volume es global: no hace falta tocar cada AudioSource.</summary>
    private static void AplicarVolumen(float volumen01, bool silenciado)
    {
        AudioListener.volume = silenciado ? 0f : Mathf.Clamp01(volumen01);
    }

    /// <summary>Baja el brillo con un velo negro: 1 = sin velo, 0 = velo máximo.</summary>
    private void AplicarBrillo(float brillo01)
    {
        if (veloBrillo == null) return;

        Color color = veloBrillo.color;
        color.a = (1f - Mathf.Clamp01(brillo01)) * oscurecimientoMaximo;
        veloBrillo.color = color;
    }

    private void ActualizarEtiquetaSilencio(bool silenciado)
    {
        if (etiquetaSilencio != null)
            etiquetaSilencio.text = silenciado ? "SONIDO: NO" : "SONIDO: SI";
    }
}
