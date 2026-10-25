using UnityEngine;

/// <summary>
/// Obstáculo que PATRULLA de costado: va y viene entre dos carriles.
///
/// Es el árbitro móvil: se para en el medio, camina al carril de al lado, vuelve,
/// y así. Obliga a mirar el hueco libre en movimiento en vez de memorizarlo.
///
/// Se mueve con PingPong sobre el eje X: recorre la distancia 'amplitudCarriles'
/// carriles de ida y vuelta a velocidad constante, sin usar física.
///
/// Carpeta: Assets/_Project/Scripts/Decisions/
/// </summary>
public class ObstaclePatrulla : ObstacleBase
{
    [Tooltip("Cuántos carriles recorre de ida (1 = va de un carril al de al lado y vuelve).")]
    [Range(1, 2)]
    [SerializeField] private int amplitudCarriles = 1;

    [Tooltip("Velocidad de la patrulla en metros por segundo. Si es 0 usa la del objeto.")]
    [SerializeField] private float velocidadPropia = 1.6f;

    // X del carril donde apareció: es un extremo del recorrido.
    private float _xOrigen;
    private float _signo = 1f;

    protected override void Preparar()
    {
        _xOrigen = transform.localPosition.x;

        // Patrulla hacia adentro de la cancha: si arranca en el carril derecho
        // camina hacia la izquierda y al revés. En el carril del medio elige
        // uno de los dos al azar (así no patrulla siempre para el mismo lado).
        if (_xOrigen > 0.01f)       _signo = -1f;
        else if (_xOrigen < -0.01f) _signo = 1f;
        else                        _signo = Random.value < 0.5f ? -1f : 1f;
    }

    protected override void Comportamiento(float deltaTime)
    {
        float recorrido = amplitudCarriles * LaneSystem.LaneWidth;
        if (recorrido <= 0.01f) return;

        float velocidad = velocidadPropia > 0.01f ? velocidadPropia : VelocidadLateral();

        // PingPong devuelve 0..1..0: el obstáculo va de un extremo al otro y vuelve.
        float avance = Mathf.PingPong(TiempoVida * velocidad, recorrido);

        Vector3 p = transform.localPosition;
        p.x = _xOrigen + _signo * avance;
        transform.localPosition = p;
    }
}
