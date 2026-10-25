#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;

/// <summary>Diagnóstico focalizado (4ª pasada): paneles duplicados e instancias.</summary>
public static class _DiagPenalty4
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== DIAG 4 ==========");

        // Todos los PenaltyHUD (incluye inactivos)
        var huds = Object.FindObjectsByType<PenaltyHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"PenaltyHUD componentes: {huds.Length}");
        for (int i = 0; i < huds.Length; i++)
        {
            var h = huds[i];
            var so = new SerializedObject(h);
            var panel = so.FindProperty("panelRoot")?.objectReferenceValue as GameObject;
            var attempts = so.FindProperty("attemptsText")?.objectReferenceValue;
            var instr = so.FindProperty("instructionText")?.objectReferenceValue;
            var result = so.FindProperty("resultText")?.objectReferenceValue;
            sb.AppendLine($"  [{i}] id={h.GetInstanceID()} '{GetPath(h.transform)}' activeSelf={h.gameObject.activeSelf}");
            sb.AppendLine($"       panelRoot={(panel != null ? panel.name + " id=" + panel.GetInstanceID() : "<NULL>")} " +
                          $"attempts={(attempts != null ? attempts.name : "<NULL>")} " +
                          $"instr={(instr != null ? instr.name : "<NULL>")} " +
                          $"result={(result != null ? result.name : "<NULL>")}");
        }

        // Todos los LevelCompleteUI
        var lcs = Object.FindObjectsByType<LevelCompleteUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"LevelCompleteUI componentes: {lcs.Length}");
        for (int i = 0; i < lcs.Length; i++)
            sb.AppendLine($"  [{i}] '{GetPath(lcs[i].transform)}' activeSelf={lcs[i].gameObject.activeSelf} id={lcs[i].GetInstanceID()}");

        // A qué panel pertenece el aimReticle del manager
        var pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm != null)
        {
            var so = new SerializedObject(pmm);
            var rt = so.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;
            if (rt != null)
            {
                Transform root = rt;
                while (root.parent != null && root.parent.GetComponent<Canvas>() == null) root = root.parent;
                var hudOnRoot = root.GetComponent<PenaltyHUD>();
                sb.AppendLine($"aimReticle pertenece al panel '{root.name}' id={root.GetInstanceID()} " +
                              $"PenaltyHUD en ese panel={(hudOnRoot != null ? "SI id=" + hudOnRoot.GetInstanceID() : "NO")}");
            }
        }

        // Estado de los paneles duplicados
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
        {
            sb.AppendLine("Paneles bajo Canvas con id:");
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var ch = canvas.transform.GetChild(i);
                var hud = ch.GetComponent<PenaltyHUD>();
                var lc = ch.GetComponent<LevelCompleteUI>();
                sb.AppendLine($"  [{i}] '{ch.name}' id={ch.GetInstanceID()} activeSelf={ch.gameObject.activeSelf} " +
                              $"hijos={ch.childCount} PenaltyHUD={(hud != null)} LevelCompleteUI={(lc != null)}");
            }
        }

        Debug.Log(sb.ToString());
    }

    private static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
