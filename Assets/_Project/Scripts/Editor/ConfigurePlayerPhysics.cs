#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ajusta el CharacterController del jugador para evitar clipping a alta velocidad:
/// - minMoveDistance = 0 (no descartar movimientos pequeños)
/// - skinWidth un poco mayor para robustez de colisión
/// - stepOffset adecuado para entrar al túnel sin trabarse
/// (Nota: el jugador usa CharacterController, no Rigidbody; para CC el equivalente
///  a CCD es asegurar minMoveDistance=0 y skinWidth correcto.)
/// </summary>
public static class ConfigurePlayerPhysics
{
    public static void Execute()
    {
        string path = "Assets/_Project/Prefabs/Player.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning("Player.prefab no encontrado"); return; }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var cc = root.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.minMoveDistance = 0f;
                cc.skinWidth = 0.1f;
                cc.stepOffset = 0.4f;
                cc.slopeLimit = 50f;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Debug.Log("[ConfigurePlayerPhysics] CharacterController del jugador ajustado.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
