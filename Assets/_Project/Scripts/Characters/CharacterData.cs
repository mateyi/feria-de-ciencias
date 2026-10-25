using UnityEngine;

/// <summary>
/// Ficha de un jugador seleccionable. Guarda el nombre, el pais, el dorsal, el
/// retrato de la tarjeta y los bonus que le da al equipo cuando esta equipado.
///
/// Los bonus se suman a las estadisticas base de PlayerStats (50 de velocidad,
/// 100 de energia, 50 de habilidad, 50 de popularidad).
///
/// Carpeta: Assets/_Project/Scripts/Characters/
/// </summary>
[CreateAssetMenu(fileName = "Character_", menuName = "Mundial Runner/Personaje")]
public class CharacterData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre interno, es el que se guarda en el archivo de partida.")]
    public string id = "Jugador";

    public string nombre    = "Jugador";
    public string pais      = "Pais";
    public string apodo     = "";

    [Tooltip("Numero de camiseta que se muestra en la tarjeta.")]
    public int dorsal = 10;

    [Tooltip("Retrato de la tarjeta.")]
    public Sprite retrato;

    [Tooltip("Color de la camiseta, se usa como acento de la tarjeta.")]
    public Color colorCamiseta = new Color(0.90f, 0.90f, 0.95f, 1f);

    [Header("Compra")]
    [Tooltip("Monedas que cuesta desbloquear a este jugador.")]
    public int precio = 5000;

    [Header("Modelo 3D")]
    [Tooltip("Material del modelo riggeado con la camiseta del pais. Si queda vacio se usa el modelo generico.")]
    public Material kit;

    [TextArea(2, 4)]
    public string estilo = "";

    [Header("Bonus al equipar (sobre las estadisticas base)")]
    [Range(-30f, 30f)] public float bonusVelocidad   = 0f;
    [Range(-30f, 30f)] public float bonusEnergia     = 0f;
    [Range(-30f, 30f)] public float bonusHabilidad   = 0f;
    [Range(-30f, 30f)] public float bonusPopularidad = 0f;
}
