#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

/// <summary>
/// SONDA TEMPORAL — exporta la textura del futbolista a un PNG para poder
/// mirarla y decidir como pintar la camiseta de cada pais.
/// action = "exportar".
/// </summary>
public static class _ExportAtlas
{
    public static void Execute(string action)
    {
        const string rutaGlb = "Assets/_Project/Animations/Futbolista_Run.glb";
        const string salida = "Assets/_Project/UI/Temp/atlas_futbolista.png";

        Texture2D origen = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(rutaGlb))
            if (a is Texture2D t) { origen = t; break; }

        if (origen == null) { Debug.LogError("[ExportAtlas] no encontre textura"); return; }

        Debug.Log("[ExportAtlas] textura origen " + origen.width + "x" + origen.height +
                  " formato=" + origen.format + " legible=" + origen.isReadable);

        // Copiamos por GPU para no depender de que sea legible.
        var rt = RenderTexture.GetTemporary(origen.width, origen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(origen, rt);

        var previo = RenderTexture.active;
        RenderTexture.active = rt;

        var copia = new Texture2D(origen.width, origen.height, TextureFormat.RGBA32, false, false);
        copia.ReadPixels(new Rect(0, 0, origen.width, origen.height), 0, 0);
        copia.Apply();

        RenderTexture.active = previo;
        RenderTexture.ReleaseTemporary(rt);

        Directory.CreateDirectory(Path.GetDirectoryName(salida));
        File.WriteAllBytes(salida, copia.EncodeToPNG());
        Object.DestroyImmediate(copia);

        AssetDatabase.ImportAsset(salida);

        Debug.Log("[ExportAtlas] exportado a " + salida);
    }
}
#endif
