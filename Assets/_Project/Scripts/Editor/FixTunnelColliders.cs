#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Corrige los bugs de "caída a través del piso" y crash en la zona final:
/// - Agrega colliders de piso donde faltan (túnel, arco meta, arena).
/// - Crea un piso continuo (bridge) que cubre desde el fin del recorrido hasta la arena
///   para que no existan gaps entre el suelo del estadio y el túnel/arena.
/// - Agrega OutOfBoundsGuard (kill zone) a la escena.
/// - Re-coloca prefabs actualizados (con colliders).
/// - Configura la detección de colisión del jugador.
/// </summary>
public static class FixTunnelColliders
{
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();

        ReplaceInstancesWithUpdatedPrefabs();
        BuildContinuousGround();
        EnsureOutOfBoundsGuard();

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[FixTunnelColliders] Colliders y red de seguridad aplicados.");
    }

    private static void ReplaceInstancesWithUpdatedPrefabs()
    {
        // Re-instanciar túnel con collider actualizado.
        var tg = Object.FindFirstObjectByType<TrackGenerator>();
        var tunnelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PenaltyTunnel_15m.prefab");
        var arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PenaltyArena.prefab");

        float tunnelZ = 301f;
        var oldTunnel = GameObject.Find("TrackGenerator/PenaltyTunnel_15m_Instance");
        if (oldTunnel == null) oldTunnel = GameObject.Find("PenaltyTunnel_15m_Instance");
        if (oldTunnel != null)
        {
            tunnelZ = oldTunnel.transform.position.z;
            Object.DestroyImmediate(oldTunnel);
        }
        if (tunnelPrefab != null)
        {
            var t = (GameObject)PrefabUtility.InstantiatePrefab(tunnelPrefab);
            t.name = "PenaltyTunnel_15m_Instance";
            if (tg != null) t.transform.SetParent(tg.transform);
            t.transform.position = new Vector3(0, 0, tunnelZ);
            t.transform.rotation = Quaternion.identity;
        }

        float arenaZ = 349f;
        var oldArena = GameObject.Find("PenaltyArena_Instance");
        if (oldArena != null)
        {
            arenaZ = oldArena.transform.position.z;
            Object.DestroyImmediate(oldArena);
        }
        if (arenaPrefab != null)
        {
            var a = (GameObject)PrefabUtility.InstantiatePrefab(arenaPrefab);
            a.name = "PenaltyArena_Instance";
            a.transform.position = new Vector3(0, 0, arenaZ);
            a.transform.rotation = Quaternion.identity;
        }
    }

    private static void BuildContinuousGround()
    {
        const string groundName = "EndGround_Bridge";
        var existing = GameObject.Find(groundName);
        if (existing) Object.DestroyImmediate(existing);

        // Determinar el rango Z que hay que cubrir: desde el arco meta hasta el fin de la arena.
        float startZ = 290f;
        float endZ = 380f;

        var finish = GameObject.Find("TrackGenerator/FinishArch_Meta_Instance");
        if (finish == null) finish = GameObject.Find("FinishArch_Meta_Instance");
        if (finish != null) startZ = finish.transform.position.z - 10f;

        var arena = GameObject.Find("PenaltyArena_Instance");
        if (arena != null) endZ = arena.transform.position.z + 25f;

        float length = Mathf.Max(20f, endZ - startZ);
        float centerZ = startZ + length * 0.5f;

        // Piso sólido continuo (ancho suficiente para cubrir carriles + arena).
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = groundName;
        ground.transform.localScale = new Vector3(40f, 0.4f, length);
        ground.transform.position = new Vector3(0, -0.2f, centerZ);

        // material del track si existe
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Mat_Track.mat");
        if (mat != null) ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var col = ground.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = false;
    }

    private static void EnsureOutOfBoundsGuard()
    {
        var guard = Object.FindFirstObjectByType<OutOfBoundsGuard>();
        if (guard == null)
        {
            var go = new GameObject("OutOfBoundsGuard");
            go.AddComponent<OutOfBoundsGuard>();
        }
    }
}
#endif
