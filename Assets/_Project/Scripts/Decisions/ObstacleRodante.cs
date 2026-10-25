using UnityEngine;

/// <summary>
/// Pelota gigante que rueda por un carril.
///
/// Se queda en su carril (no persigue al jugador, sería imposible esquivarla)
/// pero gira sobre el eje X como si avanzara hacia él: el ojo lee "viene
/// rodando" aunque esté quieta en Z.
///
/// La velocidad de giro se calcula con la misma fórmula que usaría una esfera
/// real: v = ω · r  →  ω = v / r. Así rueda "a la velocidad correcta" para su
/// tamaño y no parece patinar.
///
/// Carpeta: Assets/_Project/Scripts/Decisions/
/// </summary>
public class ObstacleRodante : ObstacleBase
{
    [Tooltip("Velocidad de avance simulada, en metros por segundo (sólo afecta al giro).")]
    [SerializeField] private float velocidadSimulada = 9f;

    [Tooltip("Radio de la pelota en metros. Se usa para calcular el giro.")]
    [SerializeField] private float radio = 1f;

    protected override void Comportamiento(float deltaTime)
    {
        // ω = v / r  →  grados por segundo.
        float radioSeguro = Mathf.Max(0.05f, radio);
        float grados = (velocidadSimulada / radioSeguro) * Mathf.Rad2Deg * deltaTime;

        // Rueda hacia adelante (el jugador la ve venir): giro alrededor de X.
        transform.Rotate(Vector3.right, grados, Space.World);
    }
}
