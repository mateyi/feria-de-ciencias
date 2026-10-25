#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// PRUEBA AUTOMÁTICA DEL MINIJUEGO (solo Editor).
///
/// Se compila en EDIT MODE y luego se entra a Play Mode, para que el dominio no
/// se recargue en medio de la prueba.
///
/// Hace dos pasadas, forzando la precisión del arquero para cubrir las DOS ramas:
///   FASE A: precisión = 1  → el arquero siempre acierta → 3 ATAJADAS → GameOver
///   FASE B: precisión = 0  → el arquero nunca acierta → GOL → LevelComplete
///
/// En cada estado deja evidencia en el log: estado de la máquina, mira visible,
/// zona resaltada, posición de la pelota y del arquero, textos del HUD.
/// </summary>
public static class _PenaltyPlayTest
{
    private static PenaltyMinigameManager _pm;
    private static RunnerGameManager     _gm;
    private static SerializedObject      _pmSO;
    private static StringBuilder         _log;

    private static int   _fase;              // 1 = A (atajadas), 2 = B (gol), 3 = terminado
    private static float _tEspera;
    private static int   _kicksHechos;
    private static int   _kicksPlaneados;
    private static bool  _activo;
    private static float _accuracyOriginal;
    private static string _estadoAnterior = "";

    public static void Execute()
    {
        _pm   = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        _gm   = Object.FindFirstObjectByType<RunnerGameManager>();
        _pmSO = new SerializedObject(_pm);
        _log  = new StringBuilder();

        _accuracyOriginal = _pmSO.FindProperty("goalkeeperAccuracy").floatValue;

        _log.AppendLine("########## PRUEBA DEL MINIJUEGO DE PENALES ##########");
        _log.AppendLine($"Precisión original del arquero: {_accuracyOriginal:F2}");
        _log.AppendLine($"Time.timeScale={Time.timeScale}  Cursor.visible={Cursor.visible}  " +
                        $"AudioListener.pause={AudioListener.pause}");

        // ── Arrancamos la FASE A ──────────────────────────────────────────────
        FijarPrecision(1f);
        _log.AppendLine("\n===== FASE A: arquero perfecto → deben ser 3 atajadas y GameOver =====");

        _gm.StartGame();
        _gm.OnFinishReached();          // simula cruzar la meta → dispara el penal

        _fase          = 1;
        _tEspera       = 0f;
        _kicksHechos   = 0;
        _kicksPlaneados = 3;
        _estadoAnterior = "";
        _activo        = true;

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void FijarPrecision(float v)
    {
        _pmSO.Update();
        _pmSO.FindProperty("goalkeeperAccuracy").floatValue = v;
        _pmSO.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Tick()
    {
        if (!_activo) return;

        if (!Application.isPlaying) { Terminar("salió de Play Mode"); return; }

        _tEspera += Time.unscaledDeltaTime;

        string estado = _pm.Estado.ToString();
        if (estado != _estadoAnterior)
        {
            _estadoAnterior = estado;

            var reticle = _pmSO.FindProperty("aimReticle").objectReferenceValue as RectTransform;
            bool miraVisible = reticle != null && reticle.gameObject.activeInHierarchy;
            string hudResult = TextoResultado();
            string gk = PosicionArquero();
            string ball = PosicionPelota();

            _log.AppendLine($"[{Time.time,5:F2}s] Estado → {estado,-22} " +
                            $"intentos={_pm.IntentosRestantes} mira={(miraVisible ? "VISIBLE" : "oculta"),-7} " +
                            $"GK={gk} pelota={ball} resultado='{hudResult}'");

            _tEspera = 0f;
        }

        // ── Cuando está apuntando: esperamos un toque y pateamos ─────────────
        if (_pm.Estado == PenaltyMinigameManager.PenaltyState.Apuntando &&
            _kicksHechos < _kicksPlaneados && _tEspera > 0.5f)
        {
            int zona = _kicksHechos % 2 == 0 ? 0 : 5;   // alterna arriba-izq / abajo-der
            _log.AppendLine($"           → PATEAR a zona {zona} (intento {_kicksHechos + 1}/{_kicksPlaneados})");
            _kicksHechos++;
            _tEspera = 0f;
            _pm.PatearDebug(zona);
        }

        // ── FASE A terminada: el nivel se cerró ──────────────────────────────
        if (_fase == 1 && _pm.Estado == PenaltyMinigameManager.PenaltyState.FinDeNivel)
        {
            _log.AppendLine($"           → FASE A OK. GameState={_gm.State} " +
                            $"(esperado: GameOver tras los 3 intentos)");
            _log.AppendLine($"           → HUD oculto: {(TextoResultado() == "" ? "sí" : "no")}");

            // Reiniciamos para la FASE B con arquero que nunca acierta.
            _fase = 2;
            FijarPrecision(0f);
            _log.AppendLine("\n===== FASE B: arquero nulo → gol y LevelComplete =====");

            _gm.StartGame();
            _gm.OnFinishReached();

            _kicksHechos    = 0;
            _kicksPlaneados = 1;
            _tEspera        = 0f;
            _estadoAnterior = "";
        }
        else if (_fase == 2 && _pm.Estado == PenaltyMinigameManager.PenaltyState.FinDeNivel)
        {
            _log.AppendLine($"           → FASE B OK. GameState={_gm.State} (esperado: LevelComplete)");
            Terminar("prueba completa");
        }

        // ── Red de seguridad: si algo se cuelga, cortamos a los 30 s ─────────
        if (Time.time > 40f) Terminar("timeout de seguridad");
    }

    private static string TextoResultado()
    {
        var hud = Object.FindFirstObjectByType<PenaltyHUD>(FindObjectsInactive.Include);
        if (hud == null) return "sin HUD";
        var hso = new SerializedObject(hud);
        var rt = hso.FindProperty("resultText").objectReferenceValue as TMPro.TMP_Text;
        if (rt == null) return "sin resultText";
        return rt.gameObject.activeInHierarchy ? rt.text : "";
    }

    private static string PosicionArquero()
    {
        var gk = Object.FindFirstObjectByType<GoalkeeperAI>(FindObjectsInactive.Include);
        if (gk == null) return "n/a";
        Vector3 p = gk.transform.position;
        return $"({p.x:F2},{p.y:F2})";
    }

    private static string PosicionPelota()
    {
        var ball = GameObject.Find("PenaltyArena_Instance/Ball");
        if (ball == null) return "n/a";
        Vector3 p = ball.transform.position;
        return $"({p.x:F2},{p.y:F2},{p.z:F1})";
    }

    private static void Terminar(string motivo)
    {
        _activo = false;
        EditorApplication.update -= Tick;

        _log.AppendLine($"\n########## FIN ({motivo}) ##########");
        Debug.Log(_log.ToString());

        // Dejamos la precisión como estaba.
        if (_pm != null) FijarPrecision(_accuracyOriginal);
    }
}
#endif
