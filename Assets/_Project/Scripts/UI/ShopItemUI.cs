using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Una fila de la tienda: icono, nombre de la mejora, nivel actual, costo del
/// siguiente nivel y el botón de comprar.
///
/// No se coloca a mano: ShopUI instancia esta fila (la "plantilla") una vez por
/// cada UpgradeData del catálogo.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Image    icono;
    [SerializeField] private TMP_Text nombreText;
    [SerializeField] private TMP_Text nivelText;
    [SerializeField] private TMP_Text costoText;
    [SerializeField] private Button   botonComprar;
    [SerializeField] private Image    fondo;
    [SerializeField] private Image    rellenoNivel;

    [Header("Colores")]
    [SerializeField] private Color colorNormal   = new Color(0.12f, 0.16f, 0.24f, 0.98f);
    [SerializeField] private Color colorComprable = new Color(0.11f, 0.26f, 0.20f, 0.98f);
    [SerializeField] private Color colorMaximo    = new Color(0.30f, 0.24f, 0.06f, 0.98f);

    private UpgradeData _mejora;
    private ShopUI      _tienda;

    // ─── Configuración ────────────────────────────────────────────────────────

    /// <summary>Engancha esta fila con una mejora concreta y con la tienda.</summary>
    public void Configurar(UpgradeData mejora, ShopUI tienda)
    {
        _mejora = mejora;
        _tienda = tienda;

        if (icono != null)
        {
            icono.sprite  = mejora.icono;
            icono.enabled = mejora.icono != null;
        }

        if (nombreText != null) nombreText.text = mejora.nombre;

        if (botonComprar != null)
        {
            botonComprar.onClick.RemoveAllListeners();
            botonComprar.onClick.AddListener(Comprar);
        }

        Refrescar();
    }

    /// <summary>Vuelve a leer el nivel guardado y las monedas del jugador.</summary>
    public void Refrescar()
    {
        if (_mejora == null) return;

        int nivel   = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetNivel(_mejora.id) : 0;
        bool maximo = nivel >= _mejora.nivelMaximo;
        int  costo  = maximo ? 0 : _mejora.CostoParaNivel(nivel + 1);
        int  monedas = GameData.Instance != null ? GameData.Instance.Coins : 0;

        if (nivelText != null)
            nivelText.text = maximo ? $"NIVEL {nivel} (MAX)" : $"Nivel {nivel}/{_mejora.nivelMaximo}";

        if (costoText != null)
            costoText.text = maximo ? "Completo" : $"{costo}";

        if (rellenoNivel != null)
        {
            rellenoNivel.enabled     = true;
            rellenoNivel.type        = Image.Type.Filled;
            rellenoNivel.fillMethod  = Image.FillMethod.Horizontal;
            rellenoNivel.fillAmount  = _mejora.nivelMaximo > 0
                ? Mathf.Clamp01((float)nivel / _mejora.nivelMaximo) : 0f;
        }

        bool puedeComprar = !maximo && monedas >= costo;

        if (botonComprar != null) botonComprar.interactable = puedeComprar;

        if (fondo != null)
            fondo.color = maximo ? colorMaximo : (puedeComprar ? colorComprable : colorNormal);
    }

    private void Comprar()
    {
        if (_mejora == null) return;
        _tienda?.Comprar(_mejora);
    }
}
