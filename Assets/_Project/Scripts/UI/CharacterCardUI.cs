using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una tarjeta de jugador dentro del panel de seleccion: retrato, nombre, pais,
/// los bonus que aporta, el precio y el boton para comprarlo o equiparlo.
///
/// No se coloca a mano: CharacterSelectUI instancia esta tarjeta (la
/// "plantilla") una vez por cada CharacterData del catalogo.
///
/// Estados del boton:
///   COMPRAR  -> todavia no lo tiene (se apaga si no le alcanzan las monedas)
///   EQUIPAR  -> ya es suyo y lo puede poner
///   EQUIPADO -> es el que esta jugando ahora
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
    [SerializeField] private TMP_Text precioText;
    [SerializeField] private Button   botonEquipar;
    [SerializeField] private TMP_Text botonLabel;
    [SerializeField] private Image    fondo;
    [SerializeField] private Image    franja;

    [Header("Colores")]
    [SerializeField] private Color colorNormal    = new Color(0.12f, 0.16f, 0.24f, 0.98f);
    [SerializeField] private Color colorEquipado  = new Color(0.10f, 0.30f, 0.20f, 0.98f);
    [SerializeField] private Color colorPrecio    = new Color(1f, 0.84f, 0.25f, 1f);
    [SerializeField] private Color colorSinPlata   = new Color(1f, 0.42f, 0.37f, 1f);

    private CharacterData     _personaje;
    private CharacterSelectUI _panel;

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
            botonEquipar.onClick.AddListener(TocarBoton);
        }

        Refrescar();
    }

    /// <summary>Repinta el precio, los bonus y el estado del boton.</summary>
    public void Refrescar()
    {
        if (_personaje == null) return;

        var cm = CharacterManager.Instance;
        bool poseido  = cm != null && cm.Posee(_personaje);
        bool equipado = cm != null && cm.Equipado == _personaje;
        bool alcanza  = cm != null && cm.PuedeComprar(_personaje);

        if (statsText != null) statsText.text = ConstruirBonus(_personaje);

        // Precio: se tacha en verde cuando ya es suyo, y se pone rojo si no alcanza.
        if (precioText != null)
        {
            if (poseido)
            {
                precioText.text  = "YA ES TUYO";
                precioText.color = new Color(0.45f, 0.85f, 0.55f, 1f);
            }
            else
            {
                precioText.text  = "$" + _personaje.precio.ToString("N0");
                precioText.color = alcanza ? colorPrecio : colorSinPlata;
            }
        }

        if (botonLabel != null)
            botonLabel.text = equipado ? "EQUIPADO" : (poseido ? "EQUIPAR" : "COMPRAR");

        if (botonEquipar != null)
            botonEquipar.interactable = equipado ? false : (poseido || alcanza);

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

    private void TocarBoton()
    {
        if (_personaje == null) return;
        _panel?.TocarJugador(_personaje);
    }
}
