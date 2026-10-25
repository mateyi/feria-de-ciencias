#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

/// <summary>
/// FASE 18 — Jugadores con precio y camiseta.
/// Idempotente. Hace cuatro cosas:
///   1. Genera una textura de camiseta por pais recoloreando el atlas original
///      (solo se pintan los pixeles azules del buque; la piel, el pelo y las
///      rayas blancas quedan intactos).
///   2. Crea un material por jugador con esa textura, sobre el mismo shader
///      glTF que usa el modelo, y lo guarda en el CharacterData.
///   3. Pone los precios de cada jugador.
///   4. Rearma Canvas/CharactersPanel con ScrollRect (para deslizar), precio en
///      cada tarjeta y engancha los campos nuevos.
/// </summary>
public static class _Fase18Jugadores
{
    const string CarpetaDatos    = "Assets/_Project/Data/Personajes";
    const string CarpetaKits     = "Assets/_Project/UI/Kits";
    const string CarpetaMaterial = "Assets/_Project/Materials/Kits";
    const string RutaGlb         = "Assets/_Project/Animations/Futbolista_Run.glb";

    // Precios pedidos: Messi 10.000, Cristiano 7.000, Neymar 5.000, Mbappe 3.000.
    // Haaland no estaba en la lista, se deja como el mas barato.
    static readonly (string id, int precio)[] Precios =
    {
        ("Messi",     10000),
        ("Cristiano",  7000),
        ("Neymar",     5000),
        ("Mbappe",     3000),
        ("Haaland",    2000),
    };

    public static void Execute(string action)
    {
        if (Application.isPlaying) { Debug.LogError("[Fase18] correr en modo edicion"); return; }

        var sb = new StringBuilder();
        sb.AppendLine("===== FASE 18 — JUGADORES CON PRECIO Y CAMISETA =====");

        AsegurarCarpeta(CarpetaKits);
        AsegurarCarpeta(CarpetaMaterial);

        // ── Atlas original ───────────────────────────────────────────────────
        Texture2D atlas = null;
        Material materialOriginal = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(RutaGlb))
        {
            if (a is Texture2D t && atlas == null) atlas = t;
            if (a is Material m && materialOriginal == null) materialOriginal = m;
        }

        if (atlas == null || materialOriginal == null)
        {
            sb.AppendLine($"ERROR: atlas={atlas} material={materialOriginal}");
            Debug.Log(sb.ToString());
            return;
        }
        sb.AppendLine($"atlas original {atlas.width}x{atlas.height}, material '{materialOriginal.name}'");
        sb.AppendLine($"shader del modelo: {materialOriginal.shader.name}");

        var pixeles = LeerPixeles(atlas);
        if (pixeles == null) { sb.AppendLine("ERROR: no pude leer el atlas"); Debug.Log(sb.ToString()); return; }

        // ── 1 y 2. Texturas y materiales por jugador ─────────────────────────
        var materiales = new Dictionary<string, Material>();
        foreach (var (id, _) in Precios)
        {
            var so = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CarpetaDatos}/Character_{id}.asset");
            if (so == null) { sb.AppendLine($"  falta {id}"); continue; }

            var kit = GenerarMaterial(so, pixeles, atlas.width, atlas.height, materialOriginal, sb);
            if (kit == null) continue;
            materiales[id] = kit;

            so.kit = kit;
        }

        // ── 3. Precios ───────────────────────────────────────────────────────
        foreach (var (id, precio) in Precios)
        {
            var so = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CarpetaDatos}/Character_{id}.asset");
            if (so == null) continue;

            so.precio = precio;
            EditorUtility.SetDirty(so);
            sb.AppendLine($"  {id}: precio {precio:N0} | kit={(so.kit != null ? so.kit.name : "NINGUNO")}");
        }

        AssetDatabase.SaveAssets();

        // ── 4. CharacterManager: material del generico ───────────────────────
        var gameData = GameObject.Find("GameData");
        if (gameData != null)
        {
            var cm = gameData.GetComponent<CharacterManager>();
            if (cm != null)
            {
                var soCm = new SerializedObject(cm);
                soCm.FindProperty("materialGenerico").objectReferenceValue = materialOriginal;
                soCm.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cm);
                sb.AppendLine("CharacterManager.materialGenerico = " + materialOriginal.name);
            }
        }

        // ── 5. Panel con scroll y precios ────────────────────────────────────
        RearmarPanel(sb);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("escena guardada");

        Debug.Log(sb.ToString());
    }

    // ─── Texturas ────────────────────────────────────────────────────────────

    /// <summary>Copia el atlas a un array de pixeles (no es legible como asset).</summary>
    private static Color[] LeerPixeles(Texture2D origen)
    {
        var rt = RenderTexture.GetTemporary(origen.width, origen.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(origen, rt);

        var previo = RenderTexture.active;
        RenderTexture.active = rt;

        var copia = new Texture2D(origen.width, origen.height, TextureFormat.RGBA32, false, false);
        copia.ReadPixels(new Rect(0, 0, origen.width, origen.height), 0, 0);
        copia.Apply();

        RenderTexture.active = previo;
        RenderTexture.ReleaseTemporary(rt);

        var pixeles = copia.GetPixels();
        Object.DestroyImmediate(copia);
        return pixeles;
    }

    /// <summary>
    /// Devuelve true si el pixel es parte de la camiseta azul. La piel y el pelo
    /// son naranjas (tono bajo) y las rayas son blancas (saturacion baja), asi
    /// que quedan afuera.
    /// </summary>
    private static bool EsCamiseta(Color c, out float valor)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        valor = v;
        return s > 0.22f && h > 0.45f && h < 0.75f;
    }

    private static Material GenerarMaterial(CharacterData so, Color[] origen, int ancho, int alto,
        Material materialOriginal, StringBuilder sb)
    {
        string archivo = $"kit_{so.id.ToLowerInvariant()}.png";
        string rutaPng  = $"{CarpetaKits}/{archivo}";
        string rutaMat  = $"{CarpetaMaterial}/Kit_{so.id}.mat";

        // Promedio de brillo de la camiseta original: sirve de referencia para
        // conservar las sombras y las luces del modelo.
        float suma = 0f;
        int cuenta = 0;
        for (int i = 0; i < origen.Length; i++)
        {
            if (origen[i].a < 0.5f) continue;
            if (!EsCamiseta(origen[i], out float v)) continue;
            suma += v;
            cuenta++;
        }

        if (cuenta == 0) { sb.AppendLine($"  ERROR {so.id}: no encontre pixeles de camiseta"); return null; }

        float promedio = suma / cuenta;
        Color objetivo = so.colorCamiseta;

        var salida = new Color[origen.Length];
        for (int i = 0; i < origen.Length; i++)
        {
            var c = origen[i];
            if (c.a < 0.5f || !EsCamiseta(c, out float v))
            {
                salida[i] = c;
                continue;
            }

            // Tono y saturacion del pais, brillo relativo del pixel original.
            float rel = promedio > 0.001f ? v / promedio : 1f;
            float r = Mathf.Clamp01(objetivo.r * rel);
            float g = Mathf.Clamp01(objetivo.g * rel);
            float b = Mathf.Clamp01(objetivo.b * rel);

            salida[i] = new Color(r, g, b, c.a);
        }

        // PNG
        var tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false, false);
        tex.SetPixels(salida);
        tex.Apply();

        Directory.CreateDirectory(Path.GetDirectoryName(rutaPng));
        File.WriteAllBytes(rutaPng, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(rutaPng, ImportAssetOptions.ForceUpdate);

        var ti = AssetImporter.GetAtPath(rutaPng) as TextureImporter;
        if (ti != null)
        {
            ti.textureType         = TextureImporterType.Default;
            ti.sRGBTexture         = true;
            ti.mipmapEnabled       = true;
            ti.wrapMode            = TextureWrapMode.Repeat;
            ti.filterMode          = FilterMode.Bilinear;
            ti.maxTextureSize      = 2048;
            ti.textureCompression  = TextureImporterCompression.CompressedHQ;
            ti.alphaSource         = TextureImporterAlphaSource.None;
            ti.SaveAndReimport();
        }

        var nueva = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaPng);
        if (nueva == null) { sb.AppendLine($"  ERROR {so.id}: no pude importar {rutaPng}"); return null; }

        // Material: copia del original para heredar shader y propiedades, y le
        // cambiamos solo la textura de color.
        var mat = new Material(materialOriginal);
        mat.name = $"Kit_{so.id}";

        if (mat.HasProperty("baseColorTexture")) mat.SetTexture("baseColorTexture", nueva);
        if (mat.HasProperty("emissiveTexture"))  mat.SetTexture("emissiveTexture", nueva);

        AssetDatabase.DeleteAsset(rutaMat);
        AssetDatabase.CreateAsset(mat, rutaMat);
        EditorUtility.SetDirty(mat);

        sb.AppendLine($"  {so.id}: camiseta {ColorUtility.ToHtmlStringRGB(so.colorCamiseta)} " +
                      $"({cuenta} pixeles pintados) -> {rutaMat}");

        return AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
    }

    // ─── Panel con scroll ────────────────────────────────────────────────────

    private static void RearmarPanel(StringBuilder sb)
    {
        var uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager == null) { sb.AppendLine("ERROR: no encontre UIManager"); return; }

        var canvas  = uiManager.transform;
        var panelTr = Buscar(canvas, "CharactersPanel");
        if (panelTr == null) { sb.AppendLine("ERROR: no encontre Canvas/CharactersPanel"); return; }

        var panel  = panelTr.gameObject;
        var fuente = BuscarFuente(canvas);

        // Encabezado
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

        var titulo = Texto(header, "Titulo", fuente, "JUGADORES", 40f, TextAlignmentOptions.Left);
        var rtTitulo = (RectTransform)titulo.transform;
        rtTitulo.anchorMin = new Vector2(0f, 0f);
        rtTitulo.anchorMax = new Vector2(0.55f, 1f);
        rtTitulo.pivot     = new Vector2(0f, 0.5f);
        rtTitulo.offsetMin = new Vector2(36f, 0f);
        rtTitulo.offsetMax = Vector2.zero;
        titulo.fontStyle = FontStyles.Bold;
        titulo.color     = Color.white;

        var monedas = Texto(header, "Monedas", fuente, "0", 36f, TextAlignmentOptions.Right);
        var rtMon = (RectTransform)monedas.transform;
        rtMon.anchorMin = new Vector2(0.45f, 0f);
        rtMon.anchorMax = new Vector2(1f, 1f);
        rtMon.pivot     = new Vector2(1f, 0.5f);
        rtMon.offsetMin = Vector2.zero;
        rtMon.offsetMax = new Vector2(-36f, 0f);
        monedas.fontStyle = FontStyles.Bold;
        monedas.color     = new Color(1f, 0.84f, 0.25f, 1f);

        // ── ScrollView ───────────────────────────────────────────────────────
        var scrollTr = Buscar(panel.transform, "ScrollView");
        if (scrollTr == null) scrollTr = CrearHijo(panel.transform, "ScrollView");
        var rtScroll = (RectTransform)scrollTr;
        rtScroll.anchorMin = Vector2.zero;
        rtScroll.anchorMax = Vector2.one;
        rtScroll.offsetMin = new Vector2(40f, 96f);   // deja libre el boton VOLVER
        rtScroll.offsetMax = new Vector2(-40f, -96f);
        rtScroll.pivot     = new Vector2(0.5f, 0.5f);

        // Un grafico transparente para que el ScrollRect reciba el arrastre del mouse.
        var imgScroll = scrollTr.GetComponent<Image>();
        if (imgScroll == null) imgScroll = scrollTr.gameObject.AddComponent<Image>();
        imgScroll.color         = new Color(0f, 0f, 0f, 0f);
        imgScroll.raycastTarget = true;

        var viewportTr = Buscar(scrollTr, "Viewport");
        if (viewportTr == null) viewportTr = CrearHijo(scrollTr, "Viewport");
        var rtViewport = (RectTransform)viewportTr;
        rtViewport.anchorMin = Vector2.zero;
        rtViewport.anchorMax = Vector2.one;
        rtViewport.offsetMin = Vector2.zero;
        rtViewport.offsetMax = Vector2.zero;
        rtViewport.pivot     = new Vector2(0f, 1f);

        var mascara = viewportTr.GetComponent<RectMask2D>();
        if (mascara == null) mascara = viewportTr.gameObject.AddComponent<RectMask2D>();

        // ── Grilla (contenido) ───────────────────────────────────────────────
        var grid = Buscar(panel.transform, "Grid");
        if (grid == null) grid = Buscar(scrollTr, "Grid");
        if (grid == null) grid = CrearHijo(viewportTr, "Grid");

        if (grid.parent != viewportTr) grid.SetParent(viewportTr, false);

        var rtGrid = (RectTransform)grid;
        rtGrid.anchorMin        = new Vector2(0f, 1f);
        rtGrid.anchorMax        = new Vector2(1f, 1f);
        rtGrid.pivot            = new Vector2(0.5f, 1f);
        rtGrid.offsetMin        = Vector2.zero;
        rtGrid.offsetMax        = Vector2.zero;
        rtGrid.anchoredPosition = Vector2.zero;

        var glg = grid.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = grid.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(480f, 300f);
        glg.spacing         = new Vector2(20f, 16f);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment  = TextAnchor.UpperCenter;
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        glg.padding         = new RectOffset(0, 0, 0, 24);

        var fitter = grid.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollTr.GetComponent<ScrollRect>();
        if (scroll == null) scroll = scrollTr.gameObject.AddComponent<ScrollRect>();
        scroll.content           = rtGrid;
        scroll.viewport          = rtViewport;
        scroll.horizontal        = false;
        scroll.vertical          = true;
        scroll.movementType      = ScrollRect.MovementType.Elastic;
        scroll.elasticity        = 0.1f;
        scroll.inertia           = true;
        scroll.decelerationRate  = 0.135f;
        scroll.scrollSensitivity = 40f;

        // ── Plantilla ────────────────────────────────────────────────────────
        var plantillaTr = Buscar(grid, "Carta_Template");
        if (plantillaTr == null) plantillaTr = CrearHijo(grid, "Carta_Template");
        ArmarTarjeta(plantillaTr, fuente);
        plantillaTr.gameObject.SetActive(false);

        int borradas = 0;
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            var h = grid.GetChild(i);
            if (h == plantillaTr || !h.name.StartsWith("Carta_")) continue;
            Object.DestroyImmediate(h.gameObject);
            borradas++;
        }
        if (borradas > 0) sb.AppendLine($"tarjetas de ejemplo borradas: {borradas}");

        // ── Boton VOLVER ─────────────────────────────────────────────────────
        var volver = Buscar(panel.transform, "BotonVolver");
        if (volver == null) volver = CrearHijo(panel.transform, "BotonVolver");
        var rtVolver = (RectTransform)volver;
        rtVolver.anchorMin = new Vector2(0.5f, 0f);
        rtVolver.anchorMax = new Vector2(0.5f, 0f);
        rtVolver.pivot     = new Vector2(0.5f, 0f);
        rtVolver.sizeDelta = new Vector2(300f, 64f);
        rtVolver.anchoredPosition = new Vector2(0f, 16f);

        var imgVolver = volver.GetComponent<Image>();
        if (imgVolver == null) imgVolver = volver.gameObject.AddComponent<Image>();
        imgVolver.color = new Color(0.15f, 0.35f, 0.75f, 1f);

        var btnVolver = volver.GetComponent<Button>();
        if (btnVolver == null) btnVolver = volver.gameObject.AddComponent<Button>();
        btnVolver.targetGraphic = imgVolver;

        var labelVolver = Texto(volver, "Label", fuente, "VOLVER", 30f, TextAlignmentOptions.Center);
        var rtLabel = (RectTransform)labelVolver.transform;
        rtLabel.anchorMin = Vector2.zero;
        rtLabel.anchorMax = Vector2.one;
        rtLabel.offsetMin = Vector2.zero;
        rtLabel.offsetMax = Vector2.zero;
        labelVolver.fontStyle = FontStyles.Bold;
        labelVolver.color     = Color.white;

        // ── CharacterSelectUI ────────────────────────────────────────────────
        var select = panel.GetComponent<CharacterSelectUI>();
        if (select == null) select = panel.gameObject.AddComponent<CharacterSelectUI>();

                // El catalogo del panel va en el mismo orden que las tarjetas de la fase 17.
        var lista = new List<CharacterData>();
        foreach (var (id, _) in Precios)
        {
            var so = AssetDatabase.LoadAssetAtPath<CharacterData>($"{CarpetaDatos}/Character_{id}.asset");
            if (so != null) lista.Add(so);
        }
        var catalogo = lista.ToArray();

        var soSel = new SerializedObject(select);
        var propPers = soSel.FindProperty("personajes");
        propPers.arraySize = catalogo.Length;
        for (int i = 0; i < catalogo.Length; i++)
            propPers.GetArrayElementAtIndex(i).objectReferenceValue = catalogo[i];

        soSel.FindProperty("contenedor").objectReferenceValue = rtGrid;
        soSel.FindProperty("plantilla").objectReferenceValue  = plantillaTr.GetComponent<CharacterCardUI>();
        soSel.FindProperty("monedasText").objectReferenceValue = monedas;
        soSel.FindProperty("botonVolver").objectReferenceValue = btnVolver;
        soSel.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(select);

        var soUi = new SerializedObject(uiManager);
        var propPanel = soUi.FindProperty("charactersPanel");
        if (propPanel != null && propPanel.objectReferenceValue == null)
        {
            propPanel.objectReferenceValue = panel;
            soUi.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiManager);
        }
        if (uiManager != null) EditorUtility.SetDirty(uiManager);

        sb.AppendLine("CharactersPanel rearmado: ScrollView + Viewport + Grid con scroll vertical");
    }

    // ─── Tarjeta ─────────────────────────────────────────────────────────────

    private static void ArmarTarjeta(Transform carta, TMP_FontAsset fuente)
    {
        var rt = (RectTransform)carta;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.sizeDelta = new Vector2(480f, 300f);

        var fondo = carta.GetComponent<Image>();
        if (fondo == null) fondo = carta.gameObject.AddComponent<Image>();
        fondo.color = new Color(0.12f, 0.16f, 0.24f, 0.98f);

        var franjaTr = Buscar(carta, "Franja");
        if (franjaTr == null) franjaTr = CrearHijo(carta, "Franja");
        var rtFranja = (RectTransform)franjaTr;
        rtFranja.anchorMin = new Vector2(0f, 1f);
        rtFranja.anchorMax = new Vector2(1f, 1f);
        rtFranja.pivot     = new Vector2(0.5f, 1f);
        rtFranja.sizeDelta = new Vector2(0f, 8f);
        rtFranja.anchoredPosition = Vector2.zero;
        var imgFranja = franjaTr.GetComponent<Image>();
        if (imgFranja == null) imgFranja = franjaTr.gameObject.AddComponent<Image>();
        imgFranja.color = Color.white;
        imgFranja.raycastTarget = false;

        var retratoTr = Buscar(carta, "Retrato");
        if (retratoTr == null) retratoTr = CrearHijo(carta, "Retrato");
        var rtRetrato = (RectTransform)retratoTr;
        rtRetrato.anchorMin = new Vector2(0f, 1f);
        rtRetrato.anchorMax = new Vector2(0f, 1f);
        rtRetrato.pivot     = new Vector2(0f, 1f);
        rtRetrato.sizeDelta = new Vector2(120f, 120f);
        rtRetrato.anchoredPosition = new Vector2(16f, -20f);
        var imgRetrato = retratoTr.GetComponent<Image>();
        if (imgRetrato == null) imgRetrato = retratoTr.gameObject.AddComponent<Image>();
        imgRetrato.preserveAspect = true;
        imgRetrato.raycastTarget  = false;

        var nombre = Texto(carta, "Nombre", fuente, "Jugador", 30f, TextAlignmentOptions.Left);
        var rtNombre = (RectTransform)nombre.transform;
        rtNombre.anchorMin = new Vector2(0f, 1f);
        rtNombre.anchorMax = new Vector2(0f, 1f);
        rtNombre.pivot     = new Vector2(0f, 1f);
        rtNombre.sizeDelta = new Vector2(320f, 38f);
        rtNombre.anchoredPosition = new Vector2(148f, -24f);
        nombre.fontStyle = FontStyles.Bold;
        nombre.color     = Color.white;

        var pais = Texto(carta, "Pais", fuente, "#10  Argentina", 22f, TextAlignmentOptions.Left);
        var rtPais = (RectTransform)pais.transform;
        rtPais.anchorMin = new Vector2(0f, 1f);
        rtPais.anchorMax = new Vector2(0f, 1f);
        rtPais.pivot     = new Vector2(0f, 1f);
        rtPais.sizeDelta = new Vector2(320f, 28f);
        rtPais.anchoredPosition = new Vector2(148f, -64f);
        pais.color = new Color(0.65f, 0.75f, 0.90f, 1f);

        var stats = Texto(carta, "Bonus", fuente, "", 22f, TextAlignmentOptions.TopLeft);
        var rtStats = (RectTransform)stats.transform;
        rtStats.anchorMin = new Vector2(0f, 1f);
        rtStats.anchorMax = new Vector2(0f, 1f);
        rtStats.pivot     = new Vector2(0f, 1f);
        rtStats.sizeDelta = new Vector2(260f, 110f);
        rtStats.anchoredPosition = new Vector2(18f, -156f);
        stats.richText = true;
        stats.color    = Color.white;

        // Precio: justo arriba del boton, alineado a la derecha.
        var precio = Texto(carta, "Precio", fuente, "$0", 24f, TextAlignmentOptions.Right);
        var rtPrecio = (RectTransform)precio.transform;
        rtPrecio.anchorMin = new Vector2(1f, 0f);
        rtPrecio.anchorMax = new Vector2(1f, 0f);
        rtPrecio.pivot     = new Vector2(1f, 0f);
        rtPrecio.sizeDelta = new Vector2(180f, 28f);
        rtPrecio.anchoredPosition = new Vector2(-16f, 74f);
        precio.fontStyle = FontStyles.Bold;
        precio.color     = new Color(1f, 0.84f, 0.25f, 1f);

        var botonTr = Buscar(carta, "Boton");
        if (botonTr == null) botonTr = CrearHijo(carta, "Boton");
        var rtBoton = (RectTransform)botonTr;
        rtBoton.anchorMin = new Vector2(1f, 0f);
        rtBoton.anchorMax = new Vector2(1f, 0f);
        rtBoton.pivot     = new Vector2(1f, 0f);
        rtBoton.sizeDelta = new Vector2(180f, 52f);
        rtBoton.anchoredPosition = new Vector2(-16f, 16f);

        var imgBoton = botonTr.GetComponent<Image>();
        if (imgBoton == null) imgBoton = botonTr.gameObject.AddComponent<Image>();
        imgBoton.color = new Color(0.18f, 0.62f, 0.32f, 1f);

        var btn = botonTr.GetComponent<Button>();
        if (btn == null) btn = botonTr.gameObject.AddComponent<Button>();
        btn.targetGraphic = imgBoton;

        var label = Texto(botonTr, "Label", fuente, "COMPRAR", 24f, TextAlignmentOptions.Center);
        var rtLabel = (RectTransform)label.transform;
        rtLabel.anchorMin = Vector2.zero;
        rtLabel.anchorMax = Vector2.one;
        rtLabel.offsetMin = Vector2.zero;
        rtLabel.offsetMax = Vector2.zero;
        label.fontStyle = FontStyles.Bold;
        label.color     = Color.white;

        var card = carta.GetComponent<CharacterCardUI>();
        if (card == null) card = carta.gameObject.AddComponent<CharacterCardUI>();

        var so = new SerializedObject(card);
        so.FindProperty("retrato").objectReferenceValue     = imgRetrato;
        so.FindProperty("nombreText").objectReferenceValue  = nombre;
        so.FindProperty("paisText").objectReferenceValue     = pais;
        so.FindProperty("statsText").objectReferenceValue    = stats;
        so.FindProperty("precioText").objectReferenceValue   = precio;
        so.FindProperty("botonEquipar").objectReferenceValue = btn;
        so.FindProperty("botonLabel").objectReferenceValue   = label;
        so.FindProperty("fondo").objectReferenceValue        = fondo;
        so.FindProperty("franja").objectReferenceValue       = imgFranja;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(card);
    }

    // ─── Utilidades ──────────────────────────────────────────────────────────

    private static void AsegurarCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string[] partes = ruta.Split('/');
        string actual = partes[0];
        for (int i = 1; i < partes.Length; i++)
        {
            string siguiente = actual + "/" + partes[i];
            if (!AssetDatabase.IsValidFolder(siguiente)) AssetDatabase.CreateFolder(actual, partes[i]);
            actual = siguiente;
        }
    }

    private static Transform Buscar(Transform raiz, string nombre)
    {
        if (raiz.name == nombre) return raiz;

        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;

        return null;
    }

    private static Transform CrearHijo(Transform padre, string nombre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        return go.transform;
    }

    private static TMP_Text Texto(Transform padre, string nombre, TMP_FontAsset fuente,
        string contenido, float tamano, TextAlignmentOptions alineacion)
    {
        var tr = Buscar(padre, nombre);
        if (tr == null) tr = CrearHijo(padre, nombre);

        var tmp = tr.GetComponent<TMP_Text>();
        if (tmp == null) tmp = tr.gameObject.AddComponent<TextMeshProUGUI>();

        tmp.font      = fuente;
        tmp.text      = contenido;
        tmp.fontSize  = tamano;
        tmp.alignment = alineacion;
        tmp.color     = Color.white;
        tmp.raycastTarget = false;

        return tmp;
    }

    private static TMP_FontAsset BuscarFuente(Transform raiz)
    {
        foreach (var t in raiz.GetComponentsInChildren<TMP_Text>(true))
            if (t.font != null) return t.font;

        return TMP_Settings.defaultFontAsset;
    }
}
#endif
