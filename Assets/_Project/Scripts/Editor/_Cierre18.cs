#if UNITY_EDITOR
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// SONDA TEMPORAL — cierra la fase 18: deja el guardado limpio (jugador
/// generico, sin compras) y confirma que todas las referencias quedaron
/// enganchadas.
/// action = "cerrar".
/// </summary>
public static class _Cierre18
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Execute(string action)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== CIERRE FASE 18 =====");

        // Guardado limpio: jugador generico y sin compras.
        SaveManager.Data.coins = 21;
        SaveManager.Data.personajeEquipado = "";
        SaveManager.Data.personajesComprados.Clear();
        SaveManager.Guardar();
        sb.AppendLine("guardado: monedas=21, equipado='', comprados=0");

        // Referencias del CharacterManager.
        var gameData = GameObject.Find("GameData");
        var cm = gameData != null ? gameData.GetComponent<CharacterManager>() : null;
        if (cm == null) { sb.AppendLine("ERROR: sin CharacterManager"); Debug.Log(sb.ToString()); return; }

        var materialGenerico = typeof(CharacterManager).GetField("materialGenerico", F)?.GetValue(cm) as Material;
        var modeloMenu       = typeof(CharacterManager).GetField("modeloMenu", F)?.GetValue(cm) as GameObject;
        sb.AppendLine("materialGenerico: " + (materialGenerico != null ? materialGenerico.name : "NULL"));
        sb.AppendLine("modeloMenu: " + (modeloMenu != null ? modeloMenu.name : "NULL"));
        sb.AppendLine("catalogo: " + (cm.Catalogo != null ? cm.Catalogo.Length : -1));

        if (cm.Catalogo != null)
        {
            foreach (var p in cm.Catalogo)
            {
                if (p == null) { sb.AppendLine("  (hueco null)"); continue; }
                sb.AppendLine("  " + p.id + " | precio=" + p.precio.ToString("N0") +
                              " | kit=" + (p.kit != null ? p.kit.name : "NINGUNO") +
                              " | retrato=" + (p.retrato != null ? p.retrato.name : "NINGUNO"));
            }
        }

        // Panel: estructura y scroll.
        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (panel != null)
        {
            var scroll = panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            sb.AppendLine("panel: " + panel.transform.name +
                          " | ScrollRect=" + (scroll != null) +
                          " | Viewport=" + (scroll != null && scroll.viewport != null ? scroll.viewport.name : "NULL") +
                          " | Content=" + (scroll != null && scroll.content != null ? scroll.content.name : "NULL"));
        }

        Debug.Log(sb.ToString());
    }
}
#endif
