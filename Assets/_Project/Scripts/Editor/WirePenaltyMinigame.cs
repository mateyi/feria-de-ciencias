using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility: wires up all PenaltyMinigame scene references in one click.
/// Menu: Mundial Runner → Wire Penalty Minigame
/// </summary>
public class WirePenaltyMinigame : Editor
{
    [MenuItem("Mundial Runner/Wire Penalty Minigame")]
    public static void Wire()
    {
        // ── 1. Find or create PenaltyMinigameManager ─────────────────────────
        PenaltyMinigameManager pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm == null)
        {
            GameObject go = new GameObject("PenaltyMinigameManager");
            pmm = go.AddComponent<PenaltyMinigameManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create PenaltyMinigameManager");
            Debug.Log("[WirePenalty] Created PenaltyMinigameManager");
        }

        SerializedObject so = new SerializedObject(pmm);

        // ── 2. Penalty spot (PenaltySpotMark) ────────────────────────────────
        GameObject spotMark = GameObject.Find("PenaltySpotMark");
        if (spotMark != null)
            so.FindProperty("penaltySpot").objectReferenceValue = spotMark.transform;

        // ── 3. Ball ───────────────────────────────────────────────────────────
        GameObject ballGO = GameObject.Find("Ball");
        if (ballGO != null)
        {
            Rigidbody rb = ballGO.GetComponent<Rigidbody>();
            if (rb != null)
                so.FindProperty("ball").objectReferenceValue = rb;
            // Ball spawn point = ball's own transform (it starts at the right place)
            so.FindProperty("ballSpawnPoint").objectReferenceValue = ballGO.transform;
        }

        // ── 4. Goalkeeper ─────────────────────────────────────────────────────
        GoalkeeperAI gk = Object.FindFirstObjectByType<GoalkeeperAI>();
        if (gk != null)
            so.FindProperty("goalkeeper").objectReferenceValue = gk;

        // ── 5. Main Camera ────────────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam != null)
            so.FindProperty("mainCamera").objectReferenceValue = cam;

        // ── 6. Penalty Camera Anchor ──────────────────────────────────────────
        // Place it behind the penalty spot, looking toward the goal.
        // PenaltyArena goal is at z≈339, spot is at z≈327 (world).
        // We want the camera at spot.z - 5, elevated, looking forward.
        Transform anchor = null;
        GameObject anchorGO = GameObject.Find("PenaltyCameraAnchor");
        if (anchorGO == null)
        {
            anchorGO = new GameObject("PenaltyCameraAnchor");
            Undo.RegisterCreatedObjectUndo(anchorGO, "Create PenaltyCameraAnchor");
        }

        // Position: behind the penalty spot (spot world z ≈ 327, arena at z=331)
        // Use spotMark world position if available
        Vector3 anchorPos = new Vector3(0f, 2.5f, 322f);
        if (spotMark != null)
        {
            anchorPos = spotMark.transform.position + new Vector3(0f, 2.5f, -5f);
        }
        anchorGO.transform.position = anchorPos;
        // Look toward the goal (forward +Z, slight downward tilt)
        anchorGO.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
        anchor = anchorGO.transform;
        so.FindProperty("penaltyCameraAnchor").objectReferenceValue = anchor;

        // ── 7. Target Zones (6 zones, order: TopL, TopC, TopR, BotL, BotC, BotR) ──
        string[] zoneNames = {
            "Zone_Top_L", "Zone_Top_C", "Zone_Top_R",
            "Zone_Bottom_L", "Zone_Bottom_C", "Zone_Bottom_R"
        };
        SerializedProperty zonesProp = so.FindProperty("targetZones");
        zonesProp.arraySize = 6;
        for (int i = 0; i < zoneNames.Length; i++)
        {
            GameObject zoneGO = GameObject.Find(zoneNames[i]);
            if (zoneGO != null)
                zonesProp.GetArrayElementAtIndex(i).objectReferenceValue = zoneGO.transform;
            else
                Debug.LogWarning($"[WirePenalty] Zone not found: {zoneNames[i]}");
        }

        so.ApplyModifiedProperties();

        // ── 8. Find or create PenaltyHUD in Canvas ───────────────────────────
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[WirePenalty] No Canvas found in scene. PenaltyHUD not created.");
        }
        else
        {
            PenaltyHUD existingHUD = Object.FindFirstObjectByType<PenaltyHUD>();
            if (existingHUD == null)
            {
                CreatePenaltyHUD(canvas);
            }
            else
            {
                Debug.Log("[WirePenalty] PenaltyHUD already exists, skipping creation.");
            }

            // ── 9. Find or create LevelCompleteUI panel ───────────────────────
            LevelCompleteUI existingLC = Object.FindFirstObjectByType<LevelCompleteUI>();
            if (existingLC == null)
            {
                CreateLevelCompletePanel(canvas);
            }
            else
            {
                Debug.Log("[WirePenalty] LevelCompleteUI already exists, skipping creation.");
            }

            // ── 10. Wire UIManager levelCompletePanel field ───────────────────
            UIManager uiMgr = Object.FindFirstObjectByType<UIManager>();
            if (uiMgr != null)
            {
                LevelCompleteUI lcUI = Object.FindFirstObjectByType<LevelCompleteUI>();
                if (lcUI != null)
                {
                    SerializedObject uiSO = new SerializedObject(uiMgr);
                    uiSO.FindProperty("levelCompletePanel").objectReferenceValue = lcUI.gameObject;
                    uiSO.ApplyModifiedProperties();
                }
            }
        }

        // ── 11. Wire aim reticle ──────────────────────────────────────────────
        // Find or create a simple reticle image in the Canvas
        PenaltyHUD hud = Object.FindFirstObjectByType<PenaltyHUD>();
        if (hud != null)
        {
            SerializedObject hudSO = new SerializedObject(pmm);
            GameObject reticleGO = GameObject.Find("AimReticle");
            if (reticleGO != null)
            {
                RectTransform rt = reticleGO.GetComponent<RectTransform>();
                if (rt != null)
                    hudSO.FindProperty("aimReticle").objectReferenceValue = rt;
            }
            hudSO.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(pmm);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[WirePenalty] ✅ Done wiring PenaltyMinigame references.");
    }

    // ─── PenaltyHUD panel builder ─────────────────────────────────────────────

    private static void CreatePenaltyHUD(Canvas canvas)
    {
        // Root panel (transparent, full screen)
        GameObject panelGO = new GameObject("PenaltyHUDPanel");
        Undo.RegisterCreatedObjectUndo(panelGO, "Create PenaltyHUDPanel");
        panelGO.transform.SetParent(canvas.transform, false);

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        // Semi-transparent background at bottom
        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f); // fully transparent root

        PenaltyHUD hudComp = panelGO.AddComponent<PenaltyHUD>();

        // ── Attempts text ─────────────────────────────────────────────────────
        TMP_Text attemptsText = CreateTMPText(panelGO.transform, "AttemptsText",
            "Intentos: 3 / 3",
            new Vector2(0.5f, 0.05f), new Vector2(0.5f, 0.05f),
            new Vector2(0f, 40f), 28, TextAlignmentOptions.Center);

        // ── Instruction text ──────────────────────────────────────────────────
        TMP_Text instructionText = CreateTMPText(panelGO.transform, "InstructionText",
            "Apuntá al arco y hacé clic para patear",
            new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f),
            new Vector2(0f, 30f), 22, TextAlignmentOptions.Center);
        instructionText.color = new Color(1f, 1f, 0.6f);

        // ── Result text (hidden by default) ───────────────────────────────────
        TMP_Text resultText = CreateTMPText(panelGO.transform, "ResultText",
            "¡GOL!",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), 72, TextAlignmentOptions.Center);
        resultText.color = new Color(0.2f, 0.9f, 0.2f);
        resultText.fontStyle = FontStyles.Bold;
        resultText.gameObject.SetActive(false);

        // ── Aim Reticle ───────────────────────────────────────────────────────
        GameObject reticleGO = new GameObject("AimReticle");
        Undo.RegisterCreatedObjectUndo(reticleGO, "Create AimReticle");
        reticleGO.transform.SetParent(panelGO.transform, false);
        RectTransform reticleRT = reticleGO.AddComponent<RectTransform>();
        reticleRT.sizeDelta = new Vector2(40f, 40f);
        reticleRT.anchorMin = new Vector2(0.5f, 0.5f);
        reticleRT.anchorMax = new Vector2(0.5f, 0.5f);
        reticleRT.pivot     = new Vector2(0.5f, 0.5f);
        Image reticleImg = reticleGO.AddComponent<Image>();
        reticleImg.color = new Color(1f, 1f, 1f, 0.85f);
        // Make it a crosshair-like circle using a default sprite
        reticleGO.SetActive(false); // hidden until penalty starts

        // ── Wire serialized fields on PenaltyHUD ──────────────────────────────
        SerializedObject hudSO = new SerializedObject(hudComp);
        hudSO.FindProperty("panelRoot").objectReferenceValue      = panelGO;
        hudSO.FindProperty("attemptsText").objectReferenceValue   = attemptsText;
        hudSO.FindProperty("instructionText").objectReferenceValue= instructionText;
        hudSO.FindProperty("resultText").objectReferenceValue     = resultText;
        hudSO.ApplyModifiedProperties();

        // ── Wire reticle on PenaltyMinigameManager ────────────────────────────
        PenaltyMinigameManager pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm != null)
        {
            SerializedObject pmmSO = new SerializedObject(pmm);
            pmmSO.FindProperty("aimReticle").objectReferenceValue = reticleRT;
            pmmSO.ApplyModifiedProperties();
        }

        // Start hidden — PenaltyMinigameManager.Show() activates it
        panelGO.SetActive(false);

        Debug.Log("[WirePenalty] Created PenaltyHUDPanel.");
    }

    // ─── LevelComplete panel builder ──────────────────────────────────────────

    private static void CreateLevelCompletePanel(Canvas canvas)
    {
        GameObject panelGO = new GameObject("LevelCompletePanel");
        Undo.RegisterCreatedObjectUndo(panelGO, "Create LevelCompletePanel");
        panelGO.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = panelGO.AddComponent<Image>();
        bg.color = new Color(0f, 0.1f, 0f, 0.85f);

        LevelCompleteUI lcComp = panelGO.AddComponent<LevelCompleteUI>();

        // Title
        TMP_Text titleText = CreateTMPText(panelGO.transform, "TitleText",
            "¡Nivel Completado! 🏆",
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f),
            new Vector2(0f, 0f), 52, TextAlignmentOptions.Center);
        titleText.color = new Color(1f, 0.85f, 0.1f);
        titleText.fontStyle = FontStyles.Bold;

        // Score
        TMP_Text scoreText = CreateTMPText(panelGO.transform, "ScoreText",
            "Puntaje: 0",
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
            new Vector2(0f, 0f), 32, TextAlignmentOptions.Center);
        scoreText.color = Color.white;

        // Coins
        TMP_Text coinsText = CreateTMPText(panelGO.transform, "CoinsText",
            "Monedas: 0",
            new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f),
            new Vector2(0f, 0f), 28, TextAlignmentOptions.Center);
        coinsText.color = new Color(1f, 0.85f, 0.1f);

        // Level
        TMP_Text levelText = CreateTMPText(panelGO.transform, "LevelText",
            "Nivel 2 desbloqueado",
            new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f),
            new Vector2(0f, 0f), 26, TextAlignmentOptions.Center);
        levelText.color = new Color(0.6f, 1f, 0.6f);

        // Next Level button
        Button nextBtn = CreateButton(panelGO.transform, "NextLevelButton",
            "Siguiente Nivel ▶",
            new Vector2(0.5f, 0.28f), new Vector2(300f, 60f));

        // Menu button
        Button menuBtn = CreateButton(panelGO.transform, "MenuButton",
            "Menú Principal",
            new Vector2(0.5f, 0.16f), new Vector2(300f, 60f));

        // Wire
        SerializedObject lcSO = new SerializedObject(lcComp);
        lcSO.FindProperty("titleText").objectReferenceValue      = titleText;
        lcSO.FindProperty("scoreText").objectReferenceValue      = scoreText;
        lcSO.FindProperty("coinsText").objectReferenceValue      = coinsText;
        lcSO.FindProperty("levelText").objectReferenceValue      = levelText;
        lcSO.FindProperty("nextLevelButton").objectReferenceValue= nextBtn;
        lcSO.FindProperty("menuButton").objectReferenceValue     = menuBtn;
        lcSO.ApplyModifiedProperties();

        panelGO.SetActive(false);
        Debug.Log("[WirePenalty] Created LevelCompletePanel.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static TMP_Text CreateTMPText(Transform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos,
        float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.anchoredPosition= anchoredPos;
        rt.sizeDelta       = new Vector2(600f, 60f);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = alignment;
        tmp.color     = Color.white;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchor;
        rt.anchorMax       = anchor;
        rt.anchoredPosition= Vector2.zero;
        rt.sizeDelta       = size;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.5f, 0.15f, 1f);
        Button btn = go.AddComponent<Button>();

        // Label child
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGO.AddComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;
        TMP_Text tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }
}
