using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Configura el panel de Game Over con textos y botones.
/// </summary>
public class SetupGameOverPanel
{
    public static void Execute()
    {
        GameObject gameOverPanel = GameObject.Find("Canvas/GameOverPanel");
        if (gameOverPanel == null)
        {
            Debug.LogError("[MundialRunner] GameOverPanel no encontrado.");
            return;
        }

        // Limpiar hijos existentes
        for (int i = gameOverPanel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(gameOverPanel.transform.GetChild(i).gameObject);

        // Fondo oscuro
        Image bg = gameOverPanel.GetComponent<Image>();
        if (bg) bg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

        // Título
        TMP_Text title = CreateText(gameOverPanel.transform, "TitleText",
            new Vector2(0.1f, 0.75f), new Vector2(0.9f, 0.95f),
            "⚽ FIN DEL PARTIDO", 42, new Color(1f, 0.8f, 0.1f));
        title.fontStyle = FontStyles.Bold;

        // Puntaje final
        TMP_Text finalScore = CreateText(gameOverPanel.transform, "FinalScoreText",
            new Vector2(0.1f, 0.62f), new Vector2(0.9f, 0.76f),
            "Puntaje: 0", 32, Color.white);

        // Mejor puntaje
        TMP_Text bestScore = CreateText(gameOverPanel.transform, "BestScoreText",
            new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.63f),
            "Mejor: 0", 24, new Color(0.8f, 0.9f, 1f));

        // Stats del jugador
        TMP_Text stats = CreateText(gameOverPanel.transform, "StatsText",
            new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.52f),
            "⚡ Velocidad: 50\n💪 Energía: 50\n⚽ Habilidad: 50\n🌟 Popularidad: 50",
            18, new Color(0.7f, 1f, 0.7f));

        // Botón Reintentar
        GameObject retryBtn = CreateButton(gameOverPanel.transform, "RetryButton",
            new Vector2(0.1f, 0.08f), new Vector2(0.48f, 0.26f),
            "🔄 REINTENTAR", new Color(0.2f, 0.7f, 0.3f));

        // Botón Menú
        GameObject menuBtn = CreateButton(gameOverPanel.transform, "MenuButton",
            new Vector2(0.52f, 0.08f), new Vector2(0.9f, 0.26f),
            "🏠 MENÚ", new Color(0.3f, 0.4f, 0.8f));

        // Conectar GameOverUI
        GameOverUI goUI = gameOverPanel.GetComponent<GameOverUI>();
        if (goUI == null) goUI = gameOverPanel.AddComponent<GameOverUI>();

        SerializedObject so = new SerializedObject(goUI);
        so.FindProperty("finalScoreText").objectReferenceValue = finalScore;
        so.FindProperty("bestScoreText").objectReferenceValue  = bestScore;
        so.FindProperty("statsText").objectReferenceValue      = stats;
        so.FindProperty("retryButton").objectReferenceValue    = retryBtn.GetComponent<Button>();
        so.FindProperty("menuButton").objectReferenceValue     = menuBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        // Conectar botones
        RunnerGameManager rgm = Object.FindFirstObjectByType<RunnerGameManager>();
        UIManager uim = Object.FindFirstObjectByType<UIManager>();

        Button retryBtnComp = retryBtn.GetComponent<Button>();
        Button menuBtnComp  = menuBtn.GetComponent<Button>();

        if (retryBtnComp != null && rgm != null)
        {
            retryBtnComp.onClick.RemoveAllListeners();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(retryBtnComp.onClick, rgm.StartGame);
        }
        if (menuBtnComp != null && uim != null)
        {
            menuBtnComp.onClick.RemoveAllListeners();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(menuBtnComp.onClick, uim.OpenMainMenu);
        }

        EditorUtility.SetDirty(gameOverPanel);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[MundialRunner] ✅ GameOver panel configurado.");
    }

    private static TMP_Text CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, string text, float fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(5, 2);
        rt.offsetMax = new Vector2(-5, -2);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private static GameObject CreateButton(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, string label, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(5, 5);
        rt.offsetMax = new Vector2(-5, -5);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();

        // Texto del botón
        TMP_Text txt = CreateText(go.transform, "Text", Vector2.zero, Vector2.one, label, 20, Color.white);
        txt.fontStyle = FontStyles.Bold;

        return go;
    }
}
