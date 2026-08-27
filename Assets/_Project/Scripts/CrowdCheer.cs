using UnityEngine;

/// <summary>
/// Animación simple y en loop para el público de fondo: cada instancia salta/oscila
/// con un desfasaje aleatorio, dando sensación de celebración continua.
/// Barata (solo mueve el Transform local).
/// </summary>
public class CrowdCheer : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.15f;
    [SerializeField] private float speed = 4f;

    private float _phase;
    private Vector3 _basePos;

    private void Start()
    {
        _basePos = transform.localPosition;
        _phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float y = Mathf.Abs(Mathf.Sin(Time.time * speed + _phase)) * amplitude;
        transform.localPosition = _basePos + new Vector3(0, y, 0);
    }
}
