using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor utility: configura la escena completa de Mundial Runner con un clic.
/// Menu: MundialRunner > Setup Scene
/// </summary>
public class MundialRunnerSetup : Editor
{
    [MenuItem("MundialRunner/Setup Scene")]
    public static void SetupScene()
    {
        SetupMaterials();
        SetupPlayer();
        SetupCamera();
        SetupTrackGenerator();
        SetupGameManagers();
        SetupDecisionSystem();
        SetupGameOverUI();
        WireExistingUI();
        CreateStartingTrack();

        Debug.Log("[MundialRunner] ✅ Escena configurada correctamente.");
    }

    // ─── Materiales ───────────────────────────────────────────────────────────

    private static void SetupMaterials()
    {
        EnsureDir("Assets/_Project/Materials");

        CreateMat("Mat_Track",      new Color(0.35f, 0.55f, 0.25f));  // verde campo
        CreateMat("Mat_TrackLine",  new Color(0.95f, 0.95f, 0.95f));  // líneas blancas
        CreateMat("Mat_Player",     new Color(0.15f, 0.45f, 0.85f));  // azul jugador
        CreateMat("Mat_Obstacle",   new Color(0.90f, 0.25f, 0.20f));  // rojo obstáculo
        CreateMat("Mat_Coin",       new Color(1.00f, 0.80f, 0.10f));  // amarillo moneda
        CreateMat("Mat_Sky",        new Color(0.40f, 0.75f, 1.00f));  // cielo
        CreateMat("Mat_Crowd",      new Color(0.85f, 0.40f, 0.10f));  // naranja tribuna
    }

    private static Material CreateMat(string name, Color color)
    {
        string path = $"Assets/_Project/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            // Estilo low-poly: sin smoothness
            mat.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // ─── Jugador ──────────────────────────────────────────────────────────────

    private static void SetupPlayer()
    {
        // Crear prefab del jugador si no existe
        string prefabPath = "Assets/_Project/Prefabs/Player.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) return;

        EnsureDir("Assets/_Project/Prefabs");

        GameObject player = new GameObject("Player");

        // Cuerpo (cápsula)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        body.transform.localScale    = new Vector3(0.6f, 0.6f, 0.6f);
        AssignMat(body, "Mat_Player");
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        // Cabeza
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(player.transform);
        head.transform.localPosition = new Vector3(0, 1.85f, 0);
        head.transform.localScale    = new Vector3(0.45f, 0.45f, 0.45f);
        AssignMat(head, "Mat_Player");
        Object.DestroyImmediate(head.GetComponent<SphereCollider>());

        // CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);

        // PlayerController script
        player.AddComponent<PlayerController>();

        // PlayerStats
        player.AddComponent<PlayerStats>();

        // Tag
        player.tag = "Player";

        // Guardar prefab
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        Object.DestroyImmediate(player);

        Debug.Log("[MundialRunner] Prefab Player creado.");
    }

    // ─── Cámara ───────────────────────────────────────────────────────────────

    private static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        if (mainCam.GetComponent<CameraFollow>() == null)
            mainCam.gameObject.AddComponent<CameraFollow>();

        mainCam.transform.position = new Vector3(0, 5f, -8f);
        mainCam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        mainCam.backgroundColor    = new Color(0.4f, 0.75f, 1f);
        mainCam.clearFlags         = CameraClearFlags.SolidColor;
    }

    // ─── Track Generator ─────────────────────────────────────────────────────

    private static void SetupTrackGenerator()
    {
        // Crear segmento de pista básico si no existe
        CreateNormalSegmentPrefab();
        CreateDecisionSegmentPrefab();

        // Crear GameObject TrackGenerator en la escena
        GameObject tgGO = GameObject.Find("TrackGenerator");
        if (tgGO == null)
        {
            tgGO = new GameObject("TrackGenerator");
            TrackGenerator tg = tgGO.AddComponent<TrackGenerator>();

            // Asignar prefabs via SerializedObject
            SerializedObject so = new SerializedObject(tg);
            SerializedProperty normalProp   = so.FindProperty("normalSegmentPrefabs");
            SerializedProperty decisionProp = so.FindProperty("decisionSegmentPrefab");
            SerializedProperty startProp    = so.FindProperty("startSegmentPrefab");

            TrackSegment normalSeg   = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Normal.prefab");
            TrackSegment decisionSeg = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Decision.prefab");

            if (normalSeg != null)
            {
                normalProp.arraySize = 1;
                normalProp.GetArrayElementAtIndex(0).objectReferenceValue = normalSeg;
            }
            if (decisionSeg != null)
                decisionProp.objectReferenceValue = decisionSeg;
            if (normalSeg != null)
                startProp.objectReferenceValue = normalSeg;

            so.ApplyModifiedProperties();
        }
    }

    private static void CreateNormalSegmentPrefab()
    {
        string path = "Assets/_Project/Prefabs/Segment_Normal.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject seg = new GameObject("Segment_Normal");
        TrackSegment ts = seg.AddComponent<TrackSegment>();

        // Suelo del segmento
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(seg.transform);
        floor.transform.localPosition = new Vector3(0, -0.1f, 15f);
        floor.transform.localScale    = new Vector3(8f, 0.2f, 30f);
        AssignMat(floor, "Mat_Track");
        Object.DestroyImmediate(floor.GetComponent<BoxCollider>());

        // Collider de suelo plano
        BoxCollider bc = floor.AddComponent<BoxCollider>();

        // Líneas de carril
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneLine";
            line.transform.SetParent(seg.transform);
            line.transform.localPosition = new Vector3(i * LaneSystem.LaneWidth * 0.5f, 0.01f, 15f);
            line.transform.localScale    = new Vector3(0.1f, 0.01f, 30f);
            AssignMat(line, "Mat_TrackLine");
            Object.DestroyImmediate(line.GetComponent<BoxCollider>());
        }

        // Puntos de referencia
        GameObject spawnPt = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(seg.transform);
        spawnPt.transform.localPosition = new Vector3(0, 0, 30f);

        GameObject despawnPt = new GameObject("DespawnPoint");
        despawnPt.transform.SetParent(seg.transform);
        despawnPt.transform.localPosition = Vector3.zero;

        // Asignar referencias via SerializedObject
        SerializedObject so = new SerializedObject(ts);
        so.FindProperty("spawnPoint").objectReferenceValue   = spawnPt.transform;
        so.FindProperty("despawnPoint").objectReferenceValue = despawnPt.transform;
        so.ApplyModifiedProperties();

        // Spawner de obstáculos
        ObstacleSpawner spawner = seg.AddComponent<ObstacleSpawner>();
        SerializedObject soSpawner = new SerializedObject(spawner);

        // Asignar prefabs de obstáculos y monedas
        GameObject obstaclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Block.prefab");
        GameObject coinPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Coin.prefab");

        if (obstaclePrefab != null)
        {
            soSpawner.FindProperty("obstaclePrefabs").arraySize = 1;
            soSpawner.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(0).objectReferenceValue = obstaclePrefab;
        }
        if (coinPrefab != null)
            soSpawner.FindProperty("coinPrefab").objectReferenceValue = coinPrefab;

        soSpawner.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(seg, path);
        Object.DestroyImmediate(seg);
    }

    private static void CreateDecisionSegmentPrefab()
    {
        string path = "Assets/_Project/Prefabs/Segment_Decision.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject seg = new GameObject("Segment_Decision");
        TrackSegment ts = seg.AddComponent<TrackSegment>();

        SerializedObject soTs = new SerializedObject(ts);
        soTs.FindProperty("segmentType").enumValueIndex = (int)TrackSegment.SegmentType.Decision;
        soTs.ApplyModifiedProperties();

        // Suelo
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(seg.transform);
        floor.transform.localPosition = new Vector3(0, -0.1f, 15f);
        floor.transform.localScale    = new Vector3(8f, 0.2f, 30f);
        AssignMat(floor, "Mat_Track");
        Object.DestroyImmediate(floor.GetComponent<BoxCollider>());
        floor.AddComponent<BoxCollider>();

        // Señales de decisión (izquierda/derecha)
        CreateDecisionSign(seg.transform, -LaneSystem.LaneWidth, "IZQ", new Color(0.2f, 0.8f, 0.3f));
        CreateDecisionSign(seg.transform,  LaneSystem.LaneWidth, "DER", new Color(0.9f, 0.3f, 0.2f));

        // Puntos de referencia
        GameObject spawnPt = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(seg.transform);
        spawnPt.transform.localPosition = new Vector3(0, 0, 30f);

        GameObject despawnPt = new GameObject("DespawnPoint");
        despawnPt.transform.SetParent(seg.transform);
        despawnPt.transform.localPosition = Vector3.zero;

        SerializedObject so = new SerializedObject(ts);
        so.FindProperty("spawnPoint").objectReferenceValue   = spawnPt.transform;
        so.FindProperty("despawnPoint").objectReferenceValue = despawnPt.transform;
        so.ApplyModifiedProperties();

        // DecisionPoint
        GameObject dpGO = new GameObject("DecisionPoint");
        dpGO.transform.SetParent(seg.transform);
        dpGO.transform.localPosition = new Vector3(0, 0, 10f);
        dpGO.AddComponent<DecisionPoint>();

        PrefabUtility.SaveAsPrefabAsset(seg, path);
        Object.DestroyImmediate(seg);
    }

    private static void CreateDecisionSign(Transform parent, float x, string label, Color color)
    {
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = $"Sign_{label}";
        sign.transform.SetParent(parent);
        sign.transform.localPosition = new Vector3(x, 1.5f, 12f);
        sign.transform.localScale    = new Vector3(1.5f, 2f, 0.2f);

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Smoothness", 0f);
        sign.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(sign.GetComponent<BoxCollider>());
    }

    // ─── Game Managers ────────────────────────────────────────────────────────

    private static void SetupGameManagers()
    {
        // RunnerGameManager
        if (GameObject.FindFirstObjectByType<RunnerGameManager>() == null)
        {
            GameObject go = new GameObject("RunnerGameManager");
            go.AddComponent<RunnerGameManager>();
        }

        // DecisionManager
        if (GameObject.FindFirstObjectByType<DecisionManager>() == null)
        {
            GameObject go = new GameObject("DecisionManager");
            go.AddComponent<DecisionManager>();
        }
    }

    // ─── Decision System ─────────────────────────────────────────────────────

    private static void SetupDecisionSystem()
    {
        // Crear DecisionData de ejemplo si no existe
        EnsureDir("Assets/_Project/Resources");
        string dataPath = "Assets/_Project/Resources/Decision_Entrenar.asset";
        if (AssetDatabase.LoadAssetAtPath<DecisionData>(dataPath) == null)
        {
            DecisionData d = ScriptableObject.CreateInstance<DecisionData>();
            d.question          = "¿Qué haces esta tarde?";
            d.leftLabel         = "🏋️ Entrenar";
            d.leftDescription   = "Te quedas a practicar tiros libres.";
            d.leftConsequence   = new StatConsequence { skillDelta = 10f, energyDelta = -5f, description = "Mejoras tu técnica." };
            d.rightLabel        = "🎉 Fiesta";
            d.rightDescription  = "Sales de fiesta con el equipo.";
            d.rightConsequence  = new StatConsequence { popularityDelta = 10f, energyDelta = -20f, description = "Ganas popularidad pero te cansas." };
            d.leftColor         = new Color(0.2f, 0.8f, 0.3f);
            d.rightColor        = new Color(0.9f, 0.3f, 0.2f);
            AssetDatabase.CreateAsset(d, dataPath);
        }

        string dataPath2 = "Assets/_Project/Resources/Decision_Dieta.asset";
        if (AssetDatabase.LoadAssetAtPath<DecisionData>(dataPath2) == null)
        {
            DecisionData d = ScriptableObject.CreateInstance<DecisionData>();
            d.question          = "El nutricionista te recomienda una dieta estricta.";
            d.leftLabel         = "🥗 Seguir la dieta";
            d.leftDescription   = "Comes sano y te recuperas mejor.";
            d.leftConsequence   = new StatConsequence { energyDelta = 20f, speedDelta = 5f, description = "Tu cuerpo responde mejor." };
            d.rightLabel        = "🍔 Comer lo que quieras";
            d.rightDescription  = "Disfrutas la comida pero tu rendimiento baja.";
            d.rightConsequence  = new StatConsequence { energyDelta = -15f, speedDelta = -5f, description = "Tu rendimiento baja." };
            d.leftColor         = new Color(0.2f, 0.8f, 0.3f);
            d.rightColor        = new Color(0.9f, 0.3f, 0.2f);
            AssetDatabase.CreateAsset(d, dataPath2);
        }

        AssetDatabase.SaveAssets();
    }

    // ─── Game Over UI ─────────────────────────────────────────────────────────

    private static void SetupGameOverUI()
    {
        GameObject gameOverPanel = GameObject.Find("Canvas/GameOverPanel");
        if (gameOverPanel == null) return;

        if (gameOverPanel.GetComponent<GameOverUI>() == null)
            gameOverPanel.AddComponent<GameOverUI>();
    }

    // ─── Wire Existing UI ─────────────────────────────────────────────────────

    private static void WireExistingUI()
    {
        // Botón Jugar → RunnerGameManager.StartGame
        GameObject jugarBtn = GameObject.Find("Canvas/MainMenuPanel/jugar");
        if (jugarBtn != null)
        {
            Button btn = jugarBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                RunnerGameManager rgm = GameObject.FindFirstObjectByType<RunnerGameManager>();
                if (rgm != null)
                {
                    SerializedObject so = new SerializedObject(btn);
                    // Añadir listener persistente via UnityEvent
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(
                        btn.onClick,
                        rgm.StartGame
                    );
                }
            }
        }

        // Conectar GameplayHUD con sus textos existentes
        GameObject gameplayPanel = GameObject.Find("Canvas/GameplayPanel");
        if (gameplayPanel != null)
        {
            GameplayHUD hud = gameplayPanel.GetComponent<GameplayHUD>();
            if (hud != null)
            {
                SerializedObject so = new SerializedObject(hud);

                GameObject coinsGO = GameObject.Find("Canvas/GameplayPanel/monedas.m");
                GameObject scoreGO = GameObject.Find("Canvas/GameplayPanel/score");
                GameObject progGO  = GameObject.Find("Canvas/GameplayPanel/progreso");
                GameObject energyGO= GameObject.Find("Canvas/GameplayPanel/energia");

                if (coinsGO) so.FindProperty("coinsText").objectReferenceValue    = coinsGO.GetComponent<TMP_Text>();
                if (scoreGO) so.FindProperty("scoreText").objectReferenceValue    = scoreGO.GetComponent<TMP_Text>();
                if (progGO)  so.FindProperty("progressBar").objectReferenceValue  = progGO.GetComponent<Slider>();
                if (energyGO)so.FindProperty("energyBar").objectReferenceValue    = energyGO.GetComponent<Slider>();

                so.ApplyModifiedProperties();
            }
        }

        // Conectar MainMenuUI con sus textos
        GameObject mainMenuPanel = GameObject.Find("Canvas/MainMenuPanel");
        if (mainMenuPanel != null)
        {
            MainMenuUI mmUI = mainMenuPanel.GetComponent<MainMenuUI>();
            if (mmUI != null)
            {
                SerializedObject so = new SerializedObject(mmUI);

                GameObject coinsGO = GameObject.Find("Canvas/MainMenuPanel/monedas");
                GameObject bestGO  = GameObject.Find("Canvas/MainMenuPanel/mejor");
                GameObject levelGO = GameObject.Find("Canvas/MainMenuPanel/nivel");

                if (coinsGO) so.FindProperty("coinsText").objectReferenceValue     = coinsGO.GetComponent<TMP_Text>();
                if (bestGO)  so.FindProperty("bestScoreText").objectReferenceValue  = bestGO.GetComponent<TMP_Text>();
                if (levelGO) so.FindProperty("levelText").objectReferenceValue      = levelGO.GetComponent<TMP_Text>();

                so.ApplyModifiedProperties();
            }
        }
    }

    // ─── Pista inicial ────────────────────────────────────────────────────────

    private static void CreateStartingTrack()
    {
        // Crear prefabs de obstáculo y moneda si no existen
        CreateObstaclePrefab();
        CreateCoinPrefab();
    }

    private static void CreateObstaclePrefab()
    {
        string path = "Assets/_Project/Prefabs/Obstacle_Block.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = "Obstacle_Block";
        obs.transform.localScale = new Vector3(1.8f, 1.5f, 1.8f);
        AssignMat(obs, "Mat_Obstacle");

        // Trigger collider
        BoxCollider bc = obs.GetComponent<BoxCollider>();
        bc.isTrigger = true;

        obs.AddComponent<Obstacle>();
        obs.tag = "Obstacle";

        PrefabUtility.SaveAsPrefabAsset(obs, path);
        Object.DestroyImmediate(obs);
    }

    private static void CreateCoinPrefab()
    {
        string path = "Assets/_Project/Prefabs/Coin.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "Coin";
        coin.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        AssignMat(coin, "Mat_Coin");

        SphereCollider sc = coin.GetComponent<SphereCollider>();
        sc.isTrigger = true;

        coin.AddComponent<Coin>();

        PrefabUtility.SaveAsPrefabAsset(coin, path);
        Object.DestroyImmediate(coin);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void AssignMat(GameObject go, string matName)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/{matName}.mat");
        if (mat == null) return;
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr) mr.sharedMaterial = mat;
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
