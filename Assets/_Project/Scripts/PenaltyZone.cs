using UnityEngine;

/// <summary>
/// Datos de una de las 6 zonas de apuntado del arco de penales.
///
/// Cada zona es un Quad con BoxCollider trigger, hijo de
/// PenaltyArena_Instance/Goal/TargetZones. Este script guarda:
///   1. El ID de la zona (0..5), que es el orden exacto que espera
///      PenaltyMinigameManager.targetZones.
///   2. La posición objetivo: el punto del espacio al que vuela la pelota
///      cuando el jugador patea a esta zona.
///
/// Además expone SetHighlighted() para que la mira del mouse pueda resaltar
/// la zona apuntada (lo usa el minijuego en la fase de puntería).
///
/// Orden de IDs (debe coincidir con PenaltyMinigameManager):
///   0 = TopLeft      1 = TopCenter      2 = TopRight
///   3 = BottomLeft   4 = BottomCenter   5 = BottomRight
///
/// Carpeta: Assets/_Project/Scripts/
/// </summary>
public class PenaltyZone : MonoBehaviour
{
    /// <summary>Identificadores de las 6 zonas del arco.</summary>
    public enum ZoneId
    {
        TopLeft      = 0,
        TopCenter    = 1,
        TopRight     = 2,
        BottomLeft   = 3,
        BottomCenter = 4,
        BottomRight  = 5,
    }

    [Header("Identidad de la zona")]
    [Tooltip("ID de la zona. Tiene que coincidir con el índice del arreglo " +
             "targetZones de PenaltyMinigameManager.")]
    [SerializeField] private ZoneId zoneId = ZoneId.BottomCenter;

    [Tooltip("Punto exacto al que vuela la pelota. Si se deja vacío se usa el " +
             "centro de esta zona.")]
    [SerializeField] private Transform aimTarget;

    [Header("Resaltado de la mira")]
    [Tooltip("Renderer del quad de la zona. Se auto-detecta si se deja vacío.")]
    [SerializeField] private MeshRenderer zoneRenderer;

    [Tooltip("Color cuando la zona NO está apuntada.")]
    [SerializeField] private Color idleColor = new Color(1f, 0.85f, 0.1f, 0.20f);

    [Tooltip("Color cuando la mira está sobre la zona.")]
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 1f, 0.55f);

    // ─── Propiedades de solo lectura ──────────────────────────────────────────

    /// <summary>ID de la zona como enum (para código legible).</summary>
    public ZoneId Id => zoneId;

    /// <summary>ID de la zona como entero 0..5 (índice en targetZones).</summary>
    public int IdIndex => (int)zoneId;

    /// <summary>
    /// Punto al que apunta la pelota al patear esta zona.
    /// Si no se asignó aimTarget, se usa el centro del propio Quad.
    /// </summary>
    public Vector3 TargetPosition => aimTarget != null ? aimTarget.position : transform.position;

    // ─── Internos ─────────────────────────────────────────────────────────────

    // MaterialPropertyBlock: nos deja cambiar el color de UNA zona sin instanciar
    // el material compartido (Mat_TargetZone lo usan las 6 zonas a la vez).
    private MaterialPropertyBlock _propertyBlock;

    // ID del shader property "_BaseColor" (color base en URP Lit).
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (zoneRenderer == null) zoneRenderer = GetComponent<MeshRenderer>();

        _propertyBlock = new MaterialPropertyBlock();

        // Arrancar siempre en color normal.
        ApplyColor(idleColor);
    }

    private void OnValidate()
    {
        // Ayuda en el Inspector: autocompletar el renderer.
        if (zoneRenderer == null) zoneRenderer = GetComponent<MeshRenderer>();
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Enciende o apaga el resaltado de la zona.
    /// Se llama cada frame desde la lógica de puntería: la zona bajo la mira
    /// se resalta y el resto vuelve a su color normal.
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        ApplyColor(highlighted ? highlightColor : idleColor);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Aplica un color al quad de la zona sin crear materiales nuevos.</summary>
    private void ApplyColor(Color color)
    {
        if (zoneRenderer == null) zoneRenderer = GetComponent<MeshRenderer>();
        if (zoneRenderer == null) return;

        // Blindaje: _propertyBlock no está serializado, así que se pierde cuando
        // Unity recarga el dominio de scripts (al recompilar, por ejemplo).
        // Si vuelve null lo recreamos acá, para que resaltar una zona nunca
        // pueda lanzar ArgumentNullException en pleno juego.
        if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

        zoneRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(BaseColorId, color);
        zoneRenderer.SetPropertyBlock(_propertyBlock);
    }
}
