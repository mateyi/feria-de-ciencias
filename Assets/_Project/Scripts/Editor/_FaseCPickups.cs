using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;

/// <summary>
/// FASE C — Construye todo el sistema de pickups:
///
///   1. Materiales low-poly (Assets/_Project/Materials/Pickups).
///   2. Prefabs de los 4 pickups (Assets/_Project/Prefabs/Pickups).
///   3. Un PickupData por pickup (Assets/_Project/Data/Pickups).
///   4. El GameObject PickupManager en la escena, ya cableado con el catálogo.
///
/// Acciones (por argumento "accion"):
///   aplicar -> crea/recréa todo y guarda la escena. Es idempotente.
///   ver     -> informa cómo quedó todo, sin tocar nada.
/// </summary>
public static class _FaseCPickups
{
    // ─── Rutas ────────────────────────────────────────────────────────────────
    const string CarpetaMats    = "Assets/_Project/Materials/Pickups";
    const string CarpetaPrefabs = "Assets/_Project/Prefabs/Pickups";
    const string CarpetaDatos   = "Assets/_Project/Data/Pickups";

    // ─── Materiales (se cargan en ConstruirMateriales) ────────────────────────
    static Material _verde, _naranja, _azul, _celeste, _blanco, _negro, _rojo;

    public static void Execute(Newtonsoft.Json.Linq.JObject args)
    {
        string accion = args != null && args["accion"] != null ? args["accion"].ToString() : "aplicar";

        if (accion == "ver") { Ver(); return; }

        Aplicar();
    }

    static void Aplicar()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE C: pickups ===");

        CrearCarpetas();
        ConstruirMateriales();

        ConstruirPrefabs();
        log.AppendLine("4 prefabs de pickup creados en " + CarpetaPrefabs);

        ConstruirDatos();
        log.AppendLine("4 PickupData creados en " + CarpetaDatos);

        log.AppendLine(CablearEscena());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(log.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  CARPETAS Y MATERIALES
    // ═════════════════════════════════════════════════════════════════════════

    static void CrearCarpetas()
    {
        foreach (string carpeta in new[] { CarpetaMats, CarpetaPrefabs, CarpetaDatos })
            if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        AssetDatabase.Refresh();
    }

    static void ConstruirMateriales()
    {
        _verde   = Mat("Mat_Pickup_Verde",   new Color(0.22f, 0.74f, 0.36f));
        _naranja = Mat("Mat_Pickup_Naranja", new Color(1.00f, 0.52f, 0.10f));
        _azul    = Mat("Mat_Pickup_Azul",    new Color(0.16f, 0.42f, 0.85f));
        _celeste = Mat("Mat_Pickup_Celeste", new Color(0.20f, 0.82f, 0.85f));
        _blanco  = Mat("Mat_Pickup_Blanco",  new Color(0.95f, 0.95f, 0.95f));
        _negro   = Mat("Mat_Pickup_Negro",   new Color(0.10f, 0.10f, 0.12f));
        _rojo    = Mat("Mat_Pickup_Rojo",    new Color(0.80f, 0.12f, 0.14f));
    }

    static Material Mat(string nombre, Color color)
    {
        string ruta = $"{CarpetaMats}/{nombre}.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(ruta) != null) AssetDatabase.DeleteAsset(ruta);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = nombre };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.35f);

        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS DE CONSTRUCCIÓN
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Crea la raíz de un pickup con su collider trigger y su PickupBase.</summary>
    static GameObject Raiz(string nombre, Vector3 tamCol)
    {
        var go = new GameObject(nombre);

        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size   = tamCol;
        col.center = Vector3.zero;

        go.AddComponent<PickupBase>();
        return go;
    }

    /// <summary>Crea una pieza visual (primitiva sin collider) colgada del padre.</summary>
    static GameObject Pieza(string nombre, PrimitiveType tipo, Transform padre,
                            Vector3 posLocal, Vector3 escala, Material mat)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;

        // Las primitivas vienen con collider: se lo sacamos porque el único
        // collider que vale es el trigger de la raíz.
        var col = go.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        go.transform.SetParent(padre, false);
        go.transform.localPosition = posLocal;
        go.transform.localScale    = escala;

        var rend = go.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;

        return go;
    }

    /// <summary>Guarda el objeto como prefab y lo saca de la escena de trabajo.</summary>
    static GameObject GuardarPrefab(GameObject raiz, string ruta)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null) AssetDatabase.DeleteAsset(ruta);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PREFABS
    // ═════════════════════════════════════════════════════════════════════════

    static void ConstruirPrefabs()
    {
        ConstruirEnergia();
        ConstruirTurbo();
        ConstruirIman();
        ConstruirEscudo();
    }

    // ── 1. Bebida isotónica: botella alta con tapa ────────────────────────────
    static void ConstruirEnergia()
    {
        GameObject raiz = Raiz("Pickup_Energia", new Vector3(0.9f, 1.1f, 0.9f));

        Pieza("Cuerpo", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, -0.04f, 0f), new Vector3(0.34f, 0.32f, 0.34f), _verde);

        Pieza("Franja", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.02f, 0f), new Vector3(0.36f, 0.05f, 0.36f), _blanco);

        Pieza("Cuello", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.32f, 0f), new Vector3(0.16f, 0.10f, 0.16f), _verde);

        Pieza("Tapa", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.45f, 0f), new Vector3(0.20f, 0.05f, 0.20f), _negro);

        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Pickup_Energia.prefab");
    }

    // ── 2. Botines turbo: suela + cuerpo + puntera ────────────────────────────
    static void ConstruirTurbo()
    {
        GameObject raiz = Raiz("Pickup_Turbo", new Vector3(1.0f, 0.8f, 0.8f));

        Pieza("Suela", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, -0.28f, 0f), new Vector3(0.90f, 0.12f, 0.42f), _negro);

        Pieza("Cuerpo", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.10f, -0.02f, 0f), new Vector3(0.55f, 0.34f, 0.40f), _naranja);

        Pieza("Puntera", PrimitiveType.Cube, raiz.transform,
              new Vector3(0.32f, -0.12f, 0f), new Vector3(0.32f, 0.22f, 0.36f), _naranja);

        Pieza("Franja", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.10f, 0.13f, 0f), new Vector3(0.58f, 0.06f, 0.42f), _blanco);

        // Tapones: cuatro cubitos abajo para que se lea como botín.
        for (int i = 0; i < 4; i++)
        {
            float x = -0.30f + i * 0.20f;
            Pieza($"Tapon_{i}", PrimitiveType.Cube, raiz.transform,
                  new Vector3(x, -0.36f, 0f), new Vector3(0.08f, 0.08f, 0.10f), _blanco);
        }

        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Pickup_Turbo.prefab");
    }

    // ── 3. Imán: herradura con las dos puntas marcadas ────────────────────────
    static void ConstruirIman()
    {
        GameObject raiz = Raiz("Pickup_Iman", new Vector3(0.9f, 1.0f, 0.6f));

        Pieza("Barra", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.24f, 0f), new Vector3(0.72f, 0.22f, 0.24f), _azul);

        Pieza("Pata_L", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.25f, -0.06f, 0f), new Vector3(0.22f, 0.60f, 0.24f), _azul);

        Pieza("Pata_R", PrimitiveType.Cube, raiz.transform,
              new Vector3(0.25f, -0.06f, 0f), new Vector3(0.22f, 0.60f, 0.24f), _azul);

        Pieza("Punta_L", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.25f, -0.42f, 0f), new Vector3(0.24f, 0.16f, 0.26f), _rojo);

        Pieza("Punta_R", PrimitiveType.Cube, raiz.transform,
              new Vector3(0.25f, -0.42f, 0f), new Vector3(0.24f, 0.16f, 0.26f), _rojo);

        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Pickup_Iman.prefab");
    }

    // ── 4. Chaleco / escudo: torso con hombreras ──────────────────────────────
    static void ConstruirEscudo()
    {
        GameObject raiz = Raiz("Pickup_Escudo", new Vector3(0.95f, 1.0f, 0.9f));

        Pieza("Torso", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, -0.06f, 0f), new Vector3(0.60f, 0.70f, 0.28f), _celeste);

        Pieza("Hombro_L", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.38f, 0.20f, 0f), new Vector3(0.20f, 0.32f, 0.26f), _celeste);

        Pieza("Hombro_R", PrimitiveType.Cube, raiz.transform,
              new Vector3(0.38f, 0.20f, 0f), new Vector3(0.20f, 0.32f, 0.26f), _celeste);

        Pieza("Cuello", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.33f, 0f), new Vector3(0.24f, 0.12f, 0.24f), _blanco);

        Pieza("Franja", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, -0.06f, 0f), new Vector3(0.62f, 0.09f, 0.30f), _blanco);

        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Pickup_Escudo.prefab");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PICKUP DATA
    // ═════════════════════════════════════════════════════════════════════════

    static void ConstruirDatos()
    {
        // Bebida isotónica: recupera 35 de energía (la barra va de 0 a 100).
        Dato("PD_Energia", "energia_isotonicas", "Bebida isotónica", TipoPickup.Energia,
             $"{CarpetaPrefabs}/Pickup_Energia.prefab",
             color: new Color(0.30f, 0.85f, 0.35f),
             valor: 35f, duracion: 0f, nivelMin: 1, peso: 1.0f);

        // Botines turbo: 35% más de velocidad durante 5 segundos.
        Dato("PD_Turbo", "botines_turbo", "Botines turbo", TipoPickup.Turbo,
             $"{CarpetaPrefabs}/Pickup_Turbo.prefab",
             color: new Color(1.00f, 0.55f, 0.10f),
             valor: 1.35f, duracion: 5f, nivelMin: 1, peso: 1.0f);

        // Imán: atrae las latas en 6 metros durante 8 segundos.
        Dato("PD_Iman", "iman_monedas", "Imán", TipoPickup.Iman,
             $"{CarpetaPrefabs}/Pickup_Iman.prefab",
             color: new Color(0.25f, 0.55f, 0.95f),
             valor: 6f, duracion: 8f, nivelMin: 1, peso: 0.9f);

        // Chaleco: suma 1 escudo (absorbe un choque).
        Dato("PD_Escudo", "chaleco_escudo", "Chaleco", TipoPickup.Escudo,
             $"{CarpetaPrefabs}/Pickup_Escudo.prefab",
             color: new Color(0.20f, 0.85f, 0.85f),
             valor: 1f, duracion: 0f, nivelMin: 1, peso: 0.8f);
    }

    static PickupData Dato(string nombre, string id, string nombreVisible, TipoPickup tipo,
                           string rutaPrefab, Color color, float valor, float duracion,
                           int nivelMin, float peso)
    {
        string ruta = $"{CarpetaDatos}/{nombre}.asset";
        if (AssetDatabase.LoadAssetAtPath<PickupData>(ruta) != null) AssetDatabase.DeleteAsset(ruta);

        var dato = ScriptableObject.CreateInstance<PickupData>();
        dato.id          = id;
        dato.nombre      = nombreVisible;
        dato.tipo        = tipo;
        dato.prefab      = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        dato.color       = color;
        dato.valor       = valor;
        dato.duracion    = duracion;
        dato.nivelMinimo = nivelMin;
        dato.peso        = peso;

        AssetDatabase.CreateAsset(dato, ruta);
        return dato;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ESCENA
    // ═════════════════════════════════════════════════════════════════════════

    static string CablearEscena()
    {
        var existente = Object.FindFirstObjectByType<PickupManager>(FindObjectsInactive.Include);
        GameObject go;

        if (existente != null) go = existente.gameObject;
        else
        {
            go = new GameObject("PickupManager");
            go.AddComponent<PickupManager>();
        }

        // Catálogo: los 4 pickups, en el orden del enum.
        var catalogo = new[]
        {
            AssetDatabase.LoadAssetAtPath<PickupData>($"{CarpetaDatos}/PD_Energia.asset"),
            AssetDatabase.LoadAssetAtPath<PickupData>($"{CarpetaDatos}/PD_Turbo.asset"),
            AssetDatabase.LoadAssetAtPath<PickupData>($"{CarpetaDatos}/PD_Iman.asset"),
            AssetDatabase.LoadAssetAtPath<PickupData>($"{CarpetaDatos}/PD_Escudo.asset")
        };

        var so = new SerializedObject(go.GetComponent<PickupManager>());

        SerializedProperty propCat = so.FindProperty("catalogo");
        if (propCat != null)
        {
            propCat.arraySize = catalogo.Length;
            for (int i = 0; i < catalogo.Length; i++)
                propCat.GetArrayElementAtIndex(i).objectReferenceValue = catalogo[i];
        }

        // Frecuencia por etapa: valores bajos a propósito.
        SerializedProperty propFrec = so.FindProperty("pickupsPorNivel");
        float[] frecuencias = { 0.30f, 0.35f, 0.40f, 0.45f, 0.50f, 0.55f };
        if (propFrec != null)
        {
            propFrec.arraySize = frecuencias.Length;
            for (int i = 0; i < frecuencias.Length; i++)
                propFrec.GetArrayElementAtIndex(i).floatValue = frecuencias[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(go);

        Scene escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);

        return $"PickupManager en la escena con {catalogo.Length} pickups. Escena guardada.";
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  VER
    // ═════════════════════════════════════════════════════════════════════════

    static void Ver()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE C: cómo quedaron los pickups ===");

        foreach (string nombre in new[] { "Pickup_Energia", "Pickup_Turbo", "Pickup_Iman", "Pickup_Escudo" })
        {
            string ruta = $"{CarpetaPrefabs}/{nombre}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            log.AppendLine($"prefab {nombre}: {(prefab != null ? "OK" : "FALTA")}");
        }

        foreach (string nombre in new[] { "PD_Energia", "PD_Turbo", "PD_Iman", "PD_Escudo" })
        {
            string ruta = $"{CarpetaDatos}/{nombre}.asset";
            var dato = AssetDatabase.LoadAssetAtPath<PickupData>(ruta);
            if (dato == null) { log.AppendLine($"dato {nombre}: FALTA"); continue; }

            log.AppendLine($"dato {nombre}: tipo={dato.tipo} valor={dato.valor} " +
                           $"duracion={dato.duracion} nivelMin={dato.nivelMinimo} peso={dato.peso} " +
                           $"prefab={(dato.prefab != null ? "OK" : "SIN PREFAB")}");
        }

        var manager = Object.FindFirstObjectByType<PickupManager>(FindObjectsInactive.Include);
        if (manager == null) log.AppendLine("PickupManager en la escena: NO ESTÁ");
        else
        {
            var so = new SerializedObject(manager);
            SerializedProperty prop = so.FindProperty("catalogo");
            log.AppendLine($"PickupManager en la escena: catalogo={(prop != null ? prop.arraySize : 0)}");

            SerializedProperty frec = so.FindProperty("pickupsPorNivel");
            if (frec != null)
            {
                var texto = new StringBuilder();
                for (int i = 0; i < frec.arraySize; i++)
                    texto.Append(frec.GetArrayElementAtIndex(i).floatValue.ToString("0.00") + " ");
                log.AppendLine("frecuencia por etapa: " + texto);
            }
        }

        Debug.Log(log.ToString());
    }
}
