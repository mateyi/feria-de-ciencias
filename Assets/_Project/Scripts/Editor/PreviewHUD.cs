using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PreviewHUD
{
    public static void ShowGameplay()
    {
        // Activar GameplayPanel, desactivar MainMenu
        SetPanel("Canvas/MainMenuPanel",  false);
        SetPanel("Canvas/GameplayPanel",  true);
        SetPanel("Canvas/GameOverPanel",  false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    public static void ShowMainMenu()
    {
        SetPanel("Canvas/MainMenuPanel",  true);
        SetPanel("Canvas/GameplayPanel",  false);
        SetPanel("Canvas/GameOverPanel",  false);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void SetPanel(string path, bool active)
    {
        GameObject go = GameObject.Find(path);
        if (go == null)
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas == null) return;
            Transform t = canvas.transform.Find(path.Replace("Canvas/", ""));
            if (t != null) go = t.gameObject;
        }
        if (go != null) { go.SetActive(active); EditorUtility.SetDirty(go); }
    }
}
