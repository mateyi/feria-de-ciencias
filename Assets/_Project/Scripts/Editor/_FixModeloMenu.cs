#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// SONDA TEMPORAL — engancha el jugador del menu en CharacterManager y aplica
/// la camiseta del jugador equipado en la escena.
/// action = "aplicar".
/// </summary>
public static class _FixModeloMenu
{
    public static void Execute(string action)
    {
        var camera = GameObject.Find("GameData");
        if (camera == null) { Debug.LogError("[FixModeloMenu] no encontre GameData"); return; }

        var cm = camera.GetComponent<CharacterManager>();
        if (cm == null) { Debug.LogError("[FixModeloMenu] GameData no tiene CharacterManager"); return; }

        var preview = GameObject.Find("PlayerPreview");
        if (preview == null) { Debug.LogError("[FixModeloMenu] no encontre PlayerPreview"); return; }

        var so = new SerializedObject(cm);
        so.FindProperty("modeloMenu").objectReferenceValue = preview;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cm);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[FixModeloMenu] CharacterManager.modeloMenu = " + preview.name + " | escena guardada");
    }
}
#endif
