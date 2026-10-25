#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// SONDA TEMPORAL — mide el efecto real de las 8 mejoras en Play Mode.
/// Compara el valor con nivel 0 contra el valor con nivel alto, llamando por
/// reflexion a los metodos internos que aplican cada efecto.
/// action = "probar".
/// </summary>
public static class _TestEfectos
{
    private const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Execute(string action)
    {
        if (!Application.isPlaying) { Debug.LogError("[Efectos] solo en Play Mode"); return; }
        if (action != "probar") return;

        var sb = new StringBuilder();
        sb.AppendLine("===== EFECTOS DE LAS MEJORAS =====");

        var um     = UpgradeManager.Instance;
        var rm     = RunnerGameManager.Instance;
        var penal  = UnityEngine.Object.FindFirstObjectByType<PenaltyMinigameManager>(
                         FindObjectsInactive.Include);

        if (um == null || rm == null)
        {
            Debug.LogError($"[Efectos] faltan referencias um={um} rm={rm}");
            return;
        }

        // El jugador y sus stats se instancian recien al arrancar la carrera.
        rm.StartGame();
        var player = PlayerController.Instance;
        var ps     = PlayerStats.Instance;

        if (player == null || ps == null)
        {
            Debug.LogError($"[Efectos] no aparecieron player={player} ps={ps}");
            return;
        }

        // Guardar niveles originales para restaurarlos al final.
        var ids = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));
        var originales = new int[ids.Length];
        for (int i = 0; i < ids.Length; i++) originales[i] = SaveManager.GetNivel(ids[i]);
        int monedasOrig = SaveManager.Data.coins;

        var sbOrig = new StringBuilder("niveles originales:");
        for (int i = 0; i < ids.Length; i++) sbOrig.Append($" {ids[i]}={originales[i]}");
        sb.AppendLine(sbOrig.ToString());

        // ─── 1. VELOCIDAD ────────────────────────────────────────────────────
        Todos(0);
        rm.StartGame();
        float velocidad0 = player.CurrentSpeed;
        SaveManager.SetNivel(UpgradeId.Velocidad, 5);
        float velocidad5 = player.CurrentSpeed;
        sb.AppendLine($"[Velocidad] nivel0={velocidad0:F2} nivel5={velocidad5:F2} " +
                      $"ratio={velocidad5 / velocidad0:F3} (esperado 1.300)");

        // ─── 2. RESISTENCIA (gasto de energia por frame) ─────────────────────
        Todos(0);
        ps.energy = 100f;
        Llamar(rm, "Update");
        float gasto0 = 100f - ps.energy;
        SaveManager.SetNivel(UpgradeId.Resistencia, 5);
        ps.energy = 100f;
        Llamar(rm, "Update");
        float gasto5 = 100f - ps.energy;
        sb.AppendLine($"[Resistencia] gasto0={gasto0:F4} gasto5={gasto5:F4} " +
                      $"ratio={gasto5 / gasto0:F3} (esperado 0.500)");

        // ─── 3. CONTROL (avance lateral por frame al cambiar de carril) ──────
        Todos(0);
        Campo(player, "_targetX", 100f);
        float xAntes = player.transform.position.x;
        Llamar(player, "Update");
        float avance0 = player.transform.position.x - xAntes;

        SaveManager.SetNivel(UpgradeId.Control, 3);
        Campo(player, "_targetX", 100f);
        float xMedio = player.transform.position.x;
        Llamar(player, "Update");
        float avance3 = player.transform.position.x - xMedio;
        sb.AppendLine($"[Control] avance0={avance0:F4} avance3={avance3:F4} " +
                      $"ratio={avance3 / avance0:F3} (esperado 1.360)");

        // ─── 4. POTENCIA (duracion del tiro) ─────────────────────────────────
        if (penal != null)
        {
            Todos(0);
            float dur0 = (float)Metodo(penal, "DuracionTiroEfectiva").Invoke(penal, null);
            SaveManager.SetNivel(UpgradeId.Potencia, 5);
            float dur5 = (float)Metodo(penal, "DuracionTiroEfectiva").Invoke(penal, null);
            sb.AppendLine($"[Potencia] duracion0={dur0:F3}s duracion5={dur5:F3}s " +
                          $"{(dur5 < dur0 ? "baja la duracion: OK" : "SIN CAMBIO")}");
        }
        else sb.AppendLine("[Potencia] no encontre PenaltyMinigameManager");

        // ─── 5. DEFINICION (precision del arquero) ───────────────────────────
        if (penal != null)
        {
            Todos(0);
            float prec0 = (float)Metodo(penal, "Precisi").Invoke(penal, null);
            SaveManager.SetNivel(UpgradeId.Definicion, 5);
            float prec5 = (float)Metodo(penal, "Precisi").Invoke(penal, null);
            sb.AppendLine($"[Definicion] precision0={prec0:F3} precision5={prec5:F3} " +
                          $"{(prec5 < prec0 ? "baja la precision del arquero: OK" : "SIN CAMBIO")}");
        }

        // ─── 6 y 7. SUERTE e IMAN (lata de prueba) ───────────────────────────
        var colliderJugador = player.GetComponent<Collider>();
        bool tieneTagPlayer = colliderJugador != null && colliderJugador.CompareTag("Player");
        sb.AppendLine($"[Suerte/Iman] el jugador tiene tag 'Player': {tieneTagPlayer}");

        Todos(0);
        float radio0 = RadioDeLata(0.35f, out var lataA);
        int monedasAntes = SaveManager.Data.coins;
        Llamar(lataA, "OnTriggerEnter", colliderJugador);
        int monedas0 = SaveManager.Data.coins - monedasAntes;
        UnityEngine.Object.DestroyImmediate(lataA);

        SaveManager.SetNivel(UpgradeId.Suerte, 4);
        var lataB = CrearLata(0.35f);
        int monedasAntes2 = SaveManager.Data.coins;
        Llamar(lataB, "OnTriggerEnter", colliderJugador);
        int monedas4 = SaveManager.Data.coins - monedasAntes2;
        UnityEngine.Object.DestroyImmediate(lataB);
        sb.AppendLine($"[Suerte] monedas por lata nivel0={monedas0} nivel4={monedas4} (esperado 1 y 3)");

        Todos(0);
        SaveManager.SetNivel(UpgradeId.Iman, 4);
        float radio4 = RadioDeLata(0.35f, out var lataC);
        UnityEngine.Object.DestroyImmediate(lataC);
        sb.AppendLine($"[Iman] radio nivel0={radio0:F2} nivel4={radio4:F2} " +
                      $"diferencia={radio4 - radio0:F2} (esperado +2.00)");

        // ─── 8. ESCUDO ───────────────────────────────────────────────────────
        Todos(0);
        SaveManager.SetNivel(UpgradeId.Escudo, 2);
        rm.StartGame();
        sb.AppendLine($"[Escudo] al empezar: escudos={um.EscudosRestantes} " +
                      $"vivo={Vivo(player)}");
        player.Die();
        sb.AppendLine($"[Escudo] choque 1: escudos={um.EscudosRestantes} vivo={Vivo(player)}");
        player.Die();
        sb.AppendLine($"[Escudo] choque 2: escudos={um.EscudosRestantes} vivo={Vivo(player)}");
        player.Die();
        sb.AppendLine($"[Escudo] choque 3 (sin escudos): escudos={um.EscudosRestantes} " +
                      $"vivo={Vivo(player)}");

        // ─── Restaurar ───────────────────────────────────────────────────────
        for (int i = 0; i < ids.Length; i++) SaveManager.SetNivel(ids[i], originales[i]);
        SaveManager.Data.coins = monedasOrig;
        SaveManager.Guardar();
        if (UIManager.Instance != null) UIManager.Instance.OpenMainMenu();

        var sbFin = new StringBuilder("restaurado:");
        for (int i = 0; i < ids.Length; i++) sbFin.Append($" {ids[i]}={SaveManager.GetNivel(ids[i])}");
        sb.AppendLine(sbFin.ToString());

        Debug.Log(sb.ToString());
    }

    // ─── Utilidades ──────────────────────────────────────────────────────────

    private static void Todos(int nivel)
    {
        foreach (UpgradeId id in (UpgradeId[])Enum.GetValues(typeof(UpgradeId)))
            SaveManager.SetNivel(id, nivel);
    }

    private static Coin CrearLata(float radio)
    {
        var go = new GameObject("LataDePrueba");
        var col = go.AddComponent<SphereCollider>();
        col.radius = radio;
        col.isTrigger = true;
        return go.AddComponent<Coin>();
    }

    /// <summary>Crea una lata, le corre su Start y devuelve el radio final.</summary>
    private static float RadioDeLata(float radioBase, out Coin lata)
    {
        lata = CrearLata(radioBase);
        Metodo(lata, "Start").Invoke(lata, null);
        return lata.GetComponent<SphereCollider>().radius;
    }

    private static bool Vivo(PlayerController p)
    {
        var prop = typeof(PlayerController).GetProperty("IsAlive",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return prop != null ? (bool)prop.GetValue(p) : false;
    }

    private static MethodInfo Metodo(object obj, string nombre)
    {
        var m = obj.GetType().GetMethod(nombre, F);
        if (m != null) return m;

        // Busqueda por prefijo, por si el nombre lleva acentos.
        foreach (var cand in obj.GetType().GetMethods(F))
            if (cand.Name.StartsWith(nombre, StringComparison.Ordinal)) return cand;

        throw new MissingMethodException($"{obj.GetType().Name}.{nombre}");
    }

    private static void Llamar(object obj, string nombre, params object[] args)
        => Metodo(obj, nombre).Invoke(obj, args);

    private static void Campo(object obj, string nombre, object valor)
        => obj.GetType().GetField(nombre, F)?.SetValue(obj, valor);
}
#endif
