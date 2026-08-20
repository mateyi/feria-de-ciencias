using UnityEngine;

/// <summary>
/// Componente que va en cada segmento de pista prefabricado.
/// Expone el punto de inicio y fin para que el generador encadene segmentos.
/// </summary>
public class TrackSegment : MonoBehaviour
{
    [Header("Puntos de referencia")]
    [SerializeField] private Transform spawnPoint;   // donde aparece el siguiente segmento
    [SerializeField] private Transform despawnPoint; // cuando el jugador pasa este punto, se recicla

    public Vector3 SpawnPoint   => spawnPoint   != null ? spawnPoint.position   : transform.position + Vector3.forward * 30f;
    public Vector3 DespawnPoint => despawnPoint != null ? despawnPoint.position : transform.position;

    [Header("Tipo de segmento")]
    public SegmentType segmentType = SegmentType.Normal;

    public enum SegmentType { Normal, Decision, Finish }
}
