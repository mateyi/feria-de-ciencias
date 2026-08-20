using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Genera la pista de forma procedural reciclando segmentos prefabricados.
/// Mantiene siempre N segmentos adelante del jugador y recicla los que quedan atrás.
/// </summary>
public class TrackGenerator : MonoBehaviour
{
    public static TrackGenerator Instance { get; private set; }

    [Header("Prefabs de segmentos")]
    [SerializeField] private TrackSegment[] normalSegmentPrefabs;
    [SerializeField] private TrackSegment   decisionSegmentPrefab;
    [SerializeField] private TrackSegment   startSegmentPrefab;

    [Header("Configuración")]
    [SerializeField] private int   segmentsAhead    = 5;
    [SerializeField] private int   segmentsBehind   = 2;
    [SerializeField] private int   decisionInterval = 4; // cada N segmentos normales

    private readonly List<TrackSegment> _activeSegments = new List<TrackSegment>();
    private Vector3 _nextSpawnPos;
    private int _segmentCount;
    private int _normalSegmentsSinceDecision;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ResetTrack();
    }

    public void ResetTrack()
    {
        // Destruir segmentos existentes
        foreach (var seg in _activeSegments)
            if (seg != null) Destroy(seg.gameObject);
        _activeSegments.Clear();

        _nextSpawnPos = Vector3.zero;
        _segmentCount = 0;
        _normalSegmentsSinceDecision = 0;

        // Segmento inicial
        if (startSegmentPrefab != null)
            SpawnSegment(startSegmentPrefab);

        // Pre-generar segmentos
        for (int i = 0; i < segmentsAhead; i++)
            SpawnNextSegment();
    }

    private void Update()
    {
        if (PlayerController.Instance == null) return;

        float playerZ = PlayerController.Instance.transform.position.z;

        // Generar nuevos segmentos adelante
        while (_activeSegments.Count > 0)
        {
            var last = _activeSegments[_activeSegments.Count - 1];
            if (last.SpawnPoint.z < playerZ + segmentsAhead * 30f)
                SpawnNextSegment();
            else
                break;
        }

        // Reciclar segmentos atrás
        while (_activeSegments.Count > 0)
        {
            var first = _activeSegments[0];
            if (first.DespawnPoint.z < playerZ - segmentsBehind * 30f)
            {
                _activeSegments.RemoveAt(0);
                Destroy(first.gameObject);
            }
            else break;
        }
    }

    private void SpawnNextSegment()
    {
        bool spawnDecision = _normalSegmentsSinceDecision >= decisionInterval
                             && decisionSegmentPrefab != null;

        if (spawnDecision)
        {
            SpawnSegment(decisionSegmentPrefab);
            _normalSegmentsSinceDecision = 0;
        }
        else
        {
            if (normalSegmentPrefabs == null || normalSegmentPrefabs.Length == 0) return;
            int idx = Random.Range(0, normalSegmentPrefabs.Length);
            SpawnSegment(normalSegmentPrefabs[idx]);
            _normalSegmentsSinceDecision++;
        }
    }

    private void SpawnSegment(TrackSegment prefab)
    {
        if (prefab == null) return;
        TrackSegment seg = Instantiate(prefab, _nextSpawnPos, Quaternion.identity);
        _nextSpawnPos = seg.SpawnPoint;
        _activeSegments.Add(seg);
        _segmentCount++;
    }
}
