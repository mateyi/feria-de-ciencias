using UnityEngine;
using System.Collections;

/// <summary>
/// Gestiona el flujo de las decisiones: pausa el juego, muestra el panel,
/// espera la elección del jugador y aplica las consecuencias.
/// </summary>
public class DecisionManager : MonoBehaviour
{
    public static DecisionManager Instance { get; private set; }

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
        if (data == null) return;
        _currentDecision = data;
        _currentPoint    = point;
        _waitingForChoice = true;
        _timer = decisionTimeLimit;

        RunnerGameManager.Instance?.PauseGame();
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
        RunnerGameManager.Instance?.ResumeGame();

        // Mostrar feedback breve
        string label = isLeft ? _currentDecision.leftLabel : _currentDecision.rightLabel;
        string desc  = isLeft ? _currentDecision.leftDescription : _currentDecision.rightDescription;
        StartCoroutine(ShowFeedback(label, desc, consequence));
    }

    private IEnumerator ShowFeedback(string label, string desc, StatConsequence c)
    {
        decisionUI?.ShowFeedback(label, desc, c);
        yield return new WaitForSeconds(2.5f);
        decisionUI?.HideFeedback();
    }
}
