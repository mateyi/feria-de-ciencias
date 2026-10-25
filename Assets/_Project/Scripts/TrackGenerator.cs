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
    [Tooltip("Cada cuántos segmentos normales aparece uno de decisión. " +
             "En 1 el cartel aparece en segmentos alternados.")]
    [SerializeField] private int   decisionInterval = 1;

    [Tooltip("Z a partir de la cual ya no se generan segmentos de decisión. " +
             "Sirve para que el último cartel quede ANTES del arco de META " +
             "(z=300) y no caiga dentro del túnel.")]
    [SerializeField] private float decisionStopZ    = 270f;

    private readonly List<TrackSegment> _activeSegments = new List<TrackSegment>();
    private Vector3 _nextSpawnPos;
    private int _segmentCount;
    private int _normalSegmentsSinceDecision;

    // Modo finito (nivel con final definido)
    private bool _finiteMode;
    private float _finishZ;
    private float _finiteCleanupPadding;

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

        _finiteMode = false;
        _finishZ = float.PositiveInfinity;
        _finiteCleanupPadding = 10f;

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

    /// <summary>
    /// Activa un modo de corrida finita. A partir de finishZ, no se generan más segmentos
    /// y se destruyen los que queden "más allá".
    /// </summary>
    public void StopGeneratingAfter(float finishZ, float cleanupZPadding = 10f)
    {
        _finiteMode = true;
        _finishZ = finishZ;
        _finiteCleanupPadding = Mathf.Max(0f, cleanupZPadding);
    }

    private void Update()
    {
        if (PlayerController.Instance == null) return;

        float playerZ = PlayerController.Instance.transform.position.z;

        // Si el nivel ya se marcó como finito, limpiamos segmentos que queden demasiado adelante.
        if (_finiteMode)
        {
            for (int i = _activeSegments.Count - 1; i >= 0; i--)
            {
                var seg = _activeSegments[i];
                if (seg == null) { _activeSegments.RemoveAt(i); continue; }

                if (seg.SpawnPoint.z > _finishZ + _finiteCleanupPadding)
                {
                    _activeSegments.RemoveAt(i);
                    Destroy(seg.gameObject);
                }
            }
        }

        // Generar nuevos segmentos adelante.
        // Guard de iteraciones para evitar loops infinitos (congelamiento) si
        // un segmento no avanza la posición de spawn o falta un prefab.
        int safety = 0;
        const int MaxSpawnsPerFrame = 32;
        while (_activeSegments.Count > 0 && safety < MaxSpawnsPerFrame)
        {
            safety++;

            var last = _activeSegments[_activeSegments.Count - 1];
            if (last == null) { _activeSegments.RemoveAt(_activeSegments.Count - 1); continue; }

            float targetZ = playerZ + segmentsAhead * 30f;
            if (_finiteMode) targetZ = Mathf.Min(targetZ, _finishZ);

            if (last.SpawnPoint.z < targetZ)
            {
                int countBefore = _activeSegments.Count;
                float spawnZBefore = _nextSpawnPos.z;

                SpawnNextSegment();

                // Si no se agregó un segmento nuevo o el spawn no avanzó,
                // cortar para no quedar atrapados en un loop infinito.
                if (_activeSegments.Count == countBefore ||
                    Mathf.Approximately(_nextSpawnPos.z, spawnZBefore))
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        // Reciclar segmentos atrás
        while (_activeSegments.Count > 0)
        {
            var first = _activeSegments[0];
            if (first == null) { _activeSegments.RemoveAt(0); continue; }

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
                             && decisionSegmentPrefab != null
                             && _nextSpawnPos.z < decisionStopZ;

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
