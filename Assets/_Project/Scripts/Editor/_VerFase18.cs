#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SONDA TEMPORAL — mide el scroll del panel de jugadores con el layout ya
/// recalculado y comprueba que la grilla se pueda deslizar de verdad.
/// action = "scroll".
/// </summary>
public static class _VerFase18
{
    public static void Execute(string action)
    {
        if (!Application.isPlaying) { Debug.LogError("[Ver18] solo en Play Mode"); return; }

        UIManager.Instance.OpenCharacters();

        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        if (panel == null) { Debug.LogError("[Ver18] no encontre el panel"); return; }

        // Forzar el reacomodo: el ContentSizeFitter no corre en el mismo frame.
        Canvas.ForceUpdateCanvases();
        var grid = panel.transform.Find("ScrollView/Viewport/Grid") as RectTransform;
        if (grid != null) LayoutRebuilder.ForceRebuildLayoutImmediate(grid);
        Canvas.ForceUpdateCanvases();

        var sr = panel.GetComponentInChildren<ScrollRect>(true);
        if (sr == null) { Debug.LogError("[Ver18] no hay ScrollRect"); return; }

        Debug.Log("[Ver18] scroll vertical=" + sr.vertical +
                  " horizontal=" + sr.horizontal +
                  " movimiento=" + sr.movementType);
        Debug.Log("[Ver18] alto contenido=" + sr.content.rect.height +
                  " alto ventana=" + sr.viewport.rect.height +
                  " | hace falta deslizar=" + (sr.content.rect.height > sr.viewport.rect.height));

        // Posicion de la ultima tarjeta antes y despues de bajar del todo.
        var ultima = grid.GetChild(grid.childCount - 1) as RectTransform;
        Debug.Log("[Ver18] ultima tarjeta '" + ultima.name + "' y=" + ultima.anchoredPosition.y +
                  " (negativo = mas abajo)");

        sr.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
        Debug.Log("[Ver18] tras bajar del todo: verticalNormalizedPosition=" + sr.verticalNormalizedPosition +
                  " contenido movido a y=" + sr.content.anchoredPosition.y);

        sr.verticalNormalizedPosition = 1f;
        Canvas.ForceUpdateCanvases();
        Debug.Log("[Ver18] tras volver arriba: contenido y=" + sr.content.anchoredPosition.y);
    }
}
#endif
