#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SONDA TEMPORAL — comprueba que tres partidas buenas alcancen para Messi y
/// que el panel se actualice al tener monedas. Deja el guardado como estaba.
/// action = "probar".
/// </summary>
public static class _TestEconomia
{
    private static void Paso(string texto) => Debug.Log("[Eco] " + texto);

    public static void Execute(string action)
    {
        if (!Application.isPlaying) { Debug.LogError("[Eco] solo en Play Mode"); return; }

        var cm = CharacterManager.Instance;
        var rm = RunnerGameManager.Instance;
        if (cm == null || rm == null) { Debug.LogError("[Eco] faltan singletons"); return; }

        int monedasOriginal = GameData.Instance.Coins;
        string equipadoOriginal = SaveManager.Data.personajeEquipado;
        var compradosOriginal = new List<string>(SaveManager.Data.personajesComprados);

        var campoScore = typeof(RunnerGameManager).GetField("<Score>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Paso("monedas al entrar: " + monedasOriginal);

        int inicio = GameData.Instance.Coins;
        for (int i = 1; i <= 3; i++)
        {
            rm.StartGame();
            if (campoScore != null) campoScore.SetValue(rm, 660);

            int antes = GameData.Instance.Coins;
            rm.OnPlayerDied();
            Paso("partida " + i + " (puntaje 660): " + antes + " -> " + GameData.Instance.Coins +
                 " (pago " + (GameData.Instance.Coins - antes) + ")");
        }

        Paso("total ganado en 3 partidas: " + (GameData.Instance.Coins - inicio));
        var messi = cm.PorId("Messi");
        Paso("Messi cuesta " + messi.precio.ToString("N0") +
             " -> alcanza: " + cm.PuedeComprar(messi));

        // Panel con plata: los precios tienen que estar habilitados.
        UIManager.Instance.OpenCharacters();
        var panel = Object.FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
        Canvas.ForceUpdateCanvases();
        var grid = panel.transform.Find("ScrollView/Viewport/Grid") as RectTransform;
        if (grid != null) LayoutRebuilder.ForceRebuildLayoutImmediate(grid);

        for (int i = 0; i < grid.childCount; i++)
        {
            var h = grid.GetChild(i);
            if (!h.gameObject.activeSelf) continue;
            var precio = h.Find("Precio")?.GetComponent<TMP_Text>();
            var boton  = h.Find("Boton")?.GetComponent<Button>();
            var rotulo = h.Find("Boton/Label")?.GetComponent<TMP_Text>();
            Paso("  " + h.name + " precio='" + (precio != null ? precio.text : "?") +
                 "' color=" + (precio != null ? ColorUtility.ToHtmlStringRGB(precio.color) : "?") +
                 " boton='" + (rotulo != null ? rotulo.text : "?") +
                 "' interactable=" + (boton != null ? boton.interactable.ToString() : "?"));
        }

        // Comprar a Messi con lo ganado.
        var carta = grid.Find("Carta_Messi");
        var btn = carta != null ? carta.Find("Boton").GetComponent<Button>() : null;
        if (btn != null && btn.interactable)
        {
            int antes = GameData.Instance.Coins;
            btn.onClick.Invoke();
            Paso("compra de Messi: " + antes + " -> " + GameData.Instance.Coins +
                 " | equipado=" + (cm.Equipado != null ? cm.Equipado.id : "GENERICO"));
            var piel = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var preview = GameObject.Find("PlayerPreview")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Paso("camiseta jugador=" + (piel != null && piel.sharedMaterial != null ? piel.sharedMaterial.name : "NULL") +
                 " | camiseta menu=" + (preview != null && preview.sharedMaterial != null ? preview.sharedMaterial.name : "NULL"));
        }
        else
        {
            Paso("el boton de Messi no estaba habilitado");
        }

        // Restaurar.
        SaveManager.Data.personajeEquipado = equipadoOriginal;
        SaveManager.Data.coins = monedasOriginal;
        SaveManager.Data.personajesComprados = compradosOriginal;
        SaveManager.Guardar();
        cm.CargarEquipado();
        cm.AplicarModelo(GameObject.FindGameObjectWithTag("Player"));
        UIManager.Instance.OpenMainMenu();

        Paso("restaurado: monedas=" + GameData.Instance.Coins +
             " equipado='" + (cm.Equipado != null ? cm.Equipado.id : "GENERICO") +
             "' comprados=" + SaveManager.Data.personajesComprados.Count);
    }
}
#endif
