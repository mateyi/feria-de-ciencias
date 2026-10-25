using UnityEngine;
using UnityEditor;
using System.Text;
using System.Reflection;
using System.Collections;

/// <summary>
/// Sonda de verificación de la FASE A (se corre desde execute_script).
///
/// Acciones (campo "action" del JSON):
///   muestra  → foto instantánea del estado: segmentos vivos, panel de decisión,
///              objetos con "Decision" en el nombre, jugador y estado del juego.
///   arrancar → arranca o reinicia la partida.
///
/// No guarda estado entre llamadas: cada "muestra" lee la escena en el momento.
/// </summary>
public static class _VerFaseA
{
    public static void Execute(Newtonsoft.Json.Linq.JObject args)
    {
        string action = args != null && args["action"] != null ? args["action"].ToString() : "muestra";

        if (action == "arrancar")
        {
            Time.timeScale = 1f;
            if (RunnerGameManager.Instance == null) { Debug.LogError("[VerFaseA] no hay RunnerGameManager."); return; }
            RunnerGameManager.Instance.StartGame();
            Debug.Log($"[VerFaseA] partida iniciada. Estado={RunnerGameManager.Instance.State}");
            return;
        }

        Muestra();
    }

    private static void Muestra()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== MUESTRA FASE A ===");

        // ── Segmentos vivos ──────────────────────────────────────────────────
        var gen = TrackGenerator.Instance;
        if (gen == null)
        {
            sb.AppendLine("TrackGenerator: null");
        }
        else
        {
            var campo = typeof(TrackGenerator).GetField("_activeSegments", BindingFlags.NonPublic | BindingFlags.Instance);
            var lista = campo != null ? campo.GetValue(gen) as IList : null;
            int n = lista != null ? lista.Count : -1;
            sb.AppendLine($"segmentos vivos: {n}");
            if (lista != null)
            {
                var vistos = new System.Collections.Generic.HashSet<string>();
                foreach (var item in lista)
                {
                    var seg = item as TrackSegment;
                    if (seg == null) continue;
                    if (vistos.Add(seg.name)) sb.AppendLine($"  - {seg.name}");
                }
            }
        }

        // ── Panel de decisión ────────────────────────────────────────────────
        var dui = Object.FindFirstObjectByType<DecisionUI>(FindObjectsInactive.Include);
        sb.AppendLine(dui != null
            ? $"DecisionPanel: existe | activeSelf={dui.gameObject.activeSelf} activeInHierarchy={dui.gameObject.activeInHierarchy}"
            : "DecisionPanel: no encontrado");

        // ── Objetos con "Decision" ───────────────────────────────────────────
        int conDecision = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!t.name.Contains("Decision")) continue;
            conDecision++;
            if (conDecision <= 6) sb.AppendLine($"  objeto: {t.name} active={t.gameObject.activeInHierarchy}");
        }
        sb.AppendLine($"objetos con 'Decision' en el nombre: {conDecision}");

        // ── Jugador y estado ─────────────────────────────────────────────────
        var pc = PlayerController.Instance;
        var ps = PlayerStats.Instance;
        var rm = RunnerGameManager.Instance;
        sb.AppendLine($"jugador z: {(pc != null ? pc.transform.position.z.ToString("0.0") : "sin jugador")}" +
                      $" | energia: {(ps != null ? ps.energy.ToString("0") : "-")}" +
                      $" | estado: {(rm != null ? rm.State.ToString() : "-")}" +
                      $" | timeScale: {Time.timeScale:0.##}");

        Debug.Log(sb.ToString());
    }
}
