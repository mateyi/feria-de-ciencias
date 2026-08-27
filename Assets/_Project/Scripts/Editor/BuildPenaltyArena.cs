#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.IO;

/// <summary>
/// Genera todos los assets visuales del minijuego de penales (Penalty Shootout Arena):
/// - Arco reglamentario blanco con red y zonas de puntería
/// - Balón con física (Rigidbody + SphereCollider)
/// - Iluminación "clutch" (spotlight cenital sobre el punto de penal)
/// - Gradas con público animado en loop
/// - Arquero rival (Humanoid rig) con AnimatorController (idle/dive L/dive R/center save)
/// Guarda prefabs en Assets/_Project/Prefabs y Assets/_Project/Models.
/// </summary>
public static class BuildPenaltyArena
{
    private const string PrefabsDir = "Assets/_Project/Prefabs";
    private const string ModelsDir = "Assets/_Project/Models";
    private const string MatDir = "Assets/_Project/Materials";
    private const string AnimDir = "Assets/_Project/Animations";

    // Medidas reglamentarias (aprox, escaladas a nuestro low-poly): 7.32 x 2.44 m
    private const float GoalWidth = 7.32f;
    private const float GoalHeight = 2.44f;
    private const float PostRadius = 0.06f;
    private const float GoalDepth = 1.8f;

    [MenuItem("MundialRunner/Build Penalty Arena", false, 210)]
    public static void Build()
    {
        EnsureDir(PrefabsDir);
        EnsureDir(ModelsDir);

        var goalController = BuildGoalkeeperAnimator();

        BuildGoalPrefab();
        BuildBallPrefab();
        BuildGoalkeeperPrefab(goalController);
        BuildClutchLightPrefab();
        BuildCrowdSectionPrefab();
        BuildArenaCompositePrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildPenaltyArena] Assets del minijuego de penales generados.");
    }

    // ── Arco reglamentario ────────────────────────────────────────────
    private static void BuildGoalPrefab()
    {
        GameObject root = new GameObject("PenaltyGoal");

        Material white = MakeMat("Mat_GoalPost", new Color(0.95f, 0.95f, 0.97f), 0.05f, 0.35f);
        Material netMat = MakeMat("Mat_GoalNet", new Color(0.9f, 0.9f, 0.9f, 0.35f), 0f, 0f, transparent: true);

        float halfW = GoalWidth * 0.5f;

        // Postes verticales
        CreateCylinder("Post_L", new Vector3(-halfW, GoalHeight * 0.5f, 0), new Vector3(PostRadius * 2, GoalHeight * 0.5f, PostRadius * 2), white, root.transform);
        CreateCylinder("Post_R", new Vector3(halfW, GoalHeight * 0.5f, 0), new Vector3(PostRadius * 2, GoalHeight * 0.5f, PostRadius * 2), white, root.transform);

        // Travesaño (horizontal) -> cilindro rotado en Z
        var crossbar = CreateCylinder("Crossbar", new Vector3(0, GoalHeight, 0), new Vector3(PostRadius * 2, halfW, PostRadius * 2), white, root.transform);
        crossbar.transform.localRotation = Quaternion.Euler(0, 0, 90f);

        // Soportes traseros (parte superior inclinada de la red)
        CreateCylinder("BackSupport_L", new Vector3(-halfW, GoalHeight * 0.5f, GoalDepth), new Vector3(PostRadius * 1.6f, GoalHeight * 0.5f, PostRadius * 1.6f), white, root.transform);
        CreateCylinder("BackSupport_R", new Vector3(halfW, GoalHeight * 0.5f, GoalDepth), new Vector3(PostRadius * 1.6f, GoalHeight * 0.5f, PostRadius * 1.6f), white, root.transform);

        // Red trasera
        GameObject netBack = CreatePlaneQuad("Net_Back", new Vector3(0, GoalHeight * 0.5f, GoalDepth), new Vector3(GoalWidth, GoalHeight, 1), Quaternion.Euler(90, 0, 0), netMat, root.transform);
        // Red superior
        CreatePlaneQuad("Net_Top", new Vector3(0, GoalHeight, GoalDepth * 0.5f), new Vector3(GoalWidth, GoalDepth, 1), Quaternion.Euler(0, 0, 0), netMat, root.transform);
        // Redes laterales
        CreatePlaneQuad("Net_L", new Vector3(-halfW, GoalHeight * 0.5f, GoalDepth * 0.5f), new Vector3(GoalDepth, GoalHeight, 1), Quaternion.Euler(90, 90, 0), netMat, root.transform);
        CreatePlaneQuad("Net_R", new Vector3(halfW, GoalHeight * 0.5f, GoalDepth * 0.5f), new Vector3(GoalDepth, GoalHeight, 1), Quaternion.Euler(90, 90, 0), netMat, root.transform);

        // Zonas de puntería (6 zonas: superior/inferior x izq/centro/der)
        Material zoneMat = MakeMat("Mat_TargetZone", new Color(1f, 0.85f, 0.1f, 0.20f), 0f, 0f, transparent: true);
        BuildTargetZones(root.transform, zoneMat, halfW);

        SavePrefabBoth(root, "PenaltyGoal.prefab");
        Object.DestroyImmediate(root);
    }

    private static void BuildTargetZones(Transform parent, Material zoneMat, float halfW)
    {
        // Contenedor de zonas de puntería (usadas luego como referencia de apuntado)
        GameObject zones = new GameObject("TargetZones");
        zones.transform.SetParent(parent, false);

        float zoneW = GoalWidth / 3f;
        float zoneH = GoalHeight / 2f;
        float z = 0.05f; // apenas delante del plano del arco

        string[] cols = { "L", "C", "R" };
        string[] rows = { "Top", "Bottom" };

        for (int r = 0; r < 2; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                float x = -halfW + zoneW * (c + 0.5f);
                float y = (r == 0) ? GoalHeight * 0.75f : GoalHeight * 0.25f;
                var zone = CreatePlaneQuad($"Zone_{rows[r]}_{cols[c]}",
                    new Vector3(x, y, z), new Vector3(zoneW * 0.9f, zoneH * 0.9f, 1),
                    Quaternion.identity, zoneMat, zones.transform);

                // marcar como referencia con un BoxCollider (no trigger de gameplay aún)
                var col = zone.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(1f, 1f, 0.2f);
            }
        }
    }

    // ── Balón con física ──────────────────────────────────────────────
    private static void BuildBallPrefab()
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "PenaltyBall";
        ball.transform.localScale = Vector3.one * 0.44f; // ~diámetro reglamentario 22cm de radio

        Material ballMat = MakeMat("Mat_PenaltyBall", Color.white, 0.05f, 0.3f);
        ball.GetComponent<MeshRenderer>().sharedMaterial = ballMat;

        var col = ball.GetComponent<SphereCollider>();
        col.isTrigger = false;

        var rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.43f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.1f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var pm = new PhysicsMaterial("Mat_Ball_Bounce");
        pm.bounciness = 0.6f;
        pm.dynamicFriction = 0.4f;
        pm.staticFriction = 0.4f;
        AssetDatabase.CreateAsset(pm, $"{MatDir}/PhysMat_Ball.physicMaterial");
        col.material = pm;

        SavePrefabBoth(ball, "PenaltyBall.prefab");
        Object.DestroyImmediate(ball);
    }

    // ── Arquero rival ─────────────────────────────────────────────────
    private static AnimatorController BuildGoalkeeperAnimator()
    {
        EnsureDir(AnimDir);
        string ctrlPath = $"{AnimDir}/GoalkeeperController.controller";

        var idle = FindClip("Assets/_Project/Animations/Goalkeeper_Idle.glb", "Idle");
        var dive = FindClip("Assets/_Project/Animations/Goalkeeper_DiveSide.glb", "Jump_to_Catch_and_Fall");
        var center = FindClip("Assets/_Project/Animations/Goalkeeper_CenterSave.glb", "Block1");

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        controller.AddParameter("DiveLeft", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DiveRight", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("CenterSave", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("ToIdle", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;

        var idleState = sm.AddState("Idle");
        idleState.motion = idle;
        sm.defaultState = idleState;

        var diveLState = sm.AddState("DiveLeft");
        diveLState.motion = dive;

        var diveRState = sm.AddState("DiveRight");
        diveRState.motion = dive;

        var centerState = sm.AddState("CenterSave");
        centerState.motion = center;

        // Transiciones desde Idle
        AddTrigger(idleState, diveLState, "DiveLeft");
        AddTrigger(idleState, diveRState, "DiveRight");
        AddTrigger(idleState, centerState, "CenterSave");

        // Volver a idle
        AddTrigger(diveLState, idleState, "ToIdle");
        AddTrigger(diveRState, idleState, "ToIdle");
        AddTrigger(centerState, idleState, "ToIdle");

        // También volver a idle al terminar el clip (exit time)
        AddExitTime(diveLState, idleState);
        AddExitTime(diveRState, idleState);
        AddExitTime(centerState, idleState);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void BuildGoalkeeperPrefab(AnimatorController controller)
    {
        var riggedModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Goalkeeper_Rigged.glb");
        if (riggedModel == null)
        {
            Debug.LogWarning("Goalkeeper_Rigged.glb no encontrado.");
            return;
        }

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(riggedModel);
        root.name = "RivalGoalkeeper";

        var animator = root.GetComponent<Animator>();
        if (animator == null) animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        if (root.GetComponent<GoalkeeperAI>() == null)
            root.AddComponent<GoalkeeperAI>();

        // Desconectar la instancia del prefab del modelo para poder guardarlo como prefab propio.
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        SavePrefabBoth(root, "RivalGoalkeeper.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Iluminación clutch ────────────────────────────────────────────
    private static void BuildClutchLightPrefab()
    {
        GameObject root = new GameObject("ClutchLight");

        // Spotlight cenital sobre el punto de penal
        GameObject spotGO = new GameObject("Spotlight_PenaltySpot");
        spotGO.transform.SetParent(root.transform, false);
        spotGO.transform.localPosition = new Vector3(0, 9f, -2f);
        spotGO.transform.localRotation = Quaternion.Euler(75f, 0, 0);
        var light = spotGO.AddComponent<Light>();
        light.type = LightType.Spot;
        light.spotAngle = 40f;
        light.range = 25f;
        light.intensity = 14f;
        light.color = new Color(1f, 0.98f, 0.9f);
        light.renderMode = LightRenderMode.ForcePixel;
        light.shadows = LightShadows.Soft;

        // Círculo del punto de penal
        Material spotMat = MakeMat("Mat_PenaltySpot", new Color(0.95f, 0.95f, 0.95f), 0f, 0.1f);
        var spot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spot.name = "PenaltySpotMark";
        spot.transform.SetParent(root.transform, false);
        spot.transform.localPosition = new Vector3(0, 0.02f, -2f);
        spot.transform.localScale = new Vector3(0.25f, 0.01f, 0.25f);
        spot.GetComponent<MeshRenderer>().sharedMaterial = spotMat;
        Object.DestroyImmediate(spot.GetComponent<Collider>());

        SavePrefabBoth(root, "ClutchLight.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Gradas con público animado ────────────────────────────────────
    private static void BuildCrowdSectionPrefab()
    {
        GameObject root = new GameObject("CrowdSection");

        Material standMat = MakeMat("Mat_ArenaStand", new Color(0.2f, 0.22f, 0.28f), 0.1f, 0.1f);

        // Base de gradas (bloque inclinado simple)
        var stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "StandBase";
        stand.transform.SetParent(root.transform, false);
        stand.transform.localScale = new Vector3(24f, 4f, 5f);
        stand.transform.localPosition = new Vector3(0, 2f, 5f);
        stand.transform.localRotation = Quaternion.Euler(-18f, 0, 0);
        stand.GetComponent<MeshRenderer>().sharedMaterial = standMat;
        Object.DestroyImmediate(stand.GetComponent<Collider>());

        // Público (cubos de colores) con CrowdCheer
        Color[] palette =
        {
            new Color(0.85f,0.15f,0.15f), new Color(0.15f,0.35f,0.8f),
            new Color(0.95f,0.85f,0.2f), new Color(0.95f,0.95f,0.95f)
        };

        int cols = 20;
        int rows = 3;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var person = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                person.name = $"Fan_{r}_{c}";
                person.transform.SetParent(root.transform, false);
                float x = -11f + c * (22f / (cols - 1));
                float y = 2.2f + r * 0.9f;
                float z = 4.2f + r * 1.0f;
                person.transform.localPosition = new Vector3(x, y, z);
                person.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
                var mr = person.GetComponent<MeshRenderer>();
                mr.sharedMaterial = MakeMat($"Mat_Fan_{(r*cols+c)%palette.Length}", palette[(r * cols + c) % palette.Length], 0f, 0.05f);
                Object.DestroyImmediate(person.GetComponent<Collider>());
                var cheer = person.AddComponent<CrowdCheer>();
            }
        }

        SavePrefabBoth(root, "CrowdSection.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Composite: toda la arena junta ────────────────────────────────
    private static void BuildArenaCompositePrefab()
    {
        var goalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/PenaltyGoal.prefab");
        var gkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/RivalGoalkeeper.prefab");
        var ballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/PenaltyBall.prefab");
        var lightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/ClutchLight.prefab");
        var crowdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/CrowdSection.prefab");

        GameObject root = new GameObject("PenaltyArena");

        // Suelo de la arena
        Material fieldMat = MakeMat("Mat_ArenaField", new Color(0.12f, 0.45f, 0.16f), 0f, 0.05f);
        var field = GameObject.CreatePrimitive(PrimitiveType.Plane);
        field.name = "ArenaField";
        field.transform.SetParent(root.transform, false);
        field.transform.localScale = new Vector3(3.5f, 1f, 3.5f);
        field.GetComponent<MeshRenderer>().sharedMaterial = fieldMat;
        // El plano ya trae MeshCollider; nos aseguramos de que exista y no sea trigger.
        var fieldCol = field.GetComponent<MeshCollider>();
        if (fieldCol == null) fieldCol = field.AddComponent<MeshCollider>();
        fieldCol.convex = false;
        fieldCol.isTrigger = false;

        // Piso sólido adicional (box) bajo la arena para eliminar cualquier gap.
        var solidFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        solidFloor.name = "ArenaSolidFloor";
        solidFloor.transform.SetParent(root.transform, false);
        solidFloor.transform.localScale = new Vector3(35f, 0.5f, 35f);
        solidFloor.transform.localPosition = new Vector3(0, -0.25f, 0);
        solidFloor.GetComponent<MeshRenderer>().sharedMaterial = fieldMat;
        var sfCol = solidFloor.GetComponent<BoxCollider>();
        if (sfCol != null) sfCol.isTrigger = false;

        Instantiate(goalPrefab, new Vector3(0, 0, 8f), Quaternion.identity, root.transform, "Goal");
        Instantiate(gkPrefab, new Vector3(0, 0, 7.4f), Quaternion.Euler(0, 180f, 0), root.transform, "Goalkeeper");
        Instantiate(ballPrefab, new Vector3(0, 0.25f, -2f), Quaternion.identity, root.transform, "Ball");
        Instantiate(lightPrefab, Vector3.zero, Quaternion.identity, root.transform, "ClutchLight");

        // Gradas: detrás del arco y a los lados
        Instantiate(crowdPrefab, new Vector3(0, 0, 12f), Quaternion.identity, root.transform, "Crowd_Back");
        Instantiate(crowdPrefab, new Vector3(-14f, 0, 4f), Quaternion.Euler(0, 90f, 0), root.transform, "Crowd_Left");
        Instantiate(crowdPrefab, new Vector3(14f, 0, 4f), Quaternion.Euler(0, -90f, 0), root.transform, "Crowd_Right");

        SavePrefabBoth(root, "PenaltyArena.prefab");
        Object.DestroyImmediate(root);
    }

    // ── Helpers ───────────────────────────────────────────────────────
    private static GameObject Instantiate(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent, string name)
    {
        if (prefab == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        return go;
    }

    private static void AddTrigger(AnimatorState from, AnimatorState to, string trigger)
    {
        var t = from.AddTransition(to);
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.hasExitTime = false;
        t.duration = 0.1f;
    }

    private static void AddExitTime(AnimatorState from, AnimatorState to)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = 0.9f;
        t.duration = 0.2f;
    }

    private static AnimationClip FindClip(string glbPath, string contains)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(glbPath);
        AnimationClip fallback = null;
        foreach (var a in assets)
        {
            if (a is AnimationClip clip)
            {
                fallback = clip;
                if (clip.name.Contains(contains)) return clip;
            }
        }
        return fallback;
    }

    private static GameObject CreateCylinder(string name, Vector3 localPos, Vector3 scale, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
        return go;
    }

    private static GameObject CreatePlaneQuad(string name, Vector3 localPos, Vector3 scale, Quaternion rot, Material mat, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);
        return go;
    }

    private static Material MakeMat(string name, Color color, float metallic, float smoothness, bool transparent = false)
    {
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = name;
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);

        if (transparent)
        {
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void SavePrefabBoth(GameObject root, string fileName)
    {
        PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabsDir}/{fileName}");
        // copia en Models
        PrefabUtility.SaveAsPrefabAsset(root, $"{ModelsDir}/{fileName}");
    }

    private static void EnsureDir(string dir)
    {
        if (!AssetDatabase.IsValidFolder(dir)) Directory.CreateDirectory(dir);
    }
}
#endif
