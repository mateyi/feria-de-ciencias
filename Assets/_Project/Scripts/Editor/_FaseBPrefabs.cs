using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// FASE B — Construye todo el sistema de obstáculos:
///
///   1. Materiales low-poly (Assets/_Project/Materials/Obstaculos).
///   2. Prefabs de obstáculos (Assets/_Project/Prefabs/Obstacles).
///   3. Un ObstacleData por obstáculo (Assets/_Project/Data/Obstaculos).
///   4. El LevelConfig con los 6 niveles (Assets/_Project/Data).
///   5. El GameObject ObstacleManager en la escena, ya cableado.
///
/// Es idempotente: borra y vuelve a crear los assets que genera.
/// </summary>
public static class _FaseBPrefabs
{
    // ─── Rutas ────────────────────────────────────────────────────────────────
    const string CarpetaMats    = "Assets/_Project/Materials/Obstaculos";
    const string CarpetaPrefabs = "Assets/_Project/Prefabs/Obstacles";
    const string CarpetaDatos   = "Assets/_Project/Data/Obstaculos";
    const string RutaLevelConfig = "Assets/_Project/Data/LevelConfig.asset";

    // ─── Materiales (se cargan en ConstruirMateriales) ────────────────────────
    static Material _amarillo, _negro, _rojo, _piel, _naranja, _blanco,
                    _gris, _azulRival, _verdeCancha, _dorado;

    // ─── Datos creados ────────────────────────────────────────────────────────
    static ObstacleData _odCono, _odValla, _odBarrera, _odArbitro, _odArbitroMovil,
                        _odDefensa, _odPelota, _odFotografo, _odInvasor, _odBanderin;

    public static void Execute()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE B: sistema de obstáculos ===");

        CrearCarpetas();
        ConstruirMateriales();

        ConstruirPrefabs(log);
        ConstruirDatos(log);
        ConstruirLevelConfig(log);
        CablearEscena(log);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        log.AppendLine("Listo.");
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
        _amarillo   = Mat("Mat_Obst_Amarillo",   new Color(1.00f, 0.84f, 0.10f));
        _negro      = Mat("Mat_Obst_Negro",      new Color(0.09f, 0.09f, 0.11f));
        _rojo       = Mat("Mat_Obst_Rojo",       new Color(0.78f, 0.10f, 0.12f));
        _piel       = Mat("Mat_Obst_Piel",       new Color(0.85f, 0.66f, 0.50f));
        _naranja    = Mat("Mat_Obst_Naranja",    new Color(0.95f, 0.42f, 0.08f));
        _blanco     = Mat("Mat_Obst_Blanco",     new Color(0.95f, 0.95f, 0.95f));
        _gris       = Mat("Mat_Obst_Gris",       new Color(0.42f, 0.44f, 0.48f));
        _azulRival  = Mat("Mat_Obst_AzulRival",  new Color(0.10f, 0.25f, 0.62f));
        _verdeCancha= Mat("Mat_Obst_Verde",      new Color(0.16f, 0.45f, 0.20f));
        _dorado     = Mat("Mat_Obst_Dorado",     new Color(0.95f, 0.72f, 0.16f));
    }

    static Material Mat(string nombre, Color color)
    {
        string ruta = $"{CarpetaMats}/{nombre}.mat";
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) AssetDatabase.DeleteAsset(ruta);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader) { name = nombre };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS DE CONSTRUCCIÓN
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Crea la raíz de un obstáculo con su collider trigger.</summary>
    static GameObject Raiz(string nombre, Vector3 tamCol, Vector3 centroCol)
    {
        var go = new GameObject(nombre);
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = tamCol;
        col.center = centroCol;
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
        go.transform.localScale = escala;

        var rend = go.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;

        return go;
    }

    /// <summary>Guarda el objeto como prefab y lo saca de la escena de trabajo.</summary>
    static GameObject GuardarPrefab(GameObject raiz, string ruta)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ruta) != null)
            AssetDatabase.DeleteAsset(ruta);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        return prefab;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PREFABS
    // ═════════════════════════════════════════════════════════════════════════

    static void ConstruirPrefabs(StringBuilder log)
    {
        ConstruirCono();
        ConstruirValla();
        ConstruirBarrera();
        ConstruirArbitro("Obstaculo_Arbitro",      patrulla: false);
        ConstruirArbitro("Obstaculo_ArbitroMovil", patrulla: true);
        ConstruirDefensa();
        ConstruirPelota();
        ConstruirFotografo();
        ConstruirInvasor();
        ConstruirBanderin();

        log.AppendLine("prefabs de obstáculos creados en " + CarpetaPrefabs);
    }

    // ── 1. Cono de entrenamiento (se puede saltar) ────────────────────────────
    static void ConstruirCono()
    {
        GameObject raiz = Raiz("Obstaculo_Cono", new Vector3(0.8f, 0.85f, 0.8f), new Vector3(0f, 0.42f, 0f));

        // Base cuadrada + cuerpo troncocónico simulado con dos cilindros.
        Pieza("Base", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.03f, 0f), new Vector3(0.55f, 0.06f, 0.55f), _naranja);

        Pieza("Cuerpo", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.35f, 0f), new Vector3(0.30f, 0.32f, 0.30f), _naranja);

        Pieza("Punta", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.68f, 0f), new Vector3(0.16f, 0.14f, 0.16f), _naranja);

        // Franja blanca reflectante.
        Pieza("Franja", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.45f, 0f), new Vector3(0.26f, 0.05f, 0.26f), _blanco);

        raiz.AddComponent<ObstacleEstatico>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Cono.prefab");
    }

    // ── 2. Valla baja (se puede saltar) ───────────────────────────────────────
    static void ConstruirValla()
    {
        GameObject raiz = Raiz("Obstaculo_Valla", new Vector3(2.2f, 0.9f, 0.4f), new Vector3(0f, 0.45f, 0f));

        // Dos parantes y dos travesaños.
        Pieza("Parante_L", PrimitiveType.Cube, raiz.transform,
              new Vector3(-1.0f, 0.45f, 0f), new Vector3(0.1f, 0.9f, 0.1f), _gris);
        Pieza("Parante_R", PrimitiveType.Cube, raiz.transform,
              new Vector3( 1.0f, 0.45f, 0f), new Vector3(0.1f, 0.9f, 0.1f), _gris);

        Pieza("Travesaño_A", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.78f, 0f), new Vector3(2.2f, 0.14f, 0.12f), _rojo);
        Pieza("Travesaño_B", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.48f, 0f), new Vector3(2.2f, 0.14f, 0.12f), _blanco);

        raiz.AddComponent<ObstacleEstatico>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Valla.prefab");
    }

    // ── 3. Barrera de futbolistas (ocupa 2 carriles) ──────────────────────────
    static void ConstruirBarrera()
    {
        GameObject raiz = Raiz("Obstaculo_Barrera", new Vector3(5.1f, 1.9f, 0.9f), new Vector3(0f, 0.95f, 0f));

        // Base negra.
        Pieza("Base", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.06f, 0f), new Vector3(5.1f, 0.12f, 1.0f), _negro);

        // Tres maniquíes amarillos, hombro con hombro.
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 1.65f;
            GameObject maniqui = new GameObject($"Maniqui_{i}");
            maniqui.transform.SetParent(raiz.transform, false);
            maniqui.transform.localPosition = new Vector3(x, 0.12f, 0f);

            Pieza("Piernas", PrimitiveType.Cube, maniqui.transform,
                  new Vector3(0f, 0.45f, 0f), new Vector3(0.52f, 0.9f, 0.34f), _amarillo);
            Pieza("Torso", PrimitiveType.Cube, maniqui.transform,
                  new Vector3(0f, 1.22f, 0f), new Vector3(0.60f, 0.72f, 0.36f), _amarillo);
            Pieza("Cabeza", PrimitiveType.Sphere, maniqui.transform,
                  new Vector3(0f, 1.76f, 0f), new Vector3(0.34f, 0.34f, 0.34f), _piel);
            Pieza("Brazo_L", PrimitiveType.Cube, maniqui.transform,
                  new Vector3(-0.42f, 1.20f, 0f), new Vector3(0.14f, 0.62f, 0.16f), _amarillo);
            Pieza("Brazo_R", PrimitiveType.Cube, maniqui.transform,
                  new Vector3( 0.42f, 1.20f, 0f), new Vector3(0.14f, 0.62f, 0.16f), _amarillo);
        }

        raiz.AddComponent<ObstacleEstatico>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Barrera.prefab");
    }

    // ── 4. Árbitro (quieto o patrullando) ─────────────────────────────────────
    static void ConstruirArbitro(string nombre, bool patrulla)
    {
        GameObject raiz = Raiz(nombre, new Vector3(0.85f, 1.9f, 0.85f), new Vector3(0f, 0.95f, 0f));

        Pieza("Piernas", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.42f, 0f), new Vector3(0.44f, 0.84f, 0.32f), _negro);
        Pieza("Torso", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 1.20f, 0f), new Vector3(0.56f, 0.72f, 0.34f), _amarillo);
        Pieza("Cabeza", PrimitiveType.Sphere, raiz.transform,
              new Vector3(0f, 1.72f, 0f), new Vector3(0.32f, 0.32f, 0.32f), _piel);
        Pieza("Pelo", PrimitiveType.Sphere, raiz.transform,
              new Vector3(0f, 1.80f, -0.02f), new Vector3(0.33f, 0.20f, 0.33f), _negro);
        Pieza("Brazo_L", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.40f, 1.18f, 0f), new Vector3(0.13f, 0.6f, 0.15f), _amarillo);
        Pieza("Brazo_R", PrimitiveType.Cube, raiz.transform,
              new Vector3( 0.40f, 1.18f, 0f), new Vector3(0.13f, 0.6f, 0.15f), _amarillo);

        // Silbato y tarjeta en la mano: dos detalles chiquitos que se leen de lejos.
        Pieza("Silbato", PrimitiveType.Sphere, raiz.transform,
              new Vector3(0.46f, 0.92f, -0.12f), new Vector3(0.09f, 0.09f, 0.09f), _negro);
        Pieza("Tarjeta", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.48f, 0.95f, -0.10f), new Vector3(0.16f, 0.22f, 0.02f), _amarillo);

        if (patrulla) raiz.AddComponent<ObstaclePatrulla>();
        else          raiz.AddComponent<ObstacleEstatico>();

        GuardarPrefab(raiz, $"{CarpetaPrefabs}/{nombre}.prefab");
    }

    // ── 5. Defensa deslizante con telegrafiado ────────────────────────────────
    static void ConstruirDefensa()
    {
        GameObject raiz = Raiz("Obstaculo_Defensa", new Vector3(2.1f, 1.0f, 1.0f), new Vector3(0f, 0.5f, 0f));

        // ── Cuerpo (es el que se acuesta al barrerse) ────────────────────────
        GameObject cuerpo = new GameObject("Cuerpo");
        cuerpo.transform.SetParent(raiz.transform, false);

        Pieza("Piernas", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(0f, 0.30f, 0f), new Vector3(0.42f, 0.60f, 0.30f), _blanco);
        Pieza("Torso", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(0f, 0.88f, 0f), new Vector3(0.54f, 0.62f, 0.32f), _rojo);
        Pieza("Cabeza", PrimitiveType.Sphere, cuerpo.transform,
              new Vector3(0f, 1.34f, 0f), new Vector3(0.30f, 0.30f, 0.30f), _piel);
        Pieza("Brazo_L", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(-0.38f, 0.90f, 0f), new Vector3(0.12f, 0.55f, 0.14f), _piel);
        Pieza("Brazo_R", PrimitiveType.Cube, cuerpo.transform,
              new Vector3( 0.38f, 0.90f, 0f), new Vector3(0.12f, 0.55f, 0.14f), _piel);

        // ── Telegrafiado: marca roja en el piso ──────────────────────────────
        GameObject telegrafo = new GameObject("Telegrafo");
        telegrafo.transform.SetParent(raiz.transform, false);
        Pieza("Marca", PrimitiveType.Cube, telegrafo.transform,
              Vector3.zero, new Vector3(7.6f, 0.02f, 1.7f), _rojo);

        // ── Telegrafiado: ícono de alerta "!" ────────────────────────────────
        GameObject alerta = new GameObject("Alerta");
        alerta.transform.SetParent(raiz.transform, false);
        Pieza("Barra", PrimitiveType.Cube, alerta.transform,
              new Vector3(0f, 0.62f, 0f), new Vector3(0.26f, 0.78f, 0.26f), _rojo);
        Pieza("Punto", PrimitiveType.Cube, alerta.transform,
              new Vector3(0f, 0.10f, 0f), new Vector3(0.26f, 0.24f, 0.26f), _rojo);

        raiz.AddComponent<ObstacleDeslizante>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Defensa.prefab");
    }

    // ── 6. Pelota gigante rodando ─────────────────────────────────────────────
    static void ConstruirPelota()
    {
        GameObject raiz = Raiz("Obstaculo_PelotaGigante", new Vector3(2f, 2f, 2f), new Vector3(0f, 1f, 0f));

        // El collider de la raíz no es esférico (BoxCollider), pero alcanza y
        // evita meter otro Rigidbody en la escena.
        GameObject bola = Pieza("Cuerpo", PrimitiveType.Sphere, raiz.transform,
                                new Vector3(0f, 1f, 0f), new Vector3(2f, 2f, 2f), _blanco);

        // Cascos negros para que se lea como pelota.
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(ang) * 0.72f, 1f + Mathf.Sin(ang * 1.7f) * 0.42f,
                                      Mathf.Sin(ang) * 0.42f);

            Pieza($"Casco_{i}", PrimitiveType.Sphere, bola.transform,
                  pos / 2f, new Vector3(0.30f, 0.30f, 0.30f) / 2f, _negro);
        }

        raiz.AddComponent<ObstacleRodante>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_PelotaGigante.prefab");
    }

    // ── 7. Fotógrafo con trípode ──────────────────────────────────────────────
    static void ConstruirFotografo()
    {
        GameObject raiz = Raiz("Obstaculo_Fotografo", new Vector3(1.0f, 1.9f, 1.0f), new Vector3(0f, 0.95f, 0f));

        // Trípode.
        Pieza("Pata_L", PrimitiveType.Cube, raiz.transform,
              new Vector3(-0.30f, 0.45f, 0.10f), new Vector3(0.06f, 0.9f, 0.06f), _gris);
        Pieza("Pata_R", PrimitiveType.Cube, raiz.transform,
              new Vector3( 0.30f, 0.45f, 0.10f), new Vector3(0.06f, 0.9f, 0.06f), _gris);
        Pieza("Pata_C", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.45f, -0.22f), new Vector3(0.06f, 0.9f, 0.06f), _gris);
        Pieza("Camara", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.98f, 0f), new Vector3(0.34f, 0.26f, 0.42f), _negro);
        Pieza("Lente", PrimitiveType.Cylinder, raiz.transform,
              new Vector3(0f, 0.98f, 0.28f), new Vector3(0.16f, 0.10f, 0.16f), _gris);

        // El fotógrafo parado atrás, con chaleco.
        Pieza("Piernas", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 0.40f, -0.55f), new Vector3(0.40f, 0.80f, 0.30f), _negro);
        Pieza("Chaleco", PrimitiveType.Cube, raiz.transform,
              new Vector3(0f, 1.10f, -0.55f), new Vector3(0.52f, 0.62f, 0.32f), _gris);
        Pieza("Cabeza", PrimitiveType.Sphere, raiz.transform,
              new Vector3(0f, 1.58f, -0.55f), new Vector3(0.30f, 0.30f, 0.30f), _piel);

        raiz.AddComponent<ObstacleEstatico>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Fotografo.prefab");
    }

    // ── 8. Hincha invasor que cruza corriendo ─────────────────────────────────
    static void ConstruirInvasor()
    {
        GameObject raiz = Raiz("Obstaculo_Invasor", new Vector3(1.9f, 1.0f, 1.0f), new Vector3(0f, 0.5f, 0f));

        GameObject cuerpo = new GameObject("Cuerpo");
        cuerpo.transform.SetParent(raiz.transform, false);

        Pieza("Piernas", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(0f, 0.30f, 0f), new Vector3(0.42f, 0.60f, 0.30f), _azulRival);
        Pieza("Torso", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(0f, 0.88f, 0f), new Vector3(0.54f, 0.62f, 0.32f), _blanco);
        Pieza("Cabeza", PrimitiveType.Sphere, cuerpo.transform,
              new Vector3(0f, 1.34f, 0f), new Vector3(0.30f, 0.30f, 0.30f), _piel);
        Pieza("Brazo_L", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(-0.38f, 1.10f, 0f), new Vector3(0.12f, 0.55f, 0.14f), _piel);
        Pieza("Brazo_R", PrimitiveType.Cube, cuerpo.transform,
              new Vector3( 0.38f, 1.10f, 0f), new Vector3(0.12f, 0.55f, 0.14f), _piel);

        // El invasor también avisa con la marca en el piso y el "!".
        GameObject telegrafo = new GameObject("Telegrafo");
        telegrafo.transform.SetParent(raiz.transform, false);
        Pieza("Marca", PrimitiveType.Cube, telegrafo.transform,
              Vector3.zero, new Vector3(7.6f, 0.02f, 1.7f), _rojo);

        GameObject alerta = new GameObject("Alerta");
        alerta.transform.SetParent(raiz.transform, false);
        Pieza("Barra", PrimitiveType.Cube, alerta.transform,
              new Vector3(0f, 0.62f, 0f), new Vector3(0.26f, 0.78f, 0.26f), _rojo);
        Pieza("Punto", PrimitiveType.Cube, alerta.transform,
              new Vector3(0f, 0.10f, 0f), new Vector3(0.26f, 0.24f, 0.26f), _rojo);

        raiz.AddComponent<ObstacleDeslizante>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Invasor.prefab");
    }

    // ── 9. Banderín / cartel móvil (extra opcional) ───────────────────────────
    static void ConstruirBanderin()
    {
        GameObject raiz = Raiz("Obstaculo_Banderin", new Vector3(2.0f, 1.4f, 1.0f), new Vector3(0f, 0.7f, 0f));

        GameObject cuerpo = new GameObject("Cuerpo");
        cuerpo.transform.SetParent(raiz.transform, false);

        Pieza("Mastil", PrimitiveType.Cylinder, cuerpo.transform,
              new Vector3(0f, 0.75f, 0f), new Vector3(0.09f, 0.75f, 0.09f), _gris);
        Pieza("Tela", PrimitiveType.Cube, cuerpo.transform,
              new Vector3(0.6f, 1.25f, 0f), new Vector3(1.1f, 0.7f, 0.05f), _dorado);
        Pieza("Base", PrimitiveType.Cylinder, cuerpo.transform,
              new Vector3(0f, 0.05f, 0f), new Vector3(0.45f, 0.05f, 0.45f), _verdeCancha);

        GameObject telegrafo = new GameObject("Telegrafo");
        telegrafo.transform.SetParent(raiz.transform, false);
        Pieza("Marca", PrimitiveType.Cube, telegrafo.transform,
              Vector3.zero, new Vector3(7.6f, 0.02f, 1.7f), _rojo);

        GameObject alerta = new GameObject("Alerta");
        alerta.transform.SetParent(raiz.transform, false);
        Pieza("Barra", PrimitiveType.Cube, alerta.transform,
              new Vector3(0f, 0.62f, 0f), new Vector3(0.26f, 0.78f, 0.26f), _rojo);
        Pieza("Punto", PrimitiveType.Cube, alerta.transform,
              new Vector3(0f, 0.10f, 0f), new Vector3(0.26f, 0.24f, 0.26f), _rojo);

        raiz.AddComponent<ObstacleDeslizante>();
        GuardarPrefab(raiz, $"{CarpetaPrefabs}/Obstaculo_Banderin.prefab");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  OBSTACLE DATA
    // ═════════════════════════════════════════════════════════════════════════

    static void ConstruirDatos(StringBuilder log)
    {
        _odCono = Dato("OD_Cono", "cono", TipoObstaculo.Estatico,
                       $"{CarpetaPrefabs}/Obstaculo_Cono.prefab",
                       danio: 8f, efecto: EfectoObstaculo.Ninguno, nivelMin: 1, peso: 1f,
                       carriles: 1, saltable: true, altura: 0.85f);

        _odValla = Dato("OD_Valla", "valla", TipoObstaculo.Estatico,
                        $"{CarpetaPrefabs}/Obstaculo_Valla.prefab",
                        danio: 10f, efecto: EfectoObstaculo.Ninguno, nivelMin: 1, peso: 1f,
                        carriles: 1, saltable: true, altura: 0.9f);

        _odBarrera = Dato("OD_Barrera", "barrera_futbolistas", TipoObstaculo.Estatico,
                          $"{CarpetaPrefabs}/Obstaculo_Barrera.prefab",
                          danio: 18f, efecto: EfectoObstaculo.Ninguno, nivelMin: 1, peso: 0.8f,
                          carriles: 2, saltable: false, altura: 1.9f);

        _odArbitro = Dato("OD_Arbitro", "arbitro_quieto", TipoObstaculo.Estatico,
                          $"{CarpetaPrefabs}/Obstaculo_Arbitro.prefab",
                          danio: 12f, efecto: EfectoObstaculo.TarjetaAmarilla, nivelMin: 2, peso: 1f,
                          carriles: 1, saltable: false, altura: 1.9f, ralentizacion: 1f);

        _odArbitroMovil = Dato("OD_ArbitroMovil", "arbitro_movil", TipoObstaculo.Patrulla,
                               $"{CarpetaPrefabs}/Obstaculo_ArbitroMovil.prefab",
                               danio: 12f, efecto: EfectoObstaculo.TarjetaAmarilla, nivelMin: 3, peso: 0.9f,
                               carriles: 1, saltable: false, altura: 1.9f, ralentizacion: 1f);

        _odDefensa = Dato("OD_Defensa", "defensa_deslizante", TipoObstaculo.Deslizante,
                          $"{CarpetaPrefabs}/Obstaculo_Defensa.prefab",
                          danio: 25f, efecto: EfectoObstaculo.PelotaRobada, nivelMin: 3, peso: 0.9f,
                          carriles: 1, saltable: false, altura: 1.4f,
                          velocidadLateral: 7f, telegrafiado: 1.2f);

        _odPelota = Dato("OD_PelotaGigante", "pelota_gigante", TipoObstaculo.Rodante,
                         $"{CarpetaPrefabs}/Obstaculo_PelotaGigante.prefab",
                         danio: 20f, efecto: EfectoObstaculo.Ninguno, nivelMin: 4, peso: 0.7f,
                         carriles: 1, saltable: false, altura: 2f);

        _odFotografo = Dato("OD_Fotografo", "fotografo", TipoObstaculo.Estatico,
                            $"{CarpetaPrefabs}/Obstaculo_Fotografo.prefab",
                            danio: 10f, efecto: EfectoObstaculo.Ninguno, nivelMin: 4, peso: 0.6f,
                            carriles: 1, saltable: false, altura: 1.9f);

        _odInvasor = Dato("OD_Invasor", "invasor", TipoObstaculo.Deslizante,
                          $"{CarpetaPrefabs}/Obstaculo_Invasor.prefab",
                          danio: 15f, efecto: EfectoObstaculo.Ralentizar, nivelMin: 5, peso: 0.7f,
                          carriles: 1, saltable: false, altura: 1.4f,
                          velocidadLateral: 9f, telegrafiado: 0.9f, ralentizacion: 1f);

        _odBanderin = Dato("OD_Banderin", "banderin", TipoObstaculo.Deslizante,
                           $"{CarpetaPrefabs}/Obstaculo_Banderin.prefab",
                           danio: 10f, efecto: EfectoObstaculo.Ninguno, nivelMin: 6, peso: 0.5f,
                           carriles: 1, saltable: false, altura: 1.4f,
                           velocidadLateral: 6f, telegrafiado: 1.1f);

        log.AppendLine("10 ObstacleData creados en " + CarpetaDatos);
    }

    static ObstacleData Dato(string nombre, string id, TipoObstaculo tipo, string rutaPrefab,
                             float danio, EfectoObstaculo efecto, int nivelMin, float peso,
                             int carriles, bool saltable, float altura,
                             float velocidadLateral = 0f, float telegrafiado = 1.2f,
                             float ralentizacion = 0f)
    {
        string ruta = $"{CarpetaDatos}/{nombre}.asset";
        if (AssetDatabase.LoadAssetAtPath<ObstacleData>(ruta) != null)
            AssetDatabase.DeleteAsset(ruta);

        var dato = ScriptableObject.CreateInstance<ObstacleData>();
        dato.id                 = id;
        dato.tipo               = tipo;
        dato.prefab             = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        dato.danioEnergia       = danio;
        dato.efecto             = efecto;
        dato.nivelMinimo        = nivelMin;
        dato.peso               = peso;
        dato.carrilesQueOcupa   = carriles;
        dato.sePuedeSaltar      = saltable;
        dato.altura             = altura;
        dato.velocidadLateral   = velocidadLateral;
        dato.tiempoTelegrafiado = telegrafiado;
        dato.duracionRalentizacion = ralentizacion;

        AssetDatabase.CreateAsset(dato, ruta);
        return dato;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  LEVEL CONFIG
    // ═════════════════════════════════════════════════════════════════════════

    static void ConstruirLevelConfig(StringBuilder log)
    {
        if (AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaLevelConfig) != null)
            AssetDatabase.DeleteAsset(RutaLevelConfig);

        var config = ScriptableObject.CreateInstance<LevelConfig>();
        config.tiempoInvulnerabilidad     = 1f;
        config.velocidadLateralPorDefecto = 7f;
        config.niveles = new List<NivelObstaculos>
        {
            Nivel(1, "Entrenamiento", 1.00f, 1.0f, 30f, 0.00f,
                  E(_odCono, 2f), E(_odValla, 1f), E(_odBarrera, 0.6f)),

            Nivel(2, "Grupos", 1.05f, 1.2f, 28f, 0.10f,
                  E(_odCono, 1.5f), E(_odValla, 1f), E(_odBarrera, 0.8f), E(_odArbitro, 1f)),

            Nivel(3, "Octavos", 1.12f, 1.5f, 26f, 0.20f,
                  E(_odCono, 1f), E(_odValla, 0.8f), E(_odBarrera, 1f),
                  E(_odArbitro, 1f), E(_odArbitroMovil, 0.8f), E(_odDefensa, 0.8f)),

            Nivel(4, "Cuartos", 1.20f, 1.8f, 24f, 0.35f,
                  E(_odCono, 0.8f), E(_odValla, 0.6f), E(_odBarrera, 1f),
                  E(_odArbitro, 0.9f), E(_odArbitroMovil, 1f), E(_odDefensa, 1f),
                  E(_odPelota, 0.7f), E(_odFotografo, 0.6f)),

            Nivel(5, "Semifinal", 1.28f, 2.2f, 22f, 0.50f,
                  E(_odCono, 0.6f), E(_odValla, 0.5f), E(_odBarrera, 1.2f),
                  E(_odArbitro, 0.8f), E(_odArbitroMovil, 1.2f), E(_odDefensa, 1.3f),
                  E(_odPelota, 0.9f), E(_odFotografo, 0.7f), E(_odInvasor, 0.8f)),

            Nivel(6, "Final", 1.35f, 2.6f, 20f, 0.60f,
                  E(_odCono, 0.5f), E(_odValla, 0.4f), E(_odBarrera, 1.4f),
                  E(_odArbitro, 0.8f), E(_odArbitroMovil, 1.4f), E(_odDefensa, 1.5f),
                  E(_odPelota, 1.1f), E(_odFotografo, 0.8f), E(_odInvasor, 1f),
                  E(_odBanderin, 0.6f)),
        };

        AssetDatabase.CreateAsset(config, RutaLevelConfig);
        log.AppendLine($"LevelConfig creado con {config.niveles.Count} niveles: {RutaLevelConfig}");
    }

    static NivelObstaculos Nivel(int nivel, string nombre, float multVel, float filas,
                                 float margen, float probDos, params EntradaObstaculo[] permitidos)
    {
        var n = new NivelObstaculos
        {
            nivel = nivel,
            nombre = nombre,
            multiplicadorVelocidad = multVel,
            filasPorSegmento = filas,
            margenEntreFilas = margen,
            probabilidadDosCarriles = probDos,
            permitidos = new List<EntradaObstaculo>(permitidos)
        };
        return n;
    }

    static EntradaObstaculo E(ObstacleData dato, float peso)
        => new EntradaObstaculo { dato = dato, peso = peso };

    // ═════════════════════════════════════════════════════════════════════════
    //  ESCENA
    // ═════════════════════════════════════════════════════════════════════════

    static void CablearEscena(StringBuilder log)
    {
        var existente = Object.FindFirstObjectByType<ObstacleManager>(FindObjectsInactive.Include);
        GameObject go;

        if (existente != null)
        {
            go = existente.gameObject;
            log.AppendLine("ObstacleManager ya existía: se reutiliza.");
        }
        else
        {
            go = new GameObject("ObstacleManager");
            go.AddComponent<ObstacleManager>();
            log.AppendLine("ObstacleManager creado en la escena.");
        }

        var so = new SerializedObject(go.GetComponent<ObstacleManager>());
        var propConfig = so.FindProperty("levelConfig");
        if (propConfig != null)
        {
            propConfig.objectReferenceValue = AssetDatabase.LoadAssetAtPath<LevelConfig>(RutaLevelConfig);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(go);

        // Guardamos también el prefab del segmento normal para que su
        // ObstacleSpawner use la nueva lógica (no hace falta tocar el prefab:
        // el script ya cambió).
        var escena = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        log.AppendLine("Escena guardada.");
    }
}
