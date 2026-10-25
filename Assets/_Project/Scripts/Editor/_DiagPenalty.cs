#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// DIAGNÓSTICO DE SOLO LECTURA del minijuego de penales. No modifica nada.
/// </summary>
public static class _DiagPenalty
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("================ DIAGNOSTICO PENALES ================");
        sb.AppendLine($"timeScale={Time.timeScale} cursorVisible={Cursor.visible} lockState={Cursor.lockState}");

        // ── 2. INPUT HANDLING ────────────────────────────────────────────────
        sb.AppendLine("\n--- 2. INPUT HANDLING ---");
        var psType = System.Type.GetType("UnityEditor.PlayerSettings, UnityEditor");
        // Leemos el valor crudo desde ProjectSettings.asset
        string projSettings = System.IO.File.ReadAllText(
            System.IO.Path.Combine(Application.dataPath, "../ProjectSettings/ProjectSettings.asset"));
        foreach (var line in projSettings.Split('\n'))
            if (line.Trim().StartsWith("activeInputHandler"))
                sb.AppendLine($"  {line.Trim()}   (0=Old, 1=New, 2=Both)");

        // ── 3. PENALTYMINIGAMEMANAGER ────────────────────────────────────────
        sb.AppendLine("\n--- 3. PenaltyMinigameManager ---");
        var pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm == null) sb.AppendLine("  FALTA: no hay PenaltyMinigameManager en la escena");
        else
        {
            sb.AppendLine($"  GameObject='{pmm.gameObject.name}' activeInHierarchy={pmm.gameObject.activeInHierarchy} enabled={pmm.enabled}");
            var so = new SerializedObject(pmm);
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name == "m_Script") continue;
                string val;
                if (it.isArray)
                {
                    val = $"array[{it.arraySize}]";
                    for (int i = 0; i < it.arraySize; i++)
                    {
                        var el = it.GetArrayElementAtIndex(i).objectReferenceValue;
                        val += $" [{i}]={(el != null ? el.name : "<NULL>")}";
                    }
                }
                else if (it.propertyType == SerializedPropertyType.ObjectReference)
                    val = it.objectReferenceValue != null ? it.objectReferenceValue.name : "<NULL>";
                else if (it.propertyType == SerializedPropertyType.Float) val = it.floatValue.ToString("F2");
                else if (it.propertyType == SerializedPropertyType.Integer) val = it.intValue.ToString();
                else if (it.propertyType == SerializedPropertyType.Boolean) val = it.boolValue.ToString();
                else val = it.ToString();
                sb.AppendLine($"    {it.name} = {val}");
            }
        }

        // ── 4. Estado de bloqueo ─────────────────────────────────────────────
        sb.AppendLine("\n--- 4. ESTADO / BLOQUEOS ---");
        var gm = Object.FindFirstObjectByType<RunnerGameManager>();
        sb.AppendLine($"  RunnerGameManager: {(gm != null ? "OK" : "FALTA")}");
        var pc = Object.FindFirstObjectByType<PlayerController>();
        sb.AppendLine($"  PlayerController en escena: {(pc != null ? pc.gameObject.name : "ninguno (se instancia al Jugar)")}");
        var oob = Object.FindFirstObjectByType<OutOfBoundsGuard>();
        if (oob != null)
        {
            var oso = new SerializedObject(oob);
            sb.AppendLine($"  OutOfBoundsGuard killY={oso.FindProperty("killY").floatValue} " +
                          $"respawnY={oso.FindProperty("respawnY").floatValue} " +
                          $"killInsteadOfRecover={oso.FindProperty("killInsteadOfRecover").boolValue}");
        }

        // ── 5. CÁMARAS ───────────────────────────────────────────────────────
        sb.AppendLine("\n--- 5. CAMARAS ---");
        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"  Total camaras: {cams.Length}");
        foreach (var c in cams)
            sb.AppendLine($"    '{c.name}' active={c.gameObject.activeInHierarchy} enabled={c.enabled} " +
                          $"depth={c.depth} pos={c.transform.position} rot={c.transform.eulerAngles} " +
                          $"clearFlags={c.clearFlags}");
        sb.AppendLine($"  Camera.main = {(Camera.main != null ? Camera.main.name : "<null>")}");

        // Cinemachine
        var brains = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int cmCount = 0, cmBrain = 0;
        foreach (var mb in brains)
        {
            if (mb == null) continue;
            string t = mb.GetType().Name;
            if (t.Contains("Cinemachine"))
            {
                cmCount++;
                if (t.Contains("Brain")) { cmBrain++; sb.AppendLine($"    CINEMACHINE: '{mb.name}' :: {t}"); }
            }
        }
        sb.AppendLine($"  Componentes Cinemachine totales={cmCount} (Brains={cmBrain})");

        // ── 6. UI ────────────────────────────────────────────────────────────
        sb.AppendLine("\n--- 6. UI ---");
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"  Canvas: {canvases.Length}");
        foreach (var c in canvases)
        {
            var gr = c.GetComponent<GraphicRaycaster>();
            var cs = c.GetComponent<CanvasScaler>();
            sb.AppendLine($"    '{c.name}' active={c.gameObject.activeInHierarchy} mode={c.renderMode} " +
                          $"sortingOrder={c.sortingOrder} raycaster={(gr != null ? gr.enabled.ToString() : "NO")} " +
                          $"scaler={(cs != null ? $"{cs.uiScaleMode} ref={cs.referenceResolution} match={cs.matchWidthOrHeight}" : "NO")}");
        }
        var es = Object.FindFirstObjectByType<EventSystem>();
        sb.AppendLine($"  EventSystem: {(es != null ? $"{es.name} active={es.gameObject.activeInHierarchy}" : "FALTA")}");

        // Imágenes que tapan la pantalla y tienen raycastTarget
        sb.AppendLine("  Graficos con raycastTarget=true y tamaño >= 80% pantalla:");
        foreach (var g in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (g == null || !g.raycastTarget) continue;
            var rt = g.rectTransform;
            if (rt == null) continue;
            Vector2 size = rt.rect.size;
            Canvas rootCanvas = g.canvas;
            if (rootCanvas == null) continue;
            Vector2 refRes = new Vector2(1920, 1080);
            var sc = rootCanvas.GetComponent<CanvasScaler>();
            if (sc != null && sc.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize) refRes = sc.referenceResolution;
            if (size.x >= refRes.x * 0.8f && size.y >= refRes.y * 0.8f)
                sb.AppendLine($"    '{GetPath(g.transform)}' :: {g.GetType().Name} size={size} active={g.gameObject.activeInHierarchy}");
        }

        // PenaltyHUD
        sb.AppendLine("  --- PenaltyHUD ---");
        var hud = Object.FindFirstObjectByType<PenaltyHUD>(FindObjectsInactive.Include);
        if (hud == null) sb.AppendLine("    FALTA");
        else
        {
            var hso = new SerializedObject(hud);
            var hit = hso.GetIterator();
            while (hit.NextVisible(true))
            {
                if (hit.name == "m_Script") continue;
                string v = hit.propertyType == SerializedPropertyType.ObjectReference
                    ? (hit.objectReferenceValue != null ? hit.objectReferenceValue.name : "<NULL>")
                    : hit.ToString();
                sb.AppendLine($"      {hit.name} = {v}");
            }
            sb.AppendLine($"      componente activo={hud.gameObject.activeInHierarchy}, panelRoot==self: " +
                          $"{hso.FindProperty("panelRoot")?.objectReferenceValue == hud.gameObject}");
        }

        // ── 7. ZONAS ─────────────────────────────────────────────────────────
        sb.AppendLine("\n--- 7. ZONAS DEL ARCO ---");
        var zones = Object.FindObjectsByType<PenaltyZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"  PenaltyZone encontradas: {zones.Length}");
        foreach (var z in zones)
        {
            var col = z.GetComponent<Collider>();
            sb.AppendLine($"    '{z.name}' id={z.IdIndex} pos={z.transform.position} " +
                          $"collider={(col != null ? $"{col.GetType().Name} trigger={col.isTrigger}" : "FALTA")} " +
                          $"target={z.TargetPosition}");
        }

        // ── 8. PELOTA ────────────────────────────────────────────────────────
        sb.AppendLine("\n--- 8. PELOTA ---");
        GameObject ballGO = GameObject.Find("PenaltyArena_Instance/Ball") ?? GameObject.Find("Ball");
        if (ballGO == null) sb.AppendLine("  FALTA");
        else
        {
            var rb = ballGO.GetComponent<Rigidbody>();
            var bc = ballGO.GetComponent<Collider>();
            sb.AppendLine($"  '{ballGO.name}' pos={ballGO.transform.position} parent={(ballGO.transform.parent != null ? ballGO.transform.parent.name : "root")}");
            sb.AppendLine($"  Rigidbody: {(rb != null ? $"mass={rb.mass} kinematic={rb.isKinematic} gravity={rb.useGravity} constraints={rb.constraints} drag={rb.linearDamping}" : "FALTA")}");
            sb.AppendLine($"  Collider: {(bc != null ? $"{bc.GetType().Name} trigger={bc.isTrigger} enabled={bc.enabled}" : "FALTA")}");
        }

        // ── 9. ARQUERO ───────────────────────────────────────────────────────
        sb.AppendLine("\n--- 9. ARQUERO ---");
        var gk = Object.FindFirstObjectByType<GoalkeeperAI>(FindObjectsInactive.Include);
        if (gk == null) sb.AppendLine("  FALTA GoalkeeperAI");
        else
        {
            var an = gk.GetComponent<Animator>();
            sb.AppendLine($"  '{gk.name}' pos={gk.transform.position} rot={gk.transform.eulerAngles}");
            sb.AppendLine($"  Animator={(an != null ? $"controller={(an.runtimeAnimatorController != null ? an.runtimeAnimatorController.name : "<NULL>")} enabled={an.enabled} avatar={(an.avatar != null ? an.avatar.name : "<NULL>")}" : "FALTA")}");
            var gso = new SerializedObject(gk);
            foreach (var f in new[] { "diveLeftTrigger", "diveRightTrigger", "centerSaveTrigger", "resetTrigger" })
                sb.AppendLine($"    {f} = '{gso.FindProperty(f)?.stringValue}'");
            if (an != null && an.runtimeAnimatorController != null)
                foreach (var p in an.parameters)
                    sb.AppendLine($"    Param animator: {p.name} ({p.type})");
        }

        // ── 10. EVENTOS / SINGLETONS ─────────────────────────────────────────
        sb.AppendLine("\n--- 10. UI DE FIN DE NIVEL / EVENTOS ---");
        sb.AppendLine($"  LevelCompleteUI: {(Object.FindFirstObjectByType<LevelCompleteUI>(FindObjectsInactive.Include) != null ? "OK" : "FALTA")}");
        sb.AppendLine($"  GameOverUI: {(Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include) != null ? "OK" : "FALTA")}");
        var ghud = Object.FindFirstObjectByType<GameplayHUD>(FindObjectsInactive.Include);
        sb.AppendLine($"  GameplayHUD: {(ghud != null ? ghud.gameObject.name : "FALTA")}");

        sb.AppendLine("\n================ FIN ================");
        Debug.Log(sb.ToString());
    }

    private static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
