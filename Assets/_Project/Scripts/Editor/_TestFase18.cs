#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// SONDA TEMPORAL — prueba la fase 18 en Play Mode: arranque con jugador
/// generico, precios, compra con monedas, cambio de camiseta en el modelo 3D y
/// scroll del panel. Deja el guardado como estaba.
/// action = "probar".
/// </summary>
public static class _TestFase18
{
    private static void Paso(string texto) => Debug.Log("[Test18] " + texto);

    public static void Execute(string action)
    {
        if (!Application.isPlaying) { Debug.LogError("[Test18] solo en Play Mode"); return; }

        var cm = CharacterManager.Instance;
        var rm = RunnerGameManager.Instance;
        if (cm == null || rm == null) { Debug.LogError("[Test18] faltan singletons"); return; }

        // Guardamos el estado para restaurarlo al final.
        string  equipadoOriginal = SaveManager.Data.personajeEquipado;
        int     monedasOriginal  = SaveManager.Data.coins;
        var     compradosOriginal = new List<string>(SaveManager.Data.personajesComprados);

        Paso("===== INICIO =====");
        Paso("comprados al entrar: " + (compradosOriginal.Count == 0 ? "(ninguno)" : string.Join(",", compradosOriginal)));
        Paso("equipado guardado: '" + equipadoOriginal + "' -> resuelto: " +
             (cm.Equipado != null ? cm.Equipado.id : "GENERICO"));
        Paso("monedas: " + GameData.Instance.Coins);

        // 1. Arranque: tiene que jugar el generico.
        rm.StartGame();
        var stats = PlayerStats.Instance;
        var piel  = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Paso("generico -> stats vel=" + stats.speed + " ener=" + stats.energy +
             " hab=" + stats.skill + " pop=" + stats.popularity + " (esperado 50/100/50/50)");
        Paso("generico -> material del modelo: " + (piel != null && piel.sharedMaterial != null ? piel.sharedMaterial.name : "NULL"));

        // 2. Panel: tarjetas, precios y estado de los botones.
        UIManager.Instance.OpenCharacters();
        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (panel == null) { Debug.LogError("[Test18] no encontre el panel"); return; }

        var grid = panel.transform.Find("ScrollView/Viewport/Grid");
        if (grid == null) { Debug.LogError("[Test18] no encontre ScrollView/Viewport/Grid"); return; }
        Paso("grilla: " + grid.childCount + " hijos");

        // Scroll
        var sr = panel.GetComponentInChildren<ScrollRect>(true);
        if (sr != null && sr.viewport != null && sr.content != null)
            Paso("scroll: vertical=" + sr.vertical + " alto contenido=" + sr.content.rect.height +
                 " alto ventana=" + sr.viewport.rect.height +
                 " hace falta deslizar=" + (sr.content.rect.height > sr.viewport.rect.height));
        else
            Debug.LogError("[Test18] no hay ScrollRect configurado");

        for (int i = 0; i < grid.childCount; i++)
        {
            var h = grid.GetChild(i);
            if (!h.gameObject.activeSelf) continue;
            var etiqueta = h.Find("Boton/Label")?.GetComponent<TMP_Text>();
            var precio   = h.Find("Precio")?.GetComponent<TMP_Text>();
            var boton    = h.Find("Boton")?.GetComponent<Button>();
            Paso("  " + h.name + " | precio='" + (precio != null ? precio.text : "?") +
                 "' boton='" + (etiqueta != null ? etiqueta.text : "?") +
                 "' interactable=" + (boton != null ? boton.interactable.ToString() : "?"));
        }

        // 3. Sin plata no se puede comprar.
        var mbappe = cm.PorId("Mbappe");
        var carta  = grid.Find("Carta_Mbappe");
        var btn    = carta != null ? carta.Find("Boton").GetComponent<Button>() : null;
        Paso("Mbappe con " + GameData.Instance.Coins + " monedas: interactable=" + btn.interactable +
             " (precio " + mbappe.precio + ")");

        // 4. Con plata alcanza y la compra descuenta.
        GameData.Instance.AddCoins(mbappe.precio - GameData.Instance.Coins + 500);
        panel.Refrescar();
        Paso("ahora con " + GameData.Instance.Coins + " monedas: interactable=" + btn.interactable);

        int antes = GameData.Instance.Coins;
        btn.onClick.Invoke();
        Paso("compra de Mbappe: monedas " + antes + " -> " + GameData.Instance.Coins +
             " (esperado " + (antes - mbappe.precio) + ")");
        Paso("equipado ahora: " + (cm.Equipado != null ? cm.Equipado.id : "GENERICO"));
        Paso("lo posee: " + cm.Posee(mbappe));

        // 5. La camiseta del modelo tiene que haber cambiado.
        piel = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Paso("material del modelo tras comprar: " +
             (piel != null && piel.sharedMaterial != null ? piel.sharedMaterial.name : "NULL") +
             " (esperado Kit_Mbappe)");

        // 6. Nueva carrera con las estadisticas del comprado.
        rm.StartGame();
        stats = PlayerStats.Instance;
        Paso("Mbappe -> stats vel=" + stats.speed + " ener=" + stats.energy +
             " hab=" + stats.skill + " pop=" + stats.popularity + " (esperado 64/100/56/52)");

        // 7. Guardado en disco.
        try { Paso("archivo: " + System.IO.File.ReadAllText(SaveManager.RutaArchivo)); }
        catch (System.Exception e) { Paso("no pude leer el archivo: " + e.Message); }

        // 8. Restaurar.
        SaveManager.Data.personajeEquipado = equipadoOriginal;
        SaveManager.Data.coins = monedasOriginal;
        SaveManager.Data.personajesComprados = compradosOriginal;
        SaveManager.Guardar();
        cm.CargarEquipado();
        cm.AplicarModelo(GameObject.FindGameObjectWithTag("Player"));
        UIManager.Instance.OpenMainMenu();

        Paso("restaurado: monedas=" + GameData.Instance.Coins +
             " equipado='" + (cm.Equipado != null ? cm.Equipado.id : "GENERICO") + "'");
        Paso("===== FIN =====");
    }
}
#endif
