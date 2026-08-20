using UnityEngine;

/// <summary>
/// Spawner de obstáculos y monedas que se coloca dentro de un segmento de pista.
/// Elige aleatoriamente qué carril(es) bloquear y dónde poner monedas.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject   coinPrefab;

    [Header("Configuración")]
    [SerializeField] private int   obstacleCount  = 2;
    [SerializeField] private int   coinCount      = 5;
    [SerializeField] private float spawnZStart    = 5f;
    [SerializeField] private float spawnZEnd      = 25f;
    [SerializeField] private float coinHeight     = 0.8f;
    [SerializeField] private float obstacleHeight = 0.5f;

    private void Start()
    {
        SpawnObstacles();
        SpawnCoins();
    }

    private void SpawnObstacles()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // Elegir carriles bloqueados (nunca los 3 a la vez)
        int blockedLanes = Random.Range(1, LaneSystem.LaneCount);
        int[] lanes = GetRandomLanes(blockedLanes);

        foreach (int lane in lanes)
        {
            float x = LaneSystem.GetLaneX(lane);
            float z = transform.position.z + Random.Range(spawnZStart, spawnZEnd);
            Vector3 pos = new Vector3(x, transform.position.y + obstacleHeight, z);

            int idx = Random.Range(0, obstaclePrefabs.Length);
            Instantiate(obstaclePrefabs[idx], pos, Quaternion.identity, transform);
        }
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

    private int[] GetRandomLanes(int count)
    {
        int[] all = { 0, 1, 2 };
        // Fisher-Yates shuffle
        for (int i = all.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }
        int[] result = new int[count];
        System.Array.Copy(all, result, count);
        return result;
    }
}
