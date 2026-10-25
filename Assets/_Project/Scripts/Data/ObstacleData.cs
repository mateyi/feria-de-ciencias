using UnityEngine;

/// <summary>
/// Cómo se mueve el obstáculo una vez que aparece en la pista.
/// </summary>
public enum TipoObstaculo
{
    Estatico,   // quieto en su carril (conos, vallas, barrera, árbitro parado, fotógrafo)
    Patrulla,   // va y viene entre dos carriles (árbitro móvil)
    Deslizante, // aparece al costado y cruza la cancha de costado (defensa, invasor)
    Rodante     // avanza/rueda por un carril (pelota gigante)
}

/// <summary>
/// Qué le pasa al jugador además de perder energía.
/// </summary>
public enum EfectoObstaculo
{
    Ninguno,
    PelotaRobada,   // el tackle: la pelota se escapa y vuelve (lo usa BallDribble)
    TarjetaAmarilla,// el árbitro: tarjeta + silbato + ralentización
    Ralentizar,     // frena al jugador un instante
    Empujar         // saca al jugador de su carril
}

/// <summary>
/// Datos de UN tipo de obstáculo. Es el "molde" que lee ObstacleManager para
/// decidir qué puede aparecer en cada nivel y cuánto daño hace.
///
/// Crear desde: Assets > Create > MundialRunner > ObstacleData
/// Carpeta: Assets/_Project/Scripts/Data/
/// </summary>
[CreateAssetMenu(fileName = "Obstacle_New", menuName = "MundialRunner/ObstacleData")]
public class ObstacleData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre interno corto. Se usa en los logs y para buscar el asset.")]
    public string id = "obstaculo";

    [Tooltip("Cómo se comporta en la pista.")]
    public TipoObstaculo tipo = TipoObstaculo.Estatico;

    [Tooltip("Prefab que se instancia. Debe tener un componente que herede de ObstacleBase.")]
    public GameObject prefab;

    [Header("Daño y efecto")]
    [Tooltip("Energía que le saca al jugador al chocarlo (la barra de energía del HUD).")]
    [Range(0f, 100f)]
    public float danioEnergia = 15f;

    [Tooltip("Efecto extra además del daño.")]
    public EfectoObstaculo efecto = EfectoObstaculo.Ninguno;

    [Tooltip("Cuántos segundos dura la ralentización del efecto Ralentizar / tarjeta.")]
    [Range(0f, 3f)]
    public float duracionRalentizacion = 1f;

    [Header("Aparición")]
    [Tooltip("Nivel a partir del cual puede aparecer. 1 = desde el entrenamiento.")]
    [Range(1, 20)]
    public int nivelMinimo = 1;

    [Tooltip("Peso relativo dentro del nivel. Más peso = aparece más seguido.")]
    [Range(0f, 20f)]
    public float peso = 1f;

    [Tooltip("Cuántos carriles ocupa de ancho. 1 = uno solo, 2 = dos (nunca los tres).")]
    [Range(1, 2)]
    public int carrilesQueOcupa = 1;

    [Tooltip("Si está marcado, el jugador lo puede pasar saltando con Espacio.")]
    public bool sePuedeSaltar = false;

    [Header("Movimiento (según el tipo)")]
    [Tooltip("Velocidad lateral del deslizante / patrulla, en metros por segundo. " +
             "En 0 se usa la que define el LevelConfig del nivel.")]
    public float velocidadLateral = 0f;

    [Tooltip("Segundos de aviso antes de que el deslizante entre a la cancha " +
             "(marca roja en el piso + ícono de alerta).")]
    [Range(0f, 3f)]
    public float tiempoTelegrafiado = 1.2f;

    [Tooltip("Alto del obstáculo, en metros. Lo usan el salto y los avisos.")]
    public float altura = 1f;
}
