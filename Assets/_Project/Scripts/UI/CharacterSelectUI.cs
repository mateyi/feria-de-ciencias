using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de seleccion de jugador. Arma una tarjeta por cada CharacterData del
/// catalogo, resalta la equipada y deja cambiarla tocando su boton.
///
/// Va en Canvas/CharactersPanel. Las tarjetas se instancian desde una plantilla
/// que queda desactivada dentro del contenedor.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Catalogo")]
    [Tooltip("Jugadores que se pueden elegir. El orden es el que se ve en pantalla.")]
    [SerializeField] private CharacterData[] personajes;

    [Header("Referencias de UI")]
    [SerializeField] private RectTransform    contenedor;
    [Tooltip("Tarjeta de ejemplo, desactivada, que se clona por cada jugador.")]
    [SerializeField] private CharacterCardUI  plantilla;
    [SerializeField] private TMP_Text         monedasText;
    [SerializeField] private Button           botonVolver;

    private readonly List<CharacterCardUI> _tarjetas = new List<CharacterCardUI>();
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

    // ─── Construccion ─────────────────────────────────────────────────────────

    /// <summary>Clona la plantilla una vez por jugador del catalogo.</summary>
    private void Construir()
    {
        if (_construido) return;
        if (plantilla == null || contenedor == null) return;

        plantilla.gameObject.SetActive(false);

        // Si la escena quedo guardada con tarjetas de una prueba en el editor,
        // las borramos para no duplicar la lista al abrir el panel.
        for (int i = contenedor.childCount - 1; i >= 0; i--)
        {
            var hijo = contenedor.GetChild(i);
            if (hijo == plantilla.transform) continue;
            if (!hijo.name.StartsWith("Carta_")) continue;

            if (Application.isPlaying) Destroy(hijo.gameObject);
            else                       DestroyImmediate(hijo.gameObject);
        }

        if (personajes != null)
        {
            foreach (var p in personajes)
            {
                if (p == null) continue;

                var tarjeta = Instantiate(plantilla, contenedor);
                tarjeta.gameObject.name = $"Carta_{p.id}";
                tarjeta.gameObject.SetActive(true);
                tarjeta.Configurar(p, this);
                _tarjetas.Add(tarjeta);
            }
        }

        _construido = true;
    }

    // ─── Seleccion ────────────────────────────────────────────────────────────

    /// <summary>La llaman las tarjetas cuando tocan su boton.</summary>
    public void Equipar(CharacterData personaje)
    {
        if (CharacterManager.Instance == null || personaje == null) return;

        CharacterManager.Instance.Equipar(personaje);
        Refrescar();
    }

    // ─── Refresco ─────────────────────────────────────────────────────────────

    /// <summary>Vuelve a pintar las monedas y el estado de todas las tarjetas.</summary>
    public void Refrescar()
    {
        if (monedasText != null && GameData.Instance != null)
            monedasText.text = GameData.Instance.Coins.ToString();

        foreach (var t in _tarjetas)
            if (t != null) t.Refrescar();
    }
}
