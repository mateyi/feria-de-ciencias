#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;

/// <summary>Diagnóstico focalizado (3ª pasada), solo lectura.</summary>
public static class _DiagPenalty3
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== DIAG 3 ==========");

        // ── Mira, por referencia serializada del manager ─────────────────────
        var pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm != null)
        {
            var so = new SerializedObject(pmm);
            var rtProp = so.FindProperty("aimReticle");
            var rt = rtProp?.objectReferenceValue as RectTransform;
            if (rt == null) sb.AppendLine("aimReticle = <NULL>");
            else
            {
                var img = rt.GetComponent<Image>();
                sb.AppendLine($"AimReticle '{GetPath(rt)}' activeSelf={rt.gameObject.activeSelf} activeInHierarchy={rt.gameObject.activeInHierarchy}");
                sb.AppendLine($"   rect size={rt.rect.size} anchoredPos={rt.anchoredPosition} pivot={rt.pivot}");
                sb.AppendLine($"   Image: {(img == null ? "FALTA" : $"sprite={(img.sprite != null ? img.sprite.name : "<NULL -> NO SE DIBUJA>")} color={img.color} raycast=(img.raycastTarget)")}");
                if (img != null) sb.AppendLine($"   raycastTarget={img.raycastTarget} enabled={img.enabled}");
            }
        }

        // ── Jerarquía del Canvas con estados ─────────────────────────────────
        sb.AppendLine("\n--- Canvas / hijos directos ---");
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
        {
            for (int i = 0; i < canvas.transform.childCount; i++)
            {
                var ch = canvas.transform.GetChild(i);
                var cg = ch.GetComponent<CanvasGroup>();
                sb.AppendLine($"   '{ch.name}' activeSelf={ch.gameObject.activeSelf} " +
                              $"canvasGroupAlpha={(cg != null ? cg.alpha.ToString("F2") : "-")} " +
                              $"raycastBlock={(cg != null ? cg.blocksRaycasts.ToString() : "-")}");
            }
        }

        // ── PenaltyHUDPanel: hijos y estados ─────────────────────────────────
        sb.AppendLine("\n--- PenaltyHUDPanel ---");
        var hud = Object.FindFirstObjectByType<PenaltyHUD>(FindObjectsInactive.Include);
        if (hud != null)
        {
            var panel = hud.transform;
            sb.AppendLine($"   panel '{panel.name}' activeSelf={panel.gameObject.activeSelf}");
            for (int i = 0; i < panel.childCount; i++)
            {
                var ch = panel.GetChild(i);
                var img = ch.GetComponent<Image>();
                sb.AppendLine($"     - '{ch.name}' activeSelf={ch.gameObject.activeSelf} " +
                              $"{(img != null ? $"Image sprite={(img.sprite != null ? img.sprite.name : "<NULL>")} raycast={img.raycastTarget}" : "")}");
            }
        }

        // ── CameraFollow ─────────────────────────────────────────────────────
        sb.AppendLine("\n--- CameraFollow ---");
        var cf = Object.FindFirstObjectByType<CameraFollow>(FindObjectsInactive.Include);
        if (cf == null) sb.AppendLine("   FALTA CameraFollow");
        else
        {
            var cso = new SerializedObject(cf);
            var it = cso.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name == "m_Script") continue;
                string v = it.propertyType == SerializedPropertyType.ObjectReference
                    ? (it.objectReferenceValue != null ? it.objectReferenceValue.name : "<NULL>")
                    : it.ToString();
                sb.AppendLine($"     {it.name} = {v}");
            }
        }

        // ── Monos de UI: cursor / pantallas ──────────────────────────────────
        sb.AppendLine("\n--- Posibles bloqueos de cursor / pantalla ---");
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string n = mb.GetType().Name;
            if (n == "UIManager" || n == "GameplayHUD" || n == "GameData" || n == "DebugPenaltyTester" || n == "DEBUG_PenaltyTester")
                sb.AppendLine($"   {n} en '{mb.gameObject.name}' active={mb.gameObject.activeInHierarchy}");
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
