#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Text;

/// <summary>
/// FASE 16 — TIENDA.
///
/// 1. Copia los 8 iconos de mejoras de Assets/Generated_Images a
///    Assets/_Project/UI y los importa como Sprite.
/// 2. Crea los 8 ScriptableObject UpgradeData en Assets/_Project/Data/Mejoras.
/// 3. Agrega UpgradeManager al GameObject GameData y le engancha el catálogo.
/// 4. Arma el Canvas/ShopPanel: encabezado con las monedas, grilla de 8 filas
///    y botón VOLVER.
/// 5. Engancha el botón "tienda" del menú principal con UIManager.OpenShop.
///
/// Es idempotente: si algo ya existe lo reutiliza.
/// </summary>
public static class _Fase16Tienda
{
    const string CarpetaUI      = "Assets/_Project/UI";
    const string CarpetaMejoras = "Assets/_Project/Data/Mejoras";
    const string RutaEscena     = "Assets/VAMOSBIEN.unity";

    static readonly string[] Iconos =
    {
        "icon_up_velocidad", "icon_up_resistencia", "icon_up_control", "icon_up_potencia",
        "icon_up_definicion", "icon_up_suerte", "icon_up_iman", "icon_up_escudo",
    };

    // id, nombre, descripción, efecto por nivel, costo base, multiplicador, nivel máximo, icono
    struct DefMejora
    {
        public UpgradeId id; public string nombre; public string desc; public string efecto;
        public int costo; public float mult; public int max; public string icono;
    }

    static readonly DefMejora[] Defs =
    {
        new DefMejora{ id=UpgradeId.Velocidad,   nombre="Velocidad",
            desc="Corrés más rápido en la pista.",
            efecto="+6% de velocidad por nivel", costo=60, mult=1.55f, max=5, icono="icon_up_velocidad" },

        new DefMejora{ id=UpgradeId.Resistencia, nombre="Resistencia",
            desc="Gastás menos energía mientras corrés.",
            efecto="-10% de gasto de energia por nivel", costo=60, mult=1.55f, max=5, icono="icon_up_resistencia" },

        new DefMejora{ id=UpgradeId.Control,     nombre="Control",
            desc="Cambiás de carril más rápido.",
            efecto="+12% de cambio de carril por nivel", costo=50, mult=1.50f, max=5, icono="icon_up_control" },

        new DefMejora{ id=UpgradeId.Potencia,    nombre="Potencia",
            desc="Pateás más fuerte en el penal.",
            efecto="+8% de potencia de tiro por nivel", costo=70, mult=1.60f, max=5, icono="icon_up_potencia" },

        new DefMejora{ id=UpgradeId.Definicion,  nombre="Definición",
            desc="El arquero adivina menos tu remate.",
            efecto="-6% de precision del arquero por nivel", costo=80, mult=1.60f, max=5, icono="icon_up_definicion" },

        new DefMejora{ id=UpgradeId.Suerte,      nombre="Suerte",
            desc="Algunas latas valen más de una moneda.",
            efecto="+1 moneda extra cada 2 niveles", costo=90, mult=1.65f, max=5, icono="icon_up_suerte" },

        new DefMejora{ id=UpgradeId.Iman,        nombre="Imán de monedas",
            desc="Recogés las latas desde más lejos.",
            efecto="+0,5 m de radio por nivel", costo=70, mult=1.55f, max=5, icono="icon_up_iman" },

        new DefMejora{ id=UpgradeId.Escudo,      nombre="Escudo",
            desc="Aguantás un choque por partida.",
            efecto="+1 choque gratis por nivel", costo=120, mult=1.70f, max=5, icono="icon_up_escudo" },
    };

    public static void Execute(string action)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[Fase16] Tienda");

        ImportarIconos(sb);
        var mejoras = CrearMejoras(sb);
        EngancharUpgradeManager(mejoras, sb);
        ArmarPanelTienda(mejoras, sb);
        EngancharBotonMenu(sb);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), RutaEscena);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine("  escena guardada");
        Debug.Log(sb.ToString());
    }

    // ─── 1. Iconos ────────────────────────────────────────────────────────────

    static void ImportarIconos(StringBuilder sb)
    {
        if (!AssetDatabase.IsValidFolder(CarpetaUI))
            AssetDatabase.CreateFolder("Assets/_Project", "UI");

        int copiados = 0;
        foreach (var nombre in Iconos)
        {
            string src = $"Assets/Generated_Images/{nombre}.png";
            string dst = $"{CarpetaUI}/{nombre}.png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(src) == null) continue;

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(dst) == null)
            {
                if (!AssetDatabase.CopyAsset(src, dst)) continue;
                copiados++;
            }

            var imp = AssetImporter.GetAtPath(dst) as TextureImporter;
            if (imp != null)
            {
                imp.textureType         = TextureImporterType.Sprite;
                imp.spriteImportMode    = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled       = false;
                imp.filterMode          = FilterMode.Bilinear;
                imp.wrapMode            = TextureWrapMode.Clamp;
                imp.maxTextureSize      = 512;
                imp.spritePixelsPerUnit = 100f;
                imp.SaveAndReimport();
            }
        }

        sb.AppendLine($"  iconos: {Iconos.Length} revisados, {copiados} copiados nuevos");
    }

    // ─── 2. UpgradeData ───────────────────────────────────────────────────────

    static UpgradeData[] CrearMejoras(StringBuilder sb)
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        if (!AssetDatabase.IsValidFolder(CarpetaMejoras))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Mejoras");

        var lista = new UpgradeData[Defs.Length];

        for (int i = 0; i < Defs.Length; i++)
        {
            var d = Defs[i];
            string ruta = $"{CarpetaMejoras}/Upgrade_{d.id}.asset";

            var data = AssetDatabase.LoadAssetAtPath<UpgradeData>(ruta);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(data, ruta);
            }

            data.id                = d.id;
            data.nombre            = d.nombre;
            data.descripcion       = d.desc;
            data.efectoPorNivel    = d.efecto;
            data.costoBase         = d.costo;
            data.multiplicadorCosto = d.mult;
            data.nivelMaximo       = d.max;
            data.icono             = AssetDatabase.LoadAssetAtPath<Sprite>($"{CarpetaUI}/{d.icono}.png");

            EditorUtility.SetDirty(data);
            lista[i] = data;
        }

        AssetDatabase.SaveAssets();
        sb.AppendLine($"  mejoras creadas: {lista.Length}");
        return lista;
    }

    // ─── 3. UpgradeManager en el GameObject GameData ──────────────────────────

    static void EngancharUpgradeManager(UpgradeData[] mejoras, StringBuilder sb)
    {
        var gd = Object.FindFirstObjectByType<GameData>(FindObjectsInactive.Include);
        if (gd == null) { sb.AppendLine("  ERROR: no encontré el GameObject GameData"); return; }

        var mgr = gd.GetComponent<UpgradeManager>();
        if (mgr == null) mgr = gd.gameObject.AddComponent<UpgradeManager>();

        var so = new SerializedObject(mgr);
        var prop = so.FindProperty("catalogo");
        prop.arraySize = mejoras.Length;
        for (int i = 0; i < mejoras.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = mejoras[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(mgr);
        sb.AppendLine($"  UpgradeManager en '{gd.gameObject.name}' con {mejoras.Length} mejoras");
    }

    // ─── 4. Panel de la tienda ────────────────────────────────────────────────

    static void ArmarPanelTienda(UpgradeData[] mejoras, StringBuilder sb)
    {
        var raiz = RaizCanvas();
        var panelTr = raiz != null ? Buscar(raiz, "ShopPanel") : null;
        if (panelTr == null) { sb.AppendLine("  ERROR: no encontré Canvas/ShopPanel"); return; }
        var panel = panelTr.gameObject;

        var canvas = panel.transform.parent;
        var fuente = BuscarFuente(canvas);

        var rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var fondo = panel.GetComponent<Image>();
        if (fondo == null) fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.04f, 0.06f, 0.11f, 0.98f);
        fondo.raycastTarget = true;

        // ── Encabezado ──────────────────────────────────────────────────────
        var header = Buscar(panel.transform, "Header");
        if (header == null) header = CrearHijo(panel.transform, "Header");
        var rtHeader = (RectTransform)header;
        rtHeader.anchorMin = new Vector2(0f, 1f);
        rtHeader.anchorMax = new Vector2(1f, 1f);
        rtHeader.pivot     = new Vector2(0.5f, 1f);
        rtHeader.sizeDelta = new Vector2(0f, 84f);
        rtHeader.anchoredPosition = Vector2.zero;
        var imgHeader = header.GetComponent<Image>();
        if (imgHeader == null) imgHeader = header.gameObject.AddComponent<Image>();
        imgHeader.color = new Color(0.07f, 0.13f, 0.24f, 1f);
        imgHeader.raycastTarget = false;

        var titulo = Texto(header, "Titulo", fuente, "TIENDA", 40f, TextAlignmentOptions.Left);
        var rtTitulo = (RectTransform)titulo.transform;
        rtTitulo.anchorMin = new Vector2(0f, 0f);
        rtTitulo.anchorMax = new Vector2(0.55f, 1f);
        rtTitulo.pivot     = new Vector2(0f, 0.5f);
        rtTitulo.offsetMin = new Vector2(36f, 0f);
        rtTitulo.offsetMax = new Vector2(0f, 0f);
        titulo.fontStyle = FontStyles.Bold;
        titulo.color = Color.white;

        var monedas = Texto(header, "Monedas", fuente, "0", 36f, TextAlignmentOptions.Right);
        var rtMon = (RectTransform)monedas.transform;
        rtMon.anchorMin = new Vector2(0.45f, 0f);
        rtMon.anchorMax = new Vector2(1f, 1f);
        rtMon.pivot     = new Vector2(1f, 0.5f);
        rtMon.offsetMin = new Vector2(0f, 0f);
        rtMon.offsetMax = new Vector2(-36f, 0f);
        monedas.fontStyle = FontStyles.Bold;
        monedas.color = new Color(1f, 0.84f, 0.25f, 1f);

        // ── Aviso (mensaje corto, oculto por defecto) ───────────────────────
        var aviso = Texto(panel.transform, "Aviso", fuente, "", 24f, TextAlignmentOptions.Center);
        var rtAviso = (RectTransform)aviso.transform;
        rtAviso.anchorMin = new Vector2(0f, 1f);
        rtAviso.anchorMax = new Vector2(1f, 1f);
        rtAviso.pivot     = new Vector2(0.5f, 1f);
        rtAviso.sizeDelta = new Vector2(-80f, 34f);
        rtAviso.anchoredPosition = new Vector2(0f, -92f);
        aviso.color = new Color(1f, 0.45f, 0.40f, 1f);
        aviso.gameObject.SetActive(false);

        // ── Grilla de mejoras ───────────────────────────────────────────────
        var grid = Buscar(panel.transform, "Grid");
        if (grid == null) grid = CrearHijo(panel.transform, "Grid");
        var rtGrid = (RectTransform)grid;
        rtGrid.anchorMin = Vector2.zero;
        rtGrid.anchorMax = Vector2.one;
        rtGrid.offsetMin = new Vector2(40f, 96f);
        rtGrid.offsetMax = new Vector2(-40f, -100f);

        var glg = grid.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = grid.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(480f, 64f);
        glg.spacing         = new Vector2(20f, 12f);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment  = TextAnchor.UpperCenter;
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;

        // ── Fila plantilla ──────────────────────────────────────────────────
        var plantillaTr = Buscar(grid.transform, "Item_Template");
        if (plantillaTr == null) plantillaTr = CrearHijo(grid.transform, "Item_Template");
        ArmarFila(plantillaTr, fuente);
        plantillaTr.gameObject.SetActive(false);

        // ── Botón VOLVER ────────────────────────────────────────────────────
        var volver = Buscar(panel.transform, "BotonVolver");
        if (volver == null) volver = CrearHijo(panel.transform, "BotonVolver");
        var rtVolver = (RectTransform)volver;
        rtVolver.anchorMin = new Vector2(0.5f, 0f);
        rtVolver.anchorMax = new Vector2(0.5f, 0f);
        rtVolver.pivot     = new Vector2(0.5f, 0f);
        rtVolver.sizeDelta = new Vector2(300f, 64f);
        rtVolver.anchoredPosition = new Vector2(0f, 18f);

        var imgVolver = volver.GetComponent<Image>();
        if (imgVolver == null) imgVolver = volver.gameObject.AddComponent<Image>();
        imgVolver.color = new Color(0.15f, 0.35f, 0.75f, 1f);

        var btn = volver.GetComponent<Button>();
        if (btn == null) btn = volver.gameObject.AddComponent<Button>();
        btn.targetGraphic = imgVolver;

        var lVolver = Texto(volver, "Label", fuente, "VOLVER", 30f, TextAlignmentOptions.Center);
        var rtL = (RectTransform)lVolver.transform;
        rtL.anchorMin = Vector2.zero;
        rtL.anchorMax = Vector2.one;
        rtL.offsetMin = Vector2.zero;
        rtL.offsetMax = Vector2.zero;
        lVolver.fontStyle = FontStyles.Bold;
        lVolver.color = Color.white;

        // ── ShopUI ──────────────────────────────────────────────────────────
        var shopUi = panel.GetComponent<ShopUI>();
        if (shopUi == null) shopUi = panel.gameObject.AddComponent<ShopUI>();

        var so = new SerializedObject(shopUi);
        var propMejoras = so.FindProperty("mejoras");
        propMejoras.arraySize = mejoras.Length;
        for (int i = 0; i < mejoras.Length; i++)
            propMejoras.GetArrayElementAtIndex(i).objectReferenceValue = mejoras[i];

        so.FindProperty("contenedor").objectReferenceValue      = rtGrid;
        so.FindProperty("plantilla").objectReferenceValue       = plantillaTr.GetComponent<ShopItemUI>();
        so.FindProperty("monedasText").objectReferenceValue     = monedas;
        so.FindProperty("avisoText").objectReferenceValue       = aviso;
        so.FindProperty("botonVolver").objectReferenceValue     = btn;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shopUi);

        sb.AppendLine("  ShopPanel armado: header + grilla 2x4 + VOLVER");
    }

    static void ArmarFila(Transform fila, TMP_FontAsset fuente)
    {
        var rt = (RectTransform)fila;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(480f, 64f);

        var fondo = fila.GetComponent<Image>();
        if (fondo == null) fondo = fila.gameObject.AddComponent<Image>();
        fondo.color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

        // Icono
        var iconoTr = Buscar(fila, "Icono");
        if (iconoTr == null) iconoTr = CrearHijo(fila, "Icono");
        var rtIcono = (RectTransform)iconoTr;
        rtIcono.anchorMin = new Vector2(0f, 0.5f);
        rtIcono.anchorMax = new Vector2(0f, 0.5f);
        rtIcono.pivot     = new Vector2(0f, 0.5f);
        rtIcono.sizeDelta = new Vector2(48f, 48f);
        rtIcono.anchoredPosition = new Vector2(10f, 0f);
        var imgIcono = iconoTr.GetComponent<Image>();
        if (imgIcono == null) imgIcono = iconoTr.gameObject.AddComponent<Image>();
        imgIcono.preserveAspect = true;
        imgIcono.raycastTarget = false;

        // Nombre
        var nombre = Texto(fila, "Nombre", fuente, "Mejora", 26f, TextAlignmentOptions.Left);
        var rtNombre = (RectTransform)nombre.transform;
        rtNombre.anchorMin = new Vector2(0f, 0.5f);
        rtNombre.anchorMax = new Vector2(0f, 0.5f);
        rtNombre.pivot     = new Vector2(0f, 1f);
        rtNombre.sizeDelta = new Vector2(250f, 30f);
        rtNombre.anchoredPosition = new Vector2(70f, 24f);
        nombre.fontStyle = FontStyles.Bold;
        nombre.color = Color.white;

        // Nivel
        var nivel = Texto(fila, "Nivel", fuente, "Nivel 0/5", 20f, TextAlignmentOptions.Left);
        var rtNivel = (RectTransform)nivel.transform;
        rtNivel.anchorMin = new Vector2(0f, 0.5f);
        rtNivel.anchorMax = new Vector2(0f, 0.5f);
        rtNivel.pivot     = new Vector2(0f, 1f);
        rtNivel.sizeDelta = new Vector2(250f, 24f);
        rtNivel.anchoredPosition = new Vector2(70f, -4f);
        nivel.color = new Color(0.65f, 0.75f, 0.90f, 1f);

        // Costo
        var costo = Texto(fila, "Costo", fuente, "0", 28f, TextAlignmentOptions.Right);
        var rtCosto = (RectTransform)costo.transform;
        rtCosto.anchorMin = new Vector2(1f, 0.5f);
        rtCosto.anchorMax = new Vector2(1f, 0.5f);
        rtCosto.pivot     = new Vector2(1f, 0.5f);
        rtCosto.sizeDelta = new Vector2(100f, 34f);
        rtCosto.anchoredPosition = new Vector2(-116f, 0f);
        costo.fontStyle = FontStyles.Bold;
        costo.color = new Color(1f, 0.84f, 0.25f, 1f);

        // Botón COMPRAR
        var botonTr = Buscar(fila, "Boton");
        if (botonTr == null) botonTr = CrearHijo(fila, "Boton");
        var rtBoton = (RectTransform)botonTr;
        rtBoton.anchorMin = new Vector2(1f, 0.5f);
        rtBoton.anchorMax = new Vector2(1f, 0.5f);
        rtBoton.pivot     = new Vector2(1f, 0.5f);
        rtBoton.sizeDelta = new Vector2(100f, 46f);
        rtBoton.anchoredPosition = new Vector2(-10f, 0f);

        var imgBoton = botonTr.GetComponent<Image>();
        if (imgBoton == null) imgBoton = botonTr.gameObject.AddComponent<Image>();
        imgBoton.color = new Color(0.18f, 0.62f, 0.32f, 1f);

        var btn = botonTr.GetComponent<Button>();
        if (btn == null) btn = botonTr.gameObject.AddComponent<Button>();
        btn.targetGraphic = imgBoton;

        var label = Texto(botonTr, "Label", fuente, "COMPRAR", 20f, TextAlignmentOptions.Center);
        var rtLabel = (RectTransform)label.transform;
        rtLabel.anchorMin = Vector2.zero;
        rtLabel.anchorMax = Vector2.one;
        rtLabel.offsetMin = Vector2.zero;
        rtLabel.offsetMax = Vector2.zero;
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;

        // Componente de la fila
        var item = fila.GetComponent<ShopItemUI>();
        if (item == null) item = fila.gameObject.AddComponent<ShopItemUI>();

        var so = new SerializedObject(item);
        so.FindProperty("icono").objectReferenceValue        = imgIcono;
        so.FindProperty("nombreText").objectReferenceValue   = nombre;
        so.FindProperty("nivelText").objectReferenceValue    = nivel;
        so.FindProperty("costoText").objectReferenceValue    = costo;
        so.FindProperty("botonComprar").objectReferenceValue = btn;
        so.FindProperty("fondo").objectReferenceValue        = fondo;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    // ─── 5. Botón "tienda" del menú ───────────────────────────────────────────

    static void EngancharBotonMenu(StringBuilder sb)
    {
        var raiz = RaizCanvas();
        var menu = raiz != null ? Buscar(raiz, "MainMenuPanel") : null;
        var boton = menu != null ? Buscar(menu, "tienda") : null;
        if (boton == null) { sb.AppendLine("  ERROR: no encontré el botón tienda"); return; }

        var btn = boton.GetComponent<Button>();
        if (btn == null) { sb.AppendLine("  ERROR: el botón tienda no tiene Button"); return; }

        int antes = btn.onClick.GetPersistentEventCount();

        // ¿Ya abre la tienda? (algún listener apunta a UIManager.OpenShop)
        for (int i = 0; i < antes; i++)
            if (btn.onClick.GetPersistentMethodName(i) == "OpenShop") return;

        var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (ui == null) { sb.AppendLine("  ERROR: no encontré UIManager"); return; }

        UnityEventTools.AddPersistentListener(btn.onClick, ui.OpenShop);
        EditorUtility.SetDirty(btn);
        sb.AppendLine($"  botón TIENDA enganchado (listeners {antes} → {btn.onClick.GetPersistentEventCount()})");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>El Canvas de la escena (UIManager vive ahí).</summary>
    static Transform RaizCanvas()
    {
        var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        return ui != null ? ui.transform : null;
    }

    static TMP_FontAsset BuscarFuente(Transform canvas)
    {
        foreach (var t in canvas.GetComponentsInChildren<TMP_Text>(true))
            if (t.font != null) return t.font;
        return null;
    }

    static TMP_Text Texto(Transform padre, string nombre, TMP_FontAsset fuente,
                          string contenido, float tamano, TextAlignmentOptions alineacion)
    {
        var existente = Buscar(padre, nombre);
        Transform tr;
        if (existente != null) tr = existente;
        else tr = CrearHijo(padre, nombre);

        var t = tr.GetComponent<TMP_Text>();
        if (t == null) t = tr.gameObject.AddComponent<TextMeshProUGUI>();

        if (fuente != null) t.font = fuente;
        t.text      = contenido;
        t.fontSize  = tamano;
        t.alignment = alineacion;
        t.raycastTarget = false;
        t.color     = Color.white;
        return t;
    }

    static Transform CrearHijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
#endif
