using UnityEngine;

/// <summary>
/// Disparador del arco de "META". Cuando el jugador lo atraviesa, detiene la generación de la pista
/// y habilita un modo visual/flujo de fin de nivel (transición hacia el túnel).
/// </summary>
public class FinishArchTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float finishZOffset = 1f;

    [Tooltip("Qué distancia (en metros, aprox.) usar para destruir segmentos que queden más allá del fin.")]
    [SerializeField] private float cleanupZPadding = 10f;

    [Header("Confetti (opcional)")]
    [SerializeField] public ParticleSystem confetti;

    [Header("Transición")]
    [SerializeField] private GameObject penaltyTunnelPrefab;
    [SerializeField] private float penaltyTunnelSpawnYOffset = 0f;

    private GameObject _tunnelInstance;
    private bool _fired;

    // Exponer getters/setters para el generador de prefabs (editor)
    public float FinishZOffset => finishZOffset;
    public float CleanupZPadding => cleanupZPadding;

    public void SetConfetti(ParticleSystem ps) => confetti = ps;
    public void SetOffsets(float finishOffset, float cleanupPadding)
    {
        finishZOffset = finishOffset;
        cleanupZPadding = cleanupPadding;
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Guard total: cualquier excepción aquí NO debe congelar/crashear el juego.
        try
        {
            if (_fired) return;
            if (other == null) return;
            if (PlayerController.Instance == null) return;
            if (other.gameObject != PlayerController.Instance.gameObject) return;

            _fired = true;

            if (confetti != null) confetti.Play(true);

            float finishZ = transform.position.z + finishZOffset;

            // Instanciar el túnel una sola vez.
            if (penaltyTunnelPrefab != null && _tunnelInstance == null)
            {
                Vector3 spawnPos = transform.position;
                spawnPos.y += penaltyTunnelSpawnYOffset;
                spawnPos.z = finishZ;
                _tunnelInstance = Instantiate(penaltyTunnelPrefab, spawnPos, transform.rotation);
            }

            TrackGenerator tg = TrackGenerator.Instance;
            tg?.StopGeneratingAfter(finishZ, cleanupZPadding);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FinishArchTrigger] Error al procesar la meta: {ex.Message}");
        }
    }
}
