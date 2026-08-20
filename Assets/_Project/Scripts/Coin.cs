using UnityEngine;

/// <summary>
/// Moneda coleccionable. Al tocarla el jugador suma 1 moneda y desaparece.
/// </summary>
public class Coin : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    private Vector3 _startPos;

    private void Start()
    {
        _startPos = transform.position;
    }

    private void Update()
    {
        // Rotación y flotación visual
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunnerGameManager.Instance?.AddCoin();
            gameObject.SetActive(false);
        }
    }
}
