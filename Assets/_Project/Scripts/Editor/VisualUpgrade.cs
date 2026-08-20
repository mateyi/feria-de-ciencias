using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Mejora visual completa de Mundial Runner:
/// 1. Material de cancha con líneas
/// 2. Graderías low-poly con público
/// 3. Obstáculos: conos, vallas, pelotas
/// 4. Cámara tercera persona cercana
/// 5. HUD mejorado
/// </summary>
public class VisualUpgrade
{
    // ─── Constantes de layout ─────────────────────────────────────────────────
    private const float TRACK_WIDTH   = 8f;
    private const float LANE_WIDTH    = 2.5f;
    private const float SEG_LEN       = 30f;

    public static void Execute()
    {
        UpgradeMaterials();
        RebuildSegmentPrefabs();
        RebuildObstaclePrefabs();
        UpgradeCamera();
        UpgradeHUD();
        RefreshScene();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MundialRunner] ✅ Mejora visual completada.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 1. MATERIALES
    // ═══════════════════════════════════════════════════════════════════════════
    private static void UpgradeMaterials()
    {
        // Cancha: verde oscuro saturado tipo césped
        UpdateMat("Mat_Track",        new Color(0.13f, 0.45f, 0.13f), 0f, 0.3f);
        // Líneas de carril: blanco puro
        UpdateMat("Mat_TrackLine",    new Color(0.95f, 0.97f, 0.95f), 0f, 0.1f);
        // Jugador: azul brillante
        UpdateMat("Mat_Player",       new Color(0.10f, 0.40f, 0.90f), 0.2f, 0.6f);
        // Obstáculo: naranja neón (conos)
        UpdateMat("Mat_Obstacle",     new Color(1.00f, 0.45f, 0.00f), 0f, 0.4f);
        // Moneda: dorado
        UpdateMat("Mat_Coin",         new Color(1.00f, 0.78f, 0.05f), 0.3f, 0.8f);
        // Gradería inferior (base de cemento)
        UpdateMat("Mat_Crowd",        new Color(0.55f, 0.55f, 0.60f), 0f, 0.1f);

        // Nuevos materiales
        CreateMat("Mat_StandBase",    new Color(0.45f, 0.45f, 0.50f), 0f, 0.1f);
        CreateMat("Mat_StandTier",    new Color(0.30f, 0.30f, 0.38f), 0f, 0.05f);
        CreateMat("Mat_Audience_R",   new Color(0.85f, 0.15f, 0.15f), 0f, 0.1f);
        CreateMat("Mat_Audience_B",   new Color(0.15f, 0.25f, 0.80f), 0f, 0.1f);
        CreateMat("Mat_Audience_Y",   new Color(0.95f, 0.80f, 0.05f), 0f, 0.1f);
        CreateMat("Mat_Audience_W",   new Color(0.90f, 0.90f, 0.90f), 0f, 0.1f);
        CreateMat("Mat_ConePrimary",  new Color(1.00f, 0.42f, 0.00f), 0f, 0.3f);
        CreateMat("Mat_ConeStripe",   new Color(0.95f, 0.95f, 0.95f), 0f, 0.1f);
        CreateMat("Mat_HurdlePost",   new Color(0.90f, 0.90f, 0.90f), 0f, 0.2f);
        CreateMat("Mat_HurdleBar",    new Color(0.90f, 0.15f, 0.10f), 0f, 0.2f);
        CreateMat("Mat_Ball",         new Color(0.92f, 0.92f, 0.92f), 0.1f, 0.5f);
        CreateMat("Mat_BallPatch",    new Color(0.10f, 0.10f, 0.10f), 0f, 0.2f);
        CreateMat("Mat_TrackAlt",     new Color(0.11f, 0.40f, 0.11f), 0f, 0.3f); // franjas alternadas
        CreateMat("Mat_Grass",        new Color(0.18f, 0.52f, 0.12f), 0f, 0.2f); // pasto exterior
        CreateMat("Mat_RoofMetal",    new Color(0.60f, 0.62f, 0.65f), 0.1f, 0.5f);

        AssetDatabase.SaveAssets();
        Debug.Log("[MundialRunner] Materiales actualizados.");
    }

    private static void UpdateMat(string name, Color color, float metallic, float smoothness)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { CreateMat(name, color, metallic, smoothness); return; }
        mat.color = color;
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
    }

    private static Material CreateMat(string name, Color color, float metallic, float smoothness)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) { mat.color = color; mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness); EditorUtility.SetDirty(mat); return mat; }
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 2. SEGMENTOS DE PISTA
    // ═══════════════════════════════════════════════════════════════════════════
    private static void RebuildSegmentPrefabs()
    {
        DeleteAsset("Assets/_Project/Prefabs/Segment_Normal.prefab");
        DeleteAsset("Assets/_Project/Prefabs/Segment_Decision.prefab");
        AssetDatabase.Refresh();

        BuildNormalSegment();
        BuildDecisionSegment();
        AssetDatabase.Refresh();
        Debug.Log("[MundialRunner] Segmentos reconstruidos.");
    }

    private static void BuildNormalSegment()
    {
        GameObject root = new GameObject("Segment_Normal");
        TrackSegment ts = root.AddComponent<TrackSegment>();

        // ── Suelo principal (césped oscuro) ──────────────────────────────────
        GameObject floor = MakeCube(root, "Floor",
            new Vector3(0, -0.1f, SEG_LEN * 0.5f),
            new Vector3(TRACK_WIDTH, 0.2f, SEG_LEN),
            "Mat_Track");
        Object.DestroyImmediate(floor.GetComponent<BoxCollider>());
        floor.AddComponent<BoxCollider>();

        // ── Franjas alternadas de césped (efecto cortadora) ──────────────────
        float stripeW = TRACK_WIDTH / 6f;
        for (int i = 0; i < 6; i++)
        {
            if (i % 2 == 0) continue; // solo franjas impares
            float x = -TRACK_WIDTH * 0.5f + stripeW * i + stripeW * 0.5f;
            GameObject stripe = MakeCube(root, "Stripe",
                new Vector3(x, -0.09f, SEG_LEN * 0.5f),
                new Vector3(stripeW - 0.05f, 0.01f, SEG_LEN),
                "Mat_TrackAlt");
            Object.DestroyImmediate(stripe.GetComponent<BoxCollider>());
        }

        // ── Líneas de carril (3 líneas: izq, centro, der) ────────────────────
        float[] laneXs = { -LANE_WIDTH, 0f, LANE_WIDTH };
        foreach (float lx in laneXs)
        {
            // Línea continua
            GameObject line = MakeCube(root, "LaneLine",
                new Vector3(lx, 0.01f, SEG_LEN * 0.5f),
                new Vector3(0.12f, 0.02f, SEG_LEN),
                "Mat_TrackLine");
            Object.DestroyImmediate(line.GetComponent<BoxCollider>());
        }

        // ── Línea de borde izquierda y derecha ───────────────────────────────
        foreach (float bx in new float[] { -TRACK_WIDTH * 0.5f + 0.06f, TRACK_WIDTH * 0.5f - 0.06f })
        {
            GameObject bline = MakeCube(root, "BorderLine",
                new Vector3(bx, 0.01f, SEG_LEN * 0.5f),
                new Vector3(0.18f, 0.02f, SEG_LEN),
                "Mat_TrackLine");
            Object.DestroyImmediate(bline.GetComponent<BoxCollider>());
        }

        // ── Pasto exterior (a los lados de la pista) ─────────────────────────
        foreach (float gx in new float[] { -TRACK_WIDTH * 0.5f - 3f, TRACK_WIDTH * 0.5f + 3f })
        {
            GameObject grass = MakeCube(root, "GrassOuter",
                new Vector3(gx, -0.15f, SEG_LEN * 0.5f),
                new Vector3(6f, 0.1f, SEG_LEN),
                "Mat_Grass");
            Object.DestroyImmediate(grass.GetComponent<BoxCollider>());
        }

        // ── Graderías ────────────────────────────────────────────────────────
        BuildStand(root, -1);
        BuildStand(root, +1);

        // ── Puntos de referencia ─────────────────────────────────────────────
        GameObject spawnPt   = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(root.transform);
        spawnPt.transform.localPosition = new Vector3(0, 0, SEG_LEN);

        GameObject despawnPt = new GameObject("DespawnPoint");
        despawnPt.transform.SetParent(root.transform);
        despawnPt.transform.localPosition = Vector3.zero;

        SerializedObject soTs = new SerializedObject(ts);
        soTs.FindProperty("spawnPoint").objectReferenceValue   = spawnPt.transform;
        soTs.FindProperty("despawnPoint").objectReferenceValue = despawnPt.transform;
        soTs.ApplyModifiedProperties();

        // ── ObstacleSpawner ───────────────────────────────────────────────────
        ObstacleSpawner spawner = root.AddComponent<ObstacleSpawner>();
        AssignSpawnerPrefabs(spawner);

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Segment_Normal.prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildDecisionSegment()
    {
        GameObject root = new GameObject("Segment_Decision");
        TrackSegment ts = root.AddComponent<TrackSegment>();

        SerializedObject soTs0 = new SerializedObject(ts);
        soTs0.FindProperty("segmentType").enumValueIndex = (int)TrackSegment.SegmentType.Decision;
        soTs0.ApplyModifiedProperties();

        // Suelo
        GameObject floor = MakeCube(root, "Floor",
            new Vector3(0, -0.1f, SEG_LEN * 0.5f),
            new Vector3(TRACK_WIDTH, 0.2f, SEG_LEN),
            "Mat_Track");
        Object.DestroyImmediate(floor.GetComponent<BoxCollider>());
        floor.AddComponent<BoxCollider>();

        // Franjas
        float stripeW = TRACK_WIDTH / 6f;
        for (int i = 1; i < 6; i += 2)
        {
            float x = -TRACK_WIDTH * 0.5f + stripeW * i + stripeW * 0.5f;
            GameObject stripe = MakeCube(root, "Stripe",
                new Vector3(x, -0.09f, SEG_LEN * 0.5f),
                new Vector3(stripeW - 0.05f, 0.01f, SEG_LEN),
                "Mat_TrackAlt");
            Object.DestroyImmediate(stripe.GetComponent<BoxCollider>());
        }

        // Líneas de carril
        foreach (float lx in new float[] { -LANE_WIDTH, 0f, LANE_WIDTH })
        {
            GameObject line = MakeCube(root, "LaneLine",
                new Vector3(lx, 0.01f, SEG_LEN * 0.5f),
                new Vector3(0.12f, 0.02f, SEG_LEN),
                "Mat_TrackLine");
            Object.DestroyImmediate(line.GetComponent<BoxCollider>());
        }

        // Pasto exterior
        foreach (float gx in new float[] { -TRACK_WIDTH * 0.5f - 3f, TRACK_WIDTH * 0.5f + 3f })
        {
            GameObject grass = MakeCube(root, "GrassOuter",
                new Vector3(gx, -0.15f, SEG_LEN * 0.5f),
                new Vector3(6f, 0.1f, SEG_LEN),
                "Mat_Grass");
            Object.DestroyImmediate(grass.GetComponent<BoxCollider>());
        }

        // Graderías
        BuildStand(root, -1);
        BuildStand(root, +1);

        // Portal de decisión
        BuildDecisionPortal(root, -LANE_WIDTH, "IZQ", "Mat_DecisionLeft");
        BuildDecisionPortal(root, +LANE_WIDTH, "DER", "Mat_DecisionRight");

        // Arco superior
        BuildDecisionArch(root);

        // Puntos de referencia
        GameObject spawnPt   = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(root.transform);
        spawnPt.transform.localPosition = new Vector3(0, 0, SEG_LEN);

        GameObject despawnPt = new GameObject("DespawnPoint");
        despawnPt.transform.SetParent(root.transform);
        despawnPt.transform.localPosition = Vector3.zero;

        SerializedObject soTs = new SerializedObject(ts);
        soTs.FindProperty("spawnPoint").objectReferenceValue   = spawnPt.transform;
        soTs.FindProperty("despawnPoint").objectReferenceValue = despawnPt.transform;
        soTs.ApplyModifiedProperties();

        // DecisionPoint
        GameObject dpGO = new GameObject("DecisionPoint");
        dpGO.transform.SetParent(root.transform);
        dpGO.transform.localPosition = new Vector3(0, 0, 10f);
        DecisionPoint dp = dpGO.AddComponent<DecisionPoint>();

        // Asignar DecisionData
        DecisionData dd = AssetDatabase.LoadAssetAtPath<DecisionData>("Assets/_Project/Resources/Decision_Entrenar.asset");
        if (dd != null)
        {
            SerializedObject soDp = new SerializedObject(dp);
            soDp.FindProperty("decisionData").objectReferenceValue = dd;
            soDp.ApplyModifiedProperties();
        }

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Segment_Decision.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Gradería low-poly ─────────────────────────────────────────────────────
    private static void BuildStand(GameObject root, int side)
    {
        float baseX = side * (TRACK_WIDTH * 0.5f + 6f);

        // Piso de la gradería (base de cemento)
        GameObject standBase = MakeCube(root, "StandBase",
            new Vector3(baseX, -0.05f, SEG_LEN * 0.5f),
            new Vector3(12f, 0.1f, SEG_LEN),
            "Mat_StandBase");
        Object.DestroyImmediate(standBase.GetComponent<BoxCollider>());

        // 3 gradas escalonadas
        float[] tierHeights = { 0.6f, 1.4f, 2.4f };
        float[] tierWidths  = { 3.5f, 3.0f, 2.5f };
        float[] tierOffsets = { 0f,   1.8f, 3.6f };

        for (int t = 0; t < 3; t++)
        {
            float tierX = baseX + side * tierOffsets[t];
            GameObject tier = MakeCube(root, $"StandTier{t}",
                new Vector3(tierX, tierHeights[t] * 0.5f, SEG_LEN * 0.5f),
                new Vector3(tierWidths[t], tierHeights[t], SEG_LEN),
                "Mat_StandTier");
            Object.DestroyImmediate(tier.GetComponent<BoxCollider>());

            // Público en esta grada (figuras esquemáticas)
            BuildAudience(root, tierX, tierHeights[t], side, t);
        }

        // Techo del estadio
        float roofX = baseX + side * 5f;
        GameObject roof = MakeCube(root, "Roof",
            new Vector3(roofX, 7.5f, SEG_LEN * 0.5f),
            new Vector3(4f, 0.3f, SEG_LEN),
            "Mat_RoofMetal");
        Object.DestroyImmediate(roof.GetComponent<BoxCollider>());

        // Soporte del techo
        int supportCount = Mathf.FloorToInt(SEG_LEN / 8f);
        for (int s = 0; s <= supportCount; s++)
        {
            float z = s * (SEG_LEN / supportCount);
            GameObject support = MakeCube(root, "RoofSupport",
                new Vector3(roofX, 4.5f, z),
                new Vector3(0.25f, 6f, 0.25f),
                "Mat_RoofMetal");
            Object.DestroyImmediate(support.GetComponent<BoxCollider>());
        }
    }

    // ── Público esquemático ───────────────────────────────────────────────────
    private static void BuildAudience(GameObject root, float tierX, float tierH, int side, int tier)
    {
        string[] mats = { "Mat_Audience_R", "Mat_Audience_B", "Mat_Audience_Y", "Mat_Audience_W" };
        int cols = 8;
        float spacing = SEG_LEN / cols;
        float personH = 0.55f;
        float personW = 0.35f;

        for (int c = 0; c < cols; c++)
        {
            float z = spacing * c + spacing * 0.5f;
            string mat = mats[(c + tier) % mats.Length];

            // Cuerpo
            GameObject body = MakeCube(root, "Person_Body",
                new Vector3(tierX + side * 0.3f, tierH + personH * 0.5f, z),
                new Vector3(personW, personH, personW * 0.7f),
                mat);
            Object.DestroyImmediate(body.GetComponent<BoxCollider>());

            // Cabeza (esfera aplanada)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Person_Head";
            head.transform.SetParent(root.transform);
            head.transform.localPosition = new Vector3(tierX + side * 0.3f, tierH + personH + 0.18f, z);
            head.transform.localScale    = new Vector3(0.28f, 0.28f, 0.28f);
            AssignMat(head, "Mat_Audience_W");
            Object.DestroyImmediate(head.GetComponent<SphereCollider>());
        }
    }

    // ── Portal de decisión ────────────────────────────────────────────────────
    private static void BuildDecisionPortal(GameObject root, float laneX, string label, string matName)
    {
        float portalZ = 12f;

        // Panel principal del portal
        GameObject panel = MakeCube(root, $"Portal_{label}",
            new Vector3(laneX, 2.2f, portalZ),
            new Vector3(2.2f, 3.0f, 0.2f),
            matName);
        Object.DestroyImmediate(panel.GetComponent<BoxCollider>());

        // Borde del panel (marco blanco)
        GameObject frame = MakeCube(root, $"PortalFrame_{label}",
            new Vector3(laneX, 2.2f, portalZ - 0.15f),
            new Vector3(2.4f, 3.2f, 0.1f),
            "Mat_TrackLine");
        Object.DestroyImmediate(frame.GetComponent<BoxCollider>());

        // Poste izquierdo
        GameObject postL = MakeCube(root, $"PostL_{label}",
            new Vector3(laneX - 1.1f, 1.5f, portalZ),
            new Vector3(0.18f, 3.0f, 0.18f),
            "Mat_TrackLine");
        Object.DestroyImmediate(postL.GetComponent<BoxCollider>());

        // Poste derecho
        GameObject postR = MakeCube(root, $"PostR_{label}",
            new Vector3(laneX + 1.1f, 1.5f, portalZ),
            new Vector3(0.18f, 3.0f, 0.18f),
            "Mat_TrackLine");
        Object.DestroyImmediate(postR.GetComponent<BoxCollider>());

        // Flecha direccional (triángulo simulado con cubo rotado)
        float arrowDir = (label == "IZQ") ? -1f : 1f;
        GameObject arrow = MakeCube(root, $"Arrow_{label}",
            new Vector3(laneX + arrowDir * 0.4f, 2.2f, portalZ - 0.2f),
            new Vector3(0.6f, 0.6f, 0.1f),
            "Mat_TrackLine");
        arrow.transform.localRotation = Quaternion.Euler(0, 0, arrowDir * 45f);
        Object.DestroyImmediate(arrow.GetComponent<BoxCollider>());
    }

    private static void BuildDecisionArch(GameObject root)
    {
        float archZ = 12f;
        // Barra horizontal
        GameObject bar = MakeCube(root, "ArchBar",
            new Vector3(0, 5.0f, archZ),
            new Vector3(10f, 0.4f, 0.4f),
            "Mat_TrackLine");
        Object.DestroyImmediate(bar.GetComponent<BoxCollider>());

        // Pilares
        foreach (float px in new float[] { -4.8f, 4.8f })
        {
            GameObject pillar = MakeCube(root, "ArchPillar",
                new Vector3(px, 2.5f, archZ),
                new Vector3(0.4f, 5.0f, 0.4f),
                "Mat_TrackLine");
            Object.DestroyImmediate(pillar.GetComponent<BoxCollider>());
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 3. OBSTÁCULOS
    // ═══════════════════════════════════════════════════════════════════════════
    private static void RebuildObstaclePrefabs()
    {
        DeleteAsset("Assets/_Project/Prefabs/Obstacle_Block.prefab");
        AssetDatabase.Refresh();

        BuildCone();
        BuildHurdle();
        BuildBall();
        AssetDatabase.Refresh();

        // Actualizar spawner en el prefab de segmento normal
        UpdateSpawnerInPrefab();
        Debug.Log("[MundialRunner] Obstáculos reconstruidos.");
    }

    // ── Cono de entrenamiento ─────────────────────────────────────────────────
    private static void BuildCone()
    {
        GameObject root = new GameObject("Obstacle_Cone");

        // Base del cono (cilindro ancho y bajo)
        GameObject coneBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coneBase.name = "ConeBase";
        coneBase.transform.SetParent(root.transform);
        coneBase.transform.localPosition = new Vector3(0, 0.06f, 0);
        coneBase.transform.localScale    = new Vector3(0.7f, 0.06f, 0.7f);
        AssignMat(coneBase, "Mat_ConePrimary");
        Object.DestroyImmediate(coneBase.GetComponent<CapsuleCollider>());

        // Cuerpo del cono (cilindro que se estrecha — simulado con escala)
        GameObject coneBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coneBody.name = "ConeBody";
        coneBody.transform.SetParent(root.transform);
        coneBody.transform.localPosition = new Vector3(0, 0.45f, 0);
        coneBody.transform.localScale    = new Vector3(0.28f, 0.40f, 0.28f);
        AssignMat(coneBody, "Mat_ConePrimary");
        Object.DestroyImmediate(coneBody.GetComponent<CapsuleCollider>());

        // Franja blanca del cono
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stripe.name = "ConeStripe";
        stripe.transform.SetParent(root.transform);
        stripe.transform.localPosition = new Vector3(0, 0.35f, 0);
        stripe.transform.localScale    = new Vector3(0.32f, 0.04f, 0.32f);
        AssignMat(stripe, "Mat_ConeStripe");
        Object.DestroyImmediate(stripe.GetComponent<CapsuleCollider>());

        // Punta del cono
        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "ConeTip";
        tip.transform.SetParent(root.transform);
        tip.transform.localPosition = new Vector3(0, 0.88f, 0);
        tip.transform.localScale    = new Vector3(0.12f, 0.12f, 0.12f);
        AssignMat(tip, "Mat_ConePrimary");
        Object.DestroyImmediate(tip.GetComponent<SphereCollider>());

        // Collider trigger en el root
        CapsuleCollider col = root.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.height = 0.9f;
        col.radius = 0.35f;
        col.center = new Vector3(0, 0.45f, 0);

        root.AddComponent<Obstacle>();
        root.tag = "Obstacle";

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Obstacle_Cone.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Valla de entrenamiento ────────────────────────────────────────────────
    private static void BuildHurdle()
    {
        GameObject root = new GameObject("Obstacle_Hurdle");

        // Barra horizontal
        GameObject bar = MakeCube(root, "HurdleBar",
            new Vector3(0, 0.9f, 0),
            new Vector3(1.8f, 0.12f, 0.12f),
            "Mat_HurdleBar");
        Object.DestroyImmediate(bar.GetComponent<BoxCollider>());

        // Patas
        foreach (float px in new float[] { -0.8f, 0.8f })
        {
            GameObject leg = MakeCube(root, "HurdleLeg",
                new Vector3(px, 0.45f, 0),
                new Vector3(0.12f, 0.9f, 0.12f),
                "Mat_HurdlePost");
            Object.DestroyImmediate(leg.GetComponent<BoxCollider>());

            // Pie de la pata
            GameObject foot = MakeCube(root, "HurdleFoot",
                new Vector3(px, 0.06f, 0),
                new Vector3(0.35f, 0.12f, 0.5f),
                "Mat_HurdlePost");
            Object.DestroyImmediate(foot.GetComponent<BoxCollider>());
        }

        // Collider trigger
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size   = new Vector3(1.8f, 1.0f, 0.3f);
        col.center = new Vector3(0, 0.5f, 0);

        root.AddComponent<Obstacle>();
        root.tag = "Obstacle";

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Obstacle_Hurdle.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Pelota de fútbol grande ───────────────────────────────────────────────
    private static void BuildBall()
    {
        GameObject root = new GameObject("Obstacle_Ball");

        // Esfera principal
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "BallSphere";
        sphere.transform.SetParent(root.transform);
        sphere.transform.localPosition = new Vector3(0, 0.7f, 0);
        sphere.transform.localScale    = new Vector3(1.4f, 1.4f, 1.4f);
        AssignMat(sphere, "Mat_Ball");
        Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());

        // Parches negros (hexágonos simulados con cubos rotados)
        float[][] patches = {
            new float[]{ 0,    0.7f,  0.7f,  0f,   0f,   0f   },
            new float[]{ 0.5f, 1.1f,  0.5f,  0f,   0f,   45f  },
            new float[]{-0.5f, 1.1f,  0.5f,  0f,   0f,  -45f  },
            new float[]{ 0.5f, 0.3f,  0.5f,  0f,   0f,   45f  },
            new float[]{-0.5f, 0.3f,  0.5f,  0f,   0f,  -45f  },
        };
        foreach (var p in patches)
        {
            GameObject patch = MakeCube(root, "BallPatch",
                new Vector3(p[0], p[1], p[2]),
                new Vector3(0.38f, 0.38f, 0.05f),
                "Mat_BallPatch");
            patch.transform.localRotation = Quaternion.Euler(p[3], p[4], p[5]);
            Object.DestroyImmediate(patch.GetComponent<BoxCollider>());
        }

        // Collider trigger
        SphereCollider col = root.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.7f;
        col.center = new Vector3(0, 0.7f, 0);

        root.AddComponent<Obstacle>();
        root.tag = "Obstacle";

        PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Prefabs/Obstacle_Ball.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Actualizar spawner con los 3 tipos de obstáculos ─────────────────────
    private static void UpdateSpawnerInPrefab()
    {
        string prefabPath = "Assets/_Project/Prefabs/Segment_Normal.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null) return;

        ObstacleSpawner spawner = prefabRoot.GetComponent<ObstacleSpawner>();
        if (spawner == null) { PrefabUtility.UnloadPrefabContents(prefabRoot); return; }

        GameObject cone   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Cone.prefab");
        GameObject hurdle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Hurdle.prefab");
        GameObject ball   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Ball.prefab");
        GameObject coin   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Coin.prefab");

        SerializedObject so = new SerializedObject(spawner);
        so.FindProperty("obstaclePrefabs").arraySize = 3;
        if (cone)   so.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(0).objectReferenceValue = cone;
        if (hurdle) so.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(1).objectReferenceValue = hurdle;
        if (ball)   so.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(2).objectReferenceValue = ball;
        if (coin)   so.FindProperty("coinPrefab").objectReferenceValue = coin;
        so.FindProperty("obstacleCount").intValue  = 2;
        so.FindProperty("coinCount").intValue      = 5;
        so.FindProperty("spawnZStart").floatValue  = 5f;
        so.FindProperty("spawnZEnd").floatValue    = 25f;
        so.FindProperty("coinHeight").floatValue   = 0.6f;
        so.FindProperty("obstacleHeight").floatValue = 0f;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 4. CÁMARA
    // ═══════════════════════════════════════════════════════════════════════════
    private static void UpgradeCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Tercera persona cercana: altura de hombros, ligeramente detrás
        cam.transform.position = new Vector3(0, 2.8f, -4.5f);
        cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        cam.fieldOfView = 72f; // FOV más amplio para sensación de velocidad

        CameraFollow cf = cam.GetComponent<CameraFollow>();
        if (cf != null)
        {
            SerializedObject so = new SerializedObject(cf);
            so.FindProperty("offset").vector3Value         = new Vector3(0f, 2.8f, -4.5f);
            so.FindProperty("lookDownAngle").floatValue    = 12f;
            so.FindProperty("positionSmoothing").floatValue = 10f;
            so.FindProperty("rotationSmoothing").floatValue = 6f;
            so.ApplyModifiedProperties();
        }

        // Color de fondo: cielo azul claro
        cam.backgroundColor = new Color(0.42f, 0.72f, 0.95f);

        EditorUtility.SetDirty(cam.gameObject);
        Debug.Log("[MundialRunner] Cámara ajustada a tercera persona cercana.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 5. HUD
    // ═══════════════════════════════════════════════════════════════════════════
    private static void UpgradeHUD()
    {
        GameObject gameplayPanel = null;
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas != null)
        {
            Transform t = canvas.transform.Find("GameplayPanel");
            if (t != null) gameplayPanel = t.gameObject;
        }
        if (gameplayPanel == null) { Debug.LogWarning("[MundialRunner] GameplayPanel no encontrado."); return; }

        // Limpiar hijos existentes del panel
        for (int i = gameplayPanel.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(gameplayPanel.transform.GetChild(i).gameObject);

        // Fondo transparente
        Image panelBg = gameplayPanel.GetComponent<Image>();
        if (panelBg) panelBg.color = new Color(0, 0, 0, 0);

        // ── Barra superior (fondo semitransparente) ───────────────────────────
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(gameplayPanel.transform, false);
        RectTransform tbRT = topBar.AddComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0, 0.91f);
        tbRT.anchorMax = new Vector2(1, 1f);
        tbRT.offsetMin = tbRT.offsetMax = Vector2.zero;
        Image tbImg = topBar.AddComponent<Image>();
        tbImg.color = new Color(0, 0, 0, 0.45f);

        // ── Monedas (arriba izquierda) ────────────────────────────────────────
        GameObject coinsGO = CreateHUDText(gameplayPanel.transform, "CoinsHUD",
            new Vector2(0.01f, 0.91f), new Vector2(0.30f, 1.00f),
            "🪙  0", 28, new Color(1f, 0.82f, 0.05f), FontStyles.Bold);

        // ── Puntaje (arriba derecha) ──────────────────────────────────────────
        GameObject scoreGO = CreateHUDText(gameplayPanel.transform, "ScoreHUD",
            new Vector2(0.70f, 0.91f), new Vector2(0.99f, 1.00f),
            "0", 28, Color.white, FontStyles.Bold);
        scoreGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;

        // ── Barra de progreso (arriba centro) ─────────────────────────────────
        GameObject progressBG = new GameObject("ProgressBG");
        progressBG.transform.SetParent(gameplayPanel.transform, false);
        RectTransform pbgRT = progressBG.AddComponent<RectTransform>();
        pbgRT.anchorMin = new Vector2(0.28f, 0.925f);
        pbgRT.anchorMax = new Vector2(0.72f, 0.975f);
        pbgRT.offsetMin = pbgRT.offsetMax = Vector2.zero;
        Image pbgImg = progressBG.AddComponent<Image>();
        pbgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

        // Slider de progreso
        GameObject progressSlider = new GameObject("ProgressSlider");
        progressSlider.transform.SetParent(gameplayPanel.transform, false);
        RectTransform psRT = progressSlider.AddComponent<RectTransform>();
        psRT.anchorMin = new Vector2(0.28f, 0.925f);
        psRT.anchorMax = new Vector2(0.72f, 0.975f);
        psRT.offsetMin = psRT.offsetMax = Vector2.zero;
        Slider progSlider = progressSlider.AddComponent<Slider>();
        progSlider.minValue = 0; progSlider.maxValue = 1; progSlider.value = 0.35f;

        // Fill area del slider
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(progressSlider.transform, false);
        RectTransform faRT = fillArea.AddComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(2, 2); faRT.offsetMax = new Vector2(-2, -2);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fRT = fill.AddComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(0.35f, 1f);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.15f, 0.75f, 0.25f); // verde

        SerializedObject soSlider = new SerializedObject(progSlider);
        soSlider.FindProperty("m_FillRect").objectReferenceValue = fRT;
        soSlider.FindProperty("m_Direction").enumValueIndex = 0; // left to right
        soSlider.ApplyModifiedProperties();

        // Icono de balón antes de la barra
        GameObject ballIcon = CreateHUDText(gameplayPanel.transform, "ProgressIcon",
            new Vector2(0.24f, 0.920f), new Vector2(0.30f, 0.980f),
            "⚽", 22, Color.white, FontStyles.Normal);

        // ── Barra de energía (abajo izquierda) ────────────────────────────────
        BuildEnergyBar(gameplayPanel.transform);

        // ── Botón pausa (arriba izquierda, pequeño) ───────────────────────────
        GameObject pauseBtn = new GameObject("PauseButton");
        pauseBtn.transform.SetParent(gameplayPanel.transform, false);
        RectTransform pauseRT = pauseBtn.AddComponent<RectTransform>();
        pauseRT.anchorMin = new Vector2(0.01f, 0.91f);
        pauseRT.anchorMax = new Vector2(0.08f, 0.99f);
        pauseRT.offsetMin = pauseRT.offsetMax = Vector2.zero;
        Image pauseImg = pauseBtn.AddComponent<Image>();
        pauseImg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        Button pauseBtnComp = pauseBtn.AddComponent<Button>();
        TMP_Text pauseTxt = CreateHUDText(pauseBtn.transform, "PauseIcon",
            Vector2.zero, Vector2.one, "⏸", 20, Color.white, FontStyles.Normal).GetComponent<TMP_Text>();

        // ── Conectar GameplayHUD ───────────────────────────────────────────────
        GameplayHUD hud = gameplayPanel.GetComponent<GameplayHUD>();
        if (hud == null) hud = gameplayPanel.AddComponent<GameplayHUD>();

        SerializedObject soHud = new SerializedObject(hud);
        soHud.FindProperty("coinsText").objectReferenceValue    = coinsGO.GetComponent<TMP_Text>();
        soHud.FindProperty("scoreText").objectReferenceValue    = scoreGO.GetComponent<TMP_Text>();
        soHud.FindProperty("progressBar").objectReferenceValue  = progSlider;
        // energyBar se conecta abajo
        soHud.ApplyModifiedProperties();

        EditorUtility.SetDirty(gameplayPanel);
        Debug.Log("[MundialRunner] HUD mejorado.");
    }

    private static void BuildEnergyBar(Transform parent)
    {
        // Fondo de la barra de energía
        GameObject energyBG = new GameObject("EnergyBG");
        energyBG.transform.SetParent(parent, false);
        RectTransform ebgRT = energyBG.AddComponent<RectTransform>();
        ebgRT.anchorMin = new Vector2(0.01f, 0.01f);
        ebgRT.anchorMax = new Vector2(0.35f, 0.06f);
        ebgRT.offsetMin = ebgRT.offsetMax = Vector2.zero;
        Image ebgImg = energyBG.AddComponent<Image>();
        ebgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.75f);

        // Etiqueta
        CreateHUDText(parent, "EnergyLabel",
            new Vector2(0.01f, 0.06f), new Vector2(0.12f, 0.11f),
            "⚡ ENERGÍA", 16, new Color(0.9f, 0.9f, 0.3f), FontStyles.Bold);

        // Slider de energía
        GameObject energySlider = new GameObject("EnergySlider");
        energySlider.transform.SetParent(parent, false);
        RectTransform esRT = energySlider.AddComponent<RectTransform>();
        esRT.anchorMin = new Vector2(0.01f, 0.01f);
        esRT.anchorMax = new Vector2(0.35f, 0.06f);
        esRT.offsetMin = esRT.offsetMax = Vector2.zero;
        Slider energySliderComp = energySlider.AddComponent<Slider>();
        energySliderComp.minValue = 0; energySliderComp.maxValue = 1; energySliderComp.value = 1f;

        // Fill area
        GameObject eFillArea = new GameObject("Fill Area");
        eFillArea.transform.SetParent(energySlider.transform, false);
        RectTransform efaRT = eFillArea.AddComponent<RectTransform>();
        efaRT.anchorMin = Vector2.zero; efaRT.anchorMax = Vector2.one;
        efaRT.offsetMin = new Vector2(2, 2); efaRT.offsetMax = new Vector2(-2, -2);

        GameObject eFill = new GameObject("Fill");
        eFill.transform.SetParent(eFillArea.transform, false);
        RectTransform efRT = eFill.AddComponent<RectTransform>();
        efRT.anchorMin = Vector2.zero; efRT.anchorMax = new Vector2(1f, 1f);
        efRT.offsetMin = efRT.offsetMax = Vector2.zero;
        Image eFillImg = eFill.AddComponent<Image>();
        eFillImg.color = new Color(0.2f, 0.85f, 0.35f); // verde energía

        SerializedObject soE = new SerializedObject(energySliderComp);
        soE.FindProperty("m_FillRect").objectReferenceValue = efRT;
        soE.FindProperty("m_Direction").enumValueIndex = 0;
        soE.ApplyModifiedProperties();

        // Conectar al HUD
        GameObject gameplayPanel = parent.gameObject;
        GameplayHUD hud = gameplayPanel.GetComponent<GameplayHUD>();
        if (hud != null)
        {
            SerializedObject soHud = new SerializedObject(hud);
            soHud.FindProperty("energyBar").objectReferenceValue = energySliderComp;
            soHud.ApplyModifiedProperties();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REFRESH SCENE
    // ═══════════════════════════════════════════════════════════════════════════
    private static void RefreshScene()
    {
        // Destruir segmentos viejos
        string[] segNames = { "Segment_Normal", "Segment_Decision" };
        foreach (string n in segNames)
        {
            GameObject go = GameObject.Find(n);
            while (go != null) { Object.DestroyImmediate(go); go = GameObject.Find(n); }
        }

        // Colocar pista nueva
        TrackSegment normalPrefab   = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Normal.prefab");
        TrackSegment decisionPrefab = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Decision.prefab");
        if (normalPrefab == null) return;

        int[] pattern = { 0, 0, 0, 1, 0, 0, 0, 1, 0, 0 };
        Vector3 pos = Vector3.zero;
        foreach (int type in pattern)
        {
            TrackSegment prefab = (type == 1 && decisionPrefab != null) ? decisionPrefab : normalPrefab;
            TrackSegment seg = (TrackSegment)PrefabUtility.InstantiatePrefab(prefab);
            seg.transform.position = pos;
            pos = seg.SpawnPoint;
        }

        // Actualizar TrackGenerator
        TrackGenerator tg = Object.FindFirstObjectByType<TrackGenerator>();
        if (tg != null)
        {
            SerializedObject so = new SerializedObject(tg);
            so.FindProperty("normalSegmentPrefabs").arraySize = 1;
            so.FindProperty("normalSegmentPrefabs").GetArrayElementAtIndex(0).objectReferenceValue = normalPrefab;
            if (decisionPrefab != null) so.FindProperty("decisionSegmentPrefab").objectReferenceValue = decisionPrefab;
            so.FindProperty("startSegmentPrefab").objectReferenceValue = normalPrefab;
            so.ApplyModifiedProperties();
        }

        // Ajustar SceneView
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot     = new Vector3(0, 2f, 10f);
            sv.rotation  = Quaternion.Euler(15f, 0f, 0f);
            sv.size      = 18f;
            sv.orthographic = false;
            sv.in2DMode = false;
            sv.Repaint();
        }

        Debug.Log("[MundialRunner] Escena refrescada con nuevos segmentos.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    private static GameObject MakeCube(GameObject parent, string name, Vector3 pos, Vector3 scale, string matName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        AssignMat(go, matName);
        return go;
    }

    private static void AssignMat(GameObject go, string matName)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/{matName}.mat");
        if (mat == null) return;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr) mr.sharedMaterial = mat;
    }

    private static GameObject CreateHUDText(Transform parent, string name,
        Vector2 ancMin, Vector2 ancMax, string text, float fontSize, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax;
        rt.offsetMin = new Vector2(4, 2); rt.offsetMax = new Vector2(-4, -2);
        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.fontStyle = style; tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = false;
        return go;
    }

    private static void AssignSpawnerPrefabs(ObstacleSpawner spawner)
    {
        GameObject cone   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Cone.prefab");
        GameObject hurdle = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Hurdle.prefab");
        GameObject ball   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Ball.prefab");
        GameObject coin   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Coin.prefab");

        SerializedObject so = new SerializedObject(spawner);
        int count = (cone != null ? 1 : 0) + (hurdle != null ? 1 : 0) + (ball != null ? 1 : 0);
        so.FindProperty("obstaclePrefabs").arraySize = count;
        int idx = 0;
        if (cone)   so.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(idx++).objectReferenceValue = cone;
        if (hurdle) so.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(idx++).objectReferenceValue = hurdle;
        if (ball)   so.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(idx++).objectReferenceValue = ball;
        if (coin)   so.FindProperty("coinPrefab").objectReferenceValue = coin;
        so.FindProperty("obstacleCount").intValue   = 2;
        so.FindProperty("coinCount").intValue       = 5;
        so.FindProperty("spawnZStart").floatValue   = 5f;
        so.FindProperty("spawnZEnd").floatValue     = 25f;
        so.FindProperty("coinHeight").floatValue    = 0.6f;
        so.FindProperty("obstacleHeight").floatValue = 0f;
        so.ApplyModifiedProperties();
    }

    private static void DeleteAsset(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
