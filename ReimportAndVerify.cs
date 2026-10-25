using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class ReimportAndVerify
{
    public static void Execute()
    {
        // 1) Forzar reimportado de los .meta modificados
        AssetDatabase.ImportAsset("Assets/_Project/Scripts/Player/PlayerController.cs", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/_Project/Scripts/Managers/RunnerGameManager.cs", ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/_Project/Scripts/Managers/FinishArchTrigger.cs", ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        var sb = new StringBuilder();
        sb.AppendLine("=== VERIFICACION POST-REPARACION ===");

        sb.AppendLine($"PlayerController guid  : {AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Player/PlayerController.cs")}");
        sb.AppendLine($"RunnerGameManager guid : {AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Managers/RunnerGameManager.cs")}");
        sb.AppendLine($"FinishArchTrigger guid : {AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Managers/FinishArchTrigger.cs")}");

        // 2) Escena: scripts faltantes
        var scene = SceneManager.GetActiveScene();
        int broken = 0;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var comps = t.GetComponents<Component>();
                for (int i = 0; i < comps.Length; i++)
                    if (comps[i] == null)
                    {
                        broken++;
                        sb.AppendLine($"  [AUN ROTO] {t.name} idx{i}");
                    }
            }
        sb.AppendLine($"Scripts faltantes en escena: {broken}");

        // 3) Prefab del jugador
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
        sb.AppendLine($"Player.prefab tiene PlayerController: {(player != null ? (player.GetComponent<PlayerController>() != null).ToString() : "prefab null")}");

        // 4) RunnerGameManager: referencia al prefab y spawn intactas?
        var rgm = Object.FindFirstObjectByType<RunnerGameManager>();
        sb.AppendLine($"RunnerGameManager en escena: {(rgm != null).ToString()}");
        if (rgm != null)
        {
            var so = new SerializedObject(rgm);
            var pp = so.FindProperty("playerPrefab");
            var sp = so.FindProperty("playerSpawnPoint");
            sb.AppendLine($"  playerPrefab     = {(pp != null && pp.objectReferenceValue != null ? pp.objectReferenceValue.name : "NULL")}");
            sb.AppendLine($"  playerSpawnPoint = {(sp != null && sp.objectReferenceValue != null ? sp.objectReferenceValue.name : "NULL")}");
            sb.AppendLine($"  State = {rgm.State}  enabled = {rgm.enabled}");
        }

        // 5) FinishArchTrigger en escena
        var fat = Object.FindFirstObjectByType<FinishArchTrigger>();
        sb.AppendLine($"FinishArchTrigger en escena: {(fat != null ? fat.gameObject.name : "NO")}");

        Debug.Log(sb.ToString());
    }
}
