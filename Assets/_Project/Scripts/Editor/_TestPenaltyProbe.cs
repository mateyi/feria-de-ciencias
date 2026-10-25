#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>
/// SONDA DE PRUEBA del minijuego de penales. Se ejecuta EN PLAY MODE.
///
///  1. Llama a IniciarModoPenalDebug() (equivale a la tecla P).
///  2. Espera a que el manager entre en estado Apuntando.
///  3. Espera ~8 s (ventana para sacar captura) y patea con PatearDebug(zona).
///  4. Loguea cada cambio de estado, la posición del arquero y de la pelota.
///  5. Corre hasta FinDeNivel o hasta agotar el tiempo.
///
/// Argumento opcional: {"accuracy": 0.0}  → 0 = siempre GOL, 1 = siempre ATAJADA.
/// </summary>
public static class _TestPenaltyProbe
{
    static PenaltyMinigameManager pmm;
    static StringBuilder log;
    static float t0;
    static float kickAt = -1f;
    static float timeout = 45f;
    static bool registrado;
    static string ultimoEstado = "";
    static int disparos = 0;
    static int zonaActual = 0;
    static bool reticleVerificado;

    public static void Execute(JObject args)
    {
        pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        log = new StringBuilder();
        log.AppendLine("========== PRUEBA PENALES ==========");

        if (pmm == null) { Debug.LogError("[Probe] No hay PenaltyMinigameManager"); return; }

        float accuracy = -1f;
        if (args != null && args["accuracy"] != null) accuracy = args["accuracy"].Value<float>();

        if (accuracy >= 0f)
        {
            // Forzamos la precisión del arquero para que la prueba sea determinista.
            var so = new SerializedObject(pmm);
            so.FindProperty("goalkeeperAccuracy").floatValue = accuracy;
            so.FindProperty("accuracyPorNivel").floatValue  = 0f;
            so.FindProperty("tiempoMensajeGol").floatValue  = 0.4f;
            so.FindProperty("tiempoMensajeAtajada").floatValue = 0.4f;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine($"accuracy forzada = {accuracy} (0=siempre GOL, 1=siempre ATAJADA)");
        }

        log.AppendLine($"Estado inicial: {pmm.Estado}");
        t0 = Time.unscaledTime;
        kickAt = -1f;
        disparos = 0;
        zonaActual = 0;
        reticleVerificado = false;
        ultimoEstado = "";

        if (!registrado) { EditorApplication.update += Tick; registrado = true; }

        pmm.IniciarModoPenalDebug();
        log.AppendLine("IniciarModoPenalDebug() invocado (equivale a la tecla P)");
    }

    static void Tick()
    {
        if (pmm == null) { Cerrar("PenaltyMinigameManager destruido"); return; }

        float t = Time.unscaledTime - t0;

        // ── Registro de transiciones ──────────────────────────────────────────
        string estado = pmm.Estado.ToString();
        if (estado != ultimoEstado)
        {
            log.AppendLine($"[t={t:F2}s] Estado: {estado}   (intentos={pmm.IntentosRestantes}, " +
                           $"arquero={PosArquero()}, pelota={PosPelota()})");
            ultimoEstado = estado;
        }

        // ── Primera vez en Apuntando: verificar la mira y programar el tiro ───
        if (pmm.Estado == PenaltyMinigameManager.PenaltyState.Apuntando && kickAt < 0f)
        {
            VerificarMira();
            kickAt = t + 8f;   // ventana amplia para sacar la captura de pantalla
            log.AppendLine($"[t={t:F2}s] Apuntando alcanzado. Pateo programado en t={kickAt:F1}s");
        }

        // ── Patear ────────────────────────────────────────────────────────────
        if (pmm.Estado == PenaltyMinigameManager.PenaltyState.Apuntando && kickAt > 0f && t >= kickAt)
        {
            disparos++;
            zonaActual = (zonaActual + 2) % 6;   // cicla zonas distintas
            log.AppendLine($"[t={t:F2}s] >>> PATEAR zona {zonaActual}");
            var so = new SerializedObject(pmm);
            so.FindProperty("aimPlane"); // solo para no dejar el 'so' sin uso
            pmm.PatearDebug(zonaActual);
            kickAt = t + 2f;                     // próximo tiro (si hay reintento)
        }

        // ── Fin ───────────────────────────────────────────────────────────────
        if (pmm.Estado == PenaltyMinigameManager.PenaltyState.FinDeNivel)
        {
            Cerrar($"FinDeNivel alcanzado tras {disparos} tiro(s)");
            return;
        }

        if (t > timeout) Cerrar($"TIMEOUT a los {timeout}s (estado={pmm.Estado})");
    }

    static void VerificarMira()
    {
        if (reticleVerificado) return;
        reticleVerificado = true;

        var ret = GameObject.Find("Canvas/PenaltyHUDPanel/AimReticle");
        if (ret == null) { log.AppendLine("   MIRA: no encontrada por Find (panel inactivo?)"); return; }

        var img = ret.GetComponent<Image>();
        log.AppendLine($"   MIRA: activeInHierarchy={ret.activeInHierarchy} " +
                       $"sprite={(img != null && img.sprite != null ? img.sprite.name : "<NULL>")} " +
                       $"scale={ret.transform.localScale}");

        var hud = GameObject.Find("Canvas/PenaltyHUDPanel");
        if (hud != null)
        {
            var att = hud.transform.Find("AttemptsText");
            var ins = hud.transform.Find("InstructionText");
            log.AppendLine($"   HUD activo={hud.activeInHierarchy} " +
                           $"attempts={(att != null ? att.GetComponent<TMPro.TMP_Text>()?.text : "?")} " +
                           $"instr={(ins != null ? ins.GetComponent<TMPro.TMP_Text>()?.text : "?")}");
        }

        var cam = Camera.main;
        if (cam != null)
            log.AppendLine($"   CAMARA pos={cam.transform.position} rot={cam.transform.eulerAngles} " +
                           $"followActivo={(cam.GetComponent<CameraFollow>() != null && cam.GetComponent<CameraFollow>().enabled)}");
    }

    static string PosArquero()
    {
        var gk = Object.FindFirstObjectByType<GoalkeeperAI>();
        return gk != null ? gk.transform.position.ToString("F2") : "?";
    }

    static string PosPelota()
    {
        var b = GameObject.Find("PenaltyArena_Instance/Ball");
        return b != null ? b.transform.position.ToString("F2") : "?";
    }

    static void Cerrar(string motivo)
    {
        if (registrado) { EditorApplication.update -= Tick; registrado = false; }
        log.AppendLine($"--- CIERRE: {motivo} ---");
        log.AppendLine($"Estado final: {(pmm != null ? pmm.Estado.ToString() : "?")}");
        Debug.Log(log.ToString());
    }
}
#endif
