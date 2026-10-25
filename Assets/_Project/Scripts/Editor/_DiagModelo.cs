#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// SONDA TEMPORAL — inspecciona el modelo del futbolista: sub-assets del .glb,
/// materiales, texturas, mallas y clips de animacion.
/// action = "ver".
/// </summary>
public static class _DiagModelo
{
    public static void Execute(string action)
    {
        const string ruta = "Assets/_Project/Animations/Futbolista_Run.glb";

        var sb = new StringBuilder();
        sb.AppendLine("===== MODELO DEL FUTBOLISTA =====");

        var todos = AssetDatabase.LoadAllAssetsAtPath(ruta);
        sb.AppendLine("sub-assets: " + todos.Length);
        foreach (var a in todos)
        {
            if (a == null) continue;
            sb.AppendLine("  - " + a.GetType().Name + " '" + a.name + "'");

            if (a is Mesh m)
                sb.AppendLine("      vertices=" + m.vertexCount + " subMeshs=" + m.subMeshCount +
                              " uv=" + (m.uv != null && m.uv.Length > 0) +
                              " uv2=" + (m.uv2 != null && m.uv2.Length > 0) +
                              " colores=" + (m.colors != null && m.colors.Length > 0) +
                              " normales=" + (m.normals != null && m.normals.Length > 0));

            if (a is Material mat)
            {
                sb.AppendLine("      shader=" + mat.shader.name);
                var props = mat.GetPropertyNames(MaterialPropertyType.Float);
                sb.AppendLine("      float props: " + string.Join(",", props));
                var texProps = mat.GetPropertyNames(MaterialPropertyType.Texture);
                sb.AppendLine("      texture props: " + string.Join(",", texProps));
                foreach (var p in texProps)
                    sb.AppendLine("        " + p + " = " + (mat.GetTexture(p) != null ? mat.GetTexture(p).name : "NULL"));
                if (mat.HasProperty("_BaseColorFactor")) sb.AppendLine("      baseColorFactor=" + mat.GetColor("_BaseColorFactor"));
                if (mat.HasProperty("baseColorFactor")) sb.AppendLine("      baseColorFactor2=" + mat.GetColor("baseColorFactor"));
                if (mat.HasProperty("_BaseColor")) sb.AppendLine("      BaseColor=" + mat.GetColor("_BaseColor"));
            }

            if (a is AnimationClip clip)
                sb.AppendLine("      duracion=" + clip.length + "s");
        }

        // El objeto del prefab que instancia el modelo.
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
        if (prefab != null)
        {
            sb.AppendLine("--- jerarquia del prefab Player ---");
            Volcar(prefab.transform, 0, sb);
        }

        Debug.Log(sb.ToString());
    }

    private static void Volcar(Transform t, int nivel, StringBuilder sb)
    {
        string extra = "";
        var mr = t.GetComponent<MeshRenderer>();
        if (mr != null && mr.sharedMaterial != null)
            extra = "  mat='" + mr.sharedMaterial.name + "' shader=" + mr.sharedMaterial.shader.name;

        var sk = t.GetComponent<SkinnedMeshRenderer>();
        if (sk != null && sk.sharedMaterial != null)
            extra = "  skinned mat='" + sk.sharedMaterial.name + "' shader=" + sk.sharedMaterial.shader.name;

        sb.AppendLine(new string(' ', nivel * 2) + t.name + extra);

        for (int i = 0; i < t.childCount; i++)
            Volcar(t.GetChild(i), nivel + 1, sb);
    }
}
#endif
