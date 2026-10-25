using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Todo lo que se guarda en disco. JsonUtility necesita una clase con campos
/// públicos y serializables, por eso las mejoras van en dos listas paralelas
/// (id / nivel) en lugar de un Dictionary.
/// </summary>
[Serializable]
public class SaveData
{
    public int coins;
    public int bestScore;
    public int currentLevel = 1;

    public List<string> upgradeIds     = new List<string>();
    public List<int>    upgradeNiveles = new List<int>();

    public string personajeEquipado = "";

    // Ids de los jugadores que ya se compraron. El jugador generico no figura aca:
    // si la lista esta vacia y personajeEquipado esta vacio, se juega con el generico.
    public List<string> personajesComprados = new List<string>();

    // ─── Ajustes de sonido y pantalla (Fase F) ────────────────────────────────
    // volumen y brillo van de 0 a 1. "silenciado" es un interruptor aparte:
    // guarda el volumen elegido y lo aplica como 0 mientras esté prendido.
    public float volumen = SaveManager.VolumenPorDefecto;
    public float brillo  = SaveManager.BrilloPorDefecto;
    public bool  silenciado;

    // True recién cuando el jugador movió algún ajuste. Un guardado de antes de
    // la Fase F no tiene estos campos (quedan en 0), así que con esta marca se
    // detecta y se completan con los valores por defecto en lugar de arrancar
    // con el juego mudo y la pantalla negra.
    public bool ajustesGuardados;
}

/// <summary>
/// Guardado persistente en JSON dentro de Application.persistentDataPath
/// (en Windows: %USERPROFILE%\AppData\LocalLow\&lt;empresa&gt;\&lt;producto&gt;\mundialrunner_save.json).
///
/// Es una clase estática: se usa como SaveManager.Data, SaveManager.Guardar(), etc.
/// La primera vez que se ejecuta migra las monedas y el mejor puntaje que
/// estaban en PlayerPrefs para no perder el progreso de las pruebas viejas.
///
/// Carpeta: Assets/_Project/Scripts/Managers/
/// </summary>
public static class SaveManager
{
    const string NombreArchivo = "mundialrunner_save.json";

    /// <summary>Volumen general con el que arranca el juego la primera vez.</summary>
    public const float VolumenPorDefecto = 0.8f;

    /// <summary>Brillo con el que arranca el juego: 1 = sin velo de oscurecido.</summary>
    public const float BrilloPorDefecto = 1f;

    // Claves viejas de PlayerPrefs, sólo para la migración inicial.
    const string KeyCoinsVieja = "coins";
    const string KeyBestVieja  = "bestScore";
    const string KeyLevelVieja = "currentLevel";

    static SaveData _data;
    static bool _cargado;

    public static SaveData Data
    {
        get
        {
            if (!_cargado) Cargar();
            return _data;
        }
    }

    public static string RutaArchivo => Path.Combine(Application.persistentDataPath, NombreArchivo);

    // ─── Carga / guardado ─────────────────────────────────────────────────────

    public static void Cargar()
    {
        _cargado = true;
        _data = null;

        try
        {
            if (File.Exists(RutaArchivo))
            {
                string json = File.ReadAllText(RutaArchivo);
                if (!string.IsNullOrWhiteSpace(json))
                    _data = JsonUtility.FromJson<SaveData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] No pude leer la partida ({e.Message}). Arranco de cero.");
        }

        if (_data == null)
        {
            _data = new SaveData();

            // Migración desde las PlayerPrefs de las primeras fases.
            _data.coins        = PlayerPrefs.GetInt(KeyCoinsVieja, 0);
            _data.bestScore    = PlayerPrefs.GetInt(KeyBestVieja, 0);
            _data.currentLevel = PlayerPrefs.GetInt(KeyLevelVieja, 1);

            if (_data.coins > 0 || _data.bestScore > 0 || _data.currentLevel > 1)
            {
                Debug.Log("[SaveManager] Migré el progreso de PlayerPrefs al archivo JSON.");
                Guardar();
            }
        }

        if (_data.upgradeIds == null)     _data.upgradeIds     = new List<string>();
        if (_data.upgradeNiveles == null) _data.upgradeNiveles = new List<int>();
        if (_data.personajesComprados == null) _data.personajesComprados = new List<string>();
        if (_data.currentLevel < 1)       _data.currentLevel   = 1;

        // Ajustes de sonido y brillo (Fase F). Si el guardado es anterior a la
        // fase, no trae estos campos y hay que dejarlos en los valores por
        // defecto.
        if (!_data.ajustesGuardados)
        {
            _data.volumen    = VolumenPorDefecto;
            _data.brillo     = BrilloPorDefecto;
            _data.silenciado = false;
        }

        _data.volumen = Mathf.Clamp01(_data.volumen);
        _data.brillo  = Mathf.Clamp01(_data.brillo);
    }

    public static void Guardar()
    {
        if (!_cargado) Cargar();

        try
        {
            string json = JsonUtility.ToJson(_data, true);
            File.WriteAllText(RutaArchivo, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] No pude guardar la partida: {e.Message}");
        }
    }

    // ─── Mejoras ──────────────────────────────────────────────────────────────

    public static int GetNivel(string id)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        int i = Data.upgradeIds.IndexOf(id);
        return i >= 0 ? Data.upgradeNiveles[i] : 0;
    }

    public static void SetNivel(string id, int nivel)
    {
        if (string.IsNullOrEmpty(id)) return;

        int i = Data.upgradeIds.IndexOf(id);
        if (i >= 0) Data.upgradeNiveles[i] = Mathf.Max(0, nivel);
        else
        {
            Data.upgradeIds.Add(id);
            Data.upgradeNiveles.Add(Mathf.Max(0, nivel));
        }

        Guardar();
    }

    public static int GetNivel(UpgradeId id) => GetNivel(id.ToString());
    public static void SetNivel(UpgradeId id, int nivel) => SetNivel(id.ToString(), nivel);

    // ─── Utilidades ───────────────────────────────────────────────────────────

    /// <summary>Borra la partida (útil para las pruebas).</summary>
    public static void BorrarPartida()
    {
        _data = null;
        _cargado = false;

        try { if (File.Exists(RutaArchivo)) File.Delete(RutaArchivo); }
        catch (Exception e) { Debug.LogWarning($"[SaveManager] No pude borrar el archivo: {e.Message}"); }

        Cargar();
    }
}
