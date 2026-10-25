#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;

/// <summary>Diagnóstico focalizado (2ª pasada), solo lectura.</summary>
public static class _DiagPenalty2
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== DIAG 2 ==========");

        // ── Cámara principal y CameraFollow ──────────────────────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            sb.AppendLine($"Main Camera pos={cam.transform.position} rot={cam.transform.eulerAngles} fov={cam.fieldOfView}");
            foreach (var mb in cam.GetComponents<MonoBehaviour>())
                if (mb != null) sb.AppendLine($"   comp: {mb.GetType().Name} enabled={mb.enabled}");
        }

        // ── PenaltyCameraAnchor ──────────────────────────────────────────────
        var anchor = GameObject.Find("PenaltyCameraAnchor");
        if (anchor != null)
        {
            sb.AppendLine($"PenaltyCameraAnchor pos={anchor.transform.position} rot={anchor.transform.eulerAngles}");
            Vector3 goalPos = new Vector3(0, 1.2f, 339f);
            sb.AppendLine($"   distancia al arco≈{Vector3.Distance(anchor.transform.position, goalPos):F1} m");
        }
        else sb.AppendLine("PenaltyCameraAnchor FALTA");

        // ── Arena ────────────────────────────────────────────────────────────
        var arena = GameObject.Find("PenaltyArena_Instance");
        if (arena != null) sb.AppendLine($"PenaltyArena_Instance pos={arena.transform.position} rot={arena.transform.eulerAngles}");

        // ── AimReticle ───────────────────────────────────────────────────────
        var ret = GameObject.Find("Canvas/PenaltyHUDPanel/AimReticle");
        if (ret == null) sb.AppendLine("AimReticle FALTA");
        else
        {
            var img = ret.GetComponent<Image>();
            var rt = ret.GetComponent<RectTransform>();
            sb.AppendLine($"AimReticle activeSelf={ret.activeSelf} activeInHierarchy={ret.activeInHierarchy}");
            if (rt != null) sb.AppendLine($"   rect size={rt.rect.size} anchoredPos={rt.anchoredPosition} anchorMin={rt.anchorMin} anchorMax={rt.anchorMax}");
            if (img != null)
                sb.AppendLine($"   Image sprite={(img.sprite != null ? img.sprite.name : "<NULL -> NO SE DIBUJA>")} " +
                              $"color={img.color} raycastTarget={img.raycastTarget}");
        }

        // ── PenaltyHUDPanel ──────────────────────────────────────────────────
        var panel = GameObject.Find("Canvas/PenaltyHUDPanel");
        if (panel != null)
        {
            sb.AppendLine($"PenaltyHUDPanel activeSelf={panel.activeSelf} activeInHierarchy={panel.activeInHierarchy}");
            var img = panel.GetComponent<Image>();
            if (img != null) sb.AppendLine($"   panel Image color={img.color} raycastTarget={img.raycastTarget}");
        }

        // ── Arquero: jerarquía y animators ───────────────────────────────────
        var gkGO = GameObject.Find("PenaltyArena_Instance/Goalkeeper");
        if (gkGO == null) sb.AppendLine("Goalkeeper FALTA");
        else
        {
            sb.AppendLine($"Goalkeeper active={gkGO.activeInHierarchy} scale={gkGO.transform.localScale}");
            var anims = gkGO.GetComponentsInChildren<Animator>(true);
            sb.AppendLine($"   Animators en jerarquía: {anims.Length}");
            foreach (var a in anims)
                sb.AppendLine($"     '{a.gameObject.name}' ctrl={(a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "<NULL>")} " +
                              $"avatar={(a.avatar != null ? a.avatar.name : "<NULL>")} isHuman={(a.avatar != null ? a.avatar.isHuman.ToString() : "-")}");
            var skin = gkGO.GetComponentInChildren<SkinnedMeshRenderer>(true);
            sb.AppendLine($"   SkinnedMeshRenderer: {(skin != null ? skin.name : "NO (no es un modelo riggeado)")}");
            sb.AppendLine($"   Hijos directos: {gkGO.transform.childCount}");
            for (int i = 0; i < gkGO.transform.childCount; i++)
                sb.AppendLine($"     - {gkGO.transform.GetChild(i).name}");

            var gkAI = gkGO.GetComponent<GoalkeeperAI>();
            sb.AppendLine($"   GoalkeeperAI: {(gkAI != null ? "SI" : "NO")}");
        }

        // ── Prefab RivalGoalkeeper para comparar ─────────────────────────────
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/RivalGoalkeeper.prefab");
        if (prefab != null)
        {
            var pa = prefab.GetComponentInChildren<Animator>(true);
            sb.AppendLine($"Prefab RivalGoalkeeper -> animator ctrl={(pa != null && pa.runtimeAnimatorController != null ? pa.runtimeAnimatorController.name : "<NULL>")} " +
                          $"avatar={(pa != null && pa.avatar != null ? pa.avatar.name : "<NULL>")} " +
                          $"GoalkeeperAI={(prefab.GetComponent<GoalkeeperAI>() != null)}");
        }

        // ── Pelota: ¿toca el piso/colisiona con algo? ─────────────────────────
        GameObject ball = GameObject.Find("PenaltyArena_Instance/Ball");
        if (ball != null)
        {
            var cols = Physics.OverlapSphere(ball.transform.position, 0.6f);
            sb.Append("Objetos cerca de la pelota: ");
            foreach (var c in cols) if (c.gameObject != ball) sb.Append($"[{c.name}] ");
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }
}
#endif
