#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// SONDA TEMPORAL — corrige dos defectos medidos en la tienda:
///   1. La etiqueta COMPRAR no entra en su boton (110 px en una caja de 100).
///      Se ensancha el boton a 124 y se corre el costo a la izquierda.
///   2. El Aviso se superpone con la primera fila y ademas se dibuja detras de
///      ella. Se lo sube para que quede debajo del encabezado.
/// Despues borra los clones viejos y los vuelve a crear.
/// action = "aplicar" | "medir".
/// </summary>
public static class _FixTiendaLayout
{
    private static readonly BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void Execute(string action)
    {
        var shop = Object.FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        if (shop == null) { Debug.LogError("[Fix] no encontre ShopUI"); return; }

        var panel = shop.transform as RectTransform;
        var grid  = panel.Find("Grid") as RectTransform;
        if (grid == null) { Debug.LogError("[Fix] no encontre Grid"); return; }

        if (action == "aplicar")
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== FIX TIENDA =====");

            // Aviso: subirlo para que no invada la grilla.
            var aviso = panel.Find("Aviso") as RectTransform;
            if (aviso != null)
            {
                aviso.anchorMin = new Vector2(0f, 1f);
                aviso.anchorMax = new Vector2(1f, 1f);
                aviso.pivot     = new Vector2(0.5f, 1f);
                aviso.sizeDelta = new Vector2(-200f, 34f);
                aviso.anchoredPosition = new Vector2(0f, -60f);
                sb.AppendLine($"Aviso -> pos=(0,-60) size={aviso.sizeDelta}");
            }

            // Plantilla: boton mas ancho y costo corrido a la izquierda.
            var tpl = grid.Find("Item_Template") as RectTransform;
            if (tpl == null) { Debug.LogError("[Fix] no encontre Item_Template"); return; }

            // Nombre: bajarlo a 24 para que no toque el costo en los nombres largos.
            var nombreTr = tpl.Find("Nombre");
            var nombreTmp = nombreTr != null ? nombreTr.GetComponent<TMP_Text>() : null;
            if (nombreTmp != null)
            {
                nombreTmp.fontSize = 24f;
                sb.AppendLine($"Nombre plantilla -> fontSize={nombreTmp.fontSize}");
            }

            var boton = tpl.Find("Boton") as RectTransform;
            if (boton != null)
            {
                boton.sizeDelta        = new Vector2(124f, 46f);
                boton.anchoredPosition = new Vector2(-10f, 0f);
                sb.AppendLine($"Boton plantilla -> size={boton.sizeDelta} pos={boton.anchoredPosition}");
            }

            var costo = tpl.Find("Costo") as RectTransform;
            if (costo != null)
            {
                costo.sizeDelta        = new Vector2(100f, 34f);
                costo.anchoredPosition = new Vector2(-144f, 0f);
                sb.AppendLine($"Costo plantilla -> pos={costo.anchoredPosition}");
            }

            // Borrar los clones viejos (todo hijo de Grid que no sea la plantilla).
            var clones = new List<GameObject>();
            foreach (RectTransform h in grid)
                if (h != tpl && h.name.StartsWith("Item_")) clones.Add(h.gameObject);

            foreach (var c in clones) Object.DestroyImmediate(c);
            sb.AppendLine($"clones borrados={clones.Count}");

            // Reiniciar el estado interno de ShopUI y reconstruir.
            typeof(ShopUI).GetField("_construido", F)?.SetValue(shop, false);
            var filas = typeof(ShopUI).GetField("_filas", F)?.GetValue(shop) as List<ShopItemUI>;
            filas?.Clear();

            typeof(ShopUI).GetMethod("Construir", F)?.Invoke(shop, null);
            shop.Refrescar();

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
            Canvas.ForceUpdateCanvases();

            var nuevas = panel.GetComponentsInChildren<ShopItemUI>(true);
            int activas = 0;
            foreach (var f in nuevas) if (f.gameObject.activeSelf) activas++;
            sb.AppendLine($"filas reconstruidas totales={nuevas.Length} activas={activas}");

            EditorUtility.SetDirty(shop);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            sb.AppendLine("escena guardada");

            sb.AppendLine(Medir(shop, grid));
            Debug.Log(sb.ToString());
            return;
        }

        Debug.Log("===== MEDICION TIENDA =====\n" + Medir(shop, grid));
    }

    /// <summary>Devuelve un texto con lo que se sale de su caja.</summary>
    private static string Medir(ShopUI shop, RectTransform grid)
    {
        var sb = new StringBuilder();
        var panel = shop.transform as RectTransform;
        sb.AppendLine($"panel={panel.rect.size}");

        var aviso = panel.Find("Aviso") as RectTransform;
        if (aviso != null)
        {
            var bAviso = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, aviso);
            var bGrid  = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, grid);
            sb.AppendLine($"Aviso min.y={bAviso.min.y:F1} max.y={bAviso.max.y:F1} | " +
                          $"Grid max.y={bGrid.max.y:F1} | " +
                          $"{(bAviso.min.y < bGrid.max.y + 0.5f ? "<<< PISA LA GRILLA" : "no pisa la grilla")}");
        }

        var volver = panel.Find("BotonVolver") as RectTransform;
        if (volver != null)
        {
            var bV = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, volver);
            var bG = RectTransformUtility.CalculateRelativeRectTransformBounds(panel, grid);
            sb.AppendLine($"BotonVolver min.y={bV.min.y:F1} max.y={bV.max.y:F1} size={volver.rect.size} | " +
                          $"Grid min.y={bG.min.y:F1} | " +
                          $"{(bV.max.y > bG.min.y - 0.5f ? "<<< PISA LA GRILLA" : "no pisa la grilla")}");
        }

        foreach (var fila in shop.GetComponentsInChildren<ShopItemUI>(true))
        {
            if (!fila.gameObject.activeSelf) continue;
            var filaRt = fila.transform as RectTransform;

            // Choque entre el nombre (alineado a la izquierda de su caja) y el
            // costo (alineado a la derecha de la suya).
            var nomTr = filaRt.Find("Nombre") as RectTransform;
            var cosTr = filaRt.Find("Costo") as RectTransform;
            var nomTmp = nomTr != null ? nomTr.GetComponent<TMP_Text>() : null;
            var cosTmp = cosTr != null ? cosTr.GetComponent<TMP_Text>() : null;
            if (nomTmp != null && cosTmp != null)
            {
                var bn = RectTransformUtility.CalculateRelativeRectTransformBounds(filaRt, nomTr);
                var bc = RectTransformUtility.CalculateRelativeRectTransformBounds(filaRt, cosTr);
                float nomFin = bn.min.x + nomTmp.preferredWidth;
                float cosIni = bc.max.x - cosTmp.preferredWidth;
                sb.AppendLine($"{fila.name}: nombre termina en {nomFin:F1}, costo arranca en {cosIni:F1} " +
                              (nomFin > cosIni - 4f ? "<<< SE TOCAN" : "ok"));
            }

            foreach (var t in filaRt.GetComponentsInChildren<RectTransform>(true))
            {
                var tmp = t.GetComponent<TMP_Text>();
                if (tmp == null) continue;
                bool noEntra = tmp.preferredWidth > t.rect.width + 0.5f ||
                               tmp.preferredHeight > t.rect.height + 0.5f;
                if (!noEntra) continue;
                sb.AppendLine($"{fila.name} / '{tmp.text}' caja={t.rect.size} " +
                              $"preferido={tmp.preferredWidth:F1}x{tmp.preferredHeight:F1} " +
                              "<<< NO ENTRA");
            }
        }

        foreach (var t in panel.GetComponentsInChildren<RectTransform>(true))
        {
            var tmp = t.GetComponent<TMP_Text>();
            if (tmp == null) continue;
            bool enFila = false;
            var p = t.parent;
            while (p != null && p != panel) { if (p.GetComponent<ShopItemUI>() != null) { enFila = true; break; } p = p.parent; }
            if (enFila) continue;
            if (tmp.preferredWidth <= t.rect.width + 0.5f) continue;
            sb.AppendLine($"panel / '{tmp.text}' caja={t.rect.size} preferido={tmp.preferredWidth:F1} <<< NO ENTRA");
        }

        sb.AppendLine("fin");
        return sb.ToString();
    }
}
#endif
