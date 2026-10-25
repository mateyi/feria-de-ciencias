#if UNITY_EDITOR
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// SONDA TEMPORAL — mide el layout real de la tienda en modo edición y avisa
/// qué elementos se salen de su contenedor. action = "abrir" | "cerrar".
/// </summary>
public static class _VerTiendaLayout
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Execute(string action)
    {
        var shop = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (shop == null) { Debug.LogError("[Layout] no encontre ShopUI"); return; }

        if (action == "cerrar")
        {
            shop.gameObject.SetActive(false);
            Debug.Log("[Layout] tienda cerrada");
            return;
        }

        var menu = GameObject.Find("Canvas/MainMenuPanel");
        if (menu != null) menu.SetActive(false);

        var decision = Object.FindFirstObjectByType<DecisionUI>(FindObjectsInactive.Include);
        if (decision != null) decision.gameObject.SetActive(false);

        shop.gameObject.SetActive(true);

        if (action == "detalle")
        {
            var sb0 = new StringBuilder();
            sb0.AppendLine("===== DETALLE BOTON / CABECERA =====");
            DetalleBotones(shop, sb0);
            DetalleCabecera(shop, sb0);
            Debug.Log(sb0.ToString());
            return;
        }

        var tShop = typeof(ShopUI);
        var contenedor = (RectTransform)tShop.GetField("contenedor", F)?.GetValue(shop);
        if (contenedor == null) { Debug.LogError("[Layout] sin contenedor"); return; }

        var filas = shop.GetComponentsInChildren<ShopItemUI>(true);
        int activas = 0;
        foreach (var f in filas) if (f.gameObject.activeSelf) activas++;
        Debug.Log($"[Layout] ShopItemUI totales={filas.Length} activas={activas}");

        if (activas == 0)
        {
            tShop.GetMethod("Construir", F)?.Invoke(shop, null);
            Debug.Log("[Layout] Construir() invocado");
        }

        shop.Refrescar();

        // Forzar el acomodo: en edición el GridLayoutGroup no recalcula solo.
        Canvas.ForceUpdateCanvases();
        foreach (var g in contenedor.GetComponentsInChildren<GridLayoutGroup>(true))
            LayoutRebuilder.ForceRebuildLayoutImmediate(g.transform as RectTransform);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(shop.transform as RectTransform);
        Canvas.ForceUpdateCanvases();

        Informe(shop, contenedor);
        Selection.activeGameObject = shop.gameObject;
    }

    private static void Informe(ShopUI shop, RectTransform contenedor)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== INFORME TIENDA =====");

        var shopRt = shop.transform as RectTransform;
        sb.AppendLine($"ShopPanel: size={shopRt.rect.size} anchoredPos={shopRt.anchoredPosition}");

        var grid = contenedor.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            sb.AppendLine($"GridLayout: cell={grid.cellSize} spacing={grid.spacing} " +
                          $"constraint={grid.constraint} count={grid.constraintCount} " +
                          $"padding=({grid.padding.left},{grid.padding.right},{grid.padding.top},{grid.padding.bottom}) " +
                          $"align={grid.childAlignment}");
        }
        else sb.AppendLine("GridLayout: NO HAY GridLayoutGroup en el contenedor");

        sb.AppendLine($"Contenedor: size={contenedor.rect.size} anchoredPos={contenedor.anchoredPosition} " +
                      $"anchorsMin={contenedor.anchorMin} anchorsMax={contenedor.anchorMax}");

        int n = 0;
        foreach (RectTransform hijo in contenedor)
        {
            if (hijo.gameObject.activeSelf) n++;
            sb.AppendLine($"-- [{hijo.name}] activo={hijo.gameObject.activeSelf} " +
                          $"anchoredPos={hijo.anchoredPosition} size={hijo.rect.size}");
            if (hijo.gameObject.activeSelf) DetalleHijos(hijo, sb);
        }

        if (grid != null)
        {
            int cols = Mathf.Max(1, grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? grid.constraintCount : 2);
            int filasN = Mathf.CeilToInt(n / (float)cols);
            float alto = grid.padding.vertical + filasN * grid.cellSize.y +
                         Mathf.Max(0, filasN - 1) * grid.spacing.y;
            sb.AppendLine($"[ajuste] filas activas={n} columnas={cols} filasN={filasN} " +
                          $"alto necesario={alto} alto contenedor={contenedor.rect.height} " +
                          $"{(alto > contenedor.rect.height ? "<<< NO ENTRA" : "entra")}");
        }

        // Resto del panel
        var raiz = shopRt;
        foreach (var t in raiz.GetComponentsInChildren<RectTransform>(true))
        {
            if (t == shopRt || t == contenedor) continue;
            if (EstaDentroDe(t, contenedor)) continue;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp == null) continue;
            sb.AppendLine($"[texto] '{tmp.text}' size={t.rect.size} " +
                          $"preferido={tmp.preferredWidth:F1}x{tmp.preferredHeight:F1} " +
                          $"autoSize={tmp.enableAutoSizing} fontSize={tmp.fontSize} " +
                          $"{(tmp.preferredWidth > t.rect.width + 0.5f ? "<<< TEXTO MAS ANCHO QUE SU CAJA" : "")}");
        }

        Debug.Log(sb.ToString());
    }

    /// <summary>Mide la etiqueta COMPRAR contra la caja de su boton.</summary>
    private static void DetalleBotones(ShopUI shop, StringBuilder sb)
    {
        int i = 0;
        foreach (var fila in shop.GetComponentsInChildren<ShopItemUI>(true))
        {
            if (!fila.gameObject.activeSelf) continue;
            var filaRt = fila.transform as RectTransform;

            foreach (var c in filaRt.GetComponentsInChildren<RectTransform>(true))
            {
                if (c == filaRt) continue;
                var tmp = c.GetComponent<TMP_Text>();
                if (tmp == null) continue;
                var padre = c.parent as RectTransform;
                sb.AppendLine($"[{i}] fila='{fila.name}' texto='{tmp.text}' " +
                              $"caja={c.rect.size} pos={c.anchoredPosition} " +
                              $"preferido={tmp.preferredWidth:F1}x{tmp.preferredHeight:F1} " +
                              $"padre='{(padre != null ? padre.name : "?")}' " +
                              $"cajaPadre={(padre != null ? padre.rect.size.ToString() : "?")}" +
                              (tmp.preferredWidth > c.rect.width + 0.5f || tmp.preferredHeight > c.rect.height + 0.5f
                                  ? "  <<< NO ENTRA EN SU CAJA" : "  ok"));
            }
            i++;
        }
    }

    /// <summary>Mide titulo, monedas, aviso y volver contra el panel.</summary>
    private static void DetalleCabecera(ShopUI shop, StringBuilder sb)
    {
        var shopRt = shop.transform as RectTransform;
        foreach (var t in shopRt.GetComponentsInChildren<RectTransform>(true))
        {
            if (EstaDentroDe(t, shopRt) == false) continue;
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp == null) continue;
            // Solo los que NO estan dentro de una fila.
            bool enFila = false;
            var p = t.parent;
            while (p != null && p != shopRt) { if (p.GetComponent<ShopItemUI>() != null) { enFila = true; break; } p = p.parent; }
            if (enFila) continue;

            sb.AppendLine($"[cab] '{tmp.text}' rect={t.rect.size} pos={t.anchoredPosition} " +
                          $"anchors=({t.anchorMin.x:F2},{t.anchorMin.y:F2})-({t.anchorMax.x:F2},{t.anchorMax.y:F2}) " +
                          $"pivote=({t.pivot.x:F2},{t.pivot.y:F2}) alineacion={tmp.alignment} " +
                          $"preferido={tmp.preferredWidth:F1}x{tmp.preferredHeight:F1} fontSize={tmp.fontSize}");
        }
    }

    private static bool EstaDentroDe(Transform hijo, Transform padre)
    {
        var p = hijo.parent;
        while (p != null) { if (p == padre) return true; p = p.parent; }
        return false;
    }

    private static void DetalleHijos(RectTransform fila, StringBuilder sb)
    {
        var marco = new Bounds(Vector3.zero, new Vector3(fila.rect.width, fila.rect.height, 0f));

        foreach (RectTransform c in fila)
        {
            var b = RectTransformUtility.CalculateRelativeRectTransformBounds(fila, c);
            bool fuera = b.min.x < marco.min.x - 0.5f || b.max.x > marco.max.x + 0.5f
                      || b.min.y < marco.min.y - 0.5f || b.max.y > marco.max.y + 0.5f;

            sb.AppendLine($"     .{c.name} pos={c.anchoredPosition} size={c.rect.size} " +
                          $"min=({b.min.x:F1},{b.min.y:F1}) max=({b.max.x:F1},{b.max.y:F1})" +
                          (fuera ? "  <<< SE SALE" : ""));

            var tmp = c.GetComponent<TMP_Text>();
            if (tmp != null)
                sb.AppendLine($"        TMP '{tmp.text}' caja={c.rect.size} " +
                              $"preferido={tmp.preferredWidth:F1}x{tmp.preferredHeight:F1} " +
                              $"fontSize={tmp.fontSize} autoSize={tmp.enableAutoSizing}" +
                              (tmp.preferredWidth > c.rect.width + 0.5f ? "  <<< TEXTO MAS ANCHO QUE SU CAJA" : ""));
        }
    }
}
#endif
