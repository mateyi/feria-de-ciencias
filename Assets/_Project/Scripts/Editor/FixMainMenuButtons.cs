using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Corrige el layout del Main Menu: botones centrados con ancho correcto y texto visible.
/// </summary>
public class FixMainMenuButtons
{
    public static void Execute()
    {
        GameObject panel = GameObject.Find("Canvas/MainMenuPanel");
        if (panel == null) { Debug.LogError("MainMenuPanel no encontrado"); return; }

        // Fondo oscuro
        Image bg = panel.GetComponent<Image>();
        if (bg) bg.color = new Color(0.05f, 0.08f, 0.18f, 1f);

        // ── Título ──────────────────────────────────────────────────────────
        FixText(panel, "Text (TMP)",
            new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f),
            "⚽ MUNDIAL RUNNER", 52, new Color(1f, 0.85f, 0.1f), FontStyles.Bold);

        // ── Stats ────────────────────────────────────────────────────────────
        FixText(panel, "monedas",
            new Vector2(0.05f, 0.73f), new Vector2(0.45f, 0.82f),
            "🪙 0", 22, Color.yellow, FontStyles.Normal);

        FixText(panel, "mejor",
            new Vector2(0.55f, 0.73f), new Vector2(0.95f, 0.82f),
            "🏆 Mejor: 0", 22, new Color(0.8f, 0.9f, 1f), FontStyles.Normal);

        FixText(panel, "nivel",
            new Vector2(0.3f, 0.66f), new Vector2(0.7f, 0.74f),
            "Nivel 1", 20, new Color(0.7f, 1f, 0.7f), FontStyles.Normal);

        // ── Botones (centrados, 70% de ancho) ────────────────────────────────
        FixButton(panel, "jugar",
            new Vector2(0.15f, 0.51f), new Vector2(0.85f, 0.63f),
            "▶  JUGAR", 30, new Color(0.10f, 0.60f, 0.20f));

        FixButton(panel, "tienda",
            new Vector2(0.15f, 0.38f), new Vector2(0.85f, 0.50f),
            "🛒  TIENDA", 28, new Color(0.15f, 0.35f, 0.75f));

        FixButton(panel, "personajes",
            new Vector2(0.15f, 0.25f), new Vector2(0.85f, 0.37f),
            "👤  JUGADORES", 28, new Color(0.50f, 0.15f, 0.70f));

        FixButton(panel, "config",
            new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.24f),
            "⚙  CONFIG", 28, new Color(0.30f, 0.30f, 0.42f));

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MundialRunner] ✅ Main Menu layout corregido.");
    }

    private static void FixText(GameObject panel, string childName,
        Vector2 ancMin, Vector2 ancMax,
        string text, float fontSize, Color color, FontStyles style)
    {
        Transform t = panel.transform.Find(childName);
        if (t == null) return;

        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = new Vector2(4, 2);
            rt.offsetMax = new Vector2(-4, -2);
        }

        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
        }
        EditorUtility.SetDirty(t.gameObject);
    }

    private static void FixButton(GameObject panel, string childName,
        Vector2 ancMin, Vector2 ancMax,
        string label, float fontSize, Color bgColor)
    {
        Transform t = panel.transform.Find(childName);
        if (t == null) return;

        // Reposicionar
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = ancMin;
            rt.anchorMax = ancMax;
            rt.offsetMin = new Vector2(6, 4);
            rt.offsetMax = new Vector2(-6, -4);
        }

        // Color de fondo
        Image img = t.GetComponent<Image>();
        if (img) img.color = bgColor;

        // Texto del botón (hijo "Text (TMP)")
        TMP_Text txt = t.GetComponentInChildren<TMP_Text>(true);
        if (txt != null)
        {
            txt.text      = label;
            txt.fontSize  = fontSize;
            txt.color     = Color.white;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = false;

            // Asegurarse que el RectTransform del texto llena el botón
            RectTransform trt = txt.GetComponent<RectTransform>();
            if (trt != null)
            {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(8, 4);
                trt.offsetMax = new Vector2(-8, -4);
            }
        }
        EditorUtility.SetDirty(t.gameObject);
    }
}
