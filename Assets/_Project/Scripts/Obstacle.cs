using UnityEngine;

/// <summary>
/// Obstáculo que mata al jugador al tocarlo.
/// Coloca este componente en cualquier objeto con Collider que sea obstáculo.
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Tipo de obstáculo")]
    public ObstacleType obstacleType = ObstacleType.Block;

    public enum ObstacleType
    {
        Block,   // bloquea el carril completo
        Low,     // hay que saltar
        High     // hay que agacharse (futuro)
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController pc = other.GetComponent<PlayerController>();
            pc?.Die();
        }
    }
}
