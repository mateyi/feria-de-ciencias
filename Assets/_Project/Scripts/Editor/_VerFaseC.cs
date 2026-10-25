using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

/// <summary>
/// SONDA TEMPORAL de la Fase C (pickups). No forma parte del juego.
///
/// Acciones (argumento "accion"):
///   reparto -> simula 200 segmentos por etapa y cuenta cuántos pickups caen.
///   efectos -> aplica los 4 efectos y reporta cómo queda cada sistema.
///   estado  -> reporta el estado de los managers y del HUD.
///   recoger -> recoge un pickup real de la pista y reporta antes/después.
/// </summary>
public static class _VerFaseC
{
    public static void Execute(Newtonsoft.Json.Linq.JObject args)
    {
        string accion = args != null && args["accion"] != null ? args["accion"].ToString() : "estado";

        if (accion == "reparto") { Reparto(); return; }
        if (accion == "efectos") { Efectos(); return; }
        if (accion == "iman")    { Iman();    return; }
        if (accion == "hud")     { Hud();     return; }
        if (accion == "recoger") { Recoger(); return; }
        if (accion == "jugar")   { Jugar();   return; }

        Estado();
    }

    // ─── Jugar: arranca una partida real ──────────────────────────────────────

    static void Jugar()
    {
        if (RunnerGameManager.Instance == null) { Debug.Log("[FASE C] no hay RunnerGameManager"); return; }

        RunnerGameManager.Instance.StartGame();
        Debug.Log($"[FASE C] partida iniciada. estado={RunnerGameManager.Instance.State}");
    }

    // ─── Estado ───────────────────────────────────────────────────────────────

    static void Estado()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE C: estado ===");

        var manager = Object.FindFirstObjectByType<PickupManager>(FindObjectsInactive.Include);
        log.AppendLine($"PickupManager: {(manager != null ? manager.name : "NO ESTÁ")}  " +
                       $"instancia={(PickupManager.Instance != null)}");

        if (manager != null)
        {
            var so = new SerializedObject(manager);
            var prop = so.FindProperty("catalogo");
            log.AppendLine($"catálogo: {(prop != null ? prop.arraySize : 0)} pickups");
        }

        var pickups = Object.FindObjectsByType<PickupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int activos = 0;
        var porTipo = new Dictionary<TipoPickup, int>();
        foreach (var p in pickups)
        {
            if (p == null || !p.Activo) continue;
            activos++;
            if (p.Datos != null)
                porTipo[p.Datos.tipo] = (porTipo.TryGetValue(p.Datos.tipo, out int n) ? n + 1 : 1);
        }

        log.AppendLine($"pickups en escena: total={pickups.Length} activos={activos}");
        foreach (var kv in porTipo) log.AppendLine($"  {kv.Key} = {kv.Value}");

        var hud = Object.FindFirstObjectByType<PickupHUD>(FindObjectsInactive.Include);
        log.AppendLine($"PickupHUD: {(hud != null ? "creado" : "todavía no (se crea al recoger el primero)")}");

        if (PickupManager.Instance != null)
        {
            log.AppendLine($"turbo restante={PickupManager.Instance.TurboRestante01:0.00} " +
                           $"multiplicador={PickupManager.Instance.MultiplicadorVelocidad:0.00}");
            log.AppendLine($"imán activo={PickupManager.Instance.ImanActivo} " +
                           $"radio={PickupManager.Instance.RadioImanActual:0.0}");
            log.AppendLine($"escudos activos={PickupManager.Instance.EscudosActivos}");
        }

        var nivel = SaveManager.Data != null ? SaveManager.Data.currentLevel : -1;
        log.AppendLine($"etapa guardada = {nivel}");

        Debug.Log(log.ToString());
    }

    // ─── Reparto: cuántos pickups caen por segmento según la etapa ────────────

    static void Reparto()
    {
        var manager = PickupManager.Instance != null
            ? PickupManager.Instance
            : Object.FindFirstObjectByType<PickupManager>(FindObjectsInactive.Include);

        if (manager == null) { Debug.Log("[FASE C] no hay PickupManager"); return; }

        var log = new StringBuilder();
        log.AppendLine("=== FASE C: reparto de pickups (200 segmentos por etapa) ===");

        int nivelOriginal = SaveManager.Data.currentLevel;

        for (int nivel = 1; nivel <= 6; nivel++)
        {
            SaveManager.Data.currentLevel = nivel;

            // Segmento de prueba lejos del jugador para que nada se recicle.
            var segmento = new GameObject($"Segmento_Prueba_{nivel}");
            segmento.transform.position = new Vector3(0f, 0f, 100f);

            for (int i = 0; i < 200; i++)
                manager.PoblarSegmento(segmento.transform, 5f, 25f, 0f);

            var hijos = segmento.GetComponentsInChildren<PickupBase>(true);
            var porTipo = new Dictionary<TipoPickup, int>();
            foreach (var p in hijos)
                if (p != null && p.Datos != null)
                    porTipo[p.Datos.tipo] = (porTipo.TryGetValue(p.Datos.tipo, out int n) ? n + 1 : 1);

            log.AppendLine($"etapa {nivel}: {hijos.Length} pickups en 200 segmentos " +
                           $"= {hijos.Length / 200f:0.00} por segmento");

            foreach (var kv in porTipo) log.AppendLine($"    {kv.Key} = {kv.Value}");

            Object.DestroyImmediate(segmento);
        }

        SaveManager.Data.currentLevel = nivelOriginal;

        Debug.Log(log.ToString());
    }

    // ─── Efectos: aplica los cuatro y reporta el estado de cada sistema ───────

    static void Efectos()
    {
        var manager = PickupManager.Instance;
        if (manager == null) { Debug.Log("[FASE C] no hay PickupManager en la escena"); return; }

        var log = new StringBuilder();
        log.AppendLine("=== FASE C: efectos ===");

        MethodInfo aplicar = typeof(PickupManager).GetMethod("AplicarEfecto",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (aplicar == null) { Debug.Log("[FASE C] no encontré AplicarEfecto"); return; }

        // Punto de partida limpio.
        if (PlayerStats.Instance != null) PlayerStats.Instance.ResetStats();
        manager.ReiniciarEfectos();
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.ReiniciarRun();

        foreach (var dato in manager.Catalogo)
        {
            if (dato == null) continue;

            if (PlayerStats.Instance != null) PlayerStats.Instance.ResetStats();
            manager.ReiniciarEfectos();

            // Para probar la bebida hay que bajar la energía: ResetStats la deja
            // en 100 (el máximo) y entonces la recuperación no se notaría.
            if (dato.tipo == TipoPickup.Energia && PlayerStats.Instance != null)
                PlayerStats.Instance.DrainEnergy(80f);

            float energiaAntes = PlayerStats.Instance != null ? PlayerStats.Instance.energy : -1f;
            int escudosAntes   = manager.EscudosActivos;

            aplicar.Invoke(manager, new object[] { dato });

            log.AppendLine($"--- {dato.nombre} ({dato.tipo}) valor={dato.valor} duracion={dato.duracion}");

            switch (dato.tipo)
            {
                case TipoPickup.Energia:
                    log.AppendLine($"    energía {energiaAntes:0} -> " +
                                   $"{(PlayerStats.Instance != null ? PlayerStats.Instance.energy : -1f):0} " +
                                   $"(esperado {energiaAntes:0} -> {energiaAntes + dato.valor:0})");
                    break;

                case TipoPickup.Turbo:
                    log.AppendLine($"    multiplicador de velocidad = {manager.MultiplicadorVelocidad:0.00} " +
                                   $"(esperado {dato.valor:0.00})   restante01={manager.TurboRestante01:0.00}");
                    break;

                case TipoPickup.Iman:
                    log.AppendLine($"    imán activo={manager.ImanActivo} " +
                                   $"radio={manager.RadioImanActual:0.0} (esperado {dato.valor:0.0}) " +
                                   $"restante01={manager.ImanRestante01:0.00}");
                    break;

                case TipoPickup.Escudo:
                    log.AppendLine($"    escudos {escudosAntes} -> {manager.EscudosActivos} " +
                                   $"(esperado +{dato.valor:0})");
                    break;
            }
        }

        manager.ReiniciarEfectos();
        Debug.Log(log.ToString());
    }

    // ─── Imán: comprueba que la lata se mueva hacia el jugador ────────────────

    static void Iman()
    {
        var manager = PickupManager.Instance;
        if (manager == null) { Debug.Log("[FASE C] no hay PickupManager"); return; }
        if (PlayerController.Instance == null) { Debug.Log("[FASE C] no hay jugador"); return; }

        var log = new StringBuilder();
        log.AppendLine("=== FASE C: imán ===");

        // Prefab de la lata: el mismo que usa el ObstacleSpawner del segmento.
        var spawner = Object.FindFirstObjectByType<ObstacleSpawner>(FindObjectsInactive.Include);
        GameObject prefabLata = null;
        if (spawner != null)
        {
            var so = new SerializedObject(spawner);
            var prop = so.FindProperty("coinPrefab");
            if (prop != null) prefabLata = prop.objectReferenceValue as GameObject;
        }

        if (prefabLata == null) { log.AppendLine("no encontré el prefab de la lata"); Debug.Log(log.ToString()); return; }

        Vector3 posJugador = PlayerController.Instance.transform.position;
        Vector3 posLata    = posJugador + new Vector3(0f, 0.8f, 4f);

        var lataGo = Object.Instantiate(prefabLata, posLata, Quaternion.identity);
        var lata   = lataGo.GetComponent<Coin>();
        if (lata == null) { log.AppendLine("el prefab de la lata no tiene Coin"); Object.DestroyImmediate(lataGo); Debug.Log(log.ToString()); return; }

        log.AppendLine($"lata creada a {Vector3.Distance(posLata, posJugador):0.0} m del jugador");

        // Sin imán la lata no se mueve hacia el jugador.
        float antesSin = Vector3.Distance(lata.transform.position, posJugador);
        InvocarAtraccion(lata);
        float despuesSin = Vector3.Distance(lata.transform.position, posJugador);
        log.AppendLine($"sin imán: distancia {antesSin:0.00} -> {despuesSin:0.00}");

        // Con imán activo (radio 6 m) tiene que acercarse.
        var datoIman = BuscarDato(manager, TipoPickup.Iman);
        if (datoIman == null) { log.AppendLine("no hay dato de imán"); Debug.Log(log.ToString()); return; }

        typeof(PickupManager).GetMethod("AplicarEfecto",
            BindingFlags.NonPublic | BindingFlags.Instance).Invoke(manager, new object[] { datoIman });

        log.AppendLine($"imán activo={manager.ImanActivo} radio={manager.RadioImanActual:0.0} m");

        float antesCon = Vector3.Distance(lata.transform.position, posJugador);
        InvocarAtraccion(lata);
        float despuesCon = Vector3.Distance(lata.transform.position, posJugador);
        log.AppendLine($"con imán: distancia {antesCon:0.00} -> {despuesCon:0.00} " +
                       $"({(despuesCon < antesCon ? "se ACERCA" : "no se mueve")})");

        Object.DestroyImmediate(lataGo);
        manager.ReiniciarEfectos();

        Debug.Log(log.ToString());
    }

    static void InvocarAtraccion(Coin lata)
    {
        typeof(Coin).GetMethod("AtraerHaciaElJugador", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(lata, null);
    }

    static PickupData BuscarDato(PickupManager manager, TipoPickup tipo)
    {
        if (manager.Catalogo == null) return null;
        foreach (var d in manager.Catalogo) if (d != null && d.tipo == tipo) return d;
        return null;
    }

    // ─── HUD: deja efectos activos para poder capturar la pantalla ────────────

    static void Hud()
    {
        var manager = PickupManager.Instance;
        if (manager == null) { Debug.Log("[FASE C] no hay PickupManager"); return; }

        var log = new StringBuilder();
        log.AppendLine("=== FASE C: HUD ===");

        MethodInfo aplicar = typeof(PickupManager).GetMethod("AplicarEfecto",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Turbo e imán quedan corriendo; el escudo suma 2 para ver las marcas.
        aplicar.Invoke(manager, new object[] { BuscarDato(manager, TipoPickup.Turbo) });
        aplicar.Invoke(manager, new object[] { BuscarDato(manager, TipoPickup.Iman) });
        aplicar.Invoke(manager, new object[] { BuscarDato(manager, TipoPickup.Escudo) });
        aplicar.Invoke(manager, new object[] { BuscarDato(manager, TipoPickup.Escudo) });

        log.AppendLine($"turbo={manager.TurboRestante01:0.00} iman={manager.ImanRestante01:0.00} " +
                       $"escudos={manager.EscudosActivos}");

        PickupHUD.InstanceOrCreate()?.Destello(TipoPickup.Energia);

        // Diagnóstico de posicionamiento: dónde quedó realmente cada casilla.
        var hud = PickupHUD.Instance;
        if (hud != null)
        {
            var rt = hud.GetComponent<RectTransform>();
            log.AppendLine($"PickupHUD rect={rt.rect} padre={hud.transform.parent.name}");

            foreach (Transform hijo in hud.transform)
            {
                var hrt = hijo as RectTransform;
                log.AppendLine($"  {hijo.name}: anchoredPos={hrt.anchoredPosition} " +
                               $"size={hrt.sizeDelta} world={hrt.position}");
            }
        }

        Debug.Log(log.ToString());
    }

    // ─── Recoger: usa el camino real (PickupBase -> PickupManager) ────────────

    static void Recoger()
    {
        var manager = PickupManager.Instance;
        if (manager == null) { Debug.Log("[FASE C] no hay PickupManager"); return; }

        var todos = Object.FindObjectsByType<PickupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        PickupBase elegido = null;
        foreach (var p in todos)
            if (p != null && p.Activo && p.Datos != null) { elegido = p; break; }

        if (elegido == null) { Debug.Log("[FASE C] no hay ningún pickup activo en la pista"); return; }

        var log = new StringBuilder();
        log.AppendLine("=== FASE C: recoger ===");
        log.AppendLine($"pickup elegido: {elegido.name} tipo={elegido.Datos.tipo} " +
                       $"pos={elegido.transform.position}");

        float energiaAntes = PlayerStats.Instance != null ? PlayerStats.Instance.energy : -1f;
        int escudosAntes   = manager.EscudosActivos;

        manager.Recoger(elegido);

        log.AppendLine($"activo después de recoger = {elegido.Activo} (esperado False)");

        switch (elegido.Datos.tipo)
        {
            case TipoPickup.Energia:
                log.AppendLine($"energía {energiaAntes:0} -> " +
                               $"{(PlayerStats.Instance != null ? PlayerStats.Instance.energy : -1f):0}");
                break;
            case TipoPickup.Turbo:
                log.AppendLine($"multiplicador = {manager.MultiplicadorVelocidad:0.00}");
                break;
            case TipoPickup.Iman:
                log.AppendLine($"imán activo = {manager.ImanActivo} radio = {manager.RadioImanActual:0.0}");
                break;
            case TipoPickup.Escudo:
                log.AppendLine($"escudos {escudosAntes} -> {manager.EscudosActivos}");
                break;
        }

        var hud = PickupHUD.InstanceOrCreate();
        log.AppendLine($"PickupHUD: {(hud != null ? "creado" : "NO se pudo crear")}");

        Debug.Log(log.ToString());
    }
}
