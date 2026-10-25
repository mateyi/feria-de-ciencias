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

        // Mejora "Imán de monedas": agranda el radio de recogida de esta lata.
        if (UpgradeManager.Instance != null)
        {
            float extra = UpgradeManager.Instance.RadioImanExtra;
            if (extra > 0f)
            {
                var col = GetComponent<SphereCollider>();
                if (col != null) col.radius += extra;
            }
        }
    }

    private void Update()
    {
        // Pickup de imán (Fase C): mientras está activo, la lata vuela hacia el
        // jugador si está dentro del radio. Se suma a la mejora "Imán" de la
        // tienda, que agranda el radio del collider en Start().
        if (AtraerHaciaElJugador()) return;

        // Rotación y flotación visual
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        float y = _startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    /// <summary>
    /// Si el imán está activo y el jugador está dentro del radio, mueve la lata
    /// hacia él. Devuelve true mientras la está atrayendo.
    /// </summary>
    private bool AtraerHaciaElJugador()
    {
        if (PickupManager.Instance == null) return false;
        if (!PickupManager.Instance.ImanActivo) return false;
        if (PlayerController.Instance == null) return false;

        float radio = PickupManager.Instance.RadioImanActual;
        if (radio <= 0f) return false;

        Vector3 objetivo = PlayerController.Instance.transform.position + Vector3.up * 0.8f;
        if ((objetivo - transform.position).sqrMagnitude > radio * radio) return false;

        transform.position = Vector3.MoveTowards(transform.position, objetivo, 18f * Time.deltaTime);
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Mejora "Suerte": algunas latas valen más de una moneda.
            int monedas = 1 + (UpgradeManager.Instance != null ? UpgradeManager.Instance.MonedasExtra : 0);
            RunnerGameManager.Instance?.AddCoin(monedas);
            PlayerStats.Instance?.AddEnergy(energyRestore);
            gameObject.SetActive(false);
        }
    }
}
