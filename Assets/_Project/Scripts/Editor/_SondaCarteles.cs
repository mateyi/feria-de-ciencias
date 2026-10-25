#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// SONDA TEMPORAL. Vuelca la posición local de los carteles/portales del segmento
/// de decisión, para poder colgar los iconos de la Fase 13 en el lugar correcto.
/// </summary>
public static class _SondaCarteles
{
    public static void Execute()
    {
        const string ruta = "Assets/_Project/Prefabs/Segment_Decision.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab == null) { Debug.LogError("[Carteles] no cargó el prefab"); return; }

        var sb = new StringBuilder();
        sb.AppendLine("[Carteles] Segment_Decision — transforms de interés");

        string[] interesantes =
        {
            "DecisionPoint", "SpawnPoint", "DespawnPoint", "Floor",
            "Portal_IZQ", "Portal_DER", "PortalFrame_IZQ", "PortalFrame_DER",
            "Arrow_IZQ", "Arrow_DER", "PostL_IZQ", "PostR_IZQ", "PostL_DER", "PostR_DER",
            "ArchPillar", "ArchBar", "Stripe", "LaneLine", "Roof",
        };

        foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
        {
            foreach (string n in interesantes)
            {
                if (t.name != n) continue;

                // Centro de los renderers hijos directos, para saber el tamaño real.
                var r = t.GetComponent<Renderer>();
                string extra = r != null ? $" bounds(local) centro={r.bounds.center} tamaño={r.bounds.size}" : "";

                var mf = t.GetComponent<MeshFilter>();
                string malla = mf != null && mf.sharedMesh != null ? $" malla={mf.sharedMesh.name} v={mf.sharedMesh.vertexCount}" : "";

                sb.AppendLine($"  {t.name}: posLocal={t.localPosition} rotLocal={t.localEulerAngles} " +
                              $"escalaLocal={t.localScale} mundo={t.position}{malla}{extra}");
                break;
            }
        }

        Debug.Log(sb.ToString());
    }
}
#endif
