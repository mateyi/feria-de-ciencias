#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// PRE-FASE 16 — Dos cosas:
///
/// 1. Deja CUATRO carteles de decisión antes de la META. Para eso el
///    TrackGenerator pasa a poner un segmento de decisión cada un segmento
///    normal (decisionInterval = 1) y deja de generarlos a partir de z=270,
///    así el último queda en z=240 y ninguno cae dentro del túnel.
///
/// 2. Rearma el cartel minimalista de resultado (Canvas/DecisionPanel/
///    FeedbackPanel): título, veredicto y tres atributos.
///
/// Es idempotente: se puede correr las veces que haga falta.
/// </summary>
public static class _PreFase16
{
    const string RutaEscena = "Assets/VAMOSBIEN.unity";
    const string CarpetaDecid = "Assets/_Project/Data/Decisiones";

    public static void Execute(string action)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[PreFase16] Carteles de decisión");

        AjustarGeneradorDePista(sb);
        RearmarCartelDeResultado(sb);
        AjustarDecisionData(sb);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaEscena);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine("  escena guardada");
        Debug.Log(sb.ToString());
    }

    // ─── 1. Cuatro decisiones antes de la META ────────────────────────────────

    static void AjustarGeneradorDePista(StringBuilder sb)
    {
        var gen = Object.FindFirstObjectByType<TrackGenerator>();
        if (gen == null) { sb.AppendLine("  ERROR: no encontré TrackGenerator en la escena"); return; }

        var so = new SerializedObject(gen);
        so.FindProperty("decisionInterval").intValue = 1;
        so.FindProperty("decisionStopZ").floatValue  = 270f;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(gen);

        sb.AppendLine("  TrackGenerator: decisionInterval=1 decisionStopZ=270");
        sb.AppendLine("  → segmentos de decisión en z=60, 120, 180 y 240 " +
                      "(puntos de activación en z=70, 130, 190 y 250)");
    }

    // ─── 2. Cartel minimalista de resultado ───────────────────────────────────

    static void RearmarCartelDeResultado(StringBuilder sb)
    {
        var ui = Object.FindFirstObjectByType<DecisionUI>(FindObjectsInactive.Include);
        if (ui == null) { sb.AppendLine("  ERROR: no encontré el DecisionUI en la escena"); return; }
        var panel = ui.gameObject;

        var centro    = Buscar(panel.transform, "CenterPanel");
        var feedback  = Buscar(panel.transform, "FeedbackPanel");
        if (feedback == null) { sb.AppendLine("  ERROR: no encontré FeedbackPanel"); return; }

        // ── Fondo del cartel ────────────────────────────────────────────────
        var rtFondo = (RectTransform)feedback;
        rtFondo.anchorMin = new Vector2(0.5f, 0.5f);
        rtFondo.anchorMax = new Vector2(0.5f, 0.5f);
        rtFondo.pivot     = new Vector2(0.5f, 0.5f);
        rtFondo.sizeDelta = new Vector2(880f, 380f);
        rtFondo.anchoredPosition = Vector2.zero;

        var imgFondo = feedback.GetComponent<Image>();
        if (imgFondo == null) imgFondo = feedback.gameObject.AddComponent<Image>();
        imgFondo.color = new Color(0.035f, 0.055f, 0.095f, 0.96f);
        imgFondo.raycastTarget = false;

        // ── Franja de color arriba ──────────────────────────────────────────
        var franja = Buscar(feedback, "AccentStrip");
        if (franja == null) franja = CrearHijo(feedback, "AccentStrip");
        var rtFranja = (RectTransform)franja;
        rtFranja.anchorMin = new Vector2(0f, 1f);
        rtFranja.anchorMax = new Vector2(1f, 1f);
        rtFranja.pivot     = new Vector2(0.5f, 1f);
        rtFranja.sizeDelta = new Vector2(0f, 8f);
        rtFranja.anchoredPosition = Vector2.zero;
        var imgFranja = franja.GetComponent<Image>();
        if (imgFranja == null) imgFranja = franja.gameObject.AddComponent<Image>();
        imgFranja.color = new Color(0.30f, 0.83f, 0.48f, 1f);
        imgFranja.raycastTarget = false;
        franja.SetAsFirstSibling();

        // ── Título: la opción elegida ───────────────────────────────────────
        var titulo = Buscar(feedback, "FeedbackTitle");
        if (titulo != null)
        {
            var rt = (RectTransform)titulo;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-80f, 62f);
            rt.anchoredPosition = new Vector2(0f, -26f);

            var t = titulo.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.fontSize = 40f;
                t.fontStyle = FontStyles.Bold;
                t.color = Color.white;
                t.alignment = TextAlignmentOptions.Center;
            }
        }

        // ── Veredicto: VENTAJA / DESVENTAJA ─────────────────────────────────
        var veredicto = Buscar(feedback, "FeedbackVerdict");
        if (veredicto == null) veredicto = CrearHijo(feedback, "FeedbackVerdict");
        var rtVer = (RectTransform)veredicto;
        rtVer.anchorMin = new Vector2(0f, 1f);
        rtVer.anchorMax = new Vector2(1f, 1f);
        rtVer.pivot     = new Vector2(0.5f, 1f);
        rtVer.sizeDelta = new Vector2(-80f, 36f);
        rtVer.anchoredPosition = new Vector2(0f, -92f);
        var tVer = AsegurarTexto(veredicto.gameObject, Buscar(feedback, "FeedbackTitle"));
        tVer.fontSize  = 24f;
        tVer.fontStyle = FontStyles.Bold;
        tVer.characterSpacing = 12f;
        tVer.color     = new Color(0.30f, 0.83f, 0.48f, 1f);
        tVer.alignment = TextAlignmentOptions.Center;
        tVer.text      = "VENTAJA";

        // ── Los tres atributos ──────────────────────────────────────────────
        var stats = Buscar(feedback, "FeedbackStats");
        if (stats != null)
        {
            var rt = (RectTransform)stats;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-160f, -200f);
            rt.anchoredPosition = new Vector2(0f, -42f);

            var t = stats.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.fontSize    = 34f;
                t.fontStyle   = FontStyles.Normal;
                t.color       = Color.white;
                t.alignment   = TextAlignmentOptions.Center;
                t.lineSpacing = 18f;
                t.richText    = true;
            }
        }

        // ── La descripción larga no entra en un cartel minimalista ──────────
        var desc = Buscar(feedback, "FeedbackDesc");
        if (desc != null) desc.gameObject.SetActive(false);

        // ── Cableado del DecisionUI ─────────────────────────────────────────
        var so = new SerializedObject(ui);
        so.FindProperty("centerPanel").objectReferenceValue = centro != null ? centro.gameObject : null;
        so.FindProperty("dimOverlay").objectReferenceValue  = panel.GetComponent<Image>();
        so.FindProperty("feedbackVerdictText").objectReferenceValue =
            veredicto.GetComponent<TMP_Text>();
        so.FindProperty("feedbackAccent").objectReferenceValue =
            franja.GetComponent<Image>();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        sb.AppendLine("  FeedbackPanel rearmado: 880x380, franja + título + veredicto + 3 atributos");
    }

    // ─── 3. DecisionData: tres atributos para arriba o tres para abajo ────────

    static void AjustarDecisionData(StringBuilder sb)
    {
        // Cada opción buena mueve TRES atributos en positivo; cada opción mala
        // mueve TRES atributos en negativo. Así el cartel siempre muestra
        // "+3 atributos" o "-3 atributos", como pide el diseño.
        Ajustar("Decision_Powerade_Cerveza",
            pos: new Vector3(15f, 5f, 3f),   posDesc: "Hidratado y con energia.",
            neg: new Vector3(-20f, -5f, -4f), negDesc: "Te baja el rendimiento.",
            ordenNeg: new[] { "energy", "skill", "speed" });

        Ajustar("Decision_Cama_Fiesta",
            pos: new Vector3(20f, 3f, 3f),   posDesc: "Descansaste como un profesional.",
            neg: new Vector3(-15f, -5f, -3f), negDesc: "Te divertis pero llegas fundido.",
            ordenNeg: new[] { "energy", "skill", "speed" });

        Ajustar("Decision_Plato_Hamburguesa",
            pos: new Vector3(12f, 5f, 3f),   posDesc: "Comida de deportista.",
            neg: new Vector3(-8f, -3f, -4f), negDesc: "Rico pero te cae pesado.",
            ordenNeg: new[] { "energy", "skill", "speed" });

        Ajustar("Decision_Pesas_Sofa",
            pos: new Vector3(8f, 6f, 4f),    posDesc: "Mas fuerza y mas ritmo.",
            neg: new Vector3(-8f, -4f, -6f), negDesc: "Descansas pero perdes ritmo.",
            ordenNeg: new[] { "skill", "speed", "energy" });

        sb.AppendLine("  4 DecisionData: 3 atributos positivos / 3 negativos por par");

        void Ajustar(string nombre, Vector3 pos, string posDesc,
                      Vector3 neg, string negDesc, string[] ordenNeg)
        {
            string ruta = $"{CarpetaDecid}/{nombre}.asset";
            var d = AssetDatabase.LoadAssetAtPath<DecisionData>(ruta);
            if (d == null) return;

            // pos = (energía, velocidad, habilidad) para la opción buena.
            d.leftConsequence.energyDelta = pos.x;
            d.leftConsequence.speedDelta  = pos.y;
            d.leftConsequence.skillDelta  = pos.z;
            d.leftConsequence.popularityDelta = 0f;
            d.leftConsequence.description = posDesc;

            // neg = los mismos tres atributos, todos en negativo.
            var c = new StatConsequence { description = negDesc };
            for (int i = 0; i < 3 && i < ordenNeg.Length; i++)
            {
                switch (ordenNeg[i])
                {
                    case "energy": c.energyDelta = neg.x; break;
                    case "speed":  c.speedDelta  = neg.y; break;
                    case "skill":  c.skillDelta  = neg.z; break;
                }
            }
            d.rightConsequence = c;

            EditorUtility.SetDirty(d);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static Transform CrearHijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    static TMP_Text AsegurarTexto(GameObject go, Transform modelo)
    {
        var t = go.GetComponent<TMP_Text>();
        if (t != null) return t;

        // Copiamos la fuente del título para que quede igual al resto del panel.
        var fuente = modelo != null ? modelo.GetComponent<TMP_Text>() : null;

        var nuevo = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null && fuente.font != null) nuevo.font = fuente.font;
        nuevo.raycastTarget = false;
        return nuevo;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
#endif
