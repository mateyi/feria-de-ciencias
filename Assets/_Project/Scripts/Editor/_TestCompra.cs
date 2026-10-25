#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// SONDA TEMPORAL — deja la escena limpia (solo Item_Template) y prueba la
/// compra de la tienda de punta a punta en Play Mode.
/// action = "limpiar" (modo edicion) | "probar" (Play Mode).
/// </summary>
public static class _TestCompra
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Execute(string action)
    {
        if (action == "limpiar") { Limpiar(); return; }
        if (action == "probar")  { Probar();  return; }
        Debug.LogError("[TestCompra] accion desconocida: " + action);
    }

    // ─── Modo edicion: dejar solo la plantilla ────────────────────────────────

    private static void Limpiar()
    {
        var shop = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (shop == null) { Debug.LogError("[TestCompra] no encontre ShopUI"); return; }

        var grid = shop.transform.Find("Grid");
        var tpl  = grid != null ? grid.Find("Item_Template") : null;
        if (grid == null || tpl == null) { Debug.LogError("[TestCompra] falta Grid o Item_Template"); return; }

        int borrados = 0;
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            var h = grid.GetChild(i);
            if (h == tpl || !h.name.StartsWith("Item_")) continue;
            Object.DestroyImmediate(h.gameObject);
            borrados++;
        }

        typeof(ShopUI).GetField("_construido", F)?.SetValue(shop, false);
        (typeof(ShopUI).GetField("_filas", F)?.GetValue(shop) as System.Collections.Generic.List<ShopItemUI>)?.Clear();

        EditorUtility.SetDirty(shop);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        int quedan = 0;
        foreach (RectTransform h in grid) quedan++;
        Debug.Log($"[TestCompra] limpieza: borrados={borrados} quedan en Grid={quedan} " +
                  $"(esperado 1, la plantilla)");
    }

    // ─── Play Mode: compra real ───────────────────────────────────────────────

    private static void Probar()
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== PRUEBA DE COMPRA =====");

        if (!Application.isPlaying) { Debug.LogError("[TestCompra] esto es solo para Play Mode"); return; }

        var shop = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        var um   = UpgradeManager.Instance;
        if (shop == null || um == null || GameData.Instance == null)
        {
            Debug.LogError($"[TestCompra] faltan referencias shop={shop} um={um} gd={GameData.Instance}");
            return;
        }

        // 1. Guardar el estado original para restaurarlo al final.
        int coinsOrig  = SaveManager.Data.coins;
        int velOrig    = SaveManager.GetNivel(UpgradeId.Velocidad);
        int escOrig    = SaveManager.GetNivel(UpgradeId.Escudo);
        sb.AppendLine($"estado original: monedas={coinsOrig} Velocidad={velOrig} Escudo={escOrig}");

        // 2. Dejar la tienda en cero.
        SaveManager.Data.coins = 0;
        SaveManager.SetNivel(UpgradeId.Velocidad, 0);
        SaveManager.SetNivel(UpgradeId.Escudo, 0);

        // 3. Abrir la tienda por el camino real (UIManager -> SetActive -> Awake).
        UIManager.Instance.OpenShop();

        var filas = shop.GetComponentsInChildren<ShopItemUI>(true);
        int activas = 0;
        foreach (var f in filas) if (f.gameObject.activeSelf) activas++;
        sb.AppendLine($"filas construidas: total={filas.Length} activas={activas} (esperado 8 activas, sin duplicados)");

        var filaVel = shop.transform.Find("Grid/Item_Velocidad");
        if (filaVel == null) { Debug.LogError("[TestCompra] no encontre Item_Velocidad"); return; }

        var btnVel   = filaVel.Find("Boton").GetComponent<Button>();
        var nivelTxt = filaVel.Find("Nivel").GetComponent<TMP_Text>();
        var costoTxt = filaVel.Find("Costo").GetComponent<TMP_Text>();
        var aviso    = shop.transform.Find("Aviso").GetComponent<TMP_Text>();

        // 4. Sin monedas: el boton tiene que estar deshabilitado.
        sb.AppendLine($"SIN monedas: boton interactable={btnVel.interactable} " +
                      $"aviso activo={aviso.gameObject.activeSelf}");

        // Llamada directa para comprobar que el mensaje de error funciona aunque
        // el boton este deshabilitado (por UI el usuario no podria llegar).
        shop.Comprar(um.GetData(UpgradeId.Velocidad));
        sb.AppendLine($"SIN monedas tras pedir compra: nivel={SaveManager.GetNivel(UpgradeId.Velocidad)} " +
                      $"monedas={SaveManager.Data.coins} aviso='{aviso.text}' activo={aviso.gameObject.activeSelf}");

        // 5. Con 500 monedas: dos compras reales tocando el boton.
        SaveManager.Data.coins = 500;
        SaveManager.Guardar();
        shop.Refrescar();

        sb.AppendLine($"CON 500 monedas: boton interactable={btnVel.interactable} " +
                      $"costo en pantalla='{costoTxt.text}' nivel en pantalla='{nivelTxt.text}'");

        btnVel.onClick.Invoke();
        sb.AppendLine($"compra 1: nivel={SaveManager.GetNivel(UpgradeId.Velocidad)} " +
                      $"monedas={SaveManager.Data.coins} (esperado nivel 1, monedas 440) " +
                      $"pantalla nivel='{nivelTxt.text}' costo='{costoTxt.text}'");

        btnVel.onClick.Invoke();
        sb.AppendLine($"compra 2: nivel={SaveManager.GetNivel(UpgradeId.Velocidad)} " +
                      $"monedas={SaveManager.Data.coins} (esperado nivel 2, monedas 347) " +
                      $"pantalla nivel='{nivelTxt.text}' costo='{costoTxt.text}'");

        // 6. Gastar todo para forzar el maximo de monedas.
        SaveManager.Data.coins = 10;
        shop.Refrescar();
        sb.AppendLine($"con 10 monedas: interactable={btnVel.interactable}");

        // 7. Lo que quedo escrito en disco.
        try
        {
            sb.AppendLine("archivo en disco:\n" + File.ReadAllText(SaveManager.RutaArchivo));
        }
        catch (System.Exception e) { sb.AppendLine("no pude leer el archivo: " + e.Message); }

        // 8. Restaurar el estado original.
        SaveManager.Data.coins = coinsOrig;
        SaveManager.SetNivel(UpgradeId.Velocidad, velOrig);
        SaveManager.SetNivel(UpgradeId.Escudo, escOrig);
        SaveManager.Guardar();
        shop.Refrescar();
        UIManager.Instance.OpenMainMenu();
        sb.AppendLine($"restaurado: monedas={SaveManager.Data.coins} Velocidad={SaveManager.GetNivel(UpgradeId.Velocidad)} " +
                      $"Escudo={SaveManager.GetNivel(UpgradeId.Escudo)}");

        Debug.Log(sb.ToString());
    }
}
#endif
