#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// REPARACIÓN del minijuego de penales (una sola pasada, idempotente).
///   1. Elimina los paneles DUPLICADOS del Canvas, conservando los que están
///      realmente cableados (el del AimReticle del manager y el que referencia
///      UIManager), y re-apunta cualquier referencia que quedara huérfana.
///   2. Ejecuta la autoconfiguración del PenaltyMinigameManager, que crea el
///      AimPlane, genera el sprite de la mira y asigna todas las referencias.
///   3. Guarda la escena.
/// </summary>
public static class _RepairPenalty
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== REPARACION PENALES ==========");

        var pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm == null) { Debug.LogError("[Repair] No hay PenaltyMinigameManager"); return; }

        var so = new SerializedObject(pmm);

        // ── 1. Panel de PENAL que hay que conservar: el que contiene el AimReticle ──
        var reticle = so.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;
        if (reticle == null) { Debug.LogError("[Repair] aimReticle es null"); return; }

        Transform reticleRoot = reticle;
        while (reticleRoot.parent != null && reticleRoot.parent.GetComponent<Canvas>() == null)
            reticleRoot = reticleRoot.parent;

        sb.AppendLine($"Panel de penal a CONSERVAR: '{reticleRoot.name}' id={reticleRoot.GetInstanceID()}");

        var canvas = reticleRoot.parent;
        if (canvas == null) { Debug.LogError("[Repair] No encontré el Canvas"); return; }

        // ── 2. Panel de NIVEL COMPLETADO que hay que conservar: el que usa UIManager ──
        var ui = Object.FindFirstObjectByType<UIManager>();
        GameObject lcGanador = null;
        if (ui != null)
            lcGanador = new SerializedObject(ui).FindProperty("levelCompletePanel")?.objectReferenceValue as GameObject;

        if (lcGanador != null)
            sb.AppendLine($"Panel de nivel completado a CONSERVAR: '{lcGanador.name}' id={lcGanador.GetInstanceID()}");
        else
            sb.AppendLine("UIManager.levelCompletePanel = <NULL> -> elijo el primero que encuentre");

        // ── 3. Recolectar duplicados y fusionarlos ────────────────────────────
        var aBorrar = new List<GameObject>();

        for (int i = 0; i < canvas.childCount; i++)
        {
            var hijo = canvas.GetChild(i);
            GameObject go = hijo.gameObject;

            if (hijo == reticleRoot) continue;                       // es el bueno
            if (hijo.name == "PenaltyHUDPanel") aBorrar.Add(go);
        }

        // Para LevelCompletePanel elegimos el ganador si todavía no lo tenemos.
        if (lcGanador == null)
        {
            for (int i = 0; i < canvas.childCount; i++)
            {
                var hijo = canvas.GetChild(i);
                if (hijo.name == "LevelCompletePanel") { lcGanador = hijo.gameObject; break; }
            }
        }

        for (int i = 0; i < canvas.childCount; i++)
        {
            var hijo = canvas.GetChild(i);
            if (hijo.name == "LevelCompletePanel" && hijo.gameObject != lcGanador)
                aBorrar.Add(hijo.gameObject);
        }

        sb.AppendLine($"Duplicados a borrar: {aBorrar.Count}");
        foreach (var d in aBorrar) sb.AppendLine($"   - '{d.name}' id={d.GetInstanceID()}");

        // ── 4. Fusionar: re-apuntar referencias y destruir ────────────────────
        foreach (var doomed in aBorrar)
        {
            GameObject survivor = doomed.name == "PenaltyHUDPanel" ? reticleRoot.gameObject : lcGanador;

            int reapegadas = ReapuntarReferencias(doomed, survivor, sb);
            sb.AppendLine($"   '{doomed.name}': {reapegadas} referencia(s) re-apuntada(s) -> " +
                          $"{(survivor != null ? survivor.name : "<null>")}");

            Object.DestroyImmediate(doomed);
        }

        // ── 5. Autoconfigurar el manager (crea AimPlane, sprite de mira y asigna refs) ──
        pmm.AutoconfigurarDesdeMenu();

        // ── 6. Dejar el AimPlane prolijo dentro de la arena ───────────────────
        var aimPlaneGo = GameObject.Find("PenaltyAimPlane");
        var arena = Object.FindFirstObjectByType<PenaltyMinigameManager>() != null
            ? GameObject.Find("PenaltyArena_Instance") : null;

        if (aimPlaneGo != null && arena != null && aimPlaneGo.transform.parent == null)
        {
            aimPlaneGo.transform.SetParent(arena.transform, true);
            sb.AppendLine("AimPlane emparentado a PenaltyArena_Instance");
        }

        // ── 7. Guardar la escena ──────────────────────────────────────────────
        var escena = pmm.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        sb.AppendLine($"Escena guardada: {escena.name}");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Busca en TODOS los componentes de la escena referencias al objeto 'doomed'
    /// y las re-apunta al 'survivor'. Devuelve cuántas re-apuntó.
    /// </summary>
    private static int ReapuntarReferencias(GameObject doomed, GameObject survivor, StringBuilder sb)
    {
        if (survivor == null) return 0;

        int total = 0;
        var todos = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var mb in todos)
        {
            if (mb == null) continue;

            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            bool modificado = false;

            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue != doomed) continue;

                it.objectReferenceValue = survivor;
                modificado = true;
                total++;
                sb.AppendLine($"      {mb.GetType().Name}.{it.name} re-apuntado");
            }

            if (modificado) so.ApplyModifiedPropertiesWithoutUndo();
        }

        return total;
    }
}
#endif
