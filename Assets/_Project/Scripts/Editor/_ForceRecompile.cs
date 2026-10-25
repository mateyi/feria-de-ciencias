using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

/// <summary>
/// SONDA TEMPORAL. Fuerza a Unity a recompilar los scripts para que las sondas
/// siguientes usen la última versión del código.
/// </summary>
public static class _ForceRecompile
{
    public static void Execute()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        CompilationPipeline.RequestScriptCompilation();
        Debug.Log("[ForceRecompile] Recompilación pedida.");
    }
}
