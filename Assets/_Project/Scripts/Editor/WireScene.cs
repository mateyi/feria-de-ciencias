using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conecta todos los componentes de la escena: RunnerGameManager con el prefab del jugador,
/// DecisionManager con DecisionUI, GameOverUI con sus textos, etc.
/// </summary>
public class WireScene
{
    public static void Execute()
    {
        WireRunnerGameManager();
        WireDecisionManager();
        WireGameOverUI();
        WireMainMenuButtons();
        SetupPlayerSpawnPoint();
        SetupDecisionUI();
        SaveScene();
        Debug.Log("[MundialRunner] ✅ Escena conectada correctamente.");
    }

    private static void WireRunnerGameManager()
    {
        RunnerGameManager rgm = Object.FindFirstObjectByType<RunnerGameManager>();
        if (rgm == null) return;

        SerializedObject so = new SerializedObject(rgm);

        // Asignar prefab del jugador
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
        if (playerPrefab != null)
            so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;

        // Asignar spawn point (crear si no existe)
        GameObject spawnGO = GameObject.Find("PlayerSpawnPoint");
        if (spawnGO == null)
        {
            spawnGO = new GameObject("PlayerSpawnPoint");
            spawnGO.transform.position = new Vector3(0, 0.5f, 2f);
        }
        so.FindProperty("playerSpawnPoint").objectReferenceValue = spawnGO.transform;

        so.ApplyModifiedProperties();
        Debug.Log("[MundialRunner] RunnerGameManager conectado.");
    }

    private static void WireDecisionManager()
    {
        DecisionManager dm = Object.FindFirstObjectByType<DecisionManager>();
        if (dm == null) return;

        DecisionUI dui = Object.FindFirstObjectByType<DecisionUI>();
        if (dui == null) return;

        SerializedObject so = new SerializedObject(dm);
        so.FindProperty("decisionUI").objectReferenceValue = dui;
        so.ApplyModifiedProperties();
        Debug.Log("[MundialRunner] DecisionManager conectado con DecisionUI.");
    }

    private static void WireGameOverUI()
    {
        GameObject gameOverPanel = GameObject.Find("Canvas/GameOverPanel");
        if (gameOverPanel == null) return;

        GameOverUI goUI = gameOverPanel.GetComponent<GameOverUI>();
        if (goUI == null) goUI = gameOverPanel.AddComponent<GameOverUI>();

        SerializedObject so = new SerializedObject(goUI);

        // Buscar o crear textos en el panel
        TMP_Text[] texts = gameOverPanel.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length >= 1) so.FindProperty("finalScoreText").objectReferenceValue = texts[0];
        if (texts.Length >= 2) so.FindProperty("bestScoreText").objectReferenceValue  = texts[1];
        if (texts.Length >= 3) so.FindProperty("statsText").objectReferenceValue      = texts[2];

        Button[] buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
        if (buttons.Length >= 1) so.FindProperty("retryButton").objectReferenceValue = buttons[0];
        if (buttons.Length >= 2) so.FindProperty("menuButton").objectReferenceValue  = buttons[1];

        so.ApplyModifiedProperties();
    }

    private static void WireMainMenuButtons()
    {
        // Botón Jugar
        GameObject jugarBtn = GameObject.Find("Canvas/MainMenuPanel/jugar");
        if (jugarBtn != null)
        {
            Button btn = jugarBtn.GetComponent<Button>();
            RunnerGameManager rgm = Object.FindFirstObjectByType<RunnerGameManager>();
            if (btn != null && rgm != null)
            {
                // Limpiar listeners duplicados
                btn.onClick.RemoveAllListeners();
                // Añadir listener persistente
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, rgm.StartGame);
                EditorUtility.SetDirty(btn);
                Debug.Log("[MundialRunner] Botón Jugar conectado a StartGame.");
            }
        }

        // Botón Tienda
        GameObject tiendaBtn = GameObject.Find("Canvas/MainMenuPanel/tienda");
        if (tiendaBtn != null)
        {
            Button btn = tiendaBtn.GetComponent<Button>();
            UIManager uim = Object.FindFirstObjectByType<UIManager>();
            if (btn != null && uim != null)
            {
                btn.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, uim.OpenShop);
                EditorUtility.SetDirty(btn);
            }
        }

        // Botón Personajes
        GameObject persBtn = GameObject.Find("Canvas/MainMenuPanel/personajes");
        if (persBtn != null)
        {
            Button btn = persBtn.GetComponent<Button>();
            UIManager uim = Object.FindFirstObjectByType<UIManager>();
            if (btn != null && uim != null)
            {
                btn.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, uim.OpenCharacters);
                EditorUtility.SetDirty(btn);
            }
        }

        // Botón Config
        GameObject configBtn = GameObject.Find("Canvas/MainMenuPanel/config");
        if (configBtn != null)
        {
            Button btn = configBtn.GetComponent<Button>();
            UIManager uim = Object.FindFirstObjectByType<UIManager>();
            if (btn != null && uim != null)
            {
                btn.onClick.RemoveAllListeners();
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, uim.OpenSettings);
                EditorUtility.SetDirty(btn);
            }
        }
    }

    private static void SetupPlayerSpawnPoint()
    {
        GameObject spawnGO = GameObject.Find("PlayerSpawnPoint");
        if (spawnGO == null)
        {
            spawnGO = new GameObject("PlayerSpawnPoint");
            spawnGO.transform.position = new Vector3(0, 0.5f, 2f);
        }
    }

    private static void SetupDecisionUI()
    {
        // Crear panel de decisión en el Canvas si no existe
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        GameObject existingDecisionPanel = GameObject.Find("Canvas/DecisionPanel");
        if (existingDecisionPanel != null) return;

        // Crear panel de decisión
        GameObject decisionPanel = new GameObject("DecisionPanel");
        decisionPanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = decisionPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = decisionPanel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        DecisionUI dui = decisionPanel.AddComponent<DecisionUI>();

        // Sub-panel central
        GameObject centerPanel = new GameObject("CenterPanel");
        centerPanel.transform.SetParent(decisionPanel.transform, false);
        RectTransform cpRT = centerPanel.AddComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0.1f, 0.2f);
        cpRT.anchorMax = new Vector2(0.9f, 0.8f);
        cpRT.offsetMin = Vector2.zero;
        cpRT.offsetMax = Vector2.zero;
        Image cpBg = centerPanel.AddComponent<Image>();
        cpBg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);

        // Pregunta
        GameObject questionGO = new GameObject("QuestionText");
        questionGO.transform.SetParent(centerPanel.transform, false);
        RectTransform qRT = questionGO.AddComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0, 0.75f);
        qRT.anchorMax = new Vector2(1, 1f);
        qRT.offsetMin = new Vector2(10, 0);
        qRT.offsetMax = new Vector2(-10, 0);
        TMP_Text qText = questionGO.AddComponent<TextMeshProUGUI>();
        qText.text = "¿Qué decides?";
        qText.fontSize = 28;
        qText.alignment = TextAlignmentOptions.Center;
        qText.color = Color.white;

        // Timer slider
        GameObject timerGO = new GameObject("TimerSlider");
        timerGO.transform.SetParent(centerPanel.transform, false);
        RectTransform tRT = timerGO.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.1f, 0.68f);
        tRT.anchorMax = new Vector2(0.9f, 0.74f);
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        Slider timerSlider = timerGO.AddComponent<Slider>();
        timerSlider.value = 1f;

        // Opción Izquierda
        GameObject leftPanel = CreateOptionPanel(centerPanel.transform, "LeftOption",
            new Vector2(0, 0.05f), new Vector2(0.48f, 0.65f),
            new Color(0.2f, 0.7f, 0.3f, 0.9f));

        // Opción Derecha
        GameObject rightPanel = CreateOptionPanel(centerPanel.transform, "RightOption",
            new Vector2(0.52f, 0.05f), new Vector2(1f, 0.65f),
            new Color(0.8f, 0.25f, 0.2f, 0.9f));

        // Feedback panel
        GameObject feedbackPanel = new GameObject("FeedbackPanel");
        feedbackPanel.transform.SetParent(decisionPanel.transform, false);
        RectTransform fpRT = feedbackPanel.AddComponent<RectTransform>();
        fpRT.anchorMin = new Vector2(0.2f, 0.35f);
        fpRT.anchorMax = new Vector2(0.8f, 0.65f);
        fpRT.offsetMin = Vector2.zero;
        fpRT.offsetMax = Vector2.zero;
        Image fpBg = feedbackPanel.AddComponent<Image>();
        fpBg.color = new Color(0.05f, 0.05f, 0.15f, 0.95f);

        TMP_Text fbTitle = CreateText(feedbackPanel.transform, "FeedbackTitle",
            new Vector2(0, 0.6f), new Vector2(1, 1f), "¡Elegiste!", 26, Color.yellow);
        TMP_Text fbDesc = CreateText(feedbackPanel.transform, "FeedbackDesc",
            new Vector2(0, 0.3f), new Vector2(1, 0.6f), "", 18, Color.white);
        TMP_Text fbStats = CreateText(feedbackPanel.transform, "FeedbackStats",
            new Vector2(0, 0f), new Vector2(1, 0.3f), "", 16, new Color(0.8f, 1f, 0.8f));

        feedbackPanel.SetActive(false);

        // Asignar referencias al DecisionUI
        SerializedObject so = new SerializedObject(dui);
        so.FindProperty("decisionPanel").objectReferenceValue    = decisionPanel;
        so.FindProperty("questionText").objectReferenceValue     = qText;
        so.FindProperty("timerSlider").objectReferenceValue      = timerSlider;
        so.FindProperty("feedbackPanel").objectReferenceValue    = feedbackPanel;
        so.FindProperty("feedbackTitleText").objectReferenceValue = fbTitle;
        so.FindProperty("feedbackDescText").objectReferenceValue  = fbDesc;
        so.FindProperty("feedbackStatsText").objectReferenceValue = fbStats;

        // Opciones izquierda/derecha
        if (leftPanel != null)
        {
            so.FindProperty("leftOptionPanel").objectReferenceValue = leftPanel;
            TMP_Text[] leftTexts = leftPanel.GetComponentsInChildren<TMP_Text>(true);
            Button leftBtn = leftPanel.GetComponentInChildren<Button>(true);
            if (leftTexts.Length >= 1) so.FindProperty("leftLabelText").objectReferenceValue = leftTexts[0];
            if (leftTexts.Length >= 2) so.FindProperty("leftDescText").objectReferenceValue  = leftTexts[1];
            Image leftBg = leftPanel.GetComponent<Image>();
            if (leftBg) so.FindProperty("leftBg").objectReferenceValue = leftBg;
            if (leftBtn) so.FindProperty("leftButton").objectReferenceValue = leftBtn;
        }

        if (rightPanel != null)
        {
            so.FindProperty("rightOptionPanel").objectReferenceValue = rightPanel;
            TMP_Text[] rightTexts = rightPanel.GetComponentsInChildren<TMP_Text>(true);
            Button rightBtn = rightPanel.GetComponentInChildren<Button>(true);
            if (rightTexts.Length >= 1) so.FindProperty("rightLabelText").objectReferenceValue = rightTexts[0];
            if (rightTexts.Length >= 2) so.FindProperty("rightDescText").objectReferenceValue  = rightTexts[1];
            Image rightBg = rightPanel.GetComponent<Image>();
            if (rightBg) so.FindProperty("rightBg").objectReferenceValue = rightBg;
            if (rightBtn) so.FindProperty("rightButton").objectReferenceValue = rightBtn;
        }

        so.ApplyModifiedProperties();

        decisionPanel.SetActive(false);
        Debug.Log("[MundialRunner] DecisionUI panel creado y conectado.");
    }

    private static GameObject CreateOptionPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(5, 5);
        rt.offsetMax = new Vector2(-5, -5);
        Image img = panel.AddComponent<Image>();
        img.color = color;

        // Botón
        Button btn = panel.AddComponent<Button>();

        // Label
        TMP_Text label = CreateText(panel.transform, "Label",
            new Vector2(0, 0.6f), new Vector2(1, 1f), "Opción", 22, Color.white);
        label.fontStyle = FontStyles.Bold;

        // Descripción
        TMP_Text desc = CreateText(panel.transform, "Desc",
            new Vector2(0, 0f), new Vector2(1, 0.6f), "Descripción", 16, Color.white);

        return panel;
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

    private static void SaveScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }
}
