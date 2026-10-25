#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text;

/// <summary>
/// Reparación de la ESCENA del penal (se ejecuta una sola vez).
///   1. Borra los paneles DUPLICADOS del Canvas (PenaltyHUDPanel y LevelCompletePanel).
///   2. Genera un sprite PNG real para la mira (una Image sin sprite no se dibuja).
///   3. Autoconfigura el PenaltyMinigameManager (AimPlane, referencias, controller).
///   4. Guarda la escena.
/// </summary>
public static class _RepararEscenaPenal
{
    public static void Execute()
    {
        var log = new StringBuilder();
        log.AppendLine("======== REPARANDO ESCENA DEL PENAL ========");

        // ─── 1. PANELES DUPLICADOS ───────────────────────────────────────────
        var pmm = Object.FindFirstObjectByType<PenaltyMinigameManager>();
        if (pmm == null) { Debug.LogError("No hay PenaltyMinigameManager en la escena."); return; }

        var so = new SerializedObject(pmm);
        var reticle = so.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;

        // Panel del HUD que HAY QUE CONSERVAR: el que contiene la mira que usa el manager.
        GameObject panelHudGanador = null;
        if (reticle != null)
        {
            Transform t = reticle;
            while (t.parent != null && t.parent.GetComponent<Canvas>() == null) t = t.parent;
            panelHudGanador = t.gameObject;
        }

        // Borrar todos los PenaltyHUDPanel que NO sean el ganador.
        foreach (var hud in Object.FindObjectsByType<PenaltyHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hud == null) continue;
            if (hud.gameObject == panelHudGanador) continue;

            log.AppendLine($"  Borrando PenaltyHUDPanel duplicado id={hud.gameObject.GetInstanceID()}");
            Object.DestroyImmediate(hud.gameObject);
        }

        // ─── LevelCompletePanel: conservar el que referencia UIManager ───────
        GameObject lcGanador = null;
        var uiMgr = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiMgr != null)
        {
            var uso = new SerializedObject(uiMgr);
            var prop = uso.FindProperty("levelCompletePanel");
            if (prop != null && prop.objectReferenceValue != null)
                lcGanador = prop.objectReferenceValue as GameObject;
        }

        // Si UIManager no tenía referencia, conservamos el primero y lo asignamos.
        if (lcGanador == null)
        {
            var lcs = Object.FindObjectsByType<LevelCompleteUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (lcs.Length > 0) lcGanador = lcs[0].gameObject;
        }

        int borrados = 0;
        foreach (var lc in Object.FindObjectsByType<LevelCompleteUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lc == null) continue;
            if (lc.gameObject == lcGanador) continue;

            log.AppendLine($"  Borrando LevelCompletePanel duplicado id={lc.gameObject.GetInstanceID()}");
            Object.DestroyImmediate(lc.gameObject);
            borrados++;
        }

        // Asegurar que UIManager apunte al panel que quedó.
        if (uiMgr != null && lcGanador != null)
        {
            var uso = new SerializedObject(uiMgr);
            var prop = uso.FindProperty("levelCompletePanel");
            if (prop != null)
            {
                prop.objectReferenceValue = lcGanador;
                uso.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine($"  UIManager.levelCompletePanel → '{lcGanador.name}'");
            }
        }

        // ─── 2. SPRITE DE LA MIRA ────────────────────────────────────────────
        Sprite spriteMira = AsegurarSpriteMira(log);

        if (reticle != null && spriteMira != null)
        {
            var img = reticle.GetComponent<Image>();
            if (img == null) img = reticle.gameObject.AddComponent<Image>();
            img.sprite = spriteMira;
            img.color = Color.white;
            // La mira no debe bloquear clics de UI.
            img.raycastTarget = false;
            EditorUtility.SetDirty(img);
            log.AppendLine("  Sprite asignado al AimReticle.");
        }

        // ─── 3. AUTOCONFIGURAR EL MANAGER ────────────────────────────────────
        pmm.AutoconfigurarDesdeMenu();

        // ─── 4. Asegurar que el panel del HUD arranque inactivo ──────────────
        if (panelHudGanador != null)
            panelHudGanador.SetActive(false);

        // ─── 5. GUARDAR ──────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(pmm.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();
        log.AppendLine("  Escena guardada.");

        log.AppendLine("======== FIN ========");
        Debug.Log(log.ToString());
    }

    /// <summary>
    /// Genera (si no existe) Assets/_Project/UI/MiraPenal.png con una mira
    /// (círculo + cruz) y lo importa como Sprite.
    /// </summary>
    private static Sprite AsegurarSpriteMira(StringBuilder log)
    {
        const string carpeta = "Assets/_Project/UI";
        const string ruta    = carpeta + "/MiraPenal.png";

        if (!AssetDatabase.IsValidFolder(carpeta))
            AssetDatabase.CreateFolder("Assets/_Project", "UI");

        if (!File.Exists(ruta))
        {
            const int T = 128;
            var tex = new Texture2D(T, T, TextureFormat.RGBA32, false);
            var px  = new Color32[T * T];
            Vector2 c = new Vector2(T / 2f - 0.5f, T / 2f - 0.5f);

            for (int y = 0; y < T; y++)
            {
                for (int x = 0; x < T; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);

                    bool anillo = Mathf.Abs(d - 44f) < 4.5f;    // círculo exterior
                    bool punto  = d < 6f;                       // punto central
                    bool cruzH  = Mathf.Abs(y - c.y) < 3f && Mathf.Abs(x - c.x) < 26f && d > 18f;
                    bool cruzV  = Mathf.Abs(x - c.x) < 3f && Mathf.Abs(y - c.y) < 26f && d > 18f;

                    px[y * T + x] = (anillo || punto || cruzH || cruzV)
                        ? new Color32(255, 240, 60, 255)
                        : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
            log.AppendLine("  MiraPenal.png generado.");
        }

        // Configurar el importador como Sprite de UI.
        var importer = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
            log.AppendLine("  MiraPenal.png configurado como Sprite.");
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (sprite == null) log.AppendLine("  ⚠ No pude cargar el sprite de la mira.");
        return sprite;
    }
}
#endif
