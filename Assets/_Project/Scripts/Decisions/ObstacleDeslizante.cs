using UnityEngine;

/// <summary>
/// Defensa rival que se DESLIZA de costado para sacarte la pelota (tackle).
///
/// Tiene tres fases, y la primera es la que lo hace justo:
///
///   FASE 0 — AVISO (≈1,2 s): el defensor está afuera de la cancha, se prende
///            una marca roja en el piso sobre el carril que va a cruzar y un
///            ícono de alerta "!". El jugador ve por dónde viene y puede
///            cambiarse de carril a tiempo.
///   FASE 1 — DESLIZAMIENTO: el defensor entra cruzando la cancha de costado a
///            velocidad constante y se acuesta (rotación de 90° del cuerpo)
///            para que se lea como una barrida.
///   FASE 2 — SALIDA: terminó de cruzar. El ObstacleManager lo devuelve al pool
///            cuando quedó atrás.
///
/// Todo el movimiento es por código (no física), así nunca se escapa ni
/// atraviesa el piso.
///
/// Carpeta: Assets/_Project/Scripts/Decisions/
/// </summary>
public class ObstacleDeslizante : ObstacleBase
{
    [Header("Nombres de los hijos del prefab")]
    [Tooltip("Marca roja en el piso que avisa por dónde va a cruzar.")]
    [SerializeField] private string nombreTelegrafo = "Telegrafo";

    [Tooltip("Ícono de alerta que se ve arriba de la marca.")]
    [SerializeField] private string nombreAlerta = "Alerta";

    [Tooltip("Modelo del defensor: se acuesta al deslizarse.")]
    [SerializeField] private string nombreCuerpo = "Cuerpo";

    [Header("Deslizamiento")]
    [Tooltip("Segundos que tarda el cuerpo en acostarse al empezar la barrida.")]
    [SerializeField] private float duracionAcostarse = 0.18f;

    [Tooltip("Cuánto se inclina el cuerpo al acostarse. 90 = tirado del todo.")]
    [SerializeField] private float inclinacionBarrida = 90f;

    private enum Fase { Aviso, Deslizando, Saliendo }

    private Transform _telegrafo;
    private Transform _alerta;
    private Transform _cuerpo;

    private Fase  _fase;
    private float _xInicio;
    private float _xFin;
    private float _tiempoFase;
    private float _tiempoAviso;
    private float _velocidad;

    // ─── Pool: armar y apagar ─────────────────────────────────────────────────

    protected override void Preparar()
    {
        CachearHijos();

        _xInicio = transform.localPosition.x;

        // Cruza de un borde de la cancha al otro (el signo lo decide de qué
        // lado arrancó).
        _xFin = -Mathf.Sign(_xInicio) * Mathf.Abs(_xInicio);
        if (Mathf.Approximately(_xInicio, 0f)) _xFin = 0f;

        _tiempoAviso = Datos != null ? Datos.tiempoTelegrafiado : 1.2f;
        _velocidad   = VelocidadLateral();

        _fase      = Fase.Aviso;
        _tiempoFase = 0f;

        // Arranca derecho y con el aviso prendido.
        if (_cuerpo != null) _cuerpo.localRotation = Quaternion.identity;
        MostrarAviso(true);

        // La marca en el piso y el "!" van SIEMPRE en el centro de la cancha,
        // no donde está el defensor (que todavía está afuera).
        UbicarAvisoEnLaCancha();
    }

    public override void Desactivar()
    {
        MostrarAviso(false);
        base.Desactivar();
    }

    // ─── Comportamiento ───────────────────────────────────────────────────────

    protected override void Comportamiento(float deltaTime)
    {
        _tiempoFase += deltaTime;

        switch (_fase)
        {
            case Fase.Aviso:
                // El aviso ya está prendido; sólo esperamos que pase el tiempo.
                if (_tiempoFase >= _tiempoAviso)
                {
                    _fase = Fase.Deslizando;
                    _tiempoFase = 0f;
                    MostrarAviso(false);
                }
                break;

            case Fase.Deslizando:
                Deslizarse(deltaTime);
                break;

            case Fase.Saliendo:
                // Ya cruzó: no hace nada más. El manager lo recicla.
                break;
        }
    }

    /// <summary>
    /// Mueve el defensor de costado a velocidad constante y lo va acostando.
    /// </summary>
    private void Deslizarse(float deltaTime)
    {
        // Acostar el cuerpo (una sola vez, suavizado).
        if (_cuerpo != null)
        {
            float t = Mathf.Clamp01(_tiempoFase / Mathf.Max(0.01f, duracionAcostarse));
            _cuerpo.localRotation = Quaternion.Slerp(
                Quaternion.identity,
                Quaternion.Euler(0f, 0f, Mathf.Sign(_xFin) * inclinacionBarrida),
                t);
        }

        // Avanzar de costado.
        Vector3 p = transform.localPosition;
        float paso = _velocidad * deltaTime;

        // MoveTowards no se pasa del destino, así el cruce termina justo.
        p.x = Mathf.MoveTowards(p.x, _xFin, paso);
        transform.localPosition = p;

        if (Mathf.Approximately(p.x, _xFin))
        {
            _fase = Fase.Saliendo;
            _tiempoFase = 0f;
        }
    }

    // ─── Aviso (marca roja + "!") ─────────────────────────────────────────────

    private void CachearHijos()
    {
        if (_telegrafo == null) _telegrafo = BuscarHijo(nombreTelegrafo);
        if (_alerta    == null) _alerta    = BuscarHijo(nombreAlerta);
        if (_cuerpo    == null) _cuerpo    = BuscarHijo(nombreCuerpo);
    }

    private Transform BuscarHijo(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        Transform t = transform.Find(nombre);
        return t;
    }

    private void MostrarAviso(bool visible)
    {
        if (_telegrafo != null) _telegrafo.gameObject.SetActive(visible);
        if (_alerta    != null) _alerta.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Pone la marca y el "!" en el centro de la cancha, a la altura del piso y
    /// en la Z del obstáculo. Como el defensor arranca afuera, el aviso queda
    /// donde el jugador lo va a ver.
    /// </summary>
    private void UbicarAvisoEnLaCancha()
    {
        Vector3 centro = new Vector3(0f, transform.position.y, transform.position.z);

        if (_telegrafo != null)
            _telegrafo.position = centro + Vector3.up * 0.03f;   // apenas sobre el césped

        if (_alerta != null)
            _alerta.position = centro + Vector3.up * 2.0f;        // arriba, bien visible
    }
}
