using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una tarjeta de jugador dentro del panel de seleccion: retrato, nombre, pais,
/// los bonus que aporta y el boton para equiparlo.
///
/// No se coloca a mano: CharacterSelectUI instancia esta tarjeta (la
/// "plantilla") una vez por cada CharacterData del catalogo.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class CharacterCardUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image    retrato;
    [SerializeField] private TMP_Text nombreText;
    [SerializeField] private TMP_Text paisText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button   botonEquipar;
    [SerializeField] private TMP_Text botonLabel;
    [SerializeField] private Image    fondo;
    [SerializeField] private Image    franja;

    [Header("Colores")]
    [SerializeField] private Color colorNormal    = new Color(0.12f, 0.16f, 0.24f, 0.98f);
    [SerializeField] private Color colorEquipado  = new Color(0.10f, 0.30f, 0.20f, 0.98f);

    private CharacterData      _personaje;
    private CharacterSelectUI   _panel;

    // ─── Configuracion ────────────────────────────────────────────────────────

    /// <summary>Engancha la tarjeta con un jugador y con el panel que la contiene.</summary>
    public void Configurar(CharacterData personaje, CharacterSelectUI panel)
    {
        _personaje = personaje;
        _panel     = panel;

        if (retrato != null)
        {
            retrato.sprite  = personaje.retrato;
            retrato.enabled = personaje.retrato != null;
        }

        if (nombreText != null) nombreText.text = personaje.nombre;
        if (paisText != null)   paisText.text   = $"#{personaje.dorsal}  {personaje.pais}";

        if (franja != null) franja.color = personaje.colorCamiseta;

        if (botonEquipar != null)
        {
            botonEquipar.onClick.RemoveAllListeners();
            botonEquipar.onClick.AddListener(Equipar);
        }

        Refrescar();
    }

    /// <summary>Repinta los bonus y el estado del boton segun quien este equipado.</summary>
    public void Refrescar()
    {
        if (_personaje == null) return;

        bool equipado = CharacterManager.Instance != null &&
                        CharacterManager.Instance.Equipado == _personaje;

        if (statsText != null) statsText.text = ConstruirBonus(_personaje);

        if (botonLabel != null) botonLabel.text = equipado ? "EQUIPADO" : "EQUIPAR";
        if (botonEquipar != null) botonEquipar.interactable = !equipado;

        if (fondo != null) fondo.color = equipado ? colorEquipado : colorNormal;
    }

    /// <summary>Arma el texto de bonus mostrando solo los que no son cero.</summary>
    private static string ConstruirBonus(CharacterData p)
    {
        var sb = new System.Text.StringBuilder();

        void Linea(string etiqueta, float valor)
        {
            if (Mathf.Abs(valor) < 0.01f) return;
            string color = valor > 0f ? "#4CD37A" : "#FF6A5E";
            string signo = valor > 0f ? "+" : "";
            sb.AppendLine($"<color={color}>{signo}{Mathf.RoundToInt(valor)} {etiqueta}</color>");
        }

        Linea("Velocidad",    p.bonusVelocidad);
        Linea("Energia",      p.bonusEnergia);
        Linea("Habilidad",    p.bonusHabilidad);
        Linea("Popularidad",  p.bonusPopularidad);

        return sb.ToString().TrimEnd();
    }

    private void Equipar()
    {
        if (_personaje == null) return;
        _panel?.Equipar(_personaje);
    }
}
