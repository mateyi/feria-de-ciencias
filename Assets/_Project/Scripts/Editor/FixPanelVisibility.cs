using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Corrige la visibilidad de los paneles: solo MainMenu visible al inicio.
/// También ajusta el layout del MainMenu para que se vea bien.
/// </summary>
public class FixPanelVisibility
{
    public static void Execute()
    {
        FixPanels();
        FixMainMenuLayout();
        FixGameOverLayout();
        FixGameplayLayout();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MundialRunner] ✅ Visibilidad de paneles corregida.");
    }

    private static void FixPanels()
    {
        // Solo MainMenu activo, el resto desactivado
        SetPanelActive("Canvas/MainMenuPanel",  true);
        SetPanelActive("Canvas/GameplayPanel",  false);
        SetPanelActive("Canvas/ShopPanel",      false);
        SetPanelActive("Canvas/CharactersPanel",false);
        SetPanelActive("Canvas/SettingsPanel",  false);
        SetPanelActive("Canvas/GameOverPanel",  false);
        SetPanelActive("Canvas/DecisionPanel",  false);
    }

    private static void SetPanelActive(string path, bool active)
    {
        GameObject go = GameObject.Find(path);
        // GameObject.Find no encuentra objetos inactivos, usar transform search
        if (go == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            string childName = path.Replace("Canvas/", "");
            Transform t = canvas.transform.Find(childName);
            if (t != null) go = t.gameObject;
        }
        if (go != null)
        {
            go.SetActive(active);
            EditorUtility.SetDirty(go);
        }
    }

    private static void FixMainMenuLayout()
    {
        GameObject panel = GameObject.Find("Canvas/MainMenuPanel");
        if (panel == null) return;

        // Fondo del panel
        Image bg = panel.GetComponent<Image>();
        if (bg) bg.color = new Color(0.05f, 0.08f, 0.18f, 1f);

        // Título del juego
        GameObject titleGO = panel.transform.Find("Text (TMP)")?.gameObject;
        if (titleGO != null)
        {
            TMP_Text t = titleGO.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.text = "⚽ MUNDIAL RUNNER";
                t.fontSize = 52;
                t.color = new Color(1f, 0.85f, 0.1f);
                t.fontStyle = FontStyles.Bold;
                t.alignment = TextAlignmentOptions.Center;
            }
            RectTransform rt = titleGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.05f, 0.82f);
                rt.anchorMax = new Vector2(0.95f, 0.97f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
        }

        // Monedas
        RepositionText("Canvas/MainMenuPanel/monedas", new Vector2(0.05f, 0.72f), new Vector2(0.45f, 0.82f), "🪙 0", 22, Color.yellow);
        // Mejor puntaje
        RepositionText("Canvas/MainMenuPanel/mejor",   new Vector2(0.55f, 0.72f), new Vector2(0.95f, 0.82f), "🏆 Mejor: 0", 22, new Color(0.8f, 0.9f, 1f));
        // Nivel
        RepositionText("Canvas/MainMenuPanel/nivel",   new Vector2(0.3f,  0.65f), new Vector2(0.7f,  0.73f), "Nivel 1", 20, new Color(0.7f, 1f, 0.7f));

        // Botones
        RepositionButton("Canvas/MainMenuPanel/jugar",      new Vector2(0.1f, 0.50f), new Vector2(0.9f, 0.63f), "▶ JUGAR",      new Color(0.15f, 0.65f, 0.25f));
        RepositionButton("Canvas/MainMenuPanel/tienda",     new Vector2(0.1f, 0.37f), new Vector2(0.9f, 0.49f), "🛒 TIENDA",    new Color(0.20f, 0.40f, 0.75f));
        RepositionButton("Canvas/MainMenuPanel/personajes", new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.36f), "👤 JUGADORES", new Color(0.55f, 0.20f, 0.70f));
        RepositionButton("Canvas/MainMenuPanel/config",     new Vector2(0.1f, 0.11f), new Vector2(0.9f, 0.23f), "⚙ CONFIG",    new Color(0.35f, 0.35f, 0.45f));
    }

    private static void FixGameOverLayout()
    {
        GameObject panel = null;
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            Transform t = canvas.transform.Find("GameOverPanel");
            if (t != null) panel = t.gameObject;
        }
        if (panel == null) return;

        Image bg = panel.GetComponent<Image>();
        if (bg) bg.color = new Color(0.05f, 0.05f, 0.15f, 0.97f);

        // Reposicionar elementos del GameOver
        RepositionTextInPanel(panel, "TitleText",     new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.97f));
        RepositionTextInPanel(panel, "FinalScoreText",new Vector2(0.1f,  0.70f), new Vector2(0.9f,  0.82f));
        RepositionTextInPanel(panel, "BestScoreText", new Vector2(0.1f,  0.60f), new Vector2(0.9f,  0.70f));
        RepositionTextInPanel(panel, "StatsText",     new Vector2(0.1f,  0.35f), new Vector2(0.9f,  0.60f));
        RepositionButtonInPanel(panel, "RetryButton", new Vector2(0.05f, 0.10f), new Vector2(0.47f, 0.30f));
        RepositionButtonInPanel(panel, "MenuButton",  new Vector2(0.53f, 0.10f), new Vector2(0.95f, 0.30f));
    }

    private static void FixGameplayLayout()
    {
        GameObject panel = GameObject.Find("Canvas/GameplayPanel");
        if (panel == null) return;

        Image bg = panel.GetComponent<Image>();
        if (bg) bg.color = new Color(0, 0, 0, 0);  // transparente

        // Reposicionar HUD elements
        RepositionSlider("Canvas/GameplayPanel/progreso", new Vector2(0.1f, 0.93f), new Vector2(0.9f, 0.99f));
        RepositionSlider("Canvas/GameplayPanel/energia",  new Vector2(0.02f, 0.85f), new Vector2(0.25f, 0.92f));
        RepositionText("Canvas/GameplayPanel/monedas.m",  new Vector2(0.02f, 0.92f), new Vector2(0.25f, 0.99f), "🪙 0", 24, Color.yellow);
        RepositionText("Canvas/GameplayPanel/score",      new Vector2(0.75f, 0.92f), new Vector2(0.98f, 0.99f), "0", 28, Color.white);

        // Botón pausa (el "<" existente)
        GameObject pauseBtn = null;
        Transform t = panel.transform.Find("<");
        if (t != null) pauseBtn = t.gameObject;
        if (pauseBtn != null)
        {
            RectTransform rt = pauseBtn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.02f, 0.92f);
                rt.anchorMax = new Vector2(0.12f, 0.99f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            TMP_Text txt = pauseBtn.GetComponentInChildren<TMP_Text>();
            if (txt) { txt.text = "⏸"; txt.fontSize = 22; }
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void RepositionText(string path, Vector2 ancMin, Vector2 ancMax,
        string text, float fontSize, Color color)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) { rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null) { tmp.text = text; tmp.fontSize = fontSize; tmp.color = color; tmp.alignment = TextAlignmentOptions.Center; }
        EditorUtility.SetDirty(go);
    }

    private static void RepositionButton(string path, Vector2 ancMin, Vector2 ancMax,
        string label, Color color)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) { rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = new Vector2(8,4); rt.offsetMax = new Vector2(-8,-4); }
        Image img = go.GetComponent<Image>();
        if (img) img.color = color;
        TMP_Text txt = go.GetComponentInChildren<TMP_Text>();
        if (txt != null) { txt.text = label; txt.fontSize = 26; txt.color = Color.white; txt.fontStyle = FontStyles.Bold; txt.alignment = TextAlignmentOptions.Center; }
        EditorUtility.SetDirty(go);
    }

    private static void RepositionSlider(string path, Vector2 ancMin, Vector2 ancMax)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null) { rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = rt.offsetMax = Vector2.zero; }
        EditorUtility.SetDirty(go);
    }

    private static void RepositionTextInPanel(GameObject panel, string childName,
        Vector2 ancMin, Vector2 ancMax)
    {
        Transform t = panel.transform.Find(childName);
        if (t == null) return;
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null) { rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = new Vector2(5,2); rt.offsetMax = new Vector2(-5,-2); }
        EditorUtility.SetDirty(t.gameObject);
    }

    private static void RepositionButtonInPanel(GameObject panel, string childName,
        Vector2 ancMin, Vector2 ancMax)
    {
        Transform t = panel.transform.Find(childName);
        if (t == null) return;
        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt != null) { rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.offsetMin = new Vector2(8,4); rt.offsetMax = new Vector2(-8,-4); }
        EditorUtility.SetDirty(t.gameObject);
    }
}
