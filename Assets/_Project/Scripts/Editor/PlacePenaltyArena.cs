#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlacePenaltyArena
{
    public static void Execute()
    {
        var arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PenaltyArena.prefab");
        if (arenaPrefab == null) { Debug.LogWarning("PenaltyArena.prefab no encontrado"); return; }

        var existing = GameObject.Find("PenaltyArena_Instance");
        if (existing) Object.DestroyImmediate(existing);

        // Ubicar la arena después del túnel. El arco de meta está ~Z=300, túnel ~18m.
        float arenaZ = 330f;
        var tunnel = GameObject.Find("TrackGenerator/PenaltyTunnel_15m_Instance");
        if (tunnel != null) arenaZ = tunnel.transform.position.z + 22f;

        var arena = (GameObject)PrefabUtility.InstantiatePrefab(arenaPrefab);
        arena.name = "PenaltyArena_Instance";
        arena.transform.position = new Vector3(0, 0, arenaZ);
        arena.transform.rotation = Quaternion.identity;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[PlacePenaltyArena] Arena colocada en Z={arenaZ:0.##}");
    }
}
#endif
