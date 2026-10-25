#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

/// <summary>
/// PILOTO AUTOMÁTICO del minijuego de penales (v3) — dirigido por ESTADO.
///
/// En lugar de seguir un guion de tiempos fijos, mira el estado real del
/// PenaltyMinigameManager y actúa en consecuencia:
///
///   - Cuando entra en "Apuntando", patea a una zona concreta.
///   - Cuando termina en "FinDeNivel", verifica el GameState y sale.
///
/// Se corre en dos modos para cubrir los dos desenlaces:
///   ProbarGol()   → arquero con precisión 0  → siempre GOL   → LevelComplete
///   ProbarFallo() → arquero con precisión 1  → siempre ATAJADA → 3 intentos → GameOver
///
/// NOTA: el tiro se lanza con PatearDebug() en vez de simular el mouse. El Editor
/// sobrescribe cada frame el estado del mouse real, así que un clic encolado por
/// código nunca llega al juego. El clic de verdad lo lee Update() con
/// Mouse.current.leftButton.wasPressedThisFrame, que es la API del Input System
/// nuevo que usa este proyecto.
/// </summary>
public static class _PilotoPenal
{
    private static float _t0;
    private static float _proximoTiro;
    private static int   _tiros;
    private static string _etiqueta;
    private static StringBuilder _log;
    private static bool  _logueadoApuntando;
    private static bool  _logueadoResultado;
    private static string _estadoAnterior = "";

    // ─── Puntos de entrada ────────────────────────────────────────────────────

    /// <summary>Arquero que nunca acierta: el tiro siempre termina en GOL.</summary>
    public static void ProbarGol()   => Iniciar(0f, "GOL");

    /// <summary>Arquero que siempre acierta: 3 atajadas seguidas → Game Over.</summary>
    public static void ProbarFallo() => Iniciar(1f, "FALLO");

    private static void Iniciar(float precisionArquero, string etiqueta)
    {
        _log = new StringBuilder();
        _etiqueta = etiqueta;
        _log.AppendLine($"========== PILOTO {etiqueta} ==========");

        var pmm = PenaltyMinigameManager.Instance;
        if (pmm == null) { Debug.LogError("[Piloto] No hay PenaltyMinigameManager."); return; }

        // Forzamos la precisión del arquero para que el resultado sea determinista.
        var so = new SerializedObject(pmm);
        var prop = so.FindProperty("goalkeeperAccuracy");
        if (prop != null)
        {
            prop.floatValue = precisionArquero;
            so.ApplyModifiedPropertiesWithoutUndo();
            _log.AppendLine($"Precisión del arquero forzada a {precisionArquero}");
        }

        _t0 = Time.realtimeSinceStartup;
        _proximoTiro = 0f;
        _tiros = 0;
        _logueadoApuntando = false;
        _logueadoResultado = false;
        _estadoAnterior = "";

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    // ─── Bucle principal ──────────────────────────────────────────────────────

    private static void Tick()
    {
        if (!Application.isPlaying) { Terminar("Play Mode terminó"); return; }

        float t = Time.realtimeSinceStartup - _t0;
        var pmm = PenaltyMinigameManager.Instance;
        var gm  = RunnerGameManager.Instance;

        if (pmm == null) { Terminar("PenaltyMinigameManager NULL"); return; }

        // ── Arranque ─────────────────────────────────────────────────────────
        if (pmm.Estado == PenaltyMinigameManager.PenaltyState.Inactivo)
        {
            if (t < 30f && _tiros == 0 && t > 0.3f)
            {
                _log.AppendLine($"[{t:F1}] Arrancando partida y saltando al penal...");
                pmm.IniciarModoPenalDebug();
            }
            return;
        }

        string estado = pmm.Estado.ToString();

        // ── Registrar cada cambio de estado ──────────────────────────────────
        if (estado != _estadoAnterior)
        {
            _log.AppendLine($"[{t:F1}] ESTADO → {estado}   (GameManager={(gm != null ? gm.State.ToString() : "NULL")})");
            _estadoAnterior = estado;
        }

        switch (pmm.Estado)
        {
            // ── En el arco: patear ───────────────────────────────────────────
            case PenaltyMinigameManager.PenaltyState.Apuntando:
                if (!_logueadoApuntando)
                {
                    _logueadoApuntando = true;
                    _logueadoResultado = false;   // permite capturar el próximo resultado
                    DumpUI(pmm);
                    Capturar($"{_etiqueta}_1_apuntando");
                }

                if (t > _proximoTiro)
                {
                    _tiros++;
                    int zona = 2; // arriba-derecha
                    _log.AppendLine($"[{t:F1}] TIRO #{_tiros} → zona {zona} (arriba-derecha). Intentos restantes={pmm.IntentosRestantes}");
                    pmm.PatearDebug(zona);
                    _proximoTiro = t + 3.0f;   // margen para que se resuelva
                    _logueadoApuntando = false;
                }
                break;

            // ── Resultado: capturar una vez ──────────────────────────────────
            case PenaltyMinigameManager.PenaltyState.Gol:
            case PenaltyMinigameManager.PenaltyState.Atajada:
                if (!_logueadoResultado)
                {
                    _logueadoResultado = true;
                    DumpPelota();
                    Capturar($"{_etiqueta}_2_resultado_{estado}");
                }
                break;

            // ── Fin del minijuego ────────────────────────────────────────────
            case PenaltyMinigameManager.PenaltyState.FinDeNivel:
                _log.AppendLine($"[{t:F1}] FIN DEL MINIJUEGO. Tiros ejecutados={_tiros}");
                _log.AppendLine($"[{t:F1}] GameManager.State = {(gm != null ? gm.State.ToString() : "NULL")} | Score={(gm != null ? gm.Score : -1)}");
                Capturar($"{_etiqueta}_3_final");
                Terminar("nivel cerrado");
                return;
        }

        // ── Timeout de seguridad ─────────────────────────────────────────────
        if (t > 45f)
            Terminar("TIMEOUT — el flujo no terminó");
    }

    // ─── Utilidades ───────────────────────────────────────────────────────────

    private static void DumpUI(PenaltyMinigameManager pmm)
    {
        var hud = PenaltyHUD.Instance;
        _log.AppendLine($"  HUD: {(hud != null ? (hud.gameObject.activeInHierarchy ? "visible" : "OCULTO") : "NULL")}");

        var so = new SerializedObject(pmm);
        var ret = so.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;
        if (ret != null)
        {
            var img = ret.GetComponent<UnityEngine.UI.Image>();
            _log.AppendLine($"  Mira: visible={ret.gameObject.activeInHierarchy} pos={ret.position} " +
                            $"sprite={(img != null && img.sprite != null ? img.sprite.name : "NULL")}");
        }

        var cam = Camera.main;
        if (cam != null)
            _log.AppendLine($"  Cámara: pos={cam.transform.position} rot={cam.transform.eulerAngles}");
    }

    private static void DumpPelota()
    {
        var ball = GameObject.Find("PenaltyArena_Instance/Ball");
        if (ball != null) _log.AppendLine($"  Pelota: pos={ball.transform.position}");

        var gk = Object.FindFirstObjectByType<GoalkeeperAI>();
        if (gk != null)
            _log.AppendLine($"  Arquero: pos={gk.transform.position} rot={gk.transform.eulerAngles} eligió={gk.ChosenZone}");
    }

    private static void Capturar(string nombre)
    {
        try
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Capturas");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, nombre + ".png"));
        }
        catch (System.Exception e) { _log.AppendLine($"  ⚠ captura: {e.Message}"); }
    }

    private static void Terminar(string motivo)
    {
        EditorApplication.update -= Tick;
        _log.AppendLine($"========== FIN PILOTO {_etiqueta} ({motivo}) ==========");
        Debug.Log(_log.ToString());
    }
}
#endif
