using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

/// <summary>
/// SONDA TEMPORAL de la Fase B. No forma parte del juego.
///
/// Se usa desde execute_script con un argumento "accion":
///   arrancar -> pone la partida en Running para que empiecen a salir segmentos.
///   muestra  -> mide las filas de obstáculos vivas y comprueba que ninguna
///               tape los tres carriles.
///   validar  -> revisa los datos de los 6 niveles del LevelConfig.
/// </summary>
public static class _VerFaseB
{
    public static void Execute(JObject args)
    {
        string accion = args != null && args["accion"] != null
            ? args["accion"].ToString()
            : "muestra";

        if (accion == "arrancar") { Arrancar(); return; }
        if (accion == "validar")  { Validar();  return; }

        Muestra();
    }

    // ─── Validar los datos de los 6 niveles ───────────────────────────────────

    static void Validar()
    {
        var log = new StringBuilder();
        log.AppendLine("=== VER FASE B: validar datos ===");

        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/_Project/Data/LevelConfig.asset");
        if (config == null) { log.AppendLine("LevelConfig: NULL"); Debug.Log(log.ToString()); return; }

        log.AppendLine($"niveles en el config = {config.niveles.Count}");
        log.AppendLine($"tiempoInvulnerabilidad = {config.tiempoInvulnerabilidad.ToString("0.00")}  " +
                       $"velLateralPorDefecto = {config.velocidadLateralPorDefecto.ToString("0.00")}");

        int problemas = 0;

        // 1. Cada prefab tiene que tener un ObstacleBase, o el manager lo descarta.
        var guids = AssetDatabase.FindAssets("t:ObstacleData", new[] { "Assets/_Project/Data/Obstaculos" });
        log.AppendLine($"ObstacleData encontrados = {guids.Length}");

        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var dato = AssetDatabase.LoadAssetAtPath<ObstacleData>(ruta);
            if (dato == null) { log.AppendLine($"  {ruta}: no se pudo cargar"); problemas++; continue; }

            bool okPrefab = dato.prefab != null;
            bool okComp   = okPrefab && dato.prefab.GetComponent<ObstacleBase>() != null;

            if (!okPrefab || !okComp) problemas++;

            log.AppendLine($"  {dato.id,-20} tipo={dato.tipo,-10} danio={dato.danioEnergia,4:0} " +
                           $"nivelMin={dato.nivelMinimo} carriles={dato.carrilesQueOcupa} " +
                           $"salta={dato.sePuedeSaltar} efecto={dato.efecto} " +
                           $"prefab={(okPrefab ? "OK" : "FALTA")} " +
                           $"componente={(okComp ? "OK" : "FALTA")}");
        }

        // 2. Cada nivel: cuántos obstáculos puede sortear de verdad.
        for (int n = 1; n <= 6; n++)
        {
            var nivel = config.Obtener(n);
            if (nivel == null) { log.AppendLine($"nivel {n}: NULL"); problemas++; continue; }

            var disponibles = new List<string>();
            foreach (var e in nivel.permitidos)
            {
                if (e == null || e.dato == null || e.dato.prefab == null) continue;
                if (e.dato.nivelMinimo > nivel.nivel) continue;
                disponibles.Add($"{e.dato.id}(peso {e.peso.ToString("0.0")})");
            }

            if (disponibles.Count == 0) problemas++;

            log.AppendLine($"nivel {nivel.nivel} '{nivel.nombre}': vel={nivel.multiplicadorVelocidad.ToString("0.00")} " +
                           $"filas={nivel.filasPorSegmento.ToString("0.0")} " +
                           $"margen={nivel.margenEntreFilas.ToString("0")} " +
                           $"prob2={nivel.probabilidadDosCarriles.ToString("0.00")} " +
                           $"sortables={disponibles.Count} -> {string.Join(", ", disponibles)}");
        }

        log.AppendLine($"PROBLEMAS = {problemas}");
        Debug.Log(log.ToString());
    }

    // ─── Arrancar la carrera ──────────────────────────────────────────────────

    static void Arrancar()
    {
        var log = new StringBuilder();
        log.AppendLine("=== VER FASE B: arrancar ===");

        int nivel = GameData.Instance != null ? GameData.Instance.CurrentLevel : -1;
        log.AppendLine($"GameData.CurrentLevel = {nivel}");

        var gm = RunnerGameManager.Instance;
        if (gm == null) { log.AppendLine("RunnerGameManager: NULL"); Debug.Log(log.ToString()); return; }

        log.AppendLine($"estado antes = {gm.State}");
        gm.StartGame();
        log.AppendLine($"estado despues = {gm.State}");

        var om = ObstacleManager.Instance;
        if (om == null) log.AppendLine("ObstacleManager.Instance: NULL");
        else
        {
            log.AppendLine($"manager nivel = {om.NivelActual?.nombre ?? "(null)"}");
            log.AppendLine($"manager vel   = {om.MultiplicadorVelocidadNivel.ToString("0.00")}");
            log.AppendLine($"invulnerab.   = {om.TiempoInvulnerabilidad.ToString("0.00")}");
        }

        Debug.Log(log.ToString());
    }

    // ─── Medir las filas ──────────────────────────────────────────────────────

    static void Muestra()
    {
        var log = new StringBuilder();
        log.AppendLine("=== VER FASE B: muestra ===");

        var om = ObstacleManager.Instance;
        log.AppendLine($"ObstacleManager.Instance = {(om != null ? "OK" : "NULL")}");
        if (om != null)
        {
            log.AppendLine($"nivel jugado = {om.NivelActual?.nombre ?? "(null)"}  " +
                           $"vel={om.MultiplicadorVelocidadNivel.ToString("0.00")}");
        }

        var gm = RunnerGameManager.Instance;
        log.AppendLine($"estado = {(gm != null ? gm.State.ToString() : "sin GM")}");

        // Segmentos vivos en la escena.
        var generador = Object.FindFirstObjectByType<TrackGenerator>();
        log.AppendLine($"segmentos bajo el generador = " +
                       $"{(generador != null ? generador.transform.childCount.ToString() : "sin generador")}");

        // Todos los obstáculos, activos e inactivos.
        var todos = Object.FindObjectsByType<ObstacleBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int activos = 0;
        var vivos = new List<ObstacleBase>();

        foreach (var o in todos)
        {
            if (o == null) continue;
            if (!o.Activo) continue;
            activos++;
            vivos.Add(o);
        }

        log.AppendLine($"obstáculos instanciados = {todos.Length}   activos en pista = {activos}");

        if (vivos.Count == 0) { Debug.Log(log.ToString()); return; }

        // ── Agrupar por filas (misma Z aproximada) ────────────────────────────
        vivos.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));

        var filas = new List<List<ObstacleBase>>();
        foreach (var o in vivos)
        {
            if (filas.Count == 0 ||
                Mathf.Abs(o.transform.position.z - filas[filas.Count - 1][0].transform.position.z) > 1.5f)
                filas.Add(new List<ObstacleBase>());

            filas[filas.Count - 1].Add(o);
        }

        log.AppendLine($"filas detectadas = {filas.Count}");

        int filasConTres = 0;

        for (int f = 0; f < filas.Count; f++)
        {
            var fila = filas[f];

            // Qué carriles quedan tapados por esta fila: un carril está tapado
            // si su centro cae dentro del ancho real del obstáculo.
            var tapados = new List<int>();
            for (int carril = 0; carril < LaneSystem.LaneCount; carril++)
            {
                float xCarril = LaneSystem.GetLaneX(carril);

                foreach (var o in fila)
                {
                    if (Tapado(o, xCarril)) { tapados.Add(carril); break; }
                }
            }

            bool tres = tapados.Count >= 3;
            if (tres) filasConTres++;

            var nombres = new List<string>();
            foreach (var o in fila)
                nombres.Add(o.Datos != null ? o.Datos.id : o.gameObject.name);

            string carrilesTxt = tapados.Count == 0 ? "ninguno" : string.Join(",", tapados);

            log.AppendLine($"  fila {f}: z={fila[0].transform.position.z.ToString("0.0")}  " +
                           $"tapados=[{carrilesTxt}]  piezas={fila.Count}  " +
                           $"tipo(s)={string.Join("+", nombres)}" +
                           (tres ? "   <<< PROBLEMA: TAPA LOS TRES" : ""));
        }

        // ── Resumen por efecto ────────────────────────────────────────────────
        int conEfecto = 0;
        var efectos = new SortedDictionary<string, int>();
        foreach (var o in vivos)
        {
            if (o.Datos == null || o.Datos.efecto == EfectoObstaculo.Ninguno) continue;
            conEfecto++;
            string k = o.Datos.efecto.ToString();
            efectos[k] = efectos.TryGetValue(k, out int c) ? c + 1 : 1;
        }

        log.AppendLine($"con efecto especial = {conEfecto}");
        foreach (var kv in efectos) log.AppendLine($"   {kv.Key}: {kv.Value}");

        log.AppendLine($"FILAS QUE TAPAN LOS TRES CARRILES = {filasConTres}");
        Debug.Log(log.ToString());
    }

    /// <summary>
    /// True si el obstáculo ocupa el carril cuyo centro está en xCarril.
    ///
    /// Se compara el ancho real del obstáculo contra la franja del carril: hay
    /// ocupación cuando se solapan al menos un 30 % del ancho del carril. Así un
    /// cono angosto cuenta igual que una barrera ancha, y un obstáculo que quedó
    /// justo en el borde no se cuenta dos veces.
    /// </summary>
    static bool Tapado(ObstacleBase o, float xCarril)
    {
        float medio = LaneSystem.LaneWidth * 0.5f;
        float carrilMin = xCarril - medio;
        float carrilMax = xCarril + medio;

        float obsMin, obsMax;

        var col = o.GetComponent<Collider>();
        if (col != null)
        {
            Bounds b = col.bounds;
            obsMin = b.min.x;
            obsMax = b.max.x;
        }
        else
        {
            // Sin collider: medio carril de ancho a cada lado de su X.
            obsMin = o.transform.position.x - medio;
            obsMax = o.transform.position.x + medio;
        }

        float solape = Mathf.Min(obsMax, carrilMax) - Mathf.Max(obsMin, carrilMin);
        return solape > LaneSystem.LaneWidth * 0.30f;
    }
}
