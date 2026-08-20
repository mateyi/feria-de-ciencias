using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye el prefab del jugador usando el modelo 3D generado con animación.
/// </summary>
public class RebuildPlayerPrefab
{
    public static void Execute()
    {
        // Cargar el modelo animado
        string modelPath = "Assets/_Project/Animations/Futbolista_Run.glb";
        string controllerPath = "Assets/_Project/Animations/FutbolistaController.controller";
        string prefabPath = "Assets/_Project/Prefabs/Player.prefab";

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            Debug.LogError($"[MundialRunner] Modelo no encontrado: {modelPath}");
            return;
        }

        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

        // Eliminar prefab viejo
        if (AssetDatabase.LoadAssetAtPath<Object>(prefabPath) != null)
            AssetDatabase.DeleteAsset(prefabPath);
        AssetDatabase.Refresh();

        // Crear nuevo root del jugador
        GameObject playerRoot = new GameObject("Player");
        playerRoot.tag = "Player";

        // Instanciar el modelo como hijo
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        modelInstance.name = "FutbolistaModel";
        modelInstance.transform.SetParent(playerRoot.transform);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale    = Vector3.one * 0.9f;

        // Asignar AnimatorController
        Animator animator = modelInstance.GetComponent<Animator>();
        if (animator == null)
            animator = modelInstance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false; // importante: el PlayerController maneja el movimiento
        EditorUtility.SetDirty(animator);

        // CharacterController en el root
        CharacterController cc = playerRoot.AddComponent<CharacterController>();
        cc.height = 1.75f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.875f, 0);

        // PlayerController script
        playerRoot.AddComponent<PlayerController>();

        // PlayerStats script
        playerRoot.AddComponent<PlayerStats>();

        // Guardar como prefab
        PrefabUtility.SaveAsPrefabAsset(playerRoot, prefabPath);
        Object.DestroyImmediate(playerRoot);

        // Actualizar RunnerGameManager con el nuevo prefab
        RunnerGameManager rgm = Object.FindFirstObjectByType<RunnerGameManager>();
        if (rgm != null)
        {
            GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SerializedObject so = new SerializedObject(rgm);
            so.FindProperty("playerPrefab").objectReferenceValue = newPrefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(rgm);
            Debug.Log("[MundialRunner] RunnerGameManager actualizado con nuevo prefab.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();

        Debug.Log("[MundialRunner] ✅ Prefab Player reconstruido con modelo 3D animado.");
    }
}
