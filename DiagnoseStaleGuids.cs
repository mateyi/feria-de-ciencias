using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public static class DiagnoseStaleGuids
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== GUIDs DE SCRIPT HUERFANOS ===");

        string[] files = Directory.GetFiles("Assets", "*.*", SearchOption.AllDirectories);
        var rx = new Regex(@"m_Script:\s*\{fileID:\s*(\d+),\s*guid:\s*([a-f0-9]{32})", RegexOptions.Compiled);

        var seen = new HashSet<string>();

        foreach (string raw in files)
        {
            string path = raw.Replace('\\', '/');
            if (!path.EndsWith(".unity") && !path.EndsWith(".prefab")) continue;

            string text;
            try { text = File.ReadAllText(path); } catch { continue; }

            foreach (Match m in rx.Matches(text))
            {
                string guid = m.Groups[2].Value;
                string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(scriptPath)) continue;   // resuelve bien

                string key = path + "|" + guid;
                if (!seen.Add(key)) continue;
                sb.AppendLine($"  [HUERFANO] {path}  guid={guid}");
            }
        }

        // ── Referencias actuales de los scripts clave ────────────────────
        sb.AppendLine("--- GUIDs actuales ---");
        sb.AppendLine($"  PlayerController  : {AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Player/PlayerController.cs")}");
        sb.AppendLine($"  RunnerGameManager : {AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Managers/RunnerGameManager.cs")}");
        sb.AppendLine($"  FinishArchTrigger : {(AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Managers/FinishArchTrigger.cs") == "" ? "(no existe esa ruta)" : AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Managers/FinishArchTrigger.cs"))}");

        // ── Quien referencia los GUIDs ACTUALES (para saber si son seguros de cambiar) ──
        foreach (var pair in new[]
        {
            new KeyValuePair<string,string>("PlayerController","c47058d1694ebf349a5587ea07937f9a"),
            new KeyValuePair<string,string>("RunnerGameManager","77cc7537c03b292419bb4687b6837a66"),
            new KeyValuePair<string,string>("FinishArchTrigger",""),
        })
        {
            if (string.IsNullOrEmpty(pair.Value)) continue;
            var refs = new List<string>();
            foreach (string raw in files)
            {
                string path = raw.Replace('\\', '/');
                if (path.EndsWith(".meta")) continue;
                if (!path.EndsWith(".unity") && !path.EndsWith(".prefab") && !path.EndsWith(".asset") && !path.EndsWith(".controller")) continue;
                string text;
                try { text = File.ReadAllText(path); } catch { continue; }
                if (text.Contains(pair.Value)) refs.Add(path);
            }
            sb.AppendLine($"  {pair.Key} [{pair.Value}] referenciado por: {(refs.Count == 0 ? "NADIE" : string.Join(", ", refs))}");
        }

        Debug.Log(sb.ToString());
    }
}
