using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Íconos de los pickups en el HUD (Fase C).
///
/// Muestra una casilla por tipo de pickup, abajo a la derecha de la pantalla,
/// lejos de la barra de energía y del marcador:
///   - Turbo e Imán: la casilla se prende mientras dura el efecto y una barrita
///     se va vaciando con los segundos que quedan.
///   - Escudo: la casilla muestra una marca por cada escudo disponible (contador).
///   - Energía: la casilla hace un destello al recoger la bebida.
///
/// Igual que AvisosHUD, se arma sola por código y usa sólo Images: no depende
/// del atlas de TextMeshPro ni de glifos que la fuente pueda no tener.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class PickupHUD : MonoBehaviour
{
    public static PickupHUD Instance { get; private set; }

    // ─── Colores de respaldo (si todavía no hay catálogo cargado) ─────────────
    static readonly Color ColorEnergia = new Color(0.30f, 0.85f, 0.35f, 1f);
    static readonly Color ColorTurbo   = new Color(1.00f, 0.55f, 0.10f, 1f);
    static readonly Color ColorIman    = new Color(0.25f, 0.55f, 0.95f, 1f);
    static readonly Color ColorEscudo  = new Color(0.20f, 0.85f, 0.85f, 1f);

    // Alpha de la casilla cuando el efecto no está activo.
    const float AlphaApagado = 0.18f;

    // Segundos que dura el destello de la bebida.
    const float DuracionDestello = 0.5f;

    // Orden fijo de las casillas de izquierda a derecha.
    static readonly TipoPickup[] Orden =
    {
        TipoPickup.Energia, TipoPickup.Turbo, TipoPickup.Iman, TipoPickup.Escudo
    };

    /// <summary>Una casilla del HUD: fondo, barra de tiempo y marcas del escudo.</summary>
    private class Casilla
    {
        public RectTransform raiz;
        public Image         fondo;
        public RectTransform barra;        // sólo Turbo e Imán
        public Image[]       marcas;       // sólo Escudo
        public float         destello;     // segundos que quedan de destello
    }

    private readonly Dictionary<TipoPickup, Casilla> _casillas =
        new Dictionary<TipoPickup, Casilla>();

    // ─── Creación automática ──────────────────────────────────────────────────

    /// <summary>
    /// Devuelve el HUD, creándolo si todavía no existe. Se crea solo para no
    /// tener que acordarse de ponerlo en la escena.
    /// </summary>
    public static PickupHUD InstanceOrCreate()
    {
        if (Instance != null) return Instance;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return null;   // sin Canvas no hay UI posible

        // Contenedor a pantalla completa: así las anclas de las casillas se
        // miden contra toda la pantalla y no contra un rectángulo suelto.
        var go = new GameObject("PickupHUD", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale       = Vector3.one;

        return go.AddComponent<PickupHUD>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Construir();
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>Hace un destello corto en la casilla del pickup recogido.</summary>
    public void Destello(TipoPickup tipo)
    {
        if (_casillas.TryGetValue(tipo, out Casilla c) && c != null)
            c.destello = DuracionDestello;
    }

    // ─── Update: lee el estado del PickupManager y pinta las casillas ─────────

    private void Update()
    {
        PickupManager gm = PickupManager.Instance;

        foreach (var tipo in Orden)
        {
            if (!_casillas.TryGetValue(tipo, out Casilla c) || c == null) continue;

            float intensidad;
            float barra = 0f;

            switch (tipo)
            {
                case TipoPickup.Turbo:
                    barra      = gm != null ? gm.TurboRestante01 : 0f;
                    intensidad = barra > 0f ? 1f : 0f;
                    break;

                case TipoPickup.Iman:
                    barra      = gm != null ? gm.ImanRestante01 : 0f;
                    intensidad = barra > 0f ? 1f : 0f;
                    break;

                case TipoPickup.Escudo:
                    int escudos = gm != null ? gm.EscudosActivos : 0;
                    intensidad  = escudos > 0 ? 1f : 0f;
                    PintarMarcas(c, escudos);
                    break;

                default:   // Energía: no dura, sólo se anuncia con el destello.
                    intensidad = 0f;
                    break;
            }

            // El destello enciende la casilla aunque el efecto ya haya terminado.
            if (c.destello > 0f)
            {
                intensidad = 1f;
                c.destello = Mathf.Max(0f, c.destello - Time.unscaledDeltaTime);

                float t = Mathf.Clamp01(c.destello / DuracionDestello);
                c.raiz.localScale = Vector3.one * Mathf.Lerp(1f, 1.18f, t);
            }
            else
            {
                c.raiz.localScale = Vector3.one;
            }

            // Fondo: apagado o encendido según el estado.
            Color color = c.fondo.color;
            color.a = Mathf.Lerp(AlphaApagado, 1f, intensidad);
            c.fondo.color = color;

            // Barra de tiempo restante (ancho, anclada a la izquierda).
            if (c.barra != null)
                c.barra.localScale = new Vector3(Mathf.Clamp01(barra), 1f, 1f);
        }
    }

    private static void PintarMarcas(Casilla c, int escudos)
    {
        if (c.marcas == null) return;

        for (int i = 0; i < c.marcas.Length; i++)
        {
            if (c.marcas[i] == null) continue;
            c.marcas[i].color = i < escudos ? Color.white : new Color(1f, 1f, 1f, 0.22f);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CONSTRUCCIÓN DE LA UI
    // ═════════════════════════════════════════════════════════════════════════

    private void Construir()
    {
        const float lado       = 72f;  // lado de la casilla
        const float separacion = 92f;  // distancia entre casillas
        const float margenDer  = 34f;  // margen contra el borde derecho
        const float margenAba  = 34f;  // margen contra el borde inferior

        for (int i = 0; i < Orden.Length; i++)
        {
            TipoPickup tipo = Orden[i];

            var casilla = new Casilla();

            // ── Casilla (fondo de color) ─────────────────────────────────────
            var raiz = new GameObject($"Pickup_{tipo}", typeof(RectTransform), typeof(Image));
            raiz.transform.SetParent(transform, false);

            casilla.raiz = raiz.GetComponent<RectTransform>();
            // Ancladas a la esquina inferior derecha: las cuatro se acomodan de
            // derecha a izquierda así la última del orden queda pegada al borde.
            casilla.raiz.anchorMin = new Vector2(1f, 0f);
            casilla.raiz.anchorMax = new Vector2(1f, 0f);
            casilla.raiz.pivot     = new Vector2(1f, 0f);
            casilla.raiz.anchoredPosition = new Vector2(
                -(margenDer + (Orden.Length - 1 - i) * separacion), margenAba);
            casilla.raiz.sizeDelta = new Vector2(lado, lado);

            casilla.fondo = raiz.GetComponent<Image>();
            Color baseColor = ColorDe(tipo);
            baseColor.a = AlphaApagado;
            casilla.fondo.color = baseColor;
            casilla.fondo.raycastTarget = false;

            // ── Símbolo (dos o tres rectángulos blancos que identifican el tipo) ──
            ConstruirSimbolo(tipo, casilla.raiz, lado);

            // ── Barra de tiempo o marcas del escudo ──────────────────────────
            if (tipo == TipoPickup.Turbo || tipo == TipoPickup.Iman)
            {
                // Barrita adentro, pegada al borde de abajo.
                Image barra = CrearRect("Barra", casilla.raiz,
                                        Vector2.zero, Vector2.zero,
                                        new Vector2(4f, 4f), new Vector2(lado - 8f, 8f),
                                        new Color(1f, 1f, 1f, 0.95f));

                // Pivote a la izquierda: al achicar localScale.x la barra se
                // vacía desde la derecha, como una barra de tiempo.
                barra.rectTransform.pivot = Vector2.zero;
                casilla.barra = barra.rectTransform;
            }
            else if (tipo == TipoPickup.Escudo)
            {
                // Tres marcas: una por escudo disponible.
                casilla.marcas = new Image[3];
                const float ladoMarca = 14f;
                const float pasoMarca = 20f;
                float inicio = (lado - (ladoMarca + pasoMarca * 2f)) * 0.5f;

                for (int m = 0; m < 3; m++)
                {
                    casilla.marcas[m] = CrearRect($"Marca_{m}", casilla.raiz,
                                                  Vector2.zero, Vector2.zero,
                                                  new Vector2(inicio + m * pasoMarca, 4f),
                                                  new Vector2(ladoMarca, ladoMarca),
                                                  new Color(1f, 1f, 1f, 0.22f));
                }
            }

            _casillas[tipo] = casilla;
        }
    }

    /// <summary>
    /// Dibuja el símbolo de cada tipo con rectángulos blancos: una botella, un
    /// botín, un imán y un bíceps (escudo). Nada de texto ni de sprites externos.
    /// </summary>
    private static void ConstruirSimbolo(TipoPickup tipo, RectTransform padre, float lado)
    {
        Color blanco = new Color(1f, 1f, 1f, 0.95f);

        // Los símbolos se anclan al centro de la casilla y se mueven con
        // anchoredPosition: así quedan siempre adentro del cuadro de color.
        Vector2 centro = new Vector2(0.5f, 0.5f);

        switch (tipo)
        {
            // Botella: cuerpo alto y tapa chica arriba.
            case TipoPickup.Energia:
                CrearRect("Cuerpo", padre, centro, centro, new Vector2(0f, -3f),
                          new Vector2(16f, 34f), blanco);
                CrearRect("Tapa", padre, centro, centro, new Vector2(0f, 19f),
                          new Vector2(11f, 10f), blanco);
                break;

            // Botín de fútbol de perfil: cuerpo, talón alto atrás, puntera
            // adelante, suela y tres tapones abajo.
            case TipoPickup.Turbo:
                CrearRect("Cuerpo", padre, centro, centro, new Vector2(-4f, -1f),
                          new Vector2(30f, 15f), blanco);
                CrearRect("Talon", padre, centro, centro, new Vector2(-17f, 7f),
                          new Vector2(12f, 22f), blanco);
                CrearRect("Puntera", padre, centro, centro, new Vector2(18f, -7f),
                          new Vector2(16f, 9f), blanco);
                CrearRect("Suela", padre, centro, centro, new Vector2(0f, -13f),
                          new Vector2(46f, 7f), blanco);

                for (int t = 0; t < 3; t++)
                    CrearRect($"Tapon_{t}", padre, centro, centro,
                              new Vector2(-16f + t * 13f, -20f), new Vector2(6f, 6f), blanco);
                break;

            // Imán: barra arriba y dos patas abajo (herradura abierta).
            case TipoPickup.Iman:
                CrearRect("Barra", padre, centro, centro, new Vector2(0f, 12f),
                          new Vector2(36f, 12f), blanco);
                CrearRect("Pata_L", padre, centro, centro, new Vector2(-12f, -8f),
                          new Vector2(12f, 26f), blanco);
                CrearRect("Pata_R", padre, centro, centro, new Vector2(12f, -8f),
                          new Vector2(12f, 26f), blanco);
                break;

            // Bíceps flexionado: brazo abajo, músculo arriba y antebrazo con el
            // puño hacia arriba. Se lee de un vistazo como "más fuerza".
            case TipoPickup.Escudo:
                CrearRect("Brazo", padre, centro, centro, new Vector2(-6f, -11f),
                          new Vector2(26f, 12f), blanco);
                CrearRect("Biceps", padre, centro, centro, new Vector2(-7f, 3f),
                          new Vector2(24f, 18f), blanco);
                CrearRect("Antebrazo", padre, centro, centro, new Vector2(11f, 1f),
                          new Vector2(12f, 28f), blanco);
                CrearRect("Puno", padre, centro, centro, new Vector2(11f, 20f),
                          new Vector2(18f, 14f), blanco);
                break;
        }
    }

    // ─── Helpers de construcción ──────────────────────────────────────────────

    /// <summary>Crea un rectángulo de UI (Image sin sprite = cuadro blanco teñible).</summary>
    private static Image CrearRect(string nombre, Transform padre,
                                   Vector2 anclaMin, Vector2 anclaMax,
                                   Vector2 posicion, Vector2 tamano, Color color)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;   // nunca tapa clics

        return img;
    }

    /// <summary>Color del tipo: lo saca del catálogo del PickupManager si existe.</summary>
    private static Color ColorDe(TipoPickup tipo)
    {
        PickupManager gm = PickupManager.Instance;
        if (gm != null && gm.Catalogo != null)
        {
            foreach (var dato in gm.Catalogo)
                if (dato != null && dato.tipo == tipo) return dato.color;
        }

        // Respaldo por si el manager todavía no está en la escena.
        switch (tipo)
        {
            case TipoPickup.Turbo:  return ColorTurbo;
            case TipoPickup.Iman:   return ColorIman;
            case TipoPickup.Escudo: return ColorEscudo;
            default:                return ColorEnergia;
        }
    }
}
