#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PreviewCoin
{
    public static void Execute()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Coin.prefab");
        if (prefab == null) return;

        var existing = GameObject.Find("__CoinPreview");
        if (existing) Object.DestroyImmediate(existing);

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "__CoinPreview";
        go.transform.position = new Vector3(0, 1f, 10f);
        Selection.activeObject = go;
    }

    public static void Cleanup()
    {
        var existing = GameObject.Find("__CoinPreview");
        if (existing) Object.DestroyImmediate(existing);
    }
}
#endif
