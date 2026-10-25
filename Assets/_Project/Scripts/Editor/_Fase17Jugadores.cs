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
/// FASE 17 — Jugadores. Idempotente: crea los 5 CharacterData con sus retratos,
/// agrega CharacterManager al GameObject GameData, arma Canvas/CharactersPanel
/// (encabezado + tarjetas 2x3 + VOLVER) y engancha CharacterSelectUI.
/// Se puede volver a correr sin duplicar nada.
/// </summary>
public static class _Fase17Jugadores
{
    const string CarpetaRetratos = "Assets/_Project/UI/Personajes";
    const string CarpetaDatos    = "Assets/_Project/Data/Personajes";

    public static void Execute(string action)
    {
        if (Application.isPlaying) { Debug.LogError("[Fase17] correr en modo edicion"); return; }

        var sb = new StringBuilder();
        sb.AppendLine("===== FASE 17 — JUGADORES =====");

        AsegurarCarpeta(CarpetaRetratos);
        AsegurarCarpeta(CarpetaDatos);

        // ── 1. Personajes ────────────────────────────────────────────────────
        var messi = Crear("Messi", "Lionel", "Argentina", 10, Hex(0x75, 0xAA, 0xDB),
            "pers_argentina", 4f, 0f, 12f, 6f);
        var cris = Crear("Cristiano", "Cristiano", "Portugal", 7, Hex(0xC8, 0x10, 0x2E),
            "pers_portugal", 10f, 0f, 8f, 4f);
        var ney = Crear("Neymar", "Neymar", "Brasil", 10, Hex(0xFF, 0xDF, 0x00),
            "pers_brasil", 4f, 0f, 10f, 10f);
        var mbappe = Crear("Mbappe", "Mbappe", "Francia", 10, Hex(0x00, 0x55, 0xA4),
            "pers_francia", 14f, 0f, 6f, 2f);
        var haaland = Crear("Haaland", "Haaland", "Noruega", 9, Hex(0xBA, 0x0C, 0x2F),
            "pers_noruega", 6f, 0f, 14f, 4f);

        var catalogo = new CharacterData[] { messi, cris, ney, mbappe, haaland };
        int listos = 0;
        foreach (var c in catalogo) if (c != null) listos++;
        sb.AppendLine($"personajes listos: {listos}/5 en {CarpetaDatos}");

        // ── 2. CharacterManager en GameData ──────────────────────────────────
        var gameData = GameObject.Find("GameData");
        if (gameData == null) { sb.AppendLine("ERROR: no encontre el GameObject 'GameData'"); Debug.Log(sb.ToString()); return; }

        var cm = gameData.GetComponent<CharacterManager>();
        if (cm == null) cm = gameData.AddComponent<CharacterManager>();

        var soCm = new SerializedObject(cm);
        var propCatalogo = soCm.FindProperty("catalogo");
        propCatalogo.arraySize = catalogo.Length;
        for (int i = 0; i < catalogo.Length; i++)
            propCatalogo.GetArrayElementAtIndex(i).objectReferenceValue = catalogo[i];
        soCm.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cm);
        sb.AppendLine("CharacterManager en 'GameData' con 5 personajes");

        // ── 3. Panel de seleccion ────────────────────────────────────────────
        ArmarPanel(catalogo, sb);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        sb.AppendLine("escena guardada");

        Debug.Log(sb.ToString());
    }

    // ─── Datos ───────────────────────────────────────────────────────────────

    private static Color Hex(int r, int g, int b) => new Color32((byte)r, (byte)g, (byte)b, 255);

    private static CharacterData Crear(string id, string nombre, string pais, int dorsal,
        Color color, string archivoRetrato, float vel, float ener, float hab, float pop)
    {
        string ruta = $"{CarpetaDatos}/Character_{id}.asset";

        var so = AssetDatabase.LoadAssetAtPath<CharacterData>(ruta);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(so, ruta);
        }

        so.id               = id;
        so.nombre           = nombre;
        so.pais             = pais;
        so.dorsal           = dorsal;
        so.colorCamiseta    = color;
        so.bonusVelocidad   = vel;
        so.bonusEnergia     = ener;
        so.bonusHabilidad   = hab;
        so.bonusPopularidad = pop;
        so.retrato          = ImportarSprite(archivoRetrato);

        EditorUtility.SetDirty(so);
        return so;
    }

    /// <summary>Mueve el PNG a la carpeta de retratos y lo importa como Sprite.</summary>
    private static Sprite ImportarSprite(string archivo)
    {
        string generado = $"Assets/Generated_Images/{archivo}.png";
        string destino  = $"{CarpetaRetratos}/{archivo}.png";

        if (!File.Exists(destino) && File.Exists(generado))
        {
            string error = AssetDatabase.MoveAsset(generado, destino);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[Fase17] no pude mover {archivo}: {error}");
                destino = generado;
            }
        }
        else if (File.Exists(generado) && File.Exists(destino))
        {
            AssetDatabase.DeleteAsset(generado);
        }

        if (!File.Exists(destino)) return null;

        var ti = AssetImporter.GetAtPath(destino) as TextureImporter;
        if (ti != null)
        {
            ti.textureType         = TextureImporterType.Sprite;
            ti.spriteImportMode    = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled       = false;
            ti.spritePixelsPerUnit = 100f;
            ti.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(destino);
    }

    // ─── Panel ───────────────────────────────────────────────────────────────

    private static void ArmarPanel(CharacterData[] catalogo, StringBuilder sb)
    {
        var uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager == null) { sb.AppendLine("ERROR: no encontre UIManager"); return; }

        var canvas = uiManager.transform;
        var panelTr = Buscar(canvas, "CharactersPanel");
        if (panelTr == null) { sb.AppendLine("ERROR: no encontre Canvas/CharactersPanel"); return; }

        var panel   = panelTr.gameObject;
        var fuente  = BuscarFuente(canvas);

        // Fondo opaco: el panel venia con un Image casi transparente.
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var fondo = panel.GetComponent<Image>();
        if (fondo == null) fondo = panel.AddComponent<Image>();
        fondo.color = new Color(0.04f, 0.06f, 0.11f, 0.98f);
        fondo.raycastTarget = true;

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

        // Grilla de tarjetas
        var grid = Buscar(panel.transform, "Grid");
        if (grid == null) grid = CrearHijo(panel.transform, "Grid");
        var rtGrid = (RectTransform)grid;
        rtGrid.anchorMin = Vector2.zero;
        rtGrid.anchorMax = Vector2.one;
        rtGrid.offsetMin = new Vector2(40f, 96f);
        rtGrid.offsetMax = new Vector2(-40f, -96f);

        var glg = grid.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = grid.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(480f, 300f);
        glg.spacing         = new Vector2(20f, 16f);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment  = TextAnchor.UpperCenter;
        glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;

        // Tarjeta plantilla
        var plantillaTr = Buscar(grid.transform, "Carta_Template");
        if (plantillaTr == null) plantillaTr = CrearHijo(grid.transform, "Carta_Template");
        ArmarTarjeta(plantillaTr, fuente);
        plantillaTr.gameObject.SetActive(false);

        // Borrar tarjetas de ejemplo que hayan quedado de una vista previa en el
        // editor: en la escena solo vive la plantilla.
        int borradas = 0;
        for (int i = grid.transform.childCount - 1; i >= 0; i--)
        {
            var h = grid.transform.GetChild(i);
            if (h == plantillaTr || !h.name.StartsWith("Carta_")) continue;
            Object.DestroyImmediate(h.gameObject);
            borradas++;
        }
        if (borradas > 0) sb.AppendLine($"tarjetas de ejemplo borradas: {borradas}");

        // Boton VOLVER
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

        // CharacterSelectUI
        var select = panel.GetComponent<CharacterSelectUI>();
        if (select == null) select = panel.gameObject.AddComponent<CharacterSelectUI>();

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

        // UIManager tiene que conocer el panel
        var soUi = new SerializedObject(uiManager);
        var propPanel = soUi.FindProperty("charactersPanel");
        if (propPanel != null && propPanel.objectReferenceValue == null)
        {
            propPanel.objectReferenceValue = panel;
            soUi.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiManager);
            sb.AppendLine("UIManager.charactersPanel enganchado");
        }

        sb.AppendLine("CharactersPanel armado: header + grilla 2x3 + VOLVER");
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

        // Franja de color del equipo, arriba de todo.
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

        // Retrato
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

        // Nombre
        var nombre = Texto(carta, "Nombre", fuente, "Jugador", 30f, TextAlignmentOptions.Left);
        var rtNombre = (RectTransform)nombre.transform;
        rtNombre.anchorMin = new Vector2(0f, 1f);
        rtNombre.anchorMax = new Vector2(0f, 1f);
        rtNombre.pivot     = new Vector2(0f, 1f);
        rtNombre.sizeDelta = new Vector2(320f, 38f);
        rtNombre.anchoredPosition = new Vector2(148f, -24f);
        nombre.fontStyle = FontStyles.Bold;
        nombre.color     = Color.white;

        // Pais y dorsal
        var pais = Texto(carta, "Pais", fuente, "#10  Argentina", 22f, TextAlignmentOptions.Left);
        var rtPais = (RectTransform)pais.transform;
        rtPais.anchorMin = new Vector2(0f, 1f);
        rtPais.anchorMax = new Vector2(0f, 1f);
        rtPais.pivot     = new Vector2(0f, 1f);
        rtPais.sizeDelta = new Vector2(320f, 28f);
        rtPais.anchoredPosition = new Vector2(148f, -64f);
        pais.color = new Color(0.65f, 0.75f, 0.90f, 1f);

        // Bonus
        var stats = Texto(carta, "Bonus", fuente, "", 22f, TextAlignmentOptions.TopLeft);
        var rtStats = (RectTransform)stats.transform;
        rtStats.anchorMin = new Vector2(0f, 1f);
        rtStats.anchorMax = new Vector2(0f, 1f);
        rtStats.pivot     = new Vector2(0f, 1f);
        rtStats.sizeDelta = new Vector2(260f, 110f);
        rtStats.anchoredPosition = new Vector2(18f, -156f);
        stats.richText = true;
        stats.color    = Color.white;

        // Boton EQUIPAR
        var botonTr = Buscar(carta, "Boton");
        if (botonTr == null) botonTr = CrearHijo(carta, "Boton");
        var rtBoton = (RectTransform)botonTr;
        rtBoton.anchorMin = new Vector2(1f, 0f);
        rtBoton.anchorMax = new Vector2(1f, 0f);
        rtBoton.pivot     = new Vector2(1f, 0f);
        rtBoton.sizeDelta = new Vector2(180f, 52f);
        rtBoton.anchoredPosition = new Vector2(-16f, 18f);

        var imgBoton = botonTr.GetComponent<Image>();
        if (imgBoton == null) imgBoton = botonTr.gameObject.AddComponent<Image>();
        imgBoton.color = new Color(0.18f, 0.62f, 0.32f, 1f);

        var btn = botonTr.GetComponent<Button>();
        if (btn == null) btn = botonTr.gameObject.AddComponent<Button>();
        btn.targetGraphic = imgBoton;

        var label = Texto(botonTr, "Label", fuente, "EQUIPAR", 24f, TextAlignmentOptions.Center);
        var rtLabel = (RectTransform)label.transform;
        rtLabel.anchorMin = Vector2.zero;
        rtLabel.anchorMax = Vector2.one;
        rtLabel.offsetMin = Vector2.zero;
        rtLabel.offsetMax = Vector2.zero;
        label.fontStyle = FontStyles.Bold;
        label.color     = Color.white;

        // Componente de la tarjeta
        var card = carta.GetComponent<CharacterCardUI>();
        if (card == null) card = carta.gameObject.AddComponent<CharacterCardUI>();

        var so = new SerializedObject(card);
        so.FindProperty("retrato").objectReferenceValue     = imgRetrato;
        so.FindProperty("nombreText").objectReferenceValue  = nombre;
        so.FindProperty("paisText").objectReferenceValue     = pais;
        so.FindProperty("statsText").objectReferenceValue    = stats;
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
