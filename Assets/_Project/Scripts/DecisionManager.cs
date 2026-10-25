using UnityEngine;
using System.Collections;

/// <summary>
/// Gestiona el flujo de las decisiones: pausa el juego, muestra el panel,
/// espera la elección del jugador y aplica las consecuencias.
///
/// DESACTIVADO: reemplazado por el sistema de obstáculos.
/// Los carteles de decisión de la pista ya no se generan (ver TrackGenerator) y
/// este manager queda apagado por el flag "habilitado". El código se conserva
/// entero por si se quiere reusar más adelante.
/// </summary>
public class DecisionManager : MonoBehaviour
{
    public static DecisionManager Instance { get; private set; }

    [Header("Sistema de decisiones (DESACTIVADO)")]
    [Tooltip("Apagado: los carteles de decisión fueron reemplazados por los obstáculos.")]
    [SerializeField] private bool habilitado = false;

    [Header("UI de decisión")]
    [SerializeField] private DecisionUI decisionUI;

    [Header("Tiempo para elegir (segundos)")]
    [SerializeField] private float decisionTimeLimit = 5f;

    private DecisionData _currentDecision;
    private DecisionPoint _currentPoint;
    private bool _waitingForChoice;
    private float _timer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        // DESACTIVADO: reemplazado por el sistema de obstáculos.
        if (!habilitado) return;

        if (!_waitingForChoice) return;

        _timer -= Time.unscaledDeltaTime;
        decisionUI?.UpdateTimer(_timer / decisionTimeLimit);

        if (_timer <= 0f)
        {
            // Tiempo agotado: elegir izquierda por defecto
            Choose(false);
        }
    }

    public void ShowDecision(DecisionData data, DecisionPoint point)
    {
        // DESACTIVADO: reemplazado por el sistema de obstáculos.
        if (!habilitado) return;

        if (data == null) return;
        _currentDecision = data;
        _currentPoint    = point;
        _waitingForChoice = true;
        _timer = decisionTimeLimit;

        // Freno del mundo sin panel: la pausa del jugador (Fase E) es aparte.
        RunnerGameManager.Instance?.CongelarTiempo();
        decisionUI?.Show(data);
    }

    /// <summary>isLeft = true → opción izquierda, false → opción derecha</summary>
    public void Choose(bool isLeft)
    {
        if (!_waitingForChoice) return;
        _waitingForChoice = false;

        StatConsequence consequence = isLeft
            ? _currentDecision.leftConsequence
            : _currentDecision.rightConsequence;

        PlayerStats.Instance?.ApplyConsequence(consequence);
        _currentPoint?.Resolve();

        decisionUI?.Hide();
        RunnerGameManager.Instance?.ReanudarTiempo();

        // Cartel minimalista con los tres atributos que movió la elección.
        string label = isLeft ? _currentDecision.leftLabel : _currentDecision.rightLabel;
        string desc  = isLeft ? _currentDecision.leftDescription : _currentDecision.rightDescription;
        bool positivo = isLeft ? _currentDecision.leftIsPositive : _currentDecision.rightIsPositive;
        StartCoroutine(ShowFeedback(label, desc, consequence, positivo));
    }

    private IEnumerator ShowFeedback(string label, string desc, StatConsequence c, bool positivo)
    {
        decisionUI?.ShowFeedback(label, desc, c, positivo);
        yield return new WaitForSeconds(2.5f);
        decisionUI?.HideFeedback();
    }
}
