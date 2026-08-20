using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlacePlayerPreview
{
    public static void Execute()
    {
        // Remove old preview
        GameObject old = GameObject.Find("PlayerPreview");
        if (old != null) Object.DestroyImmediate(old);

        // Try placing the animated model directly
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Animations/Futbolista_Run.glb");
        if (modelAsset == null)
        {
            modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Models/Futbolista_Rigged.glb");
        }
        if (modelAsset == null)
        {
            Debug.LogError("[MundialRunner] No se encontró el modelo del futbolista.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        instance.name = "PlayerPreview";
        instance.transform.position = new Vector3(0, 0, 5f);
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * 0.9f;

        // Assign animator controller
        RuntimeAnimatorController ctrl =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/_Project/Animations/FutbolistaController.controller");
        Animator anim = instance.GetComponent<Animator>();
        if (anim == null) anim = instance.AddComponent<Animator>();
        if (ctrl != null) anim.runtimeAnimatorController = ctrl;
        anim.applyRootMotion = false;

        // Frame the player
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot     = new Vector3(0, 1.0f, 5f);
            sv.rotation  = Quaternion.Euler(10f, 180f, 0f); // frente al personaje
            sv.size      = 2.5f;
            sv.in2DMode  = false;
            sv.Repaint();
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MundialRunner] PlayerPreview colocado.");
    }

    public static void Cleanup()
    {
        GameObject go = GameObject.Find("PlayerPreview");
        if (go != null) Object.DestroyImmediate(go);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
