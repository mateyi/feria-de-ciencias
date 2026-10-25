#if UNITY_EDITOR
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// SONDA TEMPORAL — inspecciona en modo edicion el panel de personajes:
/// estado activo, referencias serializadas de CharacterSelectUI y jerarquia.
/// action = "ver".
/// </summary>
public static class _DiagPersonajes
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Execute(string action)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== DIAGNOSTICO PANEL PERSONAJES =====");

        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (panel == null) { Debug.LogError("[DiagPersonajes] no encontre CharacterSelectUI"); return; }

        sb.AppendLine($"panel path: {CaminoCompleto(panel.transform)}");
        sb.AppendLine($"panel activo en jerarquia: {panel.gameObject.activeInHierarchy}");
        sb.AppendLine($"panel activo self: {panel.gameObject.activeSelf}");
        sb.AppendLine($"panel enabled: {panel.enabled}");

        var t = typeof(CharacterSelectUI);
        var fPer = t.GetField("personajes", F);
        var fCon = t.GetField("contenedor", F);
        var fPla = t.GetField("plantilla", F);
        var fMon = t.GetField("monedasText", F);
        var fVol = t.GetField("botonVolver", F);

        var arr = fPer != null ? fPer.GetValue(panel) as CharacterData[] : null;
        sb.AppendLine($"personajes serializado: {(arr == null ? "NULL" : arr.Length.ToString() + " entradas")}");
        if (arr != null)
        {
            for (int i = 0; i < arr.Length; i++)
                sb.AppendLine($"  [{i}] {(arr[i] == null ? "NULL" : arr[i].id)}");
        }

        var con = fCon != null ? fCon.GetValue(panel) as RectTransform : null;
        sb.AppendLine($"contenedor: {(con == null ? "NULL" : con.name)}");

        var pla = fPla != null ? fPla.GetValue(panel) as CharacterCardUI : null;
        sb.AppendLine($"plantilla: {(pla == null ? "NULL" : pla.gameObject.name)}");

        sb.AppendLine($"monedasText: {(fMon?.GetValue(panel) == null ? "NULL" : "ok")}");
        sb.AppendLine($"botonVolver: {(fVol?.GetValue(panel) == null ? "NULL" : "ok")}");

        // Jerarquia del panel.
        sb.AppendLine("--- hijos del panel ---");
        Volcar(panel.transform, 0, sb);

        Debug.Log(sb.ToString());
    }

    private static void Volcar(Transform t, int nivel, StringBuilder sb)
    {
        sb.AppendLine(new string(' ', nivel * 2) + t.name +
                      $"  [activo={t.gameObject.activeSelf} comps={t.GetComponents<Component>().Length}]");

        for (int i = 0; i < t.childCount; i++)
            Volcar(t.GetChild(i), nivel + 1, sb);
    }

    private static string CaminoCompleto(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
