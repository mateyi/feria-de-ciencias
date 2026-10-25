using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text;

/// <summary>
/// Deja el arquero del penal en "mitad y mitad".
///
/// Los valores default del script NO alcanzan: la escena guarda sus propios
/// valores serializados, así que hay que reescribirlos acá.
///
/// Qué hace:
///   1. goalkeeperAccuracy      = 0.5   (probabilidad de atajada)
///   2. accuracyPorNivel        = 0     (no sube solo con los niveles)
///   3. techoProbabilidadAtajada= 0.5   (nunca ataja más de la mitad)
///   4. Loguea la probabilidad efectiva con el nivel guardado.
/// </summary>
public static class _FixArquero
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ARQUERO: mitad y mitad ===");

        var pm = Object.FindFirstObjectByType<PenaltyMinigameManager>(FindObjectsInactive.Include);
        if (pm == null)
        {
            sb.AppendLine("ERROR: no encontré el PenaltyMinigameManager en la escena.");
            Debug.Log(sb.ToString());
            return;
        }

        var so = new SerializedObject(pm);

        Asignar(so, "goalkeeperAccuracy",       0.5f, sb);
        Asignar(so, "accuracyPorNivel",         0f,   sb);
        Asignar(so, "techoProbabilidadAtajada", 0.5f, sb);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pm);

        // ── Info del nivel guardado (que era lo que rompía la dificultad) ────
        int nivel = 1;
        var gd = Object.FindFirstObjectByType<GameData>(FindObjectsInactive.Include);
        if (gd != null) nivel = Mathf.Max(1, gd.CurrentLevel);
        sb.AppendLine($"nivel guardado: {nivel}");

        // Simulación de la probabilidad con la fórmula del manager.
        float baseP  = 0.5f;
        float porNiv = 0f;
        float techo  = 0.5f;
        float pEfectiva = Mathf.Clamp(Mathf.Clamp01(baseP + porNiv * (nivel - 1)), 0f, Mathf.Clamp01(techo));
        sb.AppendLine($"probabilidad de atajada efectiva: {pEfectiva:P0}");

        // Comprobación práctica: 10.000 sorteos.
        int atajadas = 0;
        for (int i = 0; i < 10000; i++)
            if (Random.value < pEfectiva) atajadas++;
        sb.AppendLine($"simulación de 10.000 penales: {atajadas} atajadas ({atajadas / 100f:0.0}%)");

        // ── Guardar ──────────────────────────────────────────────────────────
        var escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        sb.AppendLine("Escena guardada.");

        Debug.Log(sb.ToString());
    }

    private static void Asignar(SerializedObject so, string nombre, float valor, StringBuilder sb)
    {
        var prop = so.FindProperty(nombre);
        if (prop == null) { sb.AppendLine($"ERROR: el campo '{nombre}' no existe."); return; }
        sb.AppendLine($"{nombre}: {prop.floatValue:0.###} -> {valor:0.###}");
        prop.floatValue = valor;
    }
}
