using UnityEngine;

/// <summary>
/// IA simple del arquero rival para el minijuego de penales.
/// Elige aleatoriamente reaccionar hacia una zona (izq/centro/der) y dispara
/// el trigger correspondiente en el Animator. Expone la zona elegida para
/// que la lógica de gol pueda comparar contra la puntería del jugador.
/// </summary>
[RequireComponent(typeof(Animator))]
public class GoalkeeperAI : MonoBehaviour
{
    public enum SaveZone { Left, Center, Right }

    [Header("Animator triggers")]
    [SerializeField] private string diveLeftTrigger = "DiveLeft";
    [SerializeField] private string diveRightTrigger = "DiveRight";
    [SerializeField] private string centerSaveTrigger = "CenterSave";
    [SerializeField] private string resetTrigger = "ToIdle";

    private Animator _animator;

    public SaveZone ChosenZone { get; private set; }

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>Reacciona a un penal. Devuelve la zona que cubrió el arquero.</summary>
    public SaveZone React()
    {
        ChosenZone = (SaveZone)Random.Range(0, 3);
        TriggerZone(ChosenZone);
        return ChosenZone;
    }

    /// <summary>Reacciona hacia una zona específica (para testeo o dificultad).</summary>
    public void React(SaveZone zone)
    {
        ChosenZone = zone;
        TriggerZone(zone);
    }

    private void TriggerZone(SaveZone zone)
    {
        if (_animator == null) return;
        switch (zone)
        {
            case SaveZone.Left:   _animator.SetTrigger(diveLeftTrigger);  break;
            case SaveZone.Right:  _animator.SetTrigger(diveRightTrigger); break;
            case SaveZone.Center: _animator.SetTrigger(centerSaveTrigger); break;
        }
    }

    public void ResetToIdle()
    {
        if (_animator == null) return;
        _animator.SetTrigger(resetTrigger);
    }
}
