using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Panel de la tienda. Arma una fila por cada mejora del catálogo, muestra las
/// monedas del jugador y refresca todo después de cada compra.
///
/// Va en Canvas/ShopPanel. Las filas se instancian desde una plantilla que
/// queda desactivada dentro del contenedor.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Catálogo")]
    [Tooltip("Mejoras que se pueden comprar. El orden es el que se ve en pantalla.")]
    [SerializeField] private UpgradeData[] mejoras;

    [Header("Referencias de UI")]
    [SerializeField] private RectTransform contenedor;
    [Tooltip("Fila de ejemplo, desactivada, que se clona por cada mejora.")]
    [SerializeField] private ShopItemUI    plantilla;
    [SerializeField] private TMP_Text      monedasText;
    [SerializeField] private TMP_Text      avisoText;
    [SerializeField] private Button        botonVolver;

    private readonly List<ShopItemUI> _filas = new List<ShopItemUI>();
    private Coroutine _avisoActual;
    private bool _construido;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (botonVolver != null)
        {
            botonVolver.onClick.RemoveAllListeners();
            botonVolver.onClick.AddListener(() => UIManager.Instance?.ShowScreen(UIScreen.MainMenu));
        }

        Construir();
    }

    private void OnEnable()
    {
        Refrescar();
    }

    // ─── Construcción ─────────────────────────────────────────────────────────

    /// <summary>Clona la plantilla una vez por mejora del catálogo.</summary>
    private void Construir()
    {
        if (_construido) return;
        if (plantilla == null || contenedor == null) return;

        plantilla.gameObject.SetActive(false);

        // Si la escena quedó guardada con filas de ejemplo de una prueba en el
        // editor, las borramos para no duplicar la lista al abrir la tienda.
        for (int i = contenedor.childCount - 1; i >= 0; i--)
        {
            var hijo = contenedor.GetChild(i);
            if (hijo == plantilla.transform) continue;
            if (!hijo.name.StartsWith("Item_")) continue;

            if (Application.isPlaying) Destroy(hijo.gameObject);
            else                       DestroyImmediate(hijo.gameObject);
        }

        if (mejoras != null)
        {
            foreach (var mejora in mejoras)
            {
                if (mejora == null) continue;

                var fila = Instantiate(plantilla, contenedor);
                fila.gameObject.name = $"Item_{mejora.nombre}";
                fila.gameObject.SetActive(true);
                fila.Configurar(mejora, this);
                _filas.Add(fila);
            }
        }

        _construido = true;
    }

    // ─── Compras ──────────────────────────────────────────────────────────────

    /// <summary>La llaman las filas cuando tocan su botón.</summary>
    public void Comprar(UpgradeData mejora)
    {
        if (UpgradeManager.Instance == null) return;

        bool ok = UpgradeManager.Instance.Comprar(mejora);
        if (!ok)
            Avisar("No te alcanzan las monedas");

        Refrescar();
    }

    // ─── Aviso ────────────────────────────────────────────────────────────────

    /// <summary>Muestra un mensaje corto abajo del título y lo borra solo.</summary>
    public void Avisar(string mensaje)
    {
        if (avisoText == null) return;

        avisoText.text = mensaje;
        avisoText.gameObject.SetActive(true);

        if (_avisoActual != null) StopCoroutine(_avisoActual);
        _avisoActual = StartCoroutine(BorrarAviso());
    }

    private System.Collections.IEnumerator BorrarAviso()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        if (avisoText != null) avisoText.gameObject.SetActive(false);
        _avisoActual = null;
    }

    // ─── Refresco ─────────────────────────────────────────────────────────────

    /// <summary>Vuelve a pintar monedas y filas con los datos guardados.</summary>
    public void Refrescar()
    {
        if (monedasText != null && GameData.Instance != null)
            monedasText.text = GameData.Instance.Coins.ToString();

        foreach (var fila in _filas)
            if (fila != null) fila.Refrescar();
    }
}
