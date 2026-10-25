using UnityEngine;

/// <summary>
/// Identificador de cada mejora de la tienda. El nombre del enum es la clave
/// con la que se guarda el nivel en el archivo de partida.
/// </summary>
public enum UpgradeId
{
    Velocidad,
    Resistencia,
    Control,
    Potencia,
    Definicion,
    Suerte,
    Iman,
    Escudo
}

/// <summary>
/// ScriptableObject con la definición de una mejora comprable.
/// Crea instancias desde Assets > Create > MundialRunner > UpgradeData.
///
/// El costo de cada nivel crece con multiplicadorCosto:
///   nivel 1 → costoBase
///   nivel 2 → costoBase * multiplicadorCosto
///   nivel 3 → costoBase * multiplicadorCosto² ...
///
/// Carpeta: Assets/_Project/Scripts/Upgrades/
/// </summary>
[CreateAssetMenu(fileName = "Upgrade_Nueva", menuName = "MundialRunner/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    [Header("Identidad")]
    public UpgradeId id = UpgradeId.Velocidad;
    public string nombre = "Mejora";
    [TextArea(2, 3)] public string descripcion = "";
    public Sprite icono;

    [Header("Progresión")]
    [Tooltip("Nivel máximo que se puede comprar.")]
    public int nivelMaximo = 5;
    [Tooltip("Costo del primer nivel.")]
    public int costoBase = 50;
    [Tooltip("Cuánto se multiplica el costo en cada nivel siguiente.")]
    public float multiplicadorCosto = 1.6f;

    [Tooltip("Texto corto del efecto por nivel, por ejemplo '+6% de velocidad'.")]
    public string efectoPorNivel = "";

    /// <summary>Costo de comprar el nivel indicado (1 = primer nivel).</summary>
    public int CostoParaNivel(int nivel)
    {
        if (nivel <= 1) return costoBase;
        return Mathf.RoundToInt(costoBase * Mathf.Pow(multiplicadorCosto, nivel - 1));
    }
}
