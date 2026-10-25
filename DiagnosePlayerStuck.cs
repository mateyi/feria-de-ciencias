using System.Text;
using UnityEngine;
using UnityEditor;

public static class DiagnosePlayerStuck
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== DIAGNOSTICO: jugador no avanza ===");

        // 1) RunnerGameManager en escena
        var rgm = Object.FindFirstObjectByType<RunnerGameManager>();
        sb.AppendLine($"RunnerGameManager en escena: {(rgm != null ? rgm.gameObject.name : "NO ENCONTRADO")}");
        if (rgm != null)
        {
            var go = rgm.gameObject;
            var comps = go.GetComponents<Component>();
            var names = new string[comps.Length];
            for (int i = 0; i < comps.Length; i++)
                names[i] = comps[i] == null ? "<MISSING SCRIPT>" : comps[i].GetType().Name;
            sb.AppendLine("  Componentes: " + string.Join(", ", names));
            sb.AppendLine($"  enabled: {rgm.enabled}  |  State: {rgm.State}");

            var so = new SerializedObject(rgm);
            var pp = so.FindProperty("playerPrefab");
            var psp = so.FindProperty("playerSpawnPoint");
            var ppref = pp != null ? pp.objectReferenceValue : null;
            var pspref = psp != null ? psp.objectReferenceValue : null;
            sb.AppendLine($"  playerPrefab      = {(ppref != null ? ppref.name : "NULL")}");
            sb.AppendLine($"  playerSpawnPoint  = {(pspref != null ? pspref.name : "NULL")}");

            if (ppref is GameObject prefabGo)
            {
                sb.AppendLine($"  prefab tiene CharacterController : {prefabGo.GetComponent<CharacterController>() != null}");
                sb.AppendLine($"  prefab tiene PlayerController    : {prefabGo.GetComponent<PlayerController>() != null}");
                sb.AppendLine($"  prefab tiene PlayerStats         : {prefabGo.GetComponent<PlayerStats>() != null}");
                sb.AppendLine($"  prefab tiene Animator            : {prefabGo.GetComponent<Animator>() != null}");
            }
        }

        // 2) Prefab Player.prefab directo en disco
        var playerAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
        sb.AppendLine("--- Player.prefab ---");
        if (playerAsset == null) sb.AppendLine("  NO EXISTE");
        else
        {
            var comps = playerAsset.GetComponents<Component>();
            var names = new string[comps.Length];
            for (int i = 0; i < comps.Length; i++)
                names[i] = comps[i] == null ? "<MISSING SCRIPT>" : comps[i].GetType().Name;
            sb.AppendLine("  Componentes raiz: " + string.Join(", ", names));
            var cc = playerAsset.GetComponent<CharacterController>();
            if (cc != null)
                sb.AppendLine($"  CC height={cc.height} radius={cc.radius} center={cc.center} skin={cc.skinWidth} minMove={cc.minMoveDistance}");
        }

        // 3) Spawn y obstaculos alrededor
        var spawn = GameObject.Find("PlayerSpawnPoint");
        sb.AppendLine("--- Spawn ---");
        Vector3 spawnPos = spawn != null ? spawn.transform.position : new Vector3(0f, 0.5f, 2f);
        sb.AppendLine($"  PlayerSpawnPoint: {(spawn != null ? spawnPos.ToString() : "NO EXISTE (fallback " + spawnPos + ")")}");

        var hits = Physics.OverlapBox(spawnPos + Vector3.up * 0.9f, new Vector3(0.6f, 0.9f, 0.6f),
                                      Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        sb.AppendLine($"  Colliders superpuestos en el spawn: {hits.Length}");
        foreach (var h in hits)
            sb.AppendLine($"    - {h.name} [{h.GetType().Name}] trigger={h.isTrigger} layer={LayerMask.LayerToName(h.gameObject.layer)}");

        // 4) Que hay delante del spawn (primeros 12 m, carril central)
        sb.AppendLine("--- Colisiones delante del spawn (carril central) ---");
        var ahead = Physics.OverlapBox(spawnPos + new Vector3(0f, 0.9f, 6f), new Vector3(0.6f, 0.9f, 6f),
                                       Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        sb.AppendLine($"  Colliders en los 12 m de adelante: {ahead.Length}");
        foreach (var h in ahead)
            sb.AppendLine($"    - {h.name} [{h.GetType().Name}] trigger={h.isTrigger} pos={h.transform.position}");

        // 5) PlayerPreview (posible objeto mal puesto en la pista)
        var preview = GameObject.Find("PlayerPreview");
        sb.AppendLine("--- PlayerPreview ---");
        if (preview == null) sb.AppendLine("  NO EXISTE");
        else
        {
            sb.AppendLine($"  posicion: {preview.transform.position}");
            var col = preview.GetComponentsInChildren<Collider>();
            sb.AppendLine($"  colliders en hijos: {col.Length}");
            foreach (var c in col)
                sb.AppendLine($"    - {c.name} [{c.GetType().Name}] trigger={c.isTrigger}");
            var cc2 = preview.GetComponent<CharacterController>();
            sb.AppendLine($"  tiene CharacterController: {cc2 != null}");
        }

        Debug.Log(sb.ToString());
    }
}
