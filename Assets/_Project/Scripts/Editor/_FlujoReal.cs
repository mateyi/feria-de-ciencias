#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// SONDA TEMPORAL DEL FLUJO REAL (Play Mode):
///   Carrera → META (z=300) → túnel (309..327) → penal.
///
/// Arranca la partida, deja al jugador cerca de la META y va registrando el
/// estado del GameManager, la Z del jugador y el estado del minijuego de penales
/// para comprobar que el penal arranca recién al llegar al túnel.
///
/// Se neutralizan los obstáculos para que la prueba no dependa de esquivarlos.
/// </summary>
public static class _FlujoReal
{
    static RunnerGameManager gm;
    static StringBuilder log;
    static float t0;
    static float tUltimoLog;
    static bool registrado;
    static int paso;
    static GameState prev;

    public static void Execute()
    {
        gm = Object.FindFirstObjectByType<RunnerGameManager>();
        if (gm == null) { Debug.LogError("[Flujo] No encontré RunnerGameManager."); return; }

        log = new StringBuilder();
        log.AppendLine("========== FLUJO REAL: carrera → META → túnel → penal ==========");

        t0 = Time.unscaledTime;
        tUltimoLog = -1f;
        paso = 0;
        prev = gm.State;

        if (!registrado) { EditorApplication.update += Tick; registrado = true; }
    }

    static void Tick()
    {
        if (gm == null) { Cerrar("GameManager perdido"); return; }

        float t = Time.unscaledTime - t0;

        // ── Paso 0: arrancar partida ────────────────────────────────────────
        if (paso == 0)
        {
            paso = 1;
            gm.StartGame();
            log.AppendLine($"[t={t:0.0}s] StartGame() → State={gm.State}");

            int obs = 0;
            foreach (var o in Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
                if (o != null) { o.enabled = false; obs++; }
            log.AppendLine($"Obstáculos neutralizados: {obs} (solo para esta prueba)");
        }

        // ── Paso 1: acercar al jugador a la META ────────────────────────────
        if (paso == 1 && t > 1.0f)
        {
            paso = 2;
            var pc = PlayerController.Instance;
            if (pc != null)
            {
                pc.Respawn(new Vector3(0f, 0.2f, 294f));
                log.AppendLine($"[t={t:0.0}s] Jugador teletransportado a z=294 → State={gm.State}");
            }
            else log.AppendLine("[ERROR] PlayerController.Instance == null");
        }

        // ── Registro de cambios de estado ───────────────────────────────────
        if (gm.State != prev)
        {
            log.AppendLine($"[t={t:0.0}s] ESTADO {prev} → {gm.State}");
            prev = gm.State;
        }

        // ── Muestreo cada 1 s ───────────────────────────────────────────────
        if (t - tUltimoLog >= 1f)
        {
            tUltimoLog = t;
            var pc = PlayerController.Instance;
            var pmm = PenaltyMinigameManager.Instance;

            string z = pc != null ? pc.transform.position.z.ToString("F1") : "?";
            string correr = pc != null ? pc.RunnerEnabled.ToString() : "?";
            string penal = pmm != null ? pmm.Estado.ToString() : "?";

            log.AppendLine($"[t={t:0.0}s] z={z} runnerEnabled={correr} State={gm.State} " +
                           $"penal={penal} intentos={(pmm != null ? pmm.IntentosRestantes : -1)}");
        }

        if (t > 14f) Cerrar("fin de la ventana de prueba");
    }

    static void Cerrar(string motivo)
    {
        if (registrado) { EditorApplication.update -= Tick; registrado = false; }
        log.AppendLine($"--- CIERRE: {motivo} (State={gm.State}) ---");
        Debug.Log(log.ToString());
    }
}
#endif
