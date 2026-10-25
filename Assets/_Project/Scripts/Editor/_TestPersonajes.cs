#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

/// <summary>
/// SONDA TEMPORAL — prueba el panel de personajes en Play Mode.
/// Ahora registra CADA paso por separado (no acumula todo en un unico log),
/// para que si algo falla quede rastro de donde se corto.
/// action = "probar".
/// </summary>
public static class _TestPersonajes
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    private static void Paso(string texto) => Debug.Log("[TestPersonajes] " + texto);

    public static void Execute(string action)
    {
        if (!Application.isPlaying) { Debug.LogError("[TestPersonajes] solo en Play Mode"); return; }

        Paso("===== INICIO PRUEBA DE PERSONAJES =====");

        var cm = CharacterManager.Instance;
        var rm = RunnerGameManager.Instance;
        if (cm == null || rm == null)
        {
            Debug.LogError("[TestPersonajes] faltan singletons: cm=" + (cm == null ? "NULL" : "ok") +
                           " rm=" + (rm == null ? "NULL" : "ok"));
            return;
        }

        string original = SaveManager.Data.personajeEquipado;
        string idEquipado = cm.Equipado != null ? cm.Equipado.id : "NULL";
        int totalCatalogo = cm.Catalogo != null ? cm.Catalogo.Length : -1;
        Paso("equipado guardado al entrar: '" + original + "' -> resuelto: '" + idEquipado +
             "' | catalogo=" + totalCatalogo);

        // 1. Arrancar carrera con el jugador equipado (por defecto el primero).
        rm.StartGame();
        var stats = PlayerStats.Instance;
        if (stats == null) { Debug.LogError("[TestPersonajes] PlayerStats nulo tras StartGame"); return; }
        Paso("stats con '" + idEquipado + "': vel=" + stats.speed + " ener=" + stats.energy +
             " hab=" + stats.skill + " pop=" + stats.popularity);

        // 2. Localizar el panel ANTES de abrirlo.
        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (panel == null) { Debug.LogError("[TestPersonajes] no encontre CharacterSelectUI"); return; }

        string rutaPanel = CaminoCompleto(panel.transform);
        Paso("panel encontrado en '" + rutaPanel + "': activeSelf=" + panel.gameObject.activeSelf +
             " activeInHierarchy=" + panel.gameObject.activeInHierarchy);

        // Cuantos CharacterSelectUI hay en total (por si hubiera un duplicado).
        var todos = Object.FindObjectsByType<CharacterSelectUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Paso("instancias de CharacterSelectUI en la escena: " + todos.Length);

        VolcarEstado(panel, "antes de abrir");

        // 3. Abrir el panel por el camino real.
        UIManager.Instance.OpenCharacters();
        Paso("OpenCharacters() ejecutado -> activeInHierarchy=" + panel.gameObject.activeInHierarchy);

        VolcarEstado(panel, "despues de abrir");

        // 4. Buscar la tarjeta de Haaland de forma tolerante.
        Transform grid = panel.transform.Find("Grid");
        if (grid == null)
        {
            grid = BuscarHijo(panel.transform, "Grid");
            Paso("transform.Find(\"Grid\") fue nulo; encontrado por nombre: " + (grid != null));
        }
        else
        {
            Paso("Grid encontrado, hijos=" + grid.childCount);
        }

        if (grid != null)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                var h = grid.GetChild(i);
                var card = h.GetComponent<CharacterCardUI>();
                string id = "sin componente";
                if (card != null)
                {
                    var fPer = typeof(CharacterCardUI).GetField("_personaje", F);
                    var dato = fPer != null ? fPer.GetValue(card) as CharacterData : null;
                    id = dato != null ? dato.id : "NULL";
                }
                Paso("  Grid hijo[" + i + "] '" + h.name + "' activo=" + h.gameObject.activeSelf +
                     " personaje=" + id);
            }
        }

        var carta = panel.transform.Find("Grid/Carta_Haaland");
        if (carta == null)
            Debug.LogError("[TestPersonajes] Find(\"Grid/Carta_Haaland\") == null");

        // Si el panel no construyo nada, intentamos construir a mano para saber
        // si el problema es que Construir() nunca corrio o que Construir() falla.
        if (carta == null && grid != null && grid.childCount <= 1)
        {
            Paso("la grilla quedo vacia: invoco Construir() por reflexion");
            var mConstruir = typeof(CharacterSelectUI).GetMethod("Construir", F);
            if (mConstruir == null) Paso("no encontre el metodo Construir");
            else
            {
                mConstruir.Invoke(panel, null);
                Paso("Construir() invocado -> ahora Grid hijos=" + grid.childCount);
                for (int i = 0; i < grid.childCount; i++)
                    Paso("  tras construir, hijo[" + i + "] '" + grid.GetChild(i).name + "'");
                carta = panel.transform.Find("Grid/Carta_Haaland");
            }
        }

        if (carta == null)
        {
            Debug.LogError("[TestPersonajes] ABORTO: no hay tarjeta de Haaland que tocar");
            return;
        }

        // 5. Equipar a Haaland tocando su boton.
        var boton = carta.Find("Boton");
        Button btn = boton != null ? boton.GetComponent<Button>() : null;
        if (btn == null)
        {
            Debug.LogError("[TestPersonajes] la tarjeta no tiene boton");
            return;
        }

        Paso("boton de Haaland antes: interactable=" + btn.interactable +
             " label='" + (carta.Find("Boton/Label") != null ? carta.Find("Boton/Label").GetComponent<TMP_Text>().text : "?") + "'");
        btn.onClick.Invoke();
        Debug.Log("[TestPersonajes] on click de Haaland invocado");

        idEquipado = cm.Equipado != null ? cm.Equipado.id : "NULL";
        Paso("tras equipar: equipado='" + idEquipado + "' guardado='" +
             SaveManager.Data.personajeEquipado + "' boton interactable=" + btn.interactable);

        // 6. Nueva carrera: las estadisticas tienen que reflejar a Haaland.
        rm.StartGame();
        stats = PlayerStats.Instance;
        Paso("stats con '" + idEquipado + "': vel=" + stats.speed + " ener=" + stats.energy +
             " hab=" + stats.skill + " pop=" + stats.popularity + " (esperado vel=56 hab=64 pop=54)");

        // 7. Archivo en disco.
        try { Paso("archivo: " + System.IO.File.ReadAllText(SaveManager.RutaArchivo)); }
        catch (System.Exception e) { Paso("no pude leer el archivo: " + e.Message); }

        // 8. Restaurar.
        SaveManager.Data.personajeEquipado = original;
        SaveManager.Guardar();
        cm.CargarEquipado();
        UIManager.Instance.OpenMainMenu();
        idEquipado = cm.Equipado != null ? cm.Equipado.id : "NULL";
        Paso("restaurado: equipado='" + idEquipado + "' guardado='" + SaveManager.Data.personajeEquipado + "'");
        Paso("===== FIN PRUEBA DE PERSONAJES =====");
    }

    // ─── Utilidades ───────────────────────────────────────────────────────────

    private static void VolcarEstado(CharacterSelectUI panel, string momento)
    {
        var t = typeof(CharacterSelectUI);
        var fConstruido = t.GetField("_construido", F);
        var fTarjetas   = t.GetField("_tarjetas", F);
        var fContenedor = t.GetField("contenedor", F);
        var fPlantilla  = t.GetField("plantilla", F);

        bool construido = fConstruido != null && (bool)fConstruido.GetValue(panel);
        var tarjetas = fTarjetas != null ? fTarjetas.GetValue(panel) as System.Collections.ICollection : null;
        var cont = fContenedor != null ? fContenedor.GetValue(panel) as RectTransform : null;
        var plan = fPlantilla != null ? fPlantilla.GetValue(panel) as CharacterCardUI : null;

        int hijos = cont != null ? cont.childCount : -1;

        Paso("[" + momento + "] _construido=" + construido +
             " _tarjetas=" + (tarjetas == null ? "NULL" : tarjetas.Count.ToString()) +
             " contenedor=" + (cont == null ? "NULL" : cont.name + "(" + hijos + " hijos)") +
             " plantilla=" + (plan == null ? "NULL" : plan.gameObject.name));
    }

    private static Transform BuscarHijo(Transform padre, string nombre)
    {
        for (int i = 0; i < padre.childCount; i++)
        {
            var h = padre.GetChild(i);
            if (h.name == nombre) return h;
            var encontrado = BuscarHijo(h, nombre);
            if (encontrado != null) return encontrado;
        }
        return null;
    }

    private static string CaminoCompleto(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
