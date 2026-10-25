using UnityEngine;
using UnityEditor;
using TMPro;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// SONDA TEMPORAL de cierre de la Fase B. No forma parte del juego.
///
/// Acciones:
///   sorteo  -> sortea 400 obstáculos con cada nivel y muestra qué tipos salen.
///              Sirve para comprobar que el nivel 1 sólo permite cono, valla y
///              barrera, y que el nivel 6 permite los diez.
///   roja    -> golpea cinco veces al árbitro (en Play Mode) y comprueba que
///              sólo se cuenta una tarjeta roja por partida.
///   nivel1  -> fuerza el nivel 1 en el manager (sin tocar el guardado) para
///              probarlo en juego.
///   ids     -> lista los ids de los obstáculos que están ahora en la pista.
/// </summary>
public static class _CierreFaseB
{
    public static void Execute(JObject args)
    {
        string accion = args != null && args["accion"] != null
            ? args["accion"].ToString()
            : "sorteo";

        if (accion == "roja")   { ProbarRoja();   return; }
        if (accion == "nivel1") { ForzarNivel1(); return; }
        if (accion == "ids")    { IdsEnPista();   return; }
        if (accion == "save")   { VolcarGuardado();  return; }
        if (accion == "etapa")  { ProbarEtapa(args); return; }
        if (accion == "ganar")  { GanarEtapa(args);  return; }
        if (accion == "panel")  { VolcarPanel();     return; }
        if (accion == "equipar"){ EquiparDePrueba(args); return; }
        if (accion == "restaurar") { Restaurar(args); return; }
        if (accion == "limpiar")   { Limpiar(); return; }

        Sortear();
    }

    // ─── 1. Qué obstáculos permite cada nivel ─────────────────────────────────

    static void Sortear()
    {
        var log = new StringBuilder();
        log.AppendLine("=== CIERRE FASE B: sorteo por nivel ===");

        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Data/LevelConfig.asset");
        if (config == null) { log.AppendLine("LevelConfig: NULL"); Debug.Log(log.ToString()); return; }

        var tipo = typeof(ObstacleManager);
        var sortear = tipo.GetMethod("SortearObstaculo", BindingFlags.Static | BindingFlags.NonPublic);

        if (sortear == null) { log.AppendLine("no se encontró SortearObstaculo()"); Debug.Log(log.ToString()); return; }

        for (int n = 1; n <= 6; n++)
        {
            NivelObstaculos nivel = config.Obtener(n);
            if (nivel == null) { log.AppendLine($"nivel {n}: NULL"); continue; }

            var conteo = new SortedDictionary<string, int>();

            for (int i = 0; i < 400; i++)
            {
                var dato = sortear.Invoke(null, new object[] { nivel }) as ObstacleData;
                if (dato == null) { conteo["(ninguno)"] = conteo.TryGetValue("(ninguno)", out int c0) ? c0 + 1 : 1; continue; }
                conteo[dato.id] = conteo.TryGetValue(dato.id, out int c) ? c + 1 : 1;
            }

            var lineas = new List<string>();
            foreach (var kv in conteo) lineas.Add($"{kv.Key}={kv.Value}");

            log.AppendLine($"nivel {n} '{nivel.nombre}': {conteo.Count} tipos -> {string.Join(", ", lineas)}");
        }

        Debug.Log(log.ToString());
    }

    // ─── 2. Una sola tarjeta roja por partida ─────────────────────────────────

    static void ProbarRoja()
    {
        var log = new StringBuilder();
        log.AppendLine("=== CIERRE FASE B: tarjeta roja ===");

        var om = Object.FindFirstObjectByType<ObstacleManager>();
        if (om == null) { log.AppendLine("ObstacleManager: NULL (¿está en Play Mode?)"); Debug.Log(log.ToString()); return; }

        var tipo = typeof(ObstacleManager);
        var mostrar        = tipo.GetMethod("MostrarTarjetaAmarilla", BindingFlags.Instance | BindingFlags.NonPublic);
        var campoTarjetas  = tipo.GetField("_tarjetas",      BindingFlags.Instance | BindingFlags.NonPublic);
        var campoRoja      = tipo.GetField("_rojaMostrada",  BindingFlags.Instance | BindingFlags.NonPublic);

        if (mostrar == null || campoTarjetas == null || campoRoja == null)
        {
            log.AppendLine("no se encontraron los miembros privados esperados");
            Debug.Log(log.ToString());
            return;
        }

        var estados = new StringBuilder();
        estados.AppendLine($"estado inicial: _tarjetas={campoTarjetas.GetValue(om)}  _rojaMostrada={campoRoja.GetValue(om)}");

        // Cinco toques seguidos: sólo los tres primeros pueden contar.
        for (int i = 1; i <= 5; i++)
        {
            mostrar.Invoke(om, null);
            estados.AppendLine($"  toque {i}: _tarjetas={campoTarjetas.GetValue(om)}  _rojaMostrada={campoRoja.GetValue(om)}");
        }

        int tarjetas = (int)campoTarjetas.GetValue(om);
        bool roja    = (bool)campoRoja.GetValue(om);

        log.Append(estados);
        log.AppendLine($"RESULTADO: tarjetas={tarjetas} (esperado 3)  roja={roja} (esperado True)");
        log.AppendLine(tarjetas == 3 && roja ? "OK: la roja se muestra una sola vez." : "PROBLEMA: el contador siguió subiendo.");
        log.AppendLine($"estado del juego = {(RunnerGameManager.Instance != null ? RunnerGameManager.Instance.State.ToString() : "sin GM")}");

        Debug.Log(log.ToString());
    }

    // ─── 3. Forzar el nivel 1 sin tocar el guardado ───────────────────────────

    static void ForzarNivel1()
    {
        var log = new StringBuilder();
        log.AppendLine("=== CIERRE FASE B: forzar nivel 1 ===");

        var om = Object.FindFirstObjectByType<ObstacleManager>();
        if (om == null) { log.AppendLine("ObstacleManager: NULL"); Debug.Log(log.ToString()); return; }

        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Data/LevelConfig.asset");
        NivelObstaculos nivel1 = config != null ? config.Obtener(1) : null;
        if (nivel1 == null) { log.AppendLine("no se pudo cargar el nivel 1"); Debug.Log(log.ToString()); return; }

        var tipo = typeof(ObstacleManager);
        tipo.GetField("_nivelActual",   BindingFlags.Instance | BindingFlags.NonPublic).SetValue(om, nivel1);
        tipo.GetField("_nivelCacheado", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(om, 1);

        log.AppendLine($"GameData.CurrentLevel (sin tocar) = {(GameData.Instance != null ? GameData.Instance.CurrentLevel : -1)}");
        log.AppendLine($"manager nivel = {om.NivelActual?.nombre ?? "(null)"}  vel={om.MultiplicadorVelocidadNivel.ToString("0.00")}");

        Debug.Log(log.ToString());
    }

    // ─── 4. Qué hay puesto en la pista ahora ──────────────────────────────────

    static void IdsEnPista()
    {
        var log = new StringBuilder();
        log.AppendLine("=== CIERRE FASE B: obstáculos en pista ===");

        var om = Object.FindFirstObjectByType<ObstacleManager>();
        if (om == null) { log.AppendLine("ObstacleManager: NULL"); Debug.Log(log.ToString()); return; }

        var tipo = typeof(ObstacleManager);
        var lista = tipo.GetField("_activos", BindingFlags.Instance | BindingFlags.NonPublic)
                       ?.GetValue(om) as List<ObstacleBase>;

        log.AppendLine($"nivel = {om.NivelActual?.nombre ?? "(null)"}  vel={om.MultiplicadorVelocidadNivel.ToString("0.00")}");
        log.AppendLine($"activos = {(lista != null ? lista.Count : -1)}");

        var conteo = new SortedDictionary<string, int>();
        if (lista != null)
        {
            foreach (var o in lista)
            {
                if (o == null || o.Datos == null) continue;
                conteo[o.Datos.id] = conteo.TryGetValue(o.Datos.id, out int c) ? c + 1 : 1;
            }
        }

        foreach (var kv in conteo) log.AppendLine($"   {kv.Key}: {kv.Value}");

        Debug.Log(log.ToString());
    }

    // ─── 5. Etapas del torneo ─────────────────────────────────────────────────

    /// <summary>Muestra cómo está el guardado ahora mismo.</summary>
    static void VolcarGuardado()
    {
        var d = SaveManager.Data;
        Debug.Log("=== GUARDADO ===\n" +
                  $"coins={d.coins} bestScore={d.bestScore} currentLevel={d.currentLevel}\n" +
                  $"personajeEquipado='{d.personajeEquipado}' comprados=[{string.Join(",", d.personajesComprados)}]");
    }

    /// <summary>Arranca la partida en la etapa pedida y muestra qué dice el HUD.</summary>
    static void ProbarEtapa(JObject args)
    {
        int nivel = args != null && args["nivel"] != null ? (int)args["nivel"] : 1;

        var log = new StringBuilder();
        log.AppendLine($"=== ETAPA: arrancar en {nivel} ===");

        if (GameData.Instance != null) GameData.Instance.SetCurrentLevel(nivel);

        var gm = RunnerGameManager.Instance;
        if (gm == null) { log.AppendLine("RunnerGameManager: NULL"); Debug.Log(log.ToString()); return; }

        gm.StartGame();

        var om = Object.FindFirstObjectByType<ObstacleManager>();
        log.AppendLine($"manager etapa = {om?.NivelActual?.nombre ?? "(null)"}  vel={om?.MultiplicadorVelocidadNivel.ToString("0.00")}");

        var hud = Object.FindFirstObjectByType<GameplayHUD>(FindObjectsInactive.Include);
        var texto = hud != null
            ? typeof(GameplayHUD).GetField("etapaText", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hud) as TMP_Text
            : null;
        log.AppendLine($"HUD EtapaHUD = '{texto?.text ?? "(sin enganchar)"}'");

        Debug.Log(log.ToString());
    }

    /// <summary>
    /// Simula ganar la etapa pedida: arranca la partida en esa etapa, fuerza el
    /// estado de minijuego (que es el único desde el que OnLevelComplete actúa) y
    /// llama al flujo real de victoria.
    /// </summary>
    static void GanarEtapa(JObject args)
    {
        int nivel = args != null && args["nivel"] != null ? (int)args["nivel"] : 6;

        var log = new StringBuilder();
        log.AppendLine($"=== GANAR ETAPA {nivel} ===");

        if (GameData.Instance != null) GameData.Instance.SetCurrentLevel(nivel);

        var gm = RunnerGameManager.Instance;
        if (gm == null) { log.AppendLine("RunnerGameManager: NULL"); Debug.Log(log.ToString()); return; }

        gm.StartGame();
        log.AppendLine($"nivel guardado antes = {SaveManager.Data.currentLevel}");

        // Estado de penales por reflexión sobre el campo generado por la
        // propiedad auto-implementada: es el punto de partida de OnLevelComplete.
        var campoEstado = typeof(RunnerGameManager).GetField("<State>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (campoEstado == null) { log.AppendLine("no encontré el campo de State"); Debug.Log(log.ToString()); return; }

        campoEstado.SetValue(gm, GameState.PenaltyMinigame);
        log.AppendLine($"estado forzado = {gm.State}");

        gm.OnLevelComplete();

        log.AppendLine($"estado despues = {gm.State}");
        log.AppendLine("esperando la pantalla de victoria...");

        Debug.Log(log.ToString());
    }

    /// <summary>Vuelca lo que dice el panel de victoria.</summary>
    static void VolcarPanel()
    {
        var log = new StringBuilder();
        log.AppendLine("=== PANEL DE VICTORIA ===");

        var lc = Object.FindFirstObjectByType<LevelCompleteUI>(FindObjectsInactive.Include);
        if (lc == null) { log.AppendLine("LevelCompleteUI: NULL"); Debug.Log(log.ToString()); return; }

        var tipo = typeof(LevelCompleteUI);
        foreach (string campo in new[] { "titleText", "levelText", "scoreText", "coinsText" })
        {
            var t = tipo.GetField(campo, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(lc) as TMP_Text;
            log.AppendLine($"  {campo} = '{(t != null ? t.text : "(null)")}'" +
                           (t != null ? $"  fontSize={t.fontSize.ToString("0")}" : ""));
        }

        log.AppendLine($"nivel guardado ahora = {SaveManager.Data.currentLevel} " +
                       $"('{ObstacleManager.Configuracion?.NombreDeEtapa(SaveManager.Data.currentLevel)}')");

        Debug.Log(log.ToString());
    }

    /// <summary>Equipa un jugador sin comprarlo, sólo para ver la frase de campeón.</summary>
    static void EquiparDePrueba(JObject args)
    {
        string id = args != null && args["id"] != null ? args["id"].ToString() : "Messi";

        var cm = CharacterManager.Instance;
        if (cm == null) { Debug.Log("CharacterManager: NULL"); return; }

        var personaje = cm.PorId(id);
        if (personaje == null) { Debug.Log($"no existe el jugador '{id}'"); return; }

        if (!SaveManager.Data.personajesComprados.Contains(personaje.id))
            SaveManager.Data.personajesComprados.Add(personaje.id);

        cm.Equipar(personaje);

        Debug.Log($"[PRUEBA] equipado '{personaje.id}' apodo='{personaje.apodo}' " +
                  $"equipadoReal='{(cm.Equipado != null ? cm.Equipado.id : "(null)")}'");
    }

    /// <summary>
    /// Borra el monitor temporal de runtime. Es un MonoBehaviour de diagnóstico:
    /// no debe quedar en el proyecto porque se empaquetaría en el build.
    /// </summary>
    static void Limpiar()
    {
        const string ruta = "Assets/_Project/Scripts/_DiagObstaculos.cs";

        bool habia = AssetDatabase.LoadAssetAtPath<MonoScript>(ruta) != null;
        if (habia)
        {
            AssetDatabase.DeleteAsset(ruta);
            AssetDatabase.Refresh();
        }

        Debug.Log(habia
            ? "[FASE B] borrado el monitor temporal _DiagObstaculos.cs"
            : "[FASE B] el monitor temporal ya no estaba");
    }

    /// <summary>Devuelve el guardado a los valores indicados.</summary>
    static void Restaurar(JObject args)
    {
        int coins  = args != null && args["coins"]  != null ? (int)args["coins"]  : 0;
        int nivel  = args != null && args["nivel"]  != null ? (int)args["nivel"]  : 1;
        string personaje = args != null && args["personaje"] != null ? args["personaje"].ToString() : "";

        SaveManager.Data.coins = coins;
        SaveManager.Data.currentLevel = Mathf.Max(1, nivel);
        SaveManager.Data.personajeEquipado = personaje;

        // Si el jugador de prueba no estaba comprado de verdad, se saca.
        if (args != null && args["sacarComprado"] != null)
            SaveManager.Data.personajesComprados.Remove(args["sacarComprado"].ToString());

        SaveManager.Guardar();

        var cm = CharacterManager.Instance;
        if (cm != null) cm.CargarEquipado();

        var d = SaveManager.Data;
        Debug.Log($"[PRUEBA] guardado restaurado: coins={d.coins} nivel={d.currentLevel} " +
                  $"equipado='{d.personajeEquipado}' comprados=[{string.Join(",", d.personajesComprados)}]");
    }
}
