#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SONDA TEMPORAL — compra y equipa un jugador para poder mirar su modelo 3D
/// en la vista de juego, y despues restaura el guardado.
/// action = "Messi" | "Cristiano" | "Neymar" | "Mbappe" | "Haaland" | "restaurar".
/// </summary>
public static class _VerModelo
{
    private static bool _guardado;
    private static string _equipadoOriginal;
    private static int _monedasOriginal;
    private static List<string> _compradosOriginal;

    public static void Execute(string action)
    {
        if (!Application.isPlaying) { Debug.LogError("[VerModelo] solo en Play Mode"); return; }

        var cm = CharacterManager.Instance;
        var rm = RunnerGameManager.Instance;
        if (cm == null || rm == null) { Debug.LogError("[VerModelo] faltan singletons"); return; }

        if (action == "restaurar")
        {
            if (!_guardado) { Debug.Log("[VerModelo] no habia nada que restaurar"); return; }

            SaveManager.Data.personajeEquipado = _equipadoOriginal;
            SaveManager.Data.coins = _monedasOriginal;
            SaveManager.Data.personajesComprados = _compradosOriginal;
            SaveManager.Guardar();
            cm.CargarEquipado();
            cm.AplicarModelo(GameObject.FindGameObjectWithTag("Player"));
            UIManager.Instance.OpenMainMenu();
            Time.timeScale = 1f;

            Debug.Log("[VerModelo] restaurado: monedas=" + GameData.Instance.Coins +
                      " equipado='" + (cm.Equipado != null ? cm.Equipado.id : "GENERICO") + "'");
            return;
        }

        if (!_guardado)
        {
            _equipadoOriginal  = SaveManager.Data.personajeEquipado;
            _monedasOriginal   = SaveManager.Data.coins;
            _compradosOriginal = new List<string>(SaveManager.Data.personajesComprados);
            _guardado = true;
        }

        var p = cm.PorId(action);
        if (p == null) { Debug.LogError("[VerModelo] no existe el jugador '" + action + "'"); return; }

        if (!cm.Posee(p))
        {
            GameData.Instance.AddCoins(p.precio + 1000);
            bool ok = cm.Comprar(p);
            Debug.Log("[VerModelo] compra de " + p.id + ": " + ok + " | monedas ahora " + GameData.Instance.Coins);
        }
        else
        {
            cm.Equipar(p);
        }

        rm.StartGame();
        Time.timeScale = 0f;   // congelo para poder mirar el modelo quieto

        var piel = GameObject.FindGameObjectWithTag("Player")?.GetComponentInChildren<SkinnedMeshRenderer>(true);
        var stats = PlayerStats.Instance;
        Debug.Log("[VerModelo] equipado=" + (cm.Equipado != null ? cm.Equipado.id : "GENERICO") +
                  " material=" + (piel != null && piel.sharedMaterial != null ? piel.sharedMaterial.name : "NULL") +
                  " stats vel=" + stats.speed + " hab=" + stats.skill + " pop=" + stats.popularity);
    }
}
#endif
