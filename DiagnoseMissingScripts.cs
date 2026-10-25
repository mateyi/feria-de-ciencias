using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static class DiagnoseMissingScripts
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ESCANEO DE SCRIPTS FALTANTES ===");

        // ── 1. Escena activa ─────────────────────────────────────────────
        var scene = SceneManager.GetActiveScene();
        sb.AppendLine($"--- Escena: {scene.name} ---");
        int sceneBroken = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var comps = t.GetComponents<Component>();
                var missing = new List<string>();
                for (int i = 0; i < comps.Length; i++)
                    if (comps[i] == null) missing.Add($"idx{i}");

                if (missing.Count > 0)
                {
                    sceneBroken += missing.Count;
                    sb.AppendLine($"  [ROTO] {GetPath(t)}  -> {missing.Count} script(s) faltante(s) ({string.Join(",", missing)})");
                }
            }
        }
        sb.AppendLine($"  TOTAL scripts faltantes en escena: {sceneBroken}");

        // ── 2. Prefabs ───────────────────────────────────────────────────
        sb.AppendLine("--- Prefabs ---");
        int prefabBroken = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;

            foreach (var t in go.GetComponentsInChildren<Transform>(true))
            {
                var comps = t.GetComponents<Component>();
                var missing = new List<string>();
                for (int i = 0; i < comps.Length; i++)
                    if (comps[i] == null) missing.Add($"idx{i}");

                if (missing.Count > 0)
                {
                    prefabBroken += missing.Count;
                    sb.AppendLine($"  [ROTO] {path} :: {GetPath(t)}  -> {missing.Count} faltante(s)");
                }
            }
        }
        sb.AppendLine($"  TOTAL scripts faltantes en prefabs: {prefabBroken}");

        // ── 3. Que GUID esperan los rotos vs GUID real de los scripts ────
        sb.AppendLine("--- GUIDs de los scripts clave ---");
        sb.AppendLine($"  PlayerController.cs   : {GetGuid("Assets/_Project/Scripts/Player/PlayerController.cs")}");
        sb.AppendLine($"  RunnerGameManager.cs  : {GetGuid("Assets/_Project/Scripts/Managers/RunnerGameManager.cs")}");
        sb.AppendLine($"  PlayerStats.cs        : {GetGuid("Assets/_Project/Scripts/PlayerStats.cs")}");
        sb.AppendLine($"  GameData.cs           : {GetGuid("Assets/GameData.cs")}");
        sb.AppendLine($"  UIManager.cs          : {GetGuid("Assets/UIManager.cs")}");

        Debug.Log(sb.ToString());
    }

    private static string GetGuid(string path)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        return string.IsNullOrEmpty(guid) ? "(no existe en " + path + ")" : guid;
    }

    private static string GetPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}
