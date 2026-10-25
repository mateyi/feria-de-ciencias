#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Reflection;
using System.Text;

/// <summary>
/// PRUEBA DEL APUNTADO CON MOUSE REAL — versión SÍNCRONA (solo Editor).
///
/// No usa PatearDebug ni callbacks diferidos: inyecta estados reales de mouse en
/// el Input System, los procesa con InputSystem.Update() y lee el resultado en el
/// mismo instante. Ejercita el camino exacto del stand:
///
///   Mouse.current.position                        →  ActualizarMira() → zona + resaltado
///   Mouse.current.leftButton.wasPressedThisFrame  →  Update() → ConfirmarPateada()
///
/// Primera pasada (penal inactivo): arranca la partida y salta a la arena.
/// Segunda pasada (estado Apuntando): prueba las 6 zonas y el click.
/// </summary>
public static class _PenaltyMouseTest
{
    public static void Execute()
    {
        var sb    = new StringBuilder();
        var pm    = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        var cam   = Camera.main;
        var mouse = Mouse.current;

        sb.AppendLine("########## APUNTADO CON MOUSE REAL (síncrono) ##########");
        sb.AppendLine($"Mouse.current = {(mouse != null ? mouse.displayName : "<NULL>")}");

        if (pm == null || cam == null || mouse == null)
        {
            sb.AppendLine("*** Faltan piezas: no se puede probar ***");
            Debug.Log(sb.ToString());
            return;
        }

        var so = new SerializedObject(pm);
        var tz = so.FindProperty("targetZones");
        var zonas = new Transform[tz.arraySize];
        for (int i = 0; i < tz.arraySize; i++)
            zonas[i] = tz.GetArrayElementAtIndex(i).objectReferenceValue as Transform;

        var mActualizarMira = typeof(PenaltyMinigameManager).GetMethod("ActualizarMira", BindingFlags.NonPublic | BindingFlags.Instance);
        var fApuntada       = typeof(PenaltyMinigameManager).GetField ("_zonaApuntada",  BindingFlags.NonPublic | BindingFlags.Instance);

        sb.AppendLine($"Estado actual: {pm.Estado}");

        // ── 0. Si el penal no arrancó, lo arrancamos ─────────────────────────
        if (pm.Estado == PenaltyMinigameManager.PenaltyState.Inactivo)
        {
            sb.AppendLine("→ Penal inactivo: arranco partida y salto a la arena.");
            if (RunnerGameManager.Instance != null)
            {
                RunnerGameManager.Instance.StartGame();
                RunnerGameManager.Instance.OnFinishReached();
                sb.AppendLine("   (ejecutá esta prueba otra vez en unos segundos para el test de click)");
            }
            sb.AppendLine("\n########## FIN ##########");
            Debug.Log(sb.ToString());
            return;
        }

        // ── 1. Cursor sobre cada zona, una por una ───────────────────────────
        sb.AppendLine("\n--- Las 6 zonas ---");
        int aciertos = 0;

        for (int i = 0; i < zonas.Length; i++)
        {
            if (zonas[i] == null) { sb.AppendLine($"  zona {i}: <NULL>"); continue; }

            Vector3 screen = cam.WorldToScreenPoint(zonas[i].position);

            // Inyectamos la posición del puntero y forzamos el procesamiento.
            var estado = new MouseState { position = new Vector2(screen.x, screen.y) };
            InputSystem.QueueStateEvent(mouse, estado);
            InputSystem.Update();

            // Ejecutamos el MISMO método que corre en Update().
            mActualizarMira.Invoke(pm, null);

            int detectada = (int)fApuntada.GetValue(pm);
            bool ok = detectada == i;
            if (ok) aciertos++;

            sb.AppendLine($"  cursor sobre zona {i} (pantalla {screen.x:F0},{screen.y:F0}) " +
                          $"→ apunta a {detectada} {(ok ? "OK" : "*** FALLA ***")}   " +
                          $"{ColorDeZona(zonas[detectada])}");
        }

        sb.AppendLine($"Zonas acertadas: {aciertos}/{zonas.Length}");

        // ── 2. Click izquierdo real ──────────────────────────────────────────
        sb.AppendLine("\n--- Click izquierdo ---");

        if (pm.Estado != PenaltyMinigameManager.PenaltyState.Apuntando)
        {
            // Fuera de Apuntando el manager no patea: verificamos solo la lectura.
            var prueba = new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
            prueba.WithButton(MouseButton.Left, true);
            InputSystem.QueueStateEvent(mouse, prueba);
            InputSystem.Update();
            sb.AppendLine($"  estado {pm.Estado}: se lee el botón sin patear. " +
                          $"wasPressedThisFrame={mouse.leftButton.wasPressedThisFrame}");
        }
        else
        {
            var click = new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
            click.WithButton(MouseButton.Left, true);
            InputSystem.QueueStateEvent(mouse, click);
            InputSystem.Update();

            sb.AppendLine($"  leftButton.wasPressedThisFrame = {mouse.leftButton.wasPressedThisFrame} " +
                          "(lo que lee el Update del manager)");
            sb.AppendLine($"  leftButton.isPressed          = {mouse.leftButton.isPressed}");
            sb.AppendLine("  → el manager debe PATEAR solo en su próximo Update.");
        }

        // Soltamos el botón para no dejar el estado sucio.
        var suelto = new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
        InputSystem.QueueStateEvent(mouse, suelto);
        InputSystem.Update();

        sb.AppendLine("\n########## FIN ##########");
        Debug.Log(sb.ToString());
    }

    /// <summary>Arranca la partida y salta directo al penal (para pruebas).</summary>
    public static void IniciarPenal()
    {
        if (RunnerGameManager.Instance == null) return;
        RunnerGameManager.Instance.StartGame();
        RunnerGameManager.Instance.OnFinishReached();
        Debug.Log("[Prueba] Penal iniciado.");
    }

    /// <summary>
    /// Encola un click izquierdo y NO lo procesa a mano: deja que lo tome el
    /// próximo frame del player loop, para que el Update() del manager lo vea
    /// como un click real (wasPressedThisFrame). Sirve para probar de punta a
    /// punta que el click dispara la pateada.
    /// </summary>
    public static void ClickPendiente()
    {
        var mouse = Mouse.current;
        if (mouse == null) { Debug.Log("[Prueba] Mouse.current NULL"); return; }

        var estado = new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) };
        estado.WithButton(MouseButton.Left, true);
        InputSystem.QueueStateEvent(mouse, estado);

        Debug.Log("[Prueba] Click encolado (sin procesar a mano). " +
                  "El próximo Update del manager debería patear.");
    }

    /// <summary>Loguea el estado actual del minijuego y del nivel.</summary>
    public static void EstadoActual()
    {
        var pm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        var gm = RunnerGameManager.Instance;
        string gk = "n/a";
        var keeper = Object.FindFirstObjectByType<GoalkeeperAI>(FindObjectsInactive.Include);
        if (keeper != null) gk = $"({keeper.transform.position.x:F2},{keeper.transform.position.y:F2})";

        Debug.Log($"[Prueba] Penal={pm?.Estado}  GameState={(gm != null ? gm.State.ToString() : "n/a")}  " +
                  $"intentos={pm?.IntentosRestantes}  arquero={gk}");
    }

    /// <summary>Lee el _BaseColor efectivo de una zona, para ver el resaltado.</summary>
    private static string ColorDeZona(Transform zona)
    {
        if (zona == null) return "";
        var mr = zona.GetComponent<MeshRenderer>();
        if (mr == null) return "sin renderer";

        var mpb = new MaterialPropertyBlock();
        mr.GetPropertyBlock(mpb);
        Color c = mpb.GetColor(Shader.PropertyToID("_BaseColor"));
        return $"color rgba({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";
    }
}
#endif
