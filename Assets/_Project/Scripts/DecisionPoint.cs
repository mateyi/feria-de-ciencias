using UnityEngine;

/// <summary>
/// Punto de decisión en la pista. Cuando el jugador entra en la zona trigger,
/// muestra el panel de decisión con dos opciones (izquierda/derecha).
/// El jugador elige moviéndose al carril correspondiente.
///
/// FASE 13 — CARTELES CON ICONOS:
/// Cada punto elige una decisión al azar de su lista (así dos segmentos de
/// decisión seguidos no muestran lo mismo), pinta el cartel 3D de cada lado de
/// verde (opción positiva) o rojo (opción negativa) y le pega encima el icono
/// 2D plano correspondiente.
///
/// Carpeta: Assets/_Project/Scripts/
/// </summary>
public class DecisionPoint : MonoBehaviour
{
    [Header("Decisión")]
    [Tooltip("Decisión fija. Si la lista de abajo tiene elementos, se elige al azar " +
             "de ahí y este campo se ignora.")]
    [SerializeField] private DecisionData decisionData;

    [Tooltip("Decisiones posibles. En cada segmento se sortea una.")]
    [SerializeField] private DecisionData[] opciones;

    [Header("Zona de activación")]
    [SerializeField] private float activationDistance = 15f;

    [Header("Carteles 3D (Fase 13)")]
    [Tooltip("Panel 3D de la opción IZQUIERDA. Se pinta verde o rojo.")]
    [SerializeField] private Renderer cartelIzq;
    [Tooltip("Panel 3D de la opción DERECHA. Se pinta verde o rojo.")]
    [SerializeField] private Renderer cartelDer;
    [Tooltip("SpriteRenderer donde se pega el icono de la opción IZQUIERDA.")]
    [SerializeField] private SpriteRenderer iconoIzq;
    [Tooltip("SpriteRenderer donde se pega el icono de la opción DERECHA.")]
    [SerializeField] private SpriteRenderer iconoDer;

    private bool _triggered;
    private bool _resolved;

    // Colores del cartel cuando la opción es buena / mala.
    private static readonly Color Verde = new Color(0.16f, 0.62f, 0.28f, 1f);
    private static readonly Color Rojo  = new Color(0.68f, 0.16f, 0.16f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock _bloqueIzq;
    private MaterialPropertyBlock _bloqueDer;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        ElegirDecision();
        AplicarCarteles();
    }

    private void Update()
    {
        if (_triggered || _resolved) return;
        if (PlayerController.Instance == null) return;

        float dist = transform.position.z - PlayerController.Instance.transform.position.z;
        if (dist > 0 && dist < activationDistance)
        {
            _triggered = true;
            DecisionManager.Instance?.ShowDecision(decisionData, this);
        }
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    public void Resolve()
    {
        _resolved = true;
    }

    /// <summary>
    /// Sorteo de la decisión del segmento. Si no hay lista, queda la fija.
    /// </summary>
    private void ElegirDecision()
    {
        if (opciones == null || opciones.Length == 0) return;

        // Se descartan las entradas vacías para no sortear una decisión nula.
        int validas = 0;
        foreach (var o in opciones) if (o != null) validas++;
        if (validas == 0) return;

        int elegida = Random.Range(0, validas);
        foreach (var o in opciones)
        {
            if (o == null) continue;
            if (elegida-- == 0) { decisionData = o; return; }
        }
    }

    /// <summary>
    /// Pinta los dos carteles 3D y les cuelga el icono de cada opción.
    /// Los colores base del cartel salen de DecisionData (verde/rojo) y se
    /// aplican con MaterialPropertyBlock para no tocar el material compartido
    /// del prefab (todos los segmentos lo usan).
    /// </summary>
    private void AplicarCarteles()
    {
        if (decisionData == null) return;

        if (_bloqueIzq == null) _bloqueIzq = new MaterialPropertyBlock();
        if (_bloqueDer == null) _bloqueDer = new MaterialPropertyBlock();

        Color colorIzq = decisionData.leftIsPositive ? Verde : Rojo;
        Color colorDer = decisionData.rightIsPositive ? Verde : Rojo;

        Pintar(cartelIzq, _bloqueIzq, colorIzq);
        Pintar(cartelDer, _bloqueDer, colorDer);

        if (iconoIzq != null)
        {
            iconoIzq.sprite = decisionData.leftIcon;
            iconoIzq.enabled = decisionData.leftIcon != null;
        }

        if (iconoDer != null)
        {
            iconoDer.sprite = decisionData.rightIcon;
            iconoDer.enabled = decisionData.rightIcon != null;
        }
    }

    private static void Pintar(Renderer r, MaterialPropertyBlock bloque, Color color)
    {
        if (r == null) return;

        r.GetPropertyBlock(bloque);

        // URP Lit usa _BaseColor; el shader Standard usa _Color. Se setean los dos
        // y el que no exista simplemente se ignora.
        bloque.SetColor(BaseColorId, color);
        bloque.SetColor(ColorId, color);

        r.SetPropertyBlock(bloque);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Setea las referencias desde el script de editor que arma el prefab.
    /// Sólo existe para el editor: en el juego no se usa.
    /// </summary>
    public void ConfigurarCarteles(Renderer izq, Renderer der,
                                   SpriteRenderer iconoIzquierdo, SpriteRenderer iconoDerecho,
                                   DecisionData[] lista)
    {
        cartelIzq = izq;
        cartelDer = der;
        iconoIzq  = iconoIzquierdo;
        iconoDer  = iconoDerecho;
        opciones  = lista;
    }
#endif
}
