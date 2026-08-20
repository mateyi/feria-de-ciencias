using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso final: conecta TrackGenerator con los nuevos prefabs,
/// asigna DecisionData al DecisionPoint de la escena,
/// y actualiza el UIManager con los paneles correctos.
/// </summary>
public class FinalWire
{
    public static void Execute()
    {
        WireTrackGenerator();
        WireDecisionPoints();
        WireUIManager();
        WireDecisionManagerUI();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MundialRunner] ✅ Conexión final completada.");
    }

    private static void WireTrackGenerator()
    {
        TrackGenerator tg = Object.FindFirstObjectByType<TrackGenerator>();
        if (tg == null) return;

        TrackSegment normalSeg   = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Normal.prefab");
        TrackSegment decisionSeg = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Decision.prefab");

        SerializedObject so = new SerializedObject(tg);

        if (normalSeg != null)
        {
            so.FindProperty("normalSegmentPrefabs").arraySize = 1;
            so.FindProperty("normalSegmentPrefabs").GetArrayElementAtIndex(0).objectReferenceValue = normalSeg;
            so.FindProperty("startSegmentPrefab").objectReferenceValue = normalSeg;
        }
        if (decisionSeg != null)
            so.FindProperty("decisionSegmentPrefab").objectReferenceValue = decisionSeg;

        so.FindProperty("segmentsAhead").intValue    = 5;
        so.FindProperty("segmentsBehind").intValue   = 2;
        so.FindProperty("decisionInterval").intValue = 4;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tg);
        Debug.Log("[MundialRunner] TrackGenerator conectado con prefabs.");
    }

    private static void WireDecisionPoints()
    {
        // Cargar las DecisionData de Resources
        DecisionData[] decisions = new DecisionData[]
        {
            AssetDatabase.LoadAssetAtPath<DecisionData>("Assets/_Project/Resources/Decision_Entrenar.asset"),
            AssetDatabase.LoadAssetAtPath<DecisionData>("Assets/_Project/Resources/Decision_Dieta.asset"),
        };

        DecisionPoint[] points = Object.FindObjectsByType<DecisionPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < points.Length; i++)
        {
            DecisionData data = decisions[i % decisions.Length];
            if (data == null) continue;

            SerializedObject so = new SerializedObject(points[i]);
            so.FindProperty("decisionData").objectReferenceValue = data;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(points[i]);
        }

        // También asignar en el prefab de decisión
        string prefabPath = "Assets/_Project/Prefabs/Segment_Decision.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot != null)
        {
            DecisionPoint dp = prefabRoot.GetComponentInChildren<DecisionPoint>();
            if (dp != null && decisions[0] != null)
            {
                SerializedObject so = new SerializedObject(dp);
                so.FindProperty("decisionData").objectReferenceValue = decisions[0];
                so.ApplyModifiedProperties();
            }
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        Debug.Log($"[MundialRunner] {points.Length} DecisionPoint(s) conectados con DecisionData.");
    }

    private static void WireUIManager()
    {
        UIManager uim = Object.FindFirstObjectByType<UIManager>();
        if (uim == null) return;

        SerializedObject so = new SerializedObject(uim);

        GameObject mainMenu   = GameObject.Find("Canvas/MainMenuPanel");
        GameObject gameplay   = GameObject.Find("Canvas/GameplayPanel");
        GameObject shop       = GameObject.Find("Canvas/ShopPanel");
        GameObject characters = GameObject.Find("Canvas/CharactersPanel");
        GameObject settings   = GameObject.Find("Canvas/SettingsPanel");
        GameObject gameOver   = GameObject.Find("Canvas/GameOverPanel");
        GameObject decision   = GameObject.Find("Canvas/DecisionPanel");

        if (mainMenu)   so.FindProperty("mainMenuPanel").objectReferenceValue   = mainMenu;
        if (gameplay)   so.FindProperty("gameplayPanel").objectReferenceValue   = gameplay;
        if (shop)       so.FindProperty("shopPanel").objectReferenceValue       = shop;
        if (characters) so.FindProperty("charactersPanel").objectReferenceValue = characters;
        if (settings)   so.FindProperty("settingsPanel").objectReferenceValue   = settings;
        if (gameOver)   so.FindProperty("gameOverPanel").objectReferenceValue   = gameOver;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(uim);
        Debug.Log("[MundialRunner] UIManager conectado con todos los paneles.");
    }

    private static void WireDecisionManagerUI()
    {
        DecisionManager dm = Object.FindFirstObjectByType<DecisionManager>();
        if (dm == null) return;

        DecisionUI dui = Object.FindFirstObjectByType<DecisionUI>();
        if (dui == null) return;

        SerializedObject so = new SerializedObject(dm);
        so.FindProperty("decisionUI").objectReferenceValue = dui;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(dm);
        Debug.Log("[MundialRunner] DecisionManager conectado con DecisionUI.");
    }
}
