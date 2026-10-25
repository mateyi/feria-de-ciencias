#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Text;

/// <summary>
/// FASE 13 — Arma los carteles de decisión con iconos.
///
/// 1. Crea las carpetas Assets/_Project/Data/Decisiones y los 4 ScriptableObject
///    DecisionData (Powerade/Cerveza, Cama/Fiesta, Plato/Hamburguesa, Pesas/Sofá)
///    con sus iconos ya enganchados y sus consecuencias.
/// 2. Edita el prefab Segment_Decision: baja las flechas, crea un SpriteRenderer
///    por lado para el icono y deja el DecisionPoint apuntando a los carteles 3D
///    y a la lista de decisiones posibles.
///
/// Es idempotente: si los assets o los iconos ya existen, los reutiliza.
/// </summary>
public static class _Fase13Carteles
{
    const string CarpetaDatos  = "Assets/_Project/Data";
    const string CarpetaDecid  = "Assets/_Project/Data/Decisiones";
    const string CarpetaUI     = "Assets/_Project/UI";
    const string RutaPrefab    = "Assets/_Project/Prefabs/Segment_Decision.prefab";

    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Fase13] Armando carteles de decisión");

        // ── 1. Carpetas ─────────────────────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(CarpetaDatos))
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        if (!AssetDatabase.IsValidFolder(CarpetaDecid))
            AssetDatabase.CreateFolder(CarpetaDatos, "Decisiones");

        // ── 2. Sprites ──────────────────────────────────────────────────────
        Sprite S(string n) => AssetDatabase.LoadAssetAtPath<Sprite>($"{CarpetaUI}/{n}.png");

        Sprite powerade = S("icon_powerade");
        Sprite cerveza  = S("icon_cerveza");
        Sprite cama     = S("icon_cama");
        Sprite fiesta   = S("icon_fiesta");
        Sprite plato    = S("icon_plato_saludable");
        Sprite hambur   = S("icon_hamburguesa");
        Sprite pesas    = S("icon_pesas");
        Sprite sofa     = S("icon_sofa");

        sb.AppendLine($"  sprites: powerade={(powerade != null)} cerveza={(cerveza != null)} " +
                      $"cama={(cama != null)} fiesta={(fiesta != null)} " +
                      $"plato={(plato != null)} hamburguesa={(hambur != null)} " +
                      $"pesas={(pesas != null)} sofa={(sofa != null)}");

        // ── 3. DecisionData ─────────────────────────────────────────────────
        var d1 = CrearDecision("Decision_Powerade_Cerveza",
            "¿Qué tomás para recuperarte?",
            "Powerade", "Bebida isotónica: recuperás energía y ritmo.", true, powerade,
            new StatConsequence { energyDelta = 15f, speedDelta = 5f,
                                  description = "Hidratado y con energia." },
            "Cerveza", "Una cerveza bien fría con los amigos.", false, cerveza,
            new StatConsequence { energyDelta = -20f, skillDelta = -5f,
                                  description = "Te baja el rendimiento." });

        var d2 = CrearDecision("Decision_Cama_Fiesta",
            "¿Qué hacés el sábado a la noche?",
            "Dormir temprano", "Descansás para el partido del domingo.", true, cama,
            new StatConsequence { energyDelta = 20f, speedDelta = 3f,
                                  description = "Descansaste como un profesional." },
            "Salir de fiesta", "Luces, música y amigos hasta tarde.", false, fiesta,
            new StatConsequence { energyDelta = -15f, popularityDelta = 10f, skillDelta = -5f,
                                  description = "Te divertís pero llegás fundido." });

        var d3 = CrearDecision("Decision_Plato_Hamburguesa",
            "¿Qué almorzás antes de entrenar?",
            "Plato saludable", "Pollo, brócoli y ensalada.", true, plato,
            new StatConsequence { energyDelta = 12f, speedDelta = 5f,
                                  description = "Comida de deportista." },
            "Hamburguesa con papas", "Rápido, rico y pesado.", false, hambur,
            new StatConsequence { energyDelta = -8f, popularityDelta = 5f, skillDelta = -3f,
                                  description = "Rico pero te cae pesado." });

        var d4 = CrearDecision("Decision_Pesas_Sofa",
            "Queda una hora libre. ¿Qué hacés?",
            "Ir al gimnasio", "Pesas y trabajo físico.", true, pesas,
            new StatConsequence { speedDelta = 8f, skillDelta = 6f,
                                  description = "Más fuerza y más ritmo." },
            "Sofá y control remoto", "Maratón de series en el living.", false, sofa,
            new StatConsequence { energyDelta = 8f, skillDelta = -8f, speedDelta = -4f,
                                  description = "Descansás pero perdés ritmo." });

        var todas = new[] { d1, d2, d3, d4 };
        foreach (var d in todas) sb.AppendLine($"  decisión: {d.name}");

        // ── 4. Prefab Segment_Decision ──────────────────────────────────────
        var contenido = PrefabUtility.LoadPrefabContents(RutaPrefab);
        if (contenido == null) { Debug.LogError("[Fase13] No pude abrir el prefab."); return; }

        try
        {
            var punto = contenido.GetComponentInChildren<DecisionPoint>(true);
            if (punto == null) { Debug.LogError("[Fase13] El prefab no tiene DecisionPoint."); return; }

            Transform portalIzq = Buscar(contenido.transform, "Portal_IZQ");
            Transform portalDer = Buscar(contenido.transform, "Portal_DER");
            Transform arrowIzq  = Buscar(contenido.transform, "Arrow_IZQ");
            Transform arrowDer  = Buscar(contenido.transform, "Arrow_DER");

            if (portalIzq == null || portalDer == null)
            {
                Debug.LogError("[Fase13] No encontré Portal_IZQ / Portal_DER.");
                return;
            }

            // Las flechas bajan al pie del cartel para dejar libre la parte de
            // arriba, que es donde va el icono.
            if (arrowIzq != null) arrowIzq.localPosition = new Vector3(-2.90f, 1.05f, 11.80f);
            if (arrowDer != null) arrowDer.localPosition = new Vector3( 2.90f, 1.05f, 11.80f);

            // Iconos: un SpriteRenderer por lado, apenas delante del panel
            // (el panel está en z=12 y su marco en z=11.85).
            var iconoIzq = CrearIcono(contenido.transform, "Icono_Cartel_IZQ",
                                      new Vector3(-2.50f, 2.60f, 11.72f), powerade);
            var iconoDer = CrearIcono(contenido.transform, "Icono_Cartel_DER",
                                      new Vector3( 2.50f, 2.60f, 11.72f), cerveza);

            var rndIzq = portalIzq.GetComponent<Renderer>();
            var rndDer = portalDer.GetComponent<Renderer>();

            // El panel visible desde la pista es el MARCO (está 0,15 delante del
            // portal y es más grande), así que el color verde/rojo va sobre el
            // marco. Si se pinta el portal de atrás no se ve nada.
            var marcoIzq = Buscar(contenido.transform, "PortalFrame_IZQ");
            var marcoDer = Buscar(contenido.transform, "PortalFrame_DER");
            if (marcoIzq != null && marcoIzq.GetComponent<Renderer>() != null)
                rndIzq = marcoIzq.GetComponent<Renderer>();
            if (marcoDer != null && marcoDer.GetComponent<Renderer>() != null)
                rndDer = marcoDer.GetComponent<Renderer>();

            punto.ConfigurarCarteles(rndIzq, rndDer, iconoIzq, iconoDer, todas);

            PrefabUtility.SaveAsPrefabAsset(contenido, RutaPrefab);
            sb.AppendLine("  prefab Segment_Decision actualizado " +
                          $"(cartelIzq={(rndIzq != null)} cartelDer={(rndDer != null)} " +
                          $"iconos=OK opciones={todas.Length})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contenido);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(sb.ToString());
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static DecisionData CrearDecision(string nombre, string pregunta,
        string etiquetaIzq, string descIzq, bool izqPositiva, Sprite iconoIzq,
        StatConsequence consIzq,
        string etiquetaDer, string descDer, bool derPositiva, Sprite iconoDer,
        StatConsequence consDer)
    {
        string ruta = $"{CarpetaDecid}/{nombre}.asset";
        var data = AssetDatabase.LoadAssetAtPath<DecisionData>(ruta);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<DecisionData>();
            AssetDatabase.CreateAsset(data, ruta);
        }

        data.question         = pregunta;
        data.leftLabel        = etiquetaIzq;
        data.leftDescription  = descIzq;
        data.leftConsequence  = consIzq;
        data.leftIcon         = iconoIzq;
        data.leftIsPositive   = izqPositiva;
        data.leftColor        = izqPositiva ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.2f);

        data.rightLabel       = etiquetaDer;
        data.rightDescription = descDer;
        data.rightConsequence = consDer;
        data.rightIcon        = iconoDer;
        data.rightIsPositive  = derPositiva;
        data.rightColor       = derPositiva ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.3f, 0.2f);

        EditorUtility.SetDirty(data);
        return data;
    }

    static SpriteRenderer CrearIcono(Transform padre, string nombre, Vector3 posLocal, Sprite vista)
    {
        var existente = Buscar(padre, nombre);
        GameObject go;
        if (existente != null) go = existente.gameObject;
        else
        {
            go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
        }

        go.transform.localPosition = posLocal;
        go.transform.localRotation = Quaternion.identity;

        // Los PNG son de 512 px y se importan a 100 px por unidad (5,12 unidades).
        // Con escala 0,25 el icono queda de 1,28 unidades, que entra cómodo en el
        // panel de 2,20 x 3,00.
        go.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = vista;
        sr.color        = Color.white;
        sr.sortingOrder = 10;
        return sr;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
#endif
