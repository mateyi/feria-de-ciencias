#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Coloca el PenaltyCameraAnchor en un encuadre de penal correcto:
/// detrás y por encima del pateador, mirando al centro del arco.
///
/// Usamos floats explícitos (no strings) para que la cultura del sistema
/// no pueda volver a romper los decimales.
/// </summary>
public static class _FixCameraAnchor
{
    public static void Execute()
    {
        var anchor = GameObject.Find("PenaltyCameraAnchor");
        if (anchor == null) { Debug.LogError("[Fix] No existe PenaltyCameraAnchor"); return; }

        // Punto de penal (z≈329) y centro del arco (z≈339, y≈1.22).
        Vector3 posicion  = new Vector3(0f, 3.2f, 324.5f);
        Quaternion rotacion = Quaternion.Euler(9f, 0f, 0f);

        anchor.transform.SetPositionAndRotation(posicion, rotacion);
        anchor.transform.localScale = Vector3.one;

        EditorUtility.SetDirty(anchor);

        var escena = anchor.scene;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        // Comprobación geométrica del encuadre.
        float distanciaAlArco = Vector3.Distance(posicion, new Vector3(0f, 1.22f, 339f));
        Debug.Log($"[Fix] PenaltyCameraAnchor => pos={posicion} rot={rotacion.eulerAngles} " +
                  $"(distancia al arco {distanciaAlArco:F1} m). Escena '{escena.name}' guardada.");
    }
}
#endif
