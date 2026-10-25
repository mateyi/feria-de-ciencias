#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

/// <summary>
/// PASO 2-A: reparación de la escena del penal (solo Editor).
///   1. Borra el PenaltyHUDPanel duplicado (conserva el que tiene la mira del manager).
///   2. Borra el LevelCompletePanel duplicado (conserva el que usa UIManager).
///   3. Genera un sprite PNG real para la mira y se lo asigna (una Image sin
///      sprite NO dibuja nada).
///   4. Corre la Autoconfiguración del PenaltyMinigameManager y guarda la escena.
/// </summary>
public static class _FixPenaltyScene
{
    private const string SpritePath = "Assets/_Project/UI/AimReticle.png";

    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== FIX ESCENA PENAL ==========");

        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        var pmm    = Object.FindFirstObjectByType<PenaltyMinigameManager>();

        // ── 1. Identificar el panel que DEBE sobrevivir (el de la mira) ───────
        var soPmm    = new SerializedObject(pmm);
        var reticle  = soPmm.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;

        Transform panelMira = null;
        if (reticle != null)
        {
            Transform t = reticle;
            while (t.parent != null && t.parent.GetComponent<Canvas>() == null) t = t.parent;
            panelMira = t;
        }
        sb.AppendLine($"Panel que contiene la mira: {(panelMira != null ? panelMira.name + " (Transform id=" + panelMira.GetInstanceID() + ")" : "<no encontrado>")}");

        // ── 2. Borrar PenaltyHUDPanel duplicados ──────────────────────────────
        var hudPaneles = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            var ch = canvas.transform.GetChild(i);
            if (ch.name == "PenaltyHUDPanel") hudPaneles.Add(ch);
        }
        sb.AppendLine($"PenaltyHUDPanel encontrados: {hudPaneles.Count}");

        foreach (var p in hudPaneles)
        {
            if (p == panelMira) continue;
            sb.AppendLine($"  BORRANDO duplicado '{p.name}' id={p.GetInstanceID()}");
            Object.DestroyImmediate(p.gameObject);
        }

        // ── 3. Borrar LevelCompletePanel duplicados ───────────────────────────
        var uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        GameObject panelLCdeUIManager = null;
        if (uiManager != null)
        {
            var soUi = new SerializedObject(uiManager);
            panelLCdeUIManager = soUi.FindProperty("levelCompletePanel")?.objectReferenceValue as GameObject;
            sb.AppendLine($"UIManager.levelCompletePanel = {(panelLCdeUIManager != null ? panelLCdeUIManager.name + " (GO id=" + panelLCdeUIManager.GetInstanceID() + ")" : "<NULL>")}");
        }

        var lcPaneles = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < canvas.transform.childCount; i++)
        {
            var ch = canvas.transform.GetChild(i);
            if (ch.name == "LevelCompletePanel") lcPaneles.Add(ch);
        }
        sb.AppendLine($"LevelCompletePanel encontrados: {lcPaneles.Count}");

        // Si UIManager apunta a uno de ellos, ese se conserva; si no, conservamos el primero.
        Transform panelLCconservar = null;
        if (panelLCdeUIManager != null)
        {
            foreach (var p in lcPaneles)
                if (p.gameObject == panelLCdeUIManager) panelLCconservar = p;
        }
        if (panelLCconservar == null && lcPaneles.Count > 0) panelLCconservar = lcPaneles[0];

        foreach (var p in lcPaneles)
        {
            if (p == panelLCconservar) continue;
            sb.AppendLine($"  BORRANDO duplicado '{p.name}' id={p.GetInstanceID()}");
            Object.DestroyImmediate(p.gameObject);
        }
        sb.AppendLine($"LevelCompletePanel conservado: {(panelLCconservar != null ? "id=" + panelLCconservar.GetInstanceID() : "<ninguno>")}");

        // ── 4. Sprite real para la mira ───────────────────────────────────────
        AsignarSpriteMira(sb, reticle);

        // ── 5. Autoconfigurar el manager (crea AimPlane y cablea el HUD) ──────
        if (pmm != null)
        {
            pmm.AutoconfigurarDesdeMenu();
            sb.AppendLine("Autoconfigurar ejecutado.");
        }

        // ── 6. Guardar la escena ──────────────────────────────────────────────
        var escena = pmm != null ? pmm.gameObject.scene : canvas.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        sb.AppendLine($"Escena guardada: {escena.path}");

        sb.AppendLine("========== FIN FIX ==========");
        Debug.Log(sb.ToString());
    }

    /// <summary>Genera un PNG de mira (círculo + cruz) y lo asigna a la Image.</summary>
    private static void AsignarSpriteMira(StringBuilder sb, RectTransform reticle)
    {
        if (reticle == null) { sb.AppendLine("  Mira: <no asignada>"); return; }

        var img = reticle.GetComponent<Image>();
        if (img == null) { sb.AppendLine("  Mira: sin componente Image"); return; }

        const int T = 128;
        var tex = new Texture2D(T, T, TextureFormat.RGBA32, false);
        var px  = new Color32[T * T];
        Vector2 c = new Vector2(T / 2f - 0.5f, T / 2f - 0.5f);

        for (int y = 0; y < T; y++)
        {
            for (int x = 0; x < T; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);

                bool anillo = Mathf.Abs(d - 46f) < 4.5f;                                  // círculo
                bool punto  = d < 6f;                                                     // centro
                bool cruzH  = Mathf.Abs(y - c.y) < 3f && Mathf.Abs(x - c.x) < 26f && d > 18f;
                bool cruzV  = Mathf.Abs(x - c.x) < 3f && Mathf.Abs(y - c.y) < 26f && d > 18f;

                px[y * T + x] = (anillo || punto || cruzH || cruzV)
                    ? new Color32(255, 235, 40, 255)
                    : new Color32(0, 0, 0, 0);
            }
        }

        tex.SetPixels32(px);
        tex.Apply();

        string carpeta = Path.GetDirectoryName(SpritePath);
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);

        // Configurar la importación como Sprite de UI.
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.filterMode          = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null) { sb.AppendLine("  Mira: no se pudo cargar el sprite"); return; }

        img.sprite         = sprite;
        img.color          = Color.white;
        img.raycastTarget  = false;   // es una mira: no debe interceptar clics
        img.preserveAspect = true;
        EditorUtility.SetDirty(img);

        // Un poco más grande para que se vea bien en el stand.
        if (reticle.rect.size.x < 64f)
            reticle.sizeDelta = new Vector2(64f, 64f);

        sb.AppendLine($"  Mira: sprite '{SpritePath}' asignado (raycastTarget=false).");
    }
}
#endif
