using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye los prefabs de segmento con materiales correctos y spawners conectados,
/// luego coloca la pista completa en la escena.
/// </summary>
public class RebuildPrefabsAndTrack
{
    public static void Execute()
    {
        // 1. Borrar prefabs existentes para recrearlos limpios
        DeletePrefab("Assets/_Project/Prefabs/Segment_Normal.prefab");
        DeletePrefab("Assets/_Project/Prefabs/Segment_Decision.prefab");
        DeletePrefab("Assets/_Project/Prefabs/Obstacle_Block.prefab");
        DeletePrefab("Assets/_Project/Prefabs/Coin.prefab");
        AssetDatabase.Refresh();

        // 2. Crear prefabs de obstáculo y moneda primero
        CreateObstaclePrefab();
        CreateCoinPrefab();
        AssetDatabase.Refresh();

        // 3. Crear segmentos con referencias correctas
        CreateNormalSegmentPrefab();
        CreateDecisionSegmentPrefab();
        AssetDatabase.Refresh();

        // 4. Limpiar segmentos viejos de la escena
        ClearOldSegments();

        // 5. Colocar pista nueva
        PlaceTrack();

        // 6. Guardar
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[MundialRunner] ✅ Prefabs reconstruidos y pista colocada.");
    }

    // ─── Obstacle ─────────────────────────────────────────────────────────────

    private static void CreateObstaclePrefab()
    {
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.name = "Obstacle_Block";
        obs.transform.localScale = new Vector3(1.8f, 1.5f, 1.8f);
        AssignMat(obs, "Mat_Obstacle");
        BoxCollider bc = obs.GetComponent<BoxCollider>();
        bc.isTrigger = true;
        obs.AddComponent<Obstacle>();
        obs.tag = "Obstacle";
        PrefabUtility.SaveAsPrefabAsset(obs, "Assets/_Project/Prefabs/Obstacle_Block.prefab");
        Object.DestroyImmediate(obs);
        Debug.Log("[MundialRunner] Obstacle_Block.prefab creado.");
    }

    // ─── Coin ─────────────────────────────────────────────────────────────────

    private static void CreateCoinPrefab()
    {
        // Usar un cilindro aplanado para simular moneda
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coin.name = "Coin";
        coin.transform.localScale = new Vector3(0.45f, 0.06f, 0.45f);
        AssignMat(coin, "Mat_Coin");
        CapsuleCollider cc = coin.GetComponent<CapsuleCollider>();
        cc.isTrigger = true;
        coin.AddComponent<Coin>();
        PrefabUtility.SaveAsPrefabAsset(coin, "Assets/_Project/Prefabs/Coin.prefab");
        Object.DestroyImmediate(coin);
        Debug.Log("[MundialRunner] Coin.prefab creado.");
    }

    // ─── Normal Segment ───────────────────────────────────────────────────────

    private static void CreateNormalSegmentPrefab()
    {
        const float segLen = 30f;

        GameObject seg = new GameObject("Segment_Normal");
        TrackSegment ts = seg.AddComponent<TrackSegment>();

        // Suelo
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(seg.transform);
        floor.transform.localPosition = new Vector3(0, -0.1f, segLen * 0.5f);
        floor.transform.localScale    = new Vector3(8f, 0.2f, segLen);
        AssignMat(floor, "Mat_Track");
        Object.DestroyImmediate(floor.GetComponent<BoxCollider>());
        floor.AddComponent<BoxCollider>(); // suelo sólido

        // Líneas de carril
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneLine";
            line.transform.SetParent(seg.transform);
            line.transform.localPosition = new Vector3(i * LaneSystem.LaneWidth * 0.5f, 0.01f, segLen * 0.5f);
            line.transform.localScale    = new Vector3(0.1f, 0.01f, segLen);
            AssignMat(line, "Mat_TrackLine");
            Object.DestroyImmediate(line.GetComponent<BoxCollider>());
        }

        // Bordes laterales (tribuna low-poly)
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "Border";
            border.transform.SetParent(seg.transform);
            border.transform.localPosition = new Vector3(side * 5.5f, 0.3f, segLen * 0.5f);
            border.transform.localScale    = new Vector3(3f, 0.8f, segLen);
            AssignMat(border, "Mat_Crowd");
            Object.DestroyImmediate(border.GetComponent<BoxCollider>());
        }

        // Puntos de referencia
        GameObject spawnPt = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(seg.transform);
        spawnPt.transform.localPosition = new Vector3(0, 0, segLen);

        GameObject despawnPt = new GameObject("DespawnPoint");
        despawnPt.transform.SetParent(seg.transform);
        despawnPt.transform.localPosition = Vector3.zero;

        // Asignar referencias TrackSegment
        SerializedObject soTs = new SerializedObject(ts);
        soTs.FindProperty("spawnPoint").objectReferenceValue   = spawnPt.transform;
        soTs.FindProperty("despawnPoint").objectReferenceValue = despawnPt.transform;
        soTs.ApplyModifiedProperties();

        // ObstacleSpawner
        ObstacleSpawner spawner = seg.AddComponent<ObstacleSpawner>();
        SerializedObject soSp = new SerializedObject(spawner);

        GameObject obsPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Obstacle_Block.prefab");
        GameObject coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Coin.prefab");

        if (obsPrefab != null)
        {
            soSp.FindProperty("obstaclePrefabs").arraySize = 1;
            soSp.FindProperty("obstaclePrefabs").GetArrayElementAtIndex(0).objectReferenceValue = obsPrefab;
        }
        if (coinPrefab != null)
            soSp.FindProperty("coinPrefab").objectReferenceValue = coinPrefab;

        soSp.FindProperty("obstacleCount").intValue  = 2;
        soSp.FindProperty("coinCount").intValue      = 5;
        soSp.FindProperty("spawnZStart").floatValue  = 6f;
        soSp.FindProperty("spawnZEnd").floatValue    = 24f;
        soSp.FindProperty("coinHeight").floatValue   = 0.5f;
        soSp.FindProperty("obstacleHeight").floatValue = 0.75f;
        soSp.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(seg, "Assets/_Project/Prefabs/Segment_Normal.prefab");
        Object.DestroyImmediate(seg);
        Debug.Log("[MundialRunner] Segment_Normal.prefab creado.");
    }

    // ─── Decision Segment ─────────────────────────────────────────────────────

    private static void CreateDecisionSegmentPrefab()
    {
        const float segLen = 30f;

        GameObject seg = new GameObject("Segment_Decision");
        TrackSegment ts = seg.AddComponent<TrackSegment>();

        SerializedObject soTs = new SerializedObject(ts);
        soTs.FindProperty("segmentType").enumValueIndex = (int)TrackSegment.SegmentType.Decision;
        soTs.ApplyModifiedProperties();

        // Suelo (color diferente para indicar decisión)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(seg.transform);
        floor.transform.localPosition = new Vector3(0, -0.1f, segLen * 0.5f);
        floor.transform.localScale    = new Vector3(8f, 0.2f, segLen);
        AssignMat(floor, "Mat_Track");
        Object.DestroyImmediate(floor.GetComponent<BoxCollider>());
        floor.AddComponent<BoxCollider>();

        // Líneas de carril
        for (int i = -1; i <= 1; i += 2)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "LaneLine";
            line.transform.SetParent(seg.transform);
            line.transform.localPosition = new Vector3(i * LaneSystem.LaneWidth * 0.5f, 0.01f, segLen * 0.5f);
            line.transform.localScale    = new Vector3(0.1f, 0.01f, segLen);
            AssignMat(line, "Mat_TrackLine");
            Object.DestroyImmediate(line.GetComponent<BoxCollider>());
        }

        // Señales de decisión con materiales guardados
        CreateDecisionSign(seg.transform, -LaneSystem.LaneWidth, "IZQ", "Mat_DecisionLeft");
        CreateDecisionSign(seg.transform,  LaneSystem.LaneWidth, "DER", "Mat_DecisionRight");

        // Arco de decisión (portal)
        CreateDecisionArch(seg.transform);

        // Bordes
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = "Border";
            border.transform.SetParent(seg.transform);
            border.transform.localPosition = new Vector3(side * 5.5f, 0.3f, segLen * 0.5f);
            border.transform.localScale    = new Vector3(3f, 0.8f, segLen);
            AssignMat(border, "Mat_Crowd");
            Object.DestroyImmediate(border.GetComponent<BoxCollider>());
        }

        // Puntos de referencia
        GameObject spawnPt = new GameObject("SpawnPoint");
        spawnPt.transform.SetParent(seg.transform);
        spawnPt.transform.localPosition = new Vector3(0, 0, segLen);

        GameObject despawnPt = new GameObject("DespawnPoint");
        despawnPt.transform.SetParent(seg.transform);
        despawnPt.transform.localPosition = Vector3.zero;

        soTs = new SerializedObject(ts);
        soTs.FindProperty("spawnPoint").objectReferenceValue   = spawnPt.transform;
        soTs.FindProperty("despawnPoint").objectReferenceValue = despawnPt.transform;
        soTs.ApplyModifiedProperties();

        // DecisionPoint
        GameObject dpGO = new GameObject("DecisionPoint");
        dpGO.transform.SetParent(seg.transform);
        dpGO.transform.localPosition = new Vector3(0, 0, 10f);
        dpGO.AddComponent<DecisionPoint>();

        PrefabUtility.SaveAsPrefabAsset(seg, "Assets/_Project/Prefabs/Segment_Decision.prefab");
        Object.DestroyImmediate(seg);
        Debug.Log("[MundialRunner] Segment_Decision.prefab creado.");
    }

    private static void CreateDecisionSign(Transform parent, float x, string label, string matName)
    {
        // Poste
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = $"Post_{label}";
        post.transform.SetParent(parent);
        post.transform.localPosition = new Vector3(x, 1.5f, 10f);
        post.transform.localScale    = new Vector3(0.15f, 1.5f, 0.15f);
        AssignMat(post, "Mat_TrackLine");
        Object.DestroyImmediate(post.GetComponent<CapsuleCollider>());

        // Cartel
        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = $"Sign_{label}";
        sign.transform.SetParent(parent);
        sign.transform.localPosition = new Vector3(x, 3.5f, 10f);
        sign.transform.localScale    = new Vector3(2f, 1.5f, 0.25f);
        AssignMat(sign, matName);
        Object.DestroyImmediate(sign.GetComponent<BoxCollider>());
    }

    private static void CreateDecisionArch(Transform parent)
    {
        // Barra horizontal del arco
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = "ArchBar";
        bar.transform.SetParent(parent);
        bar.transform.localPosition = new Vector3(0, 5f, 10f);
        bar.transform.localScale    = new Vector3(9f, 0.4f, 0.4f);
        AssignMat(bar, "Mat_TrackLine");
        Object.DestroyImmediate(bar.GetComponent<BoxCollider>());

        // Pilares
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.name = "ArchPillar";
            pillar.transform.SetParent(parent);
            pillar.transform.localPosition = new Vector3(side * 4.3f, 2.5f, 10f);
            pillar.transform.localScale    = new Vector3(0.4f, 5f, 0.4f);
            AssignMat(pillar, "Mat_TrackLine");
            Object.DestroyImmediate(pillar.GetComponent<BoxCollider>());
        }
    }

    // ─── Place Track ──────────────────────────────────────────────────────────

    private static void ClearOldSegments()
    {
        string[] names = { "Segment_Normal", "Segment_Decision",
                           "Segment_Normal(Clone)", "Segment_Decision(Clone)" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            while (go != null)
            {
                Object.DestroyImmediate(go);
                go = GameObject.Find(n);
            }
        }
    }

    private static void PlaceTrack()
    {
        TrackSegment normalPrefab   = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Normal.prefab");
        TrackSegment decisionPrefab = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Decision.prefab");

        if (normalPrefab == null) { Debug.LogError("Segment_Normal.prefab no encontrado"); return; }

        Vector3 pos = Vector3.zero;

        // 3 normales → 1 decisión → 3 normales → 1 decisión → 2 normales
        int[] pattern = { 0, 0, 0, 1, 0, 0, 0, 1, 0, 0 }; // 0=normal, 1=decision

        foreach (int type in pattern)
        {
            TrackSegment prefab = (type == 1 && decisionPrefab != null) ? decisionPrefab : normalPrefab;
            TrackSegment seg = (TrackSegment)PrefabUtility.InstantiatePrefab(prefab);
            seg.transform.position = pos;
            pos = seg.SpawnPoint;
        }

        // Ajustar SceneView
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot     = new Vector3(0, 2f, 50f);
            sv.rotation  = Quaternion.Euler(30f, 0f, 0f);
            sv.size      = 40f;
            sv.orthographic = false;
            sv.Repaint();
        }

        Debug.Log("[MundialRunner] Pista completa colocada en la escena.");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static void AssignMat(GameObject go, string matName)
    {
        // Intentar cargar material existente
        Material mat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Materials/{matName}.mat");

        // Si no existe, crear uno nuevo
        if (mat == null)
        {
            Color color = matName switch
            {
                "Mat_DecisionLeft"  => new Color(0.15f, 0.75f, 0.25f),
                "Mat_DecisionRight" => new Color(0.85f, 0.25f, 0.15f),
                "Mat_Track"         => new Color(0.35f, 0.55f, 0.25f),
                "Mat_TrackLine"     => new Color(0.95f, 0.95f, 0.95f),
                "Mat_Player"        => new Color(0.15f, 0.45f, 0.85f),
                "Mat_Obstacle"      => new Color(0.90f, 0.25f, 0.20f),
                "Mat_Coin"          => new Color(1.00f, 0.80f, 0.10f),
                "Mat_Crowd"         => new Color(0.85f, 0.40f, 0.10f),
                _                   => Color.magenta
            };
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color;
            mat.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(mat, $"Assets/_Project/Materials/{matName}.mat");
        }

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr) mr.sharedMaterial = mat;
    }

    private static void DeletePrefab(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
