using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador con un offset fijo, estilo endless runner.
/// Posición suavizada para sensación de velocidad.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset desde el jugador")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);

    [Header("Suavizado")]
    [SerializeField] private float positionSmoothing = 8f;
    [SerializeField] private float rotationSmoothing = 5f;

    [Header("Ángulo de cámara")]
    [SerializeField] private float lookDownAngle = 15f;

    [Header("Sacudida por impacto (Fase B)")]
    [Tooltip("Fuerza máxima del temblor, en metros. La pone en marcha ObstacleManager al chocar.")]
    [SerializeField] private float fuerzaSacudidaMaxima = 0.35f;

    // Estado de la sacudida en curso.
    private float _sacudidaFuerza;
    private float _sacudidaRestante;
    private float _sacudidaDuracion;

    /// <summary>
    /// Hace temblar la cámara un instante. La llama el ObstacleManager cuando el
    /// jugador choca, para que el golpe se sienta.
    /// </summary>
    /// <param name="fuerza01">Intensidad de 0 a 1.</param>
    /// <param name="duracion">Segundos que dura el temblor.</param>
    public void Sacudir(float fuerza01, float duracion)
    {
        _sacudidaFuerza   = Mathf.Clamp01(fuerza01) * fuerzaSacudidaMaxima;
        _sacudidaDuracion = Mathf.Max(0.01f, duracion);
        _sacudidaRestante = _sacudidaDuracion;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            // Intentar encontrar al jugador automáticamente
            if (PlayerController.Instance != null)
                target = PlayerController.Instance.transform;
            return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, positionSmoothing * Time.deltaTime);

        // ── Sacudida: desplazamiento aleatorio que se apaga solo ─────────────
        // Va DESPUÉS del seguimiento para que no se lo coma el Lerp.
        if (_sacudidaRestante > 0f)
        {
            _sacudidaRestante -= Time.deltaTime;

            // Se va haciendo más chica a medida que termina.
            float peso = Mathf.Clamp01(_sacudidaRestante / _sacudidaDuracion);
            Vector3 temblor = Random.insideUnitSphere * (_sacudidaFuerza * peso);
            transform.position += temblor;
        }

        Quaternion desiredRot = Quaternion.Euler(lookDownAngle, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSmoothing * Time.deltaTime);
    }
}
