using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca los objetos de la escena: pista inicial, jugador, cámara correctamente posicionada.
/// </summary>
public class PlaceSceneObjects
{
    public static void Execute()
    {
        PlaceInitialTrack();
        PositionCamera();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[MundialRunner] ✅ Objetos de escena colocados.");
    }

    private static void PlaceInitialTrack()
    {
        // IMPORTANTE: si la escena ya tiene un TrackGenerator, la pista se genera en
        // runtime. Colocar segmentos a mano acá crea una SEGUNDA pista superpuesta
        // (obstáculos duplicados) que hacía que el jugador muriera a los pocos metros
        // del spawn y pareciera que "no avanza".
        if (UnityEngine.Object.FindFirstObjectByType<TrackGenerator>() != null)
        {
            Debug.LogWarning("[MundialRunner] Hay un TrackGenerator en la escena: se omite la " +
                             "pista manual porque la genera TrackGenerator en runtime.");
            return;
        }

        // Verificar si ya existe pista en la escena
        if (GameObject.Find("Segment_Normal(Clone)") != null) return;
        if (GameObject.Find("Segment_Normal") != null) return;

        TrackSegment normalPrefab   = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Normal.prefab");
        TrackSegment decisionPrefab = AssetDatabase.LoadAssetAtPath<TrackSegment>("Assets/_Project/Prefabs/Segment_Decision.prefab");

        if (normalPrefab == null)
        {
            Debug.LogError("[MundialRunner] Segment_Normal.prefab no encontrado.");
            return;
        }

        // Colocar 4 segmentos normales + 1 de decisión + 2 normales más
        Vector3 pos = Vector3.zero;
        for (int i = 0; i < 4; i++)
        {
            TrackSegment seg = (TrackSegment)PrefabUtility.InstantiatePrefab(normalPrefab);
            seg.transform.position = pos;
            pos = seg.SpawnPoint;
        }

        if (decisionPrefab != null)
        {
            TrackSegment decSeg = (TrackSegment)PrefabUtility.InstantiatePrefab(decisionPrefab);
            decSeg.transform.position = pos;
            pos = decSeg.SpawnPoint;
        }

        for (int i = 0; i < 3; i++)
        {
            TrackSegment seg = (TrackSegment)PrefabUtility.InstantiatePrefab(normalPrefab);
            seg.transform.position = pos;
            pos = seg.SpawnPoint;
        }

        Debug.Log("[MundialRunner] Pista inicial colocada en la escena.");
    }

    private static void PositionCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Posición de cámara estilo endless runner: detrás y arriba del jugador
        cam.transform.position = new Vector3(0, 5f, -5f);
        cam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

        // Ajustar SceneView para ver la pista
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.pivot   = new Vector3(0, 1f, 15f);
            sv.rotation = Quaternion.Euler(25f, 0f, 0f);
            sv.size    = 20f;
            sv.orthographic = false;
            sv.Repaint();
        }
    }
}
