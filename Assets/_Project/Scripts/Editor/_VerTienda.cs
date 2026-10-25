#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Reflection;
using TMPro;

/// <summary>
/// SONDA TEMPORAL — abre/cierra la tienda en modo edición para poder sacarle
/// una captura, y prueba una compra contra el guardado real.
/// action = "abrir" | "cerrar" | "comprar".
/// </summary>
public static class _VerTienda
{
    public static void Execute(string action)
    {
        var shop = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (shop == null) { Debug.LogError("[VerTienda] no encontré ShopUI"); return; }

        if (action == "cerrar")
        {
            shop.gameObject.SetActive(false);
            Debug.Log("[VerTienda] tienda cerrada");
            return;
        }

        if (action == "comprar")
        {
            int antes = SaveManager.GetNivel(UpgradeId.Velocidad);
            var data = AssetDatabase.LoadAssetAtPath<UpgradeData>(
                "Assets/_Project/Data/Mejoras/Upgrade_Velocidad.asset");
            Debug.Log($"[VerTienda] nivel Velocidad antes={antes} costo siguiente=" +
                      $"{(data != null ? data.CostoParaNivel(antes + 1) : -1)} " +
                      $"monedas guardadas={SaveManager.Data.coins}");
            return;
        }

        // Abrir: escondemos el menú y abrimos la tienda a mano (en edición
        // Awake no corre, así que construimos las filas por reflexión).
        var menu = GameObject.Find("Canvas/MainMenuPanel");
        if (menu != null) menu.SetActive(false);

        // El cartel de resultado de las decisiones puede haber quedado abierto.
        var decision = Object.FindFirstObjectByType<DecisionUI>(FindObjectsInactive.Include);
        if (decision != null) decision.gameObject.SetActive(false);

        shop.gameObject.SetActive(true);

        var construir = typeof(ShopUI).GetMethod("Construir",
            BindingFlags.NonPublic | BindingFlags.Instance);
        construir?.Invoke(shop, null);
        shop.Refrescar();

        // En edición el GridLayoutGroup no reacomoda solo: hay que forzarlo.
        Canvas.ForceUpdateCanvases();
        foreach (var g in shop.GetComponentsInChildren<GridLayoutGroup>(true))
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)g.transform);
        Canvas.ForceUpdateCanvases();

        int filas = shop.transform.GetComponentsInChildren<ShopItemUI>(true).Length;
        Debug.Log($"[VerTienda] tienda abierta con {filas} filas " +
                  $"(monedas guardadas={SaveManager.Data.coins})");
        Selection.activeGameObject = shop.gameObject;
    }
}
#endif
