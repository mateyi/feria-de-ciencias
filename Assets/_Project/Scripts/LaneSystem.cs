using UnityEngine;

/// <summary>
/// Define los 3 carriles del runner y provee posiciones X para cada uno.
/// Carril 0 = izquierda, 1 = centro, 2 = derecha.
/// </summary>
public static class LaneSystem
{
    public const int LaneCount = 3;
    public const float LaneWidth = 2.5f;

    // Posiciones X de cada carril
    public static float GetLaneX(int lane)
    {
        lane = Mathf.Clamp(lane, 0, LaneCount - 1);
        return (lane - 1) * LaneWidth; // -2.5, 0, 2.5
    }
}
