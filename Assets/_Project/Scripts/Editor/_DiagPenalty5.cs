#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;

/// <summary>Verificación del estado de la escena después del fix (solo lectura).</summary>
public static class _DiagPenalty5
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== DIAG 5 (post-fix) ==========");

        // ── Paneles del Canvas (debe haber 1 de cada uno) ────────────────────
        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        int nPenalty = 0, nLevel = 0;
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            var ch = canvas.transform.GetChild(i);
            if (ch.name == "PenaltyHUDPanel") nPenalty++;
            if (ch.name == "LevelCompletePanel") nLevel++;
            sb.AppendLine($"   [{i}] '{ch.name}' active={ch.gameObject.activeSelf}");
        }
        sb.AppendLine($"PenaltyHUDPanel={nPenalty} (debe ser 1)  LevelCompletePanel={nLevel} (debe ser 1)");
        sb.AppendLine($"PenaltyHUD componentes={Object.FindObjectsByType<PenaltyHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}");
        sb.AppendLine($"LevelCompleteUI componentes={Object.FindObjectsByType<LevelCompleteUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length}");

        // ── Referencias del manager ──────────────────────────────────────────
        var pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        var so = new SerializedObject(pmm);
        sb.AppendLine("\n--- PenaltyMinigameManager (inspector real) ---");
        foreach (var f in new[] { "penaltySpot", "ball", "goalkeeper", "mainCamera", "penaltyCameraAnchor",
                                  "aimPlane", "aimReticle", "penaltyHUD", "uiCamera" })
        {
            var p = so.FindProperty(f);
            var v = p?.objectReferenceValue;
            sb.AppendLine($"   {f} = {(v != null ? v.name : "<NULL>")}");
        }
        sb.AppendLine($"   targetZones = {so.FindProperty("targetZones")?.arraySize} elementos");
        sb.AppendLine($"   modoDebug={so.FindProperty("modoDebug")?.boolValue} " +
                      $"kickDuration={so.FindProperty("kickDuration")?.floatValue} " +
                      $"maxAttempts={so.FindProperty("maxAttempts")?.intValue} " +
                      $"goalkeeperAccuracy={so.FindProperty("goalkeeperAccuracy")?.floatValue}");

        // ── Mira ─────────────────────────────────────────────────────────────
        var reticle = so.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;
        if (reticle != null)
        {
            var img = reticle.GetComponent<Image>();
            sb.AppendLine($"\n--- Mira ---");
            sb.AppendLine($"   '{reticle.name}' size={reticle.rect.size} activeSelf={reticle.gameObject.activeSelf}");
            sb.AppendLine($"   Image sprite={(img != null && img.sprite != null ? AssetDatabase.GetAssetPath(img.sprite) : "<NULL>")} " +
                          $"raycast={img?.raycastTarget}");
            sb.AppendLine($"   Padre: {reticle.parent?.name} (active={reticle.parent?.gameObject.activeSelf})");
        }

        // ── AimPlane ─────────────────────────────────────────────────────────
        var plane = so.FindProperty("aimPlane")?.objectReferenceValue as BoxCollider;
        if (plane != null)
            sb.AppendLine($"\n--- AimPlane ---\n   '{plane.name}' pos={plane.transform.position} size={plane.size} bounds={plane.bounds.size} trigger={plane.isTrigger}");
        else
            sb.AppendLine("\n--- AimPlane: <NULL> (se creará en Awake) ---");

        // ── Pelota / arquero ─────────────────────────────────────────────────
        var ball = GameObject.Find("PenaltyArena_Instance/Ball");
        if (ball != null)
        {
            var rb = ball.GetComponent<Rigidbody>();
            sb.AppendLine($"\nPelota pos={ball.transform.position} kinematic={rb?.isKinematic} gravity={rb?.useGravity}");
        }
        var gk = Object.FindFirstObjectByType<GoalkeeperAI>(FindObjectsInactive.Include);
        if (gk != null)
        {
            var an = gk.GetComponent<Animator>();
            var gso = new SerializedObject(gk);
            sb.AppendLine($"Arquero ctrl={(an != null && an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "<NULL>")} " +
                          $"avatar={(an != null && an.avatar != null ? an.avatar.name : "<NULL>")} " +
                          $"forzarCodigo={gso.FindProperty("forzarEstiradaPorCodigo")?.boolValue} " +
                          $"durEstirada={gso.FindProperty("duracionEstirada")?.floatValue}");
        }

        sb.AppendLine("========== FIN DIAG 5 ==========");
        Debug.Log(sb.ToString());
    }
}
#endif
