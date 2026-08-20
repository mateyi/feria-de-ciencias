using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Resetea la escala de todos los elementos UI a (1,1,1) y ajusta el CanvasScaler
/// para que el juego se vea bien en pantalla vertical (1080x1920).
/// </summary>
public class FixButtonScales
{
    public static void Execute()
    {
        // Fix CanvasScaler
        FixCanvasScaler();

        // Reset all UI element scales in MainMenuPanel
        ResetScalesInPanel("Canvas/MainMenuPanel");
        ResetScalesInPanel("Canvas/GameplayPanel");
        ResetScalesInPanel("Canvas/GameOverPanel");

        // Fix button text visibility
        FixAllButtonTexts();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MundialRunner] ✅ Escalas UI corregidas.");
    }

    private static void FixCanvasScaler()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        CanvasScaler cs = canvas.GetComponent<CanvasScaler>();
        if (cs == null) return;

        SerializedObject so = new SerializedObject(cs);
        // Mantener 1080x1920 portrait pero ajustar match a 0 (width-based)
        so.FindProperty("m_ReferenceResolution").vector2Value = new Vector2(1080, 1920);
        so.FindProperty("m_MatchWidthOrHeight").floatValue = 0f; // match width
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cs);
        Debug.Log("[MundialRunner] CanvasScaler ajustado a 1080x1920, match width.");
    }

    private static void ResetScalesInPanel(string panelPath)
    {
        GameObject panel = null;

        // Try active first
        panel = GameObject.Find(panelPath);

        // If inactive, search through canvas children
        if (panel == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                string childPath = panelPath.Replace("Canvas/", "");
                Transform t = canvas.transform.Find(childPath);
                if (t != null) panel = t.gameObject;
            }
        }

        if (panel == null) return;

        // Reset scale of panel itself
        panel.transform.localScale = Vector3.one;

        // Reset scale of all children recursively
        ResetScalesRecursive(panel.transform);
        EditorUtility.SetDirty(panel);
    }

    private static void ResetScalesRecursive(Transform t)
    {
        t.localScale = Vector3.one;
        foreach (Transform child in t)
            ResetScalesRecursive(child);
    }

    private static void FixAllButtonTexts()
    {
        // Fix each button's text child
        string[] buttonPaths = {
            "Canvas/MainMenuPanel/jugar",
            "Canvas/MainMenuPanel/tienda",
            "Canvas/MainMenuPanel/personajes",
            "Canvas/MainMenuPanel/config"
        };

        string[] labels = { "▶  JUGAR", "🛒  TIENDA", "👤  JUGADORES", "⚙  CONFIG" };

        for (int i = 0; i < buttonPaths.Length; i++)
        {
            GameObject btn = GameObject.Find(buttonPaths[i]);
            if (btn == null) continue;

            // Fix button RectTransform - use absolute pixel offsets instead of anchors
            // to avoid scale issues
            RectTransform brt = btn.GetComponent<RectTransform>();
            if (brt != null)
            {
                brt.localScale = Vector3.one;
            }

            // Fix text child
            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                txt.text = labels[i];
                txt.fontSize = 36;
                txt.color = Color.white;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.enableWordWrapping = false;
                txt.overflowMode = TextOverflowModes.Overflow;

                RectTransform trt = txt.GetComponent<RectTransform>();
                if (trt != null)
                {
                    trt.localScale = Vector3.one;
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;
                }
                EditorUtility.SetDirty(txt.gameObject);
            }
            EditorUtility.SetDirty(btn);
        }
    }
}
