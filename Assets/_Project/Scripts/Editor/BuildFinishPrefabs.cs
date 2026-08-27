#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;

public static class BuildFinishPrefabs
{
    [MenuItem("MundialRunner/Build Finish Prefabs", false, 200)]
    public static void Build()
    {
        string prefabsPath = "Assets/_Project/Prefabs";
        string modelsPath = "Assets/_Project/Models";

        if (!AssetDatabase.IsValidFolder(prefabsPath)) Directory.CreateDirectory(prefabsPath);
        if (!AssetDatabase.IsValidFolder(modelsPath)) Directory.CreateDirectory(modelsPath);

        string finishPrefabName = "FinishArch_Meta.prefab";
        string tunnelPrefabName = "PenaltyTunnel_15m.prefab";

        CreateFinishArchPrefab(Path.Combine(prefabsPath, finishPrefabName));
        CreatePenaltyTunnelPrefab(Path.Combine(prefabsPath, tunnelPrefabName));

        CreateFinishArchPrefab(Path.Combine(modelsPath, finishPrefabName));
        CreatePenaltyTunnelPrefab(Path.Combine(modelsPath, tunnelPrefabName));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildFinishPrefabs] Done.");
    }

    private static void CreateFinishArchPrefab(string assetPath)
    {
        var old = Selection.activeObject;

        GameObject root = new GameObject("FinishArch_Meta");

        float width = 6.2f;   // cruza los 3 carriles
        float height = 3.6f;
        float depth = 0.35f;

        Material frameMat = FindOrCreateMaterial("Mat_GoldFrame", MatKind.Gold);
        Material fabricMat = FindOrCreateMaterial("Mat_GoldFabric", MatKind.GoldFabric);

        // Estructura del arco (todo parenteado al root)
        GameObject barTop = CreateCube("ArchBar", new Vector3(width + 0.5f, 0.35f, depth), new Vector3(0, height, 0), frameMat, root.transform);
        CreateCube("ArchPost_L", new Vector3(0.28f, height, depth), new Vector3(-width * 0.5f, height * 0.5f, 0), frameMat, root.transform);
        CreateCube("ArchPost_R", new Vector3(0.28f, height, depth), new Vector3(width * 0.5f, height * 0.5f, 0), frameMat, root.transform);

        // Lona/tela colgando desde la barra (cinta de llegada)
        GameObject fabric = CreateCube("Fabric_Meta", new Vector3(width, 1.5f, 0.06f), new Vector3(0, height - 0.85f, 0), fabricMat, root.transform);

        // Texto "META" dorado sobre la lona, mirando al jugador (que viene desde -Z)
        GameObject textGO = new GameObject("Text_META");
        textGO.transform.SetParent(root.transform, false);
        textGO.transform.localPosition = new Vector3(0, height - 0.85f, -0.06f);
        textGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        var tmp = textGO.AddComponent<TextMeshPro>();
        tmp.text = "META";
        tmp.fontSize = 8;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.84f, 0.2f, 1f);
        tmp.fontStyle = FontStyles.Bold;
        tmp.enableWordWrapping = false;
        var rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(width, 1.4f);

        // Confetti dorado
        GameObject psGO = new GameObject("Confetti_Gold");
        psGO.transform.SetParent(root.transform, false);
        psGO.transform.localPosition = new Vector3(0, height - 0.3f, 0.02f);
        var ps = psGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.14f);
        main.gravityModifier = 0.6f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.84f, 0.2f), new Color(1f, 0.65f, 0.05f));
        main.loop = false;
        main.playOnAwake = false;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.4f, 0.4f);
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 220) });
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = FindOrCreateMaterial("Mat_Particle_Gold", MatKind.ParticleGold);

        // Trigger cruzando los 3 carriles
        GameObject triggerGO = new GameObject("Trigger_Finish");
        triggerGO.transform.SetParent(root.transform, false);
        triggerGO.transform.localPosition = new Vector3(0, 1.6f, 0f);
        var col = triggerGO.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(width + 1f, 3.6f, 1.5f);

        var trig = triggerGO.AddComponent<FinishArchTrigger>();
        trig.SetConfetti(ps);
        trig.SetOffsets(0.8f, 10f);

        SavePrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = old;
    }

    private static void CreatePenaltyTunnelPrefab(string assetPath)
    {
        var old = Selection.activeObject;

        GameObject root = new GameObject("PenaltyTunnel_15m");

        float length = 18f; // ~15-20m
        float width = 6.6f;
        float wallH = 3.4f;

        Material wallMat = FindOrCreateMaterial("Mat_TunnelWall", MatKind.TunnelWall);
        Material floorMat = FindOrCreateMaterial("Mat_TunnelFloor", MatKind.TunnelFloor);
        Material trimMat = FindOrCreateMaterial("Mat_TunnelTrim", MatKind.Gold);

        // Piso del túnel (centro en length/2, todo parenteado al root)
        // IMPORTANTE: este piso SÍ lleva collider para que el jugador no atraviese el suelo.
        GameObject floor = CreateCubeWithCollider("TunnelFloor", new Vector3(width, 0.2f, length), new Vector3(0, -0.05f, length * 0.5f), floorMat, root.transform);

        // Paredes laterales
        CreateCube("Wall_L", new Vector3(0.25f, wallH, length), new Vector3(-width * 0.5f, wallH * 0.5f, length * 0.5f), wallMat, root.transform);
        CreateCube("Wall_R", new Vector3(0.25f, wallH, length), new Vector3(width * 0.5f, wallH * 0.5f, length * 0.5f), wallMat, root.transform);

        // Techo del túnel
        CreateCube("Ceiling", new Vector3(width, 0.2f, length), new Vector3(0, wallH, length * 0.5f), wallMat, root.transform);

        // Molduras doradas en la base de las paredes (detalle vestuario)
        CreateCube("Trim_L", new Vector3(0.28f, 0.25f, length), new Vector3(-width * 0.5f + 0.1f, 0.5f, length * 0.5f), trimMat, root.transform);
        CreateCube("Trim_R", new Vector3(0.28f, 0.25f, length), new Vector3(width * 0.5f - 0.1f, 0.5f, length * 0.5f), trimMat, root.transform);

        // Spotlights dramáticos hacia abajo, distribuidos a lo largo
        int spots = 4;
        for (int i = 0; i < spots; i++)
        {
            float z = length * ((i + 0.5f) / spots);
            GameObject spotGO = new GameObject($"Spot_{i}");
            spotGO.transform.SetParent(root.transform, false);
            spotGO.transform.localPosition = new Vector3(0, wallH - 0.25f, z);
            spotGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // apunta hacia abajo
            var light = spotGO.AddComponent<Light>();
            light.type = LightType.Spot;
            light.spotAngle = 55f;
            light.range = 12f;
            light.intensity = 9f;
            light.color = new Color(1f, 0.96f, 0.82f);
            light.renderMode = LightRenderMode.ForcePixel;

            // Pequeña carcasa de la lámpara
            CreateCube($"Lamp_{i}", new Vector3(0.5f, 0.15f, 0.5f), new Vector3(0, wallH - 0.05f, z), trimMat, root.transform);
        }

        SavePrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = old;
    }

    private enum MatKind { Gold, GoldFabric, ParticleGold, TunnelWall, TunnelFloor }

    private static Material FindOrCreateMaterial(string matName, MatKind kind)
    {
        string path = $"Assets/_Project/Materials/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created = false;
        if (mat == null)
        {
            string shader = kind == MatKind.ParticleGold
                ? "Universal Render Pipeline/Unlit"
                : "Universal Render Pipeline/Lit";
            mat = new Material(Shader.Find(shader));
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, path);
            created = true;
        }

        switch (kind)
        {
            case MatKind.Gold:
                mat.color = new Color(1f, 0.82f, 0.2f);
                mat.SetFloat("_Metallic", 0.85f);
                mat.SetFloat("_Smoothness", 0.35f);
                break;
            case MatKind.GoldFabric:
                mat.color = new Color(0.95f, 0.72f, 0.12f);
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.15f);
                break;
            case MatKind.ParticleGold:
                mat.color = new Color(1f, 0.84f, 0.25f);
                break;
            case MatKind.TunnelWall:
                mat.color = new Color(0.16f, 0.16f, 0.19f);
                mat.SetFloat("_Metallic", 0.1f);
                mat.SetFloat("_Smoothness", 0.1f);
                break;
            case MatKind.TunnelFloor:
                mat.color = new Color(0.10f, 0.10f, 0.11f);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Smoothness", 0.2f);
                break;
        }

        if (created) EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateCube(string name, Vector3 size, Vector3 localPos, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;
        // los cubos visuales no necesitan collider
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);
        return go;
    }

    // Igual que CreateCube pero conservando el BoxCollider (para pisos/rampas).
    private static GameObject CreateCubeWithCollider(string name, Vector3 size, Vector3 localPos, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = mat;
        var col = go.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = false;
        return go;
    }

    private static void SavePrefabAsset(GameObject root, string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath);
        if (!AssetDatabase.IsValidFolder(directory)) Directory.CreateDirectory(directory);
        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
    }
}
#endif
