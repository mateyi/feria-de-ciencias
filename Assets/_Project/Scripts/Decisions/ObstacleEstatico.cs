using UnityEngine;

/// <summary>
/// Obstáculo QUIETO en su carril.
///
/// Se usa para: conos de entrenamiento, vallas bajas, barrera de futbolistas,
/// árbitro parado y fotógrafo con trípode.
///
/// No necesita comportamiento propio: hereda la detección de choque de
/// ObstacleBase. Existe como clase aparte para que el prefab diga claramente
/// qué es, y para poder cambiarle la pose desde Preparar() si hace falta.
///
/// Carpeta: Assets/_Project/Scripts/Decisions/
/// </summary>
public class ObstacleEstatico : ObstacleBase
{
    // Ángulo de giro decorativo opcional (por ejemplo, un cartel de publicidad
    // que gira solo). En 0 el obstáculo queda quieto.
    [SerializeField] private float giroDecorativoPorSegundo = 0f;

    protected override void Comportamiento(float deltaTime)
    {
        if (Mathf.Approximately(giroDecorativoPorSegundo, 0f)) return;

        transform.Rotate(Vector3.up, giroDecorativoPorSegundo * deltaTime, Space.World);
    }
}
