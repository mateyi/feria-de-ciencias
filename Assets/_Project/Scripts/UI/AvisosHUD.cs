using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Avisos visuales del sistema de obstáculos (Fase B).
///
/// Muestra la TARJETA AMARILLA cuando el jugador choca con un árbitro, y la
/// ROJA cuando se acumulan tres (derrota). También lleva la cuenta de cuántas
/// amarillas tiene encima el jugador en el nivel.
///
/// Todo se arma por código: no hay que tocar el Canvas en el Inspector. Se
/// cuelga del Canvas que ya existe en la escena, así respeta el CanvasScaler
/// de 1080x1920 sin romper nada.
///
/// Usa sólo Images (rectángulos) y ningún texto: así no depende del atlas de
/// TextMeshPro ni de glifos que la fuente pueda no tener.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class AvisosHUD : MonoBehaviour
{
    public static AvisosHUD Instance { get; private set; }

    // Colores de las tarjetas (amarillo y rojo de árbitro).
    private static readonly Color Amarilla = new Color(1f, 0.84f, 0.12f, 1f);
    private static readonly Color Roja     = new Color(0.86f, 0.12f, 0.12f, 1f);
    private static readonly Color Apagada  = new Color(1f, 1f, 1f, 0.18f);

    private Image   _tarjetaGrande;    // la que aparece y se va
    private Image[] _marcas;           // las 3 casillas chicas de acumulación
    private float   _tiempoVisible;

    /// <summary>
    /// Devuelve el aviso, creándolo si todavía no existe. Se crea solo para no
    /// tener que acordarse de ponerlo en la escena.
    /// </summary>
    public static AvisosHUD InstanceOrCreate()
    {
        if (Instance != null) return Instance;

        // Buscamos el Canvas del juego (el mismo que usan todos los paneles).
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;   // sin Canvas no hay UI posible

        var go = new GameObject("AvisosHUD");
        go.transform.SetParent(canvas.transform, false);

        var aviso = go.AddComponent<AvisosHUD>();
        return aviso;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Construir();
    }

    private void Update()
    {
        if (_tiempoVisible <= 0f) return;

        _tiempoVisible -= Time.unscaledDeltaTime;

        // Se achica y se desvanece al final, para que no quede pegada.
        if (_tarjetaGrande != null)
        {
            float t = Mathf.Clamp01(_tiempoVisible / 0.9f);
            Color c = _tarjetaGrande.color;
            c.a = t;
            _tarjetaGrande.color = c;

            _tarjetaGrande.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t);

            if (_tiempoVisible <= 0f) _tarjetaGrande.gameObject.SetActive(false);
        }
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>Muestra la tarjeta amarilla y actualiza las casillas acumuladas.</summary>
    public void MostrarTarjetaAmarilla(int acumuladas)
    {
        MostrarTarjeta(Amarilla);
        ActualizarMarcas(acumuladas);
    }

    /// <summary>Muestra la tarjeta roja (tres amarillas = derrota).</summary>
    public void MostrarTarjetaRoja()
    {
        MostrarTarjeta(Roja);
    }

    /// <summary>Vuelve a cero la cuenta de tarjetas (al empezar un nivel).</summary>
    public void LimpiarTarjetas()
    {
        ActualizarMarcas(0);
        if (_tarjetaGrande != null) _tarjetaGrande.gameObject.SetActive(false);
        _tiempoVisible = 0f;
    }

    // ─── Construcción de la UI ────────────────────────────────────────────────

    private void MostrarTarjeta(Color color)
    {
        if (_tarjetaGrande == null) return;

        _tarjetaGrande.gameObject.SetActive(true);
        _tarjetaGrande.color = color;
        _tarjetaGrande.rectTransform.localScale = Vector3.one;

        _tiempoVisible = 0.9f;
    }

    private void ActualizarMarcas(int acumuladas)
    {
        if (_marcas == null) return;

        for (int i = 0; i < _marcas.Length; i++)
        {
            if (_marcas[i] == null) continue;
            _marcas[i].color = i < acumuladas ? Amarilla : Apagada;
        }
    }

    /// <summary>
    /// Arma el cartel de la tarjeta y las tres casillas de acumulación.
    /// Todo anclado arriba al centro, debajo de la barra superior del HUD.
    /// </summary>
    private void Construir()
    {
        // ── Tarjeta grande (aparece al chocar) ───────────────────────────────
        _tarjetaGrande = CrearRect("TarjetaGrande", transform,
                                   new Vector2(0f, -300f), new Vector2(110f, 150f), Amarilla);
        _tarjetaGrande.gameObject.SetActive(false);

        // ── Tres casillas de acumulación ─────────────────────────────────────
        _marcas = new Image[3];
        const float ancho = 26f;
        const float separacion = 34f;

        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * separacion;
            _marcas[i] = CrearRect($"Marca_{i}", transform,
                                   new Vector2(x, -400f), new Vector2(ancho, 36f), Apagada);
        }
    }

    /// <summary>
    /// Crea un rectángulo de UI (Image sin sprite = cuadro blanco teñible).
    /// Anclado arriba al centro, con posición relativa al centro del Canvas.
    /// </summary>
    private static Image CrearRect(string nombre, Transform padre, Vector2 posicion,
                                   Vector2 tamano, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;   // nunca tapa clics
        img.enabled = true;

        return img;
    }
}
