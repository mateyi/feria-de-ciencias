using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Wires the "jugar" button's onClick to RunnerGameManager.StartGame()
/// if it has no persistent listeners yet.
/// Menu: Mundial Runner → Wire Jugar Button
/// </summary>
public class WireJugarButton
{
    [MenuItem("Mundial Runner/Wire Jugar Button")]
    public static void Execute()
    {
        RunnerGameManager rgm = Object.FindFirstObjectByType<RunnerGameManager>();
        if (rgm == null) { Debug.LogError("[WireJugar] RunnerGameManager not found."); return; }

        GameObject jugarGO = GameObject.Find("Canvas/MainMenuPanel/jugar");
        if (jugarGO == null) { Debug.LogError("[WireJugar] 'jugar' button not found."); return; }

        Button btn = jugarGO.GetComponent<Button>();
        if (btn == null) { Debug.LogError("[WireJugar] No Button component on 'jugar'."); return; }

        // Only add if not already wired
        if (btn.onClick.GetPersistentEventCount() == 0)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, rgm.StartGame);
            EditorUtility.SetDirty(btn);
            Debug.Log("[WireJugar] ✅ 'jugar' button wired to RunnerGameManager.StartGame().");
        }
        else
        {
            Debug.Log("[WireJugar] 'jugar' button already has listeners, skipping.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
