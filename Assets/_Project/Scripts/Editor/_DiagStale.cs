#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Reflection;

/// <summary>¿El assembly compilado está al día con el .cs en disco? Solo lectura.</summary>
public static class _DiagStale
{
    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== DIAG STALE ==========");

        var tipo = typeof(PenaltyMinigameManager);
        sb.AppendLine($"Assembly: {tipo.Assembly.GetName().Name}  Location: {tipo.Assembly.Location}");
        try { sb.AppendLine($"Assembly write time: {System.IO.File.GetLastWriteTimeUtc(tipo.Assembly.Location):O}"); } catch { }

        sb.AppendLine("Campos SERIALIZADOS que ve el assembly compilado:");
        foreach (var f in tipo.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (f.IsDefined(typeof(SerializeField), true) || f.IsPublic)
                sb.AppendLine($"   {f.Name} : {f.FieldType.Name}");
        }

        sb.AppendLine("\n¿Existe 'aimPlane' en el tipo compilado? " +
                      (tipo.GetField("aimPlane", BindingFlags.Instance | BindingFlags.NonPublic) != null));
        sb.AppendLine("¿Existe 'modoDebug'? " +
                      (tipo.GetField("modoDebug", BindingFlags.Instance | BindingFlags.NonPublic) != null));
        sb.AppendLine("¿Existe 'penaltyHUD'? " +
                      (tipo.GetField("penaltyHUD", BindingFlags.Instance | BindingFlags.NonPublic) != null));

        // Métodos clave que deberían existir en la versión nueva
        sb.AppendLine("¿Existe 'IniciarModoPenalDebug'? " + (tipo.GetMethod("IniciarModoPenalDebug") != null));
        sb.AppendLine("¿Existe 'PatearDebug'? " + (tipo.GetMethod("PatearDebug") != null));
        sb.AppendLine("¿Existe 'AutoconfigurarDesdeMenu'? " + (tipo.GetMethod("AutoconfigurarDesdeMenu") != null));

        sb.AppendLine($"\nFecha .cs en disco: {System.IO.File.GetLastWriteTimeUtc("Assets/_Project/Scripts/Managers/PenaltyMinigameManager.cs"):O}");

        // ¿Cuántas líneas tiene el .cs en disco?
        sb.AppendLine($"Líneas .cs en disco: {System.IO.File.ReadAllLines("Assets/_Project/Scripts/Managers/PenaltyMinigameManager.cs").Length}");

        Debug.Log(sb.ToString());
    }
}
#endif
