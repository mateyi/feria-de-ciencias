#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

/// <summary>
/// SONDA/UTILIDAD TEMPORAL. Copia los iconos generados de Assets/Generated_Images
/// a Assets/_Project/UI, los importa como Sprite y avisa si de verdad traen
/// transparencia (si el fondo negro quedó opaco hay que tratarlo aparte).
/// </summary>
public static class _ImportarIconos
{
    static readonly string[] Nombres =
    {
        "icon_powerade", "icon_cerveza",
        "icon_cama", "icon_fiesta",
        "icon_plato_saludable", "icon_hamburguesa",
        "icon_pesas", "icon_sofa",
    };

    public static void Execute()
    {
        string origen  = "Assets/Generated_Images";
        string destino = "Assets/_Project/UI";

        if (!AssetDatabase.IsValidFolder(destino))
            AssetDatabase.CreateFolder("Assets/_Project", "UI");

        var sb = new StringBuilder();
        sb.AppendLine("[Iconos] Importando a " + destino);

        AssetDatabase.StartAssetEditing();
        foreach (string nombre in Nombres)
        {
            string src = $"{origen}/{nombre}.png";
            string dst = $"{destino}/{nombre}.png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(src) == null)
            {
                sb.AppendLine($"  FALTA {src}");
                continue;
            }

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(dst) == null)
            {
                if (!AssetDatabase.CopyAsset(src, dst))
                {
                    sb.AppendLine($"  no se pudo copiar {nombre}");
                    continue;
                }
            }

            var imp = AssetImporter.GetAtPath(dst) as TextureImporter;
            if (imp != null)
            {
                imp.textureType         = TextureImporterType.Sprite;
                imp.spriteImportMode    = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled       = false;
                imp.isReadable          = true;
                imp.filterMode          = FilterMode.Bilinear;
                imp.wrapMode            = TextureWrapMode.Clamp;
                imp.maxTextureSize      = 512;
                imp.spritePixelsPerUnit = 100f;
                imp.SaveAndReimport();
            }
        }
        AssetDatabase.StopAssetEditing();
        AssetDatabase.Refresh();

        // Diagnóstico de transparencia: mínimo alfa de cada textura.
        foreach (string nombre in Nombres)
        {
            string dst = $"{destino}/{nombre}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            if (tex == null) { sb.AppendLine($"  {nombre}: no cargó"); continue; }

            var px = tex.GetPixels32();
            int minA = 255, transparentes = 0;
            foreach (var c in px)
            {
                if (c.a < minA) minA = c.a;
                if (c.a < 16) transparentes++;
            }
            sb.AppendLine($"  {nombre}: {tex.width}x{tex.height} minAlfa={minA} " +
                          $"pixTransparentes={transparentes}");
        }

        Debug.Log(sb.ToString());
    }
}
#endif
