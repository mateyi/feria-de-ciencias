using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json.Linq;
using System.Text;

/// <summary>
/// CONSTRUCTOR TEMPORAL de la Fase B: nombres de las etapas del Mundial y
/// carteles de etapa en el HUD y en el menú.
///
/// Acciones (por argumento "accion"):
///   aplicar -> renombra las 6 etapas, carga los apodos de los jugadores, pone
///              el cartel de etapa en el HUD y el nombre de la etapa en el menú,
///              y guarda la escena. Es idempotente: se puede correr dos veces.
///   ver     -> informa cómo quedó todo, sin tocar nada.
///   reset   -> deja el torneo en la primera etapa (limpia un nivel de prueba
///              viejo que haya quedado guardado).
/// </summary>
public static class _FaseBEtapas
{
    const string RutaConfig = "Assets/_Project/Data/LevelConfig.asset";
    const string RutaPersonajes = "Assets/_Project/Data/Personajes";

    // Nombres de las etapas, en el orden en que se juegan.
    static readonly string[] Etapas =
    {
        "Entrenamiento",
        "Fase de grupos",
        "Octavos de final",
        "Cuartos de final",
        "Semifinal",
        "Final"
    };

    public static void Execute(JObject args)
    {
        string accion = args != null && args["accion"] != null
            ? args["accion"].ToString()
            : "aplicar";

        if (accion == "ver")   { Ver();   return; }
        if (accion == "reset") { Reset(); return; }

        Aplicar();
    }

    // ─── Aplicar ──────────────────────────────────────────────────────────────

    static void Aplicar()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE B: etapas del torneo ===");

        log.AppendLine(RenombrarEtapas());
        log.AppendLine(CargarApodos());

        // ── Carteles en la escena ────────────────────────────────────────────
        Scene escena = SceneManager.GetActiveScene();
        Transform canvas = BuscarCanvas();

        if (canvas == null)
        {
            log.AppendLine("Canvas: NO ENCONTRADO (no toqué los carteles)");
            Debug.Log(log.ToString());
            return;
        }

        log.AppendLine(PonerMenuEtapa(canvas));
        log.AppendLine(PonerEtapaHUD(canvas));

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        log.AppendLine("Escena guardada.");

        Debug.Log(log.ToString());
    }

    // ─── 1. Nombres de las etapas en el LevelConfig ───────────────────────────

    static string RenombrarEtapas()
    {
        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaConfig);
        if (config == null) return "LevelConfig: NO ENCONTRADO";
        if (config.niveles == null || config.niveles.Count == 0) return "LevelConfig sin niveles";

        // Ordenamos por número de nivel para que la etapa N sea Etapas[N-1].
        config.niveles.Sort((a, b) => a.nivel.CompareTo(b.nivel));

        int cambiados = 0;
        for (int i = 0; i < config.niveles.Count && i < Etapas.Length; i++)
        {
            NivelObstaculos n = config.niveles[i];
            if (n == null) continue;

            if (n.nombre != Etapas[i]) { n.nombre = Etapas[i]; cambiados++; }
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        return $"etapas renombradas: {cambiados} (total {config.niveles.Count}, final = nivel {config.NivelFinal})";
    }

    // ─── 2. Apodos de los jugadores ───────────────────────────────────────────

    static string CargarApodos()
    {
        if (!AssetDatabase.IsValidFolder(RutaPersonajes)) return "carpeta de personajes: NO ENCONTRADA";

        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { RutaPersonajes });
        int cargados = 0;

        foreach (string guid in guids)
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var personaje = AssetDatabase.LoadAssetAtPath<CharacterData>(ruta);
            if (personaje == null) continue;

            // El apodo es como se lo conoce en la cancha (el apellido). Se usa
            // en la frase de campeón: "SALISTE CAMPEON DEL MUNDO CON MESSI".
            if (string.IsNullOrEmpty(personaje.apodo))
            {
                personaje.apodo = personaje.id;
                EditorUtility.SetDirty(personaje);
                cargados++;
            }
        }

        AssetDatabase.SaveAssets();
        return $"apodos cargados: {cargados} de {guids.Length} jugadores";
    }

    // ─── 3. Cartel de etapa en el menú principal ──────────────────────────────

    static string PonerMenuEtapa(Transform canvas)
    {
        Transform cartel = canvas.Find("MainMenuPanel/nivel");
        if (cartel == null) return "Canvas/MainMenuPanel/nivel: NO ENCONTRADO";

        var texto = cartel.GetComponent<TMP_Text>();
        if (texto == null) return "el cartel del menú no tiene TMP_Text";

        // Texto de edición: en Play lo reescribe MenuEtapaUI con la etapa real.
        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaConfig);
        if (config != null) texto.text = config.NombreDeEtapa(SaveManager.Data.currentLevel);

        var script = cartel.GetComponent<MenuEtapaUI>();
        bool nuevo = script == null;
        if (nuevo) cartel.gameObject.AddComponent<MenuEtapaUI>();

        return $"cartel del menú: {(nuevo ? "MenuEtapaUI agregado" : "MenuEtapaUI ya estaba")} " +
               $"texto='{texto.text}'";
    }

    // ─── 4. Cartel de etapa en el HUD de carrera ──────────────────────────────

    static string PonerEtapaHUD(Transform canvas)
    {
        var hud = canvas.GetComponentInChildren<GameplayHUD>(true);
        if (hud == null) return "GameplayHUD: NO ENCONTRADO";

        const string nombre = "EtapaHUD";

        Transform existente = hud.transform.Find(nombre);
        if (existente != null)
        {
            var ya = existente.GetComponent<TMP_Text>();
            AsignarEtapaText(hud, ya);
            return $"cartel del HUD: ya existía ('{nombre}', texto='{ya?.text}')";
        }

        // Fuente: la misma que usa el marcador, para que no cambie el estilo.
        TMP_FontAsset fuente = null;
        foreach (var t in hud.GetComponentsInChildren<TMP_Text>(true))
            if (t != null && t.font != null) { fuente = t.font; break; }

        var go = new GameObject(nombre, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(hud.transform, false);

        // Banda libre de la barra superior: las monedas ocupan 0.01-0.30 y el
        // puntaje 0.70-0.99, así que el centro queda para la etapa.
        rt.anchorMin        = new Vector2(0.31f, 0.905f);
        rt.anchorMax        = new Vector2(0.69f, 0.995f);
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(-8f, -4f);
        rt.localScale       = Vector3.one;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) tmp.font = fuente;

        tmp.fontSize      = 30f;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.overflowMode  = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;                 // no tapa clics
        tmp.color         = new Color(1f, 0.95f, 0.55f, 1f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaConfig);
        tmp.text = config != null ? config.NombreDeEtapa(SaveManager.Data.currentLevel) : "";

        AsignarEtapaText(hud, tmp);

        return $"cartel del HUD creado ('{nombre}', texto='{tmp.text}')";
    }

    /// <summary>Engancha el TMP_Text al campo privado etapaText del GameplayHUD.</summary>
    static void AsignarEtapaText(GameplayHUD hud, TMP_Text texto)
    {
        var so = new SerializedObject(hud);
        SerializedProperty prop = so.FindProperty("etapaText");
        if (prop == null) return;

        prop.objectReferenceValue = texto;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ─── Ver ──────────────────────────────────────────────────────────────────

    static void Ver()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE B: cómo quedaron las etapas ===");

        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaConfig);
        if (config == null) { log.AppendLine("LevelConfig: NO ENCONTRADO"); Debug.Log(log.ToString()); return; }

        log.AppendLine($"etapas configuradas = {config.niveles.Count}   final = nivel {config.NivelFinal}");

        foreach (var n in config.niveles)
        {
            if (n == null) continue;
            log.AppendLine($"  nivel {n.nivel}: '{config.NombreDeEtapa(n.nivel)}' " +
                           $"(siguiente: '{config.EtapaSiguiente(n.nivel)}') EsFinal={config.EsFinal(n.nivel)}");
        }

        log.AppendLine($"nivel guardado actual = {SaveManager.Data.currentLevel} " +
                       $"-> etapa '{config.NombreDeEtapa(SaveManager.Data.currentLevel)}'");

        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { RutaPersonajes });
        foreach (string guid in guids)
        {
            var p = AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null) log.AppendLine($"  jugador {p.id}: apodo='{p.apodo}'");
        }

        Transform canvas = BuscarCanvas();
        if (canvas == null) { log.AppendLine("Canvas: NO ENCONTRADO"); Debug.Log(log.ToString()); return; }

        Transform cartel = canvas.Find("MainMenuPanel/nivel");
        if (cartel != null)
        {
            var t = cartel.GetComponent<TMP_Text>();
            log.AppendLine($"menú: MenuEtapaUI={(cartel.GetComponent<MenuEtapaUI>() != null)} texto='{t?.text}'");
        }
        else log.AppendLine("menú: cartel NO ENCONTRADO");

        var hud = canvas.GetComponentInChildren<GameplayHUD>(true);
        if (hud != null)
        {
            Transform etapa = hud.transform.Find("EtapaHUD");
            var tmp = etapa != null ? etapa.GetComponent<TMP_Text>() : null;
            log.AppendLine($"HUD: EtapaHUD={(etapa != null)} texto='{tmp?.text}' fuente={(tmp != null && tmp.font != null ? tmp.font.name : "SIN FUENTE")}");
            log.AppendLine($"HUD: rect={tmp?.rectTransform.rect.ToString() ?? "(sin rect)"}");

            var so = new SerializedObject(hud);
            SerializedProperty prop = so.FindProperty("etapaText");
            log.AppendLine($"HUD: campo etapaText enganchado={(prop != null && prop.objectReferenceValue != null)}");
        }
        else log.AppendLine("HUD: GameplayHUD NO ENCONTRADO");

        Debug.Log(log.ToString());
    }

    // ─── Reset del torneo ─────────────────────────────────────────────────────

    static void Reset()
    {
        int antes = SaveManager.Data.currentLevel;
        var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaConfig);

        SaveManager.Data.currentLevel = 1;
        SaveManager.Guardar();

        // Los carteles se refrescan solos en Play (MenuEtapaUI y StartGame); acá
        // los dejamos lindos también en modo edición.
        Transform canvas = BuscarCanvas();

        Transform cartel = canvas != null ? canvas.Find("MainMenuPanel/nivel") : null;
        var texto = cartel != null ? cartel.GetComponent<TMP_Text>() : null;
        if (texto != null && config != null) texto.text = config.NombreDeEtapa(1);

        var hud = canvas != null ? canvas.GetComponentInChildren<GameplayHUD>(true) : null;
        Transform etapa = hud != null ? hud.transform.Find("EtapaHUD") : null;
        var textoHud = etapa != null ? etapa.GetComponent<TMP_Text>() : null;
        if (textoHud != null && config != null) textoHud.text = config.NombreDeEtapa(1);

        if (canvas != null)
        {
            Scene escena = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
        }

        Debug.Log($"[FASE B] torneo reiniciado: nivel guardado {antes} -> 1 " +
                  $"('{config?.NombreDeEtapa(1) ?? "Entrenamiento"}')");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Busca el Canvas de la UI aunque esté inactivo.</summary>
    static Transform BuscarCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
            if (c != null && c.name == "Canvas") return c.transform;

        return canvases.Length > 0 ? canvases[0].transform : null;
    }
}
