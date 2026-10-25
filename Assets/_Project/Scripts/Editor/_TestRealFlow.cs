#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// SONDA DEL FLUJO REAL (Play Mode):
///   Carrera → (pista generada hasta la meta) → OnFinishReached → Penal.
///
/// Lo importante que verifica es si el TrackGenerator dejó segmentos de pista
/// SUPERPUESTOS sobre la arena (z > 300), porque taparían el arco con la cámara
/// del penal.
/// </summary>
public static class _TestRealFlow
{
    static RunnerGameManager gm;
    static StringBuilder log;
    static float t0;
    static bool registrado;
    static int paso;
    static string estadoPrevio = "";

    public static void Execute()
    {
        gm = Object.FindFirstObjectByType<RunnerGameManager>();
        log = new StringBuilder();
        log.AppendLine("========== FLUJO REAL ==========");
        if (gm == null) { Debug.LogError("[RealFlow] sin RunnerGameManager"); return; }

        t0 = Time.unscaledTime;
        paso = 0;
        estadoPrevio = "";
        if (!registrado) { EditorApplication.update += Tick; registrado = true; }
    }

    static void Tick()
    {
        float t = Time.unscaledTime - t0;
        if (gm == null) { Cerrar("GameManager perdido"); return; }

        // ── Paso 0: arrancar partida y neutralizar obstáculos (solo para la prueba) ──
        if (paso == 0)
        {
            paso = 1;
            gm.StartGame();
            log.AppendLine($"StartGame() → State={gm.State}");

            int obs = 0, spw = 0;
            foreach (var o in Object.FindObjectsByType<Obstacle>(FindObjectsSortMode.None))
                if (o != null) { o.enabled = false; obs++; }
            foreach (var s in Object.FindObjectsByType<ObstacleSpawner>(FindObjectsSortMode.None))
                if (s != null) { s.enabled = false; spw++; }
            log.AppendLine($"Obstáculos neutralizados: {obs}, spawners: {spw} (solo en esta prueba)");
        }

        // ── Paso 1: simular haber corrido hasta la meta (z≈299) ──
        if (paso == 1 && t > 0.8f)
        {
            paso = 2;
            var pc = PlayerController.Instance;
            if (pc != null)
            {
                Vector3 p = pc.transform.position;
                pc.Respawn(new Vector3(0f, 0.1f, 299f));
                pc.SetRunnerEnabled(false);
                log.AppendLine($"Jugador teletransportado a z=299 (antes z={p.z:F1}) → State={gm.State}");
            }
            else log.AppendLine("PlayerController.Instance == null");
        }

        // ── Paso 2: cruzar la meta ──
        if (paso == 2 && t > 2.2f)
        {
            paso = 3;
            log.AppendLine($"Antes de la meta: State={gm.State}");

            // Si el trigger de la meta no disparó con el teletransporte, lo forzamos.
            if (gm.State == GameState.Running)
            {
                gm.OnFinishReached();
                log.AppendLine("OnFinishReached() forzado (el trigger no dispara con teletransporte)");
            }
            log.AppendLine($"Después: State={gm.State}");

            ReportarSegmentosSobreLaArena();
        }

        // ── Registro de estados ──
        string est = gm.State.ToString();
        if (est != estadoPrevio) { log.AppendLine($"[t={t:F1}s] State={est}"); estadoPrevio = est; }

        var pmm = PenaltyMinigameManager.Instance;
        if (pmm != null && pmm.Estado == PenaltyMinigameManager.PenaltyState.Apuntando && paso == 3)
        {
            paso = 4;
            log.AppendLine($"[t={t:F1}s] Penales APUNTANDO. Camara={Camera.main.transform.position} " +
                           $"rot={Camera.main.transform.eulerAngles} fov={Camera.main.fieldOfView:F0}");
        }

        if (t > 16f) Cerrar("fin de la ventana de prueba");
    }

    static void ReportarSegmentosSobreLaArena()
    {
        // ¿Qué hay entre la cámara del penal (z=324.5) y el arco (z=339)?
        int total = 0, sobreArena = 0;

        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r == null) continue;
            total++;
            Bounds b = r.bounds;
            if (b.max.z > 300f && b.min.z < 346f && b.max.y > 0.5f && b.min.y < 8f &&
                b.max.x > -6f && b.min.x < 6f)
            {
                sobreArena++;
                if (sobreArena <= 25)
                    log.AppendLine($"   OBJETO EN LA LINEA DE VISION: '{GetPath(r.transform)}' " +
                                   $"z=[{b.min.z:F1},{b.max.z:F1}] y=[{b.min.y:F1},{b.max.y:F1}]");
            }
        }

        log.AppendLine($"Renderers totales={total}. Objetos en la línea de visión del arco={sobreArena}");
    }

    static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }

    static void Cerrar(string motivo)
    {
        if (registrado) { EditorApplication.update -= Tick; registrado = false; }
        log.AppendLine($"--- CIERRE: {motivo} (State={gm.State}) ---");
        Debug.Log(log.ToString());
    }
}
#endif
