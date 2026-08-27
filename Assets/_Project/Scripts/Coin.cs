using UnityEngine;

/// <summary>
/// Coleccionable estilo lata de Red Bull. Al tocarla el jugador suma "monedas"
/// (para el contador del HUD) y recupera energía para poder seguir corriendo.
/// </summary>
public class Coin : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Recompensa")]
    [Tooltip("Energía que recupera el jugador al recoger la lata.")]
    [SerializeField] private float energyRestore = 12f;

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
            PlayerStats.Instance?.AddEnergy(energyRestore);
            gameObject.SetActive(false);
        }
    }
}
