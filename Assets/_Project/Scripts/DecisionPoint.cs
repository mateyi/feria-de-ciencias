using UnityEngine;

/// <summary>
/// Punto de decisión en la pista. Cuando el jugador entra en la zona trigger,
/// muestra el panel de decisión con dos opciones (izquierda/derecha).
/// El jugador elige moviéndose al carril correspondiente.
/// </summary>
public class DecisionPoint : MonoBehaviour
{
    [Header("Decisión")]
    [SerializeField] private DecisionData decisionData;

    [Header("Zona de activación")]
    [SerializeField] private float activationDistance = 15f;

    private bool _triggered;
    private bool _resolved;

    private void Update()
    {
        if (_triggered || _resolved) return;
        if (PlayerController.Instance == null) return;

        float dist = transform.position.z - PlayerController.Instance.transform.position.z;
        if (dist > 0 && dist < activationDistance)
        {
            _triggered = true;
            DecisionManager.Instance?.ShowDecision(decisionData, this);
        }
    }

    public void Resolve()
    {
        _resolved = true;
    }
}
