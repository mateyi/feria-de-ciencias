using UnityEditor;
using UnityEngine;

public class PreviewObstacles
{
    public static void Execute()
    {
        // Instanciar los 3 obstáculos en la escena para previsualizar
        string[] paths = {
            "Assets/_Project/Prefabs/Obstacle_Cone.prefab",
            "Assets/_Project/Prefabs/Obstacle_Hurdle.prefab",
            "Assets/_Project/Prefabs/Obstacle_Ball.prefab"
        };
        float[] xPos = { -3f, 0f, 3f };

        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
            if (prefab == null) continue;
            // Check if already in scene
            string name = prefab.name + "_Preview";
            if (GameObject.Find(name) != null) continue;
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = new Vector3(xPos[i], 0, 5f);
        }

        // Frame the obstacles
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot     = new Vector3(0, 1f, 5f);
            sv.rotation  = Quaternion.Euler(20f, 0f, 0f);
            sv.size      = 5f;
            sv.in2DMode  = false;
            sv.Repaint();
        }
    }

    public static void Cleanup()
    {
        string[] names = { "Obstacle_Cone_Preview", "Obstacle_Hurdle_Preview", "Obstacle_Ball_Preview" };
        foreach (string n in names)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
