using UnityEditor;
using UnityEngine;
using System.Linq;

public class CleanupDuplicatePanels
{
    public static void Execute()
    {
        // Remove duplicate PenaltyHUDPanels (keep first)
        RemoveDuplicates<PenaltyHUD>("PenaltyHUDPanel");
        // Remove duplicate LevelCompletePanels (keep first)
        RemoveDuplicates<LevelCompleteUI>("LevelCompletePanel");
        // Remove duplicate PenaltyCameraAnchors (keep first)
        RemoveDuplicatesByName("PenaltyCameraAnchor");
        // Remove duplicate PenaltyMinigameManagers (keep first)
        RemoveDuplicates<PenaltyMinigameManager>("PenaltyMinigameManager");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[Cleanup] Done removing duplicate panels.");
    }

    private static void RemoveDuplicates<T>(string goName) where T : Component
    {
        T[] all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        if (all.Length <= 1) return;
        for (int i = 1; i < all.Length; i++)
        {
            Debug.Log($"[Cleanup] Removing duplicate {goName} #{i}");
            Undo.DestroyObjectImmediate(all[i].gameObject);
        }
    }

    private static void RemoveDuplicatesByName(string goName)
    {
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(g => g.name == goName).ToArray();
        if (all.Length <= 1) return;
        for (int i = 1; i < all.Length; i++)
        {
            Debug.Log($"[Cleanup] Removing duplicate {goName} #{i}");
            Undo.DestroyObjectImmediate(all[i]);
        }
    }
}
