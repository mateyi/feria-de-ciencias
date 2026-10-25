#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Ajusta el encuadre de la cámara del penal y guarda la escena.
/// Se hace por script para evitar problemas de separador decimal (es-ES).
/// </summary>
public static class _SetPenaltyCamera
{
    public static void Execute()
    {
        var anchor = GameObject.Find("PenaltyCameraAnchor");
        if (anchor == null) { Debug.LogError("[Camara] No encontré PenaltyCameraAnchor"); return; }

        // Vista por encima del hombro: un poco a la derecha del pateador y elevada,
        // mirando al centro del arco (queda libre el arco y se ve la pelota).
        anchor.transform.position = new Vector3(2.4f, 3f, 324f);
        anchor.transform.rotation = Quaternion.Euler(7f, -9f, 0f);

        EditorUtility.SetDirty(anchor);
        EditorSceneManager.MarkSceneDirty(anchor.scene);
        EditorSceneManager.SaveScene(anchor.scene);

        Debug.Log($"[Camara] PenaltyCameraAnchor → pos={anchor.transform.position} " +
                  $"rot={anchor.transform.eulerAngles}. Escena guardada.");
    }
}
#endif
