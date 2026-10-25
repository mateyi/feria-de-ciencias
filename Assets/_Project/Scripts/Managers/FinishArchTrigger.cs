using UnityEngine;

/// <summary>
/// Disparador del arco de "META".
///
/// BUG CORREGIDO: antes solo llamaba a TrackGenerator.StopGeneratingAfter()
/// pero nunca detenía el movimiento del jugador ni cambiaba el GameState,
/// por lo que el personaje seguía corriendo y chocaba con la pared del túnel
/// o pasaba de largo la arena de penales.
///
/// SOLUCIÓN: ahora llama a RunnerGameManager.OnFinishReached(), que:
///   1. Cambia el estado a PenaltyMinigame.
///   2. Llama a PlayerController.SetRunnerEnabled(false) para detener el movimiento.
///   3. Notifica a PenaltyMinigameManager para que inicie la secuencia de penal.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public class FinishArchTrigger : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float finishZOffset    = 1f;
    [SerializeField] private float cleanupZPadding  = 10f;

    [Header("Confetti (opcional)")]
    [SerializeField] public ParticleSystem confetti;

    // Exponer getters para el generador de prefabs del editor
    public float FinishZOffset    => finishZOffset;
    public float CleanupZPadding  => cleanupZPadding;

    public void SetConfetti(ParticleSystem ps) => confetti = ps;
    public void SetOffsets(float finishOffset, float cleanupPadding)
    {
        finishZOffset   = finishOffset;
        cleanupZPadding = cleanupPadding;
    }

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private bool _fired;

    /// <summary>
    /// Vuelve a armar el arco cuando el jugador quedó detrás de la META otra vez
    /// (por ejemplo al reiniciar la partida). Sin esto el trigger dispara una
    /// sola vez por sesión y la segunda partida nunca llega al penal.
    /// </summary>
    private void Update()
    {
        if (!_fired) return;

        var gm = RunnerGameManager.Instance;
        if (gm == null || gm.State != GameState.Running) return;
        if (PlayerController.Instance == null) return;

        if (PlayerController.Instance.transform.position.z < transform.position.z - 5f)
            _fired = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        try
        {
            if (_fired) return;
            if (other == null) return;
            if (PlayerController.Instance == null) return;
            if (other.gameObject != PlayerController.Instance.gameObject) return;

            _fired = true;

            // ── 1. Confetti ──────────────────────────────────────────────────
            if (confetti != null) confetti.Play(true);

            // ── 2. Detener la generación de pista ────────────────────────────
            float finishZ = transform.position.z + finishZOffset;
            TrackGenerator.Instance?.StopGeneratingAfter(finishZ, cleanupZPadding);

            // ── 3. CORRECCIÓN DEL BUG: notificar al GameManager ──────────────
            // Esto detiene el movimiento del runner y activa el minijuego de penales.
            RunnerGameManager.Instance?.OnFinishReached();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FinishArchTrigger] Error al procesar la meta: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
