using UnityEngine;

/// <summary>
/// ScriptableObject que define una decisión de vida del futbolista.
/// Crea instancias desde Assets > Create > MundialRunner > DecisionData.
/// </summary>
[CreateAssetMenu(fileName = "Decision_New", menuName = "MundialRunner/DecisionData")]
public class DecisionData : ScriptableObject
{
    [Header("Pregunta")]
    [TextArea(2, 4)]
    public string question = "¿Qué decides?";

    [Header("Opción Izquierda")]
    public string leftLabel = "Entrenar";
    [TextArea(2, 3)]
    public string leftDescription = "Te quedas a entrenar extra.";
    public StatConsequence leftConsequence;

    [Header("Opción Derecha")]
    public string rightLabel = "Fiesta";
    [TextArea(2, 3)]
    public string rightDescription = "Sales de fiesta con amigos.";
    public StatConsequence rightConsequence;

    [Header("Visual")]
    public Color leftColor  = new Color(0.2f, 0.8f, 0.3f);
    public Color rightColor = new Color(0.9f, 0.3f, 0.2f);
}
