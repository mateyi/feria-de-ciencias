using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text;

/// <summary>
/// FASE A — Apaga el sistema de carteles de decisión en la escena.
///
/// Qué hace:
///   1. Deja el TrackGenerator sin prefab de decisión y con el flag apagado.
///   2. Se asegura de que la lista de segmentos normales no quede vacía.
///   3. Apaga el Canvas/DecisionPanel para que no pueda aparecer nunca.
///   4. Informa si quedó algún DecisionPoint suelto en la escena.
///
/// Es idempotente: se puede correr varias veces sin efectos raros.
/// </summary>
public static class _FaseA
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== FASE A: apagar carteles de decisión ===");

        // ── 1. TrackGenerator ────────────────────────────────────────────────
        var gen = Object.FindFirstObjectByType<TrackGenerator>(FindObjectsInactive.Include);
        if (gen == null)
        {
            sb.AppendLine("ERROR: no encontré el TrackGenerator en la escena.");
        }
        else
        {
            var so = new SerializedObject(gen);

            var propDecision = so.FindProperty("decisionSegmentPrefab");
            if (propDecision != null && propDecision.objectReferenceValue != null)
            {
                sb.AppendLine($"decisionSegmentPrefab era '{propDecision.objectReferenceValue.name}' -> se limpia.");
                propDecision.objectReferenceValue = null;
            }

            var propFlag = so.FindProperty("usarSegmentosDeDecision");
            if (propFlag != null)
            {
                propFlag.boolValue = false;
                sb.AppendLine("usarSegmentosDeDecision = false");
            }

            // La lista de segmentos normales tiene que tener al menos uno, si no
            // el generador no puede crear pista.
            var propNormales = so.FindProperty("normalSegmentPrefabs");
            if (propNormales != null)
            {
                if (propNormales.arraySize == 0)
                {
                    var normal = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Normal.prefab");
                    if (normal != null)
                    {
                        propNormales.arraySize = 1;
                        propNormales.GetArrayElementAtIndex(0).objectReferenceValue = normal;
                        sb.AppendLine("normalSegmentPrefabs estaba vacía -> se carga Segment_Normal.");
                    }
                    else
                    {
                        sb.AppendLine("ERROR: no encontré Segment_Normal.prefab.");
                    }
                }
                else
                {
                    sb.AppendLine($"normalSegmentPrefabs: {propNormales.arraySize} entrada(s), ok.");
                }
            }

            var propIntervalo = so.FindProperty("decisionInterval");
            if (propIntervalo != null) sb.AppendLine($"decisionInterval queda en {propIntervalo.intValue} (sin uso).");

            so.ApplyModifiedPropertiesWithoutUndo();

            // El prefab Segment_Normal no debe tener ningún DecisionPoint adentro.
            int puntosEnNormales = propNormales != null ? ContarDecisionPoints(propNormales) : 0;
            sb.AppendLine($"DecisionPoint dentro de los segmentos normales: {puntosEnNormales}");
        }

        // ── 2. Panel de decisión apagado ─────────────────────────────────────
        GameObject panel = Buscar("Canvas/DecisionPanel");
        if (panel == null)
        {
            // Plan B: buscar el componente DecisionUI, que vive en el panel.
            var dui = Object.FindFirstObjectByType<DecisionUI>(FindObjectsInactive.Include);
            if (dui != null) panel = dui.gameObject;
        }

        if (panel == null)
        {
            sb.AppendLine("No existe Canvas/DecisionPanel en la escena (nada que apagar).");
        }
        else
        {
            panel.SetActive(false);
            sb.AppendLine($"{Ruta(panel.transform)} -> activeSelf={panel.activeSelf}");
        }

        // ── 3. ¿Quedó algún DecisionPoint en la escena? ──────────────────────
        var puntos = Object.FindObjectsByType<DecisionPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"DecisionPoint en la escena: {puntos.Length}");
        foreach (var p in puntos) sb.AppendLine($"  - {Ruta(p.transform)}");

        // ── 4. Manager apagado ───────────────────────────────────────────────
        var dm = Object.FindFirstObjectByType<DecisionManager>(FindObjectsInactive.Include);
        if (dm != null)
        {
            var soDm = new SerializedObject(dm);
            var h = soDm.FindProperty("habilitado");
            if (h != null) { h.boolValue = false; soDm.ApplyModifiedPropertiesWithoutUndo(); }
            sb.AppendLine("DecisionManager.habilitado = false");
        }
        else
        {
            sb.AppendLine("No hay DecisionManager en la escena.");
        }

        // ── 5. Guardar ───────────────────────────────────────────────────────
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine("Escena guardada.");

        Debug.Log(sb.ToString());
    }

    // Cuenta cuántos DecisionPoint cuelgan de los prefabs de la lista de segmentos.
    private static int ContarDecisionPoints(SerializedProperty lista)
    {
        int total = 0;
        for (int i = 0; i < lista.arraySize; i++)
        {
            var seg = lista.GetArrayElementAtIndex(i).objectReferenceValue as TrackSegment;
            if (seg == null) continue;
            string ruta = AssetDatabase.GetAssetPath(seg);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (prefab == null) continue;
            total += prefab.GetComponentsInChildren<DecisionPoint>(true).Length;
        }
        return total;
    }

    private static GameObject Buscar(string ruta)
    {
        // "ruta" es la ruta completa desde la raíz de la escena,
        // por ejemplo "Canvas/DecisionPanel".
        foreach (var raiz in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (Ruta(t) == ruta) return t.gameObject;
            }
        }
        return null;
    }

    private static string Ruta(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
