using UnityEngine;

/// <summary>
/// Qué le pasa al jugador cuando toca un pickup.
/// </summary>
public enum TipoPickup
{
    Energia, // bebida isotónica: recupera energía de la barra
    Turbo,   // botines: más velocidad durante unos segundos
    Iman,    // imán: atrae las latas de monedas durante unos segundos
    Escudo   // chaleco: absorbe un choque
}

/// <summary>
/// Datos de UN tipo de pickup (Fase C).
///
/// Es el "molde" que lee el PickupManager para saber qué prefab instanciar, con
/// qué peso puede salir, desde qué etapa aparece y qué efecto aplica.
///
/// El significado de <see cref="valor"/> depende del tipo:
///   Energia → puntos de energía que recupera.
///   Turbo   → multiplicador de velocidad (1,35 = 35% más rápido).
///   Iman    → radio de atracción de las latas, en metros.
///   Escudo  → cuántos escudos suma.
///
/// Crear desde: Assets > Create > MundialRunner > PickupData
/// Carpeta: Assets/_Project/Scripts/Data/
/// </summary>
[CreateAssetMenu(fileName = "Pickup_New", menuName = "MundialRunner/PickupData")]
public class PickupData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre interno corto. Se usa en los logs y para buscar el asset.")]
    public string id = "pickup";

    [Tooltip("Nombre que se muestra en el HUD.")]
    public string nombre = "Pickup";

    [Tooltip("Qué efecto aplica al recogerlo.")]
    public TipoPickup tipo = TipoPickup.Energia;

    [Tooltip("Prefab que se instancia. Debe tener un componente PickupBase.")]
    public GameObject prefab;

    [Tooltip("Color del ícono en el HUD y de las partículas al recogerlo.")]
    public Color color = Color.white;

    [Header("Efecto")]
    [Tooltip("Energía: puntos que recupera. Turbo: multiplicador de velocidad. " +
             "Imán: radio en metros. Escudo: cantidad de escudos.")]
    public float valor = 30f;

    [Tooltip("Segundos que dura el efecto. En 0 el efecto es instantáneo " +
             "(la energía y el escudo no duran: se aplican y listo).")]
    [Range(0f, 30f)]
    public float duracion = 0f;

    [Header("Aparición")]
    [Tooltip("Etapa a partir de la cual puede salir. 1 = desde el entrenamiento.")]
    [Range(1, 20)]
    public int nivelMinimo = 1;

    [Tooltip("Peso relativo frente a los otros pickups. Más peso = sale más seguido.")]
    [Range(0f, 20f)]
    public float peso = 1f;
}
