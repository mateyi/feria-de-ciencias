using UnityEngine;

/// <summary>
/// Spawner que vive dentro de cada segmento de pista.
///
/// FASE B: los obstáculos ya NO se instancian acá. Este componente le avisa al
/// ObstacleManager que el segmento existe, y el manager se encarga de todo:
/// pool de objetos, dificultad por nivel, filas que nunca tapan los 3 carriles
/// y distancia mínima entre filas.
///
/// Lo único que sigue haciendo este script es repartir las monedas, porque eso
/// no depende de la dificultad.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Legacy (ya no se usa)")]
    [Tooltip("Se conserva sólo para no perder la referencia en el prefab. Los obstáculos " +
             "ahora salen del pool del ObstacleManager usando ObstacleData.")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Monedas")]
    [SerializeField] private GameObject coinPrefab;

    [Tooltip("Cantidad de latas de monedas que se reparten en el segmento.")]
    [SerializeField] private int coinCount = 5;

    [Header("Zona útil del segmento")]
    [Tooltip("Z local donde empieza la zona útil del segmento.")]
    [SerializeField] private float spawnZStart = 5f;

    [Tooltip("Z local donde termina la zona útil del segmento.")]
    [SerializeField] private float spawnZEnd = 25f;

    [SerializeField] private float coinHeight = 0.8f;

    [Header("Zona segura de arranque")]
    [Tooltip("Los obstáculos solo se colocan a partir de esta Z del mundo. Sirve para que el " +
             "jugador tenga pista libre al empezar y no muera a los pocos metros del spawn.")]
    [SerializeField] private float minWorldZ = 35f;

    private void Start()
    {
        PoblarObstaculos();
        SpawnCoins();
        PoblarPickups();
    }

    /// <summary>
    /// Le pide al ObstacleManager que llene este segmento con filas de obstáculos
    /// según el nivel actual. Si el manager no existe (escena de prueba), avisa
    /// por consola y la pista queda limpia: nunca tira una excepción.
    /// </summary>
    private void PoblarObstaculos()
    {
        if (ObstacleManager.Instance == null)
        {
            Debug.LogWarning("[ObstacleSpawner] No hay ObstacleManager en la escena: " +
                             "el segmento queda sin obstáculos.");
            return;
        }

        ObstacleManager.Instance.PoblarSegmento(transform, spawnZStart, spawnZEnd, minWorldZ);
    }

    private void SpawnCoins()
    {
        if (coinPrefab == null) return;

        // Línea de monedas en un carril aleatorio
        int lane = Random.Range(0, LaneSystem.LaneCount);
        float x  = LaneSystem.GetLaneX(lane);
        float zStep = (spawnZEnd - spawnZStart) / Mathf.Max(coinCount - 1, 1);

        for (int i = 0; i < coinCount; i++)
        {
            float z = transform.position.z + spawnZStart + i * zStep;
            Vector3 pos = new Vector3(x, transform.position.y + coinHeight, z);
            Instantiate(coinPrefab, pos, Quaternion.identity, transform);
        }
    }

    /// <summary>
    /// Le pide al PickupManager que reparta los pickups de la Fase C en este
    /// segmento (bebida isotónica, botines turbo, imán y chaleco). Si el manager
    /// no existe (escena de prueba) el segmento queda sin pickups.
    /// </summary>
    private void PoblarPickups()
    {
        if (PickupManager.Instance == null) return;

        PickupManager.Instance.PoblarSegmento(transform, spawnZStart, spawnZEnd, minWorldZ);
    }
}
