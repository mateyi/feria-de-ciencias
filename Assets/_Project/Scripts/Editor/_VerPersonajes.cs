#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SONDA TEMPORAL — abre el panel de personajes y fuerza el reacomodo de la
/// grilla, para poder sacar una captura.
/// action = "abrir".
/// </summary>
public static class _VerPersonajes
{
    public static void Execute(string action)
    {
        var ui = UIManager.Instance;
        if (ui == null)
        {
            // Tras un domain reload el singleton puede quedar nulo: lo buscamos.
            ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            Debug.Log("[VerPersonajes] UIManager.Instance nulo, encontrado por tipo: " + (ui != null));
        }
        if (ui == null) { Debug.LogError("[VerPersonajes] no hay UIManager en la escena"); return; }

        ui.OpenCharacters();

        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (panel == null) { Debug.LogError("[VerPersonajes] panel nulo"); return; }

        Canvas.ForceUpdateCanvases();
        var grid = panel.transform.Find("Grid") as RectTransform;
        if (grid != null) LayoutRebuilder.ForceRebuildLayoutImmediate(grid);

        // Reportamos donde quedo cada tarjeta para comprobar que no se pisan.
        if (grid != null)
        {
            for (int i = 0; i < grid.childCount; i++)
            {
                var h = grid.GetChild(i);
                if (!h.gameObject.activeSelf) continue;
                var rt = h as RectTransform;
                Debug.Log("[VerPersonajes] " + h.name + " pos=" + rt.anchoredPosition +
                          " size=" + rt.rect.size);
            }
        }

        Debug.Log("[VerPersonajes] panel abierto, grid=" + (grid != null) +
                  " panelActivo=" + panel.gameObject.activeInHierarchy);
    }
}
#endif
