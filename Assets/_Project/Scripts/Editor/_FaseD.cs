using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Text;

/// <summary>
/// Constructor y sonda de la Fase D (la pelota del dribble).
///
/// Acciones (argumento "accion"):
///   aplicar  -> arma en la escena la raíz "BallDribble" con la pelota y guarda.
///   estado   -> (Play Mode) reporta dónde está la pelota respecto del jugador.
///   robar    -> (Play Mode) simula el tackle y dispara BallDribble.Robar().
///   secuencia-> (Play Mode) arranca la carrera, roba a los 1,5 s y muestrea
///               cada 0,4 s hasta los 6 s. Todo en una sola llamada, porque
///               cada ida y vuelta de herramienta tarda más que la carrera.
///
/// Carpeta: Assets/_Project/Scripts/Editor/
/// </summary>
public static class _FaseD
{
    const string RutaMaterial = "Assets/_Project/Materials/Mat_PenaltyBall.mat";

    public static void Execute(Newtonsoft.Json.Linq.JObject args)
    {
        string accion = args != null && args["accion"] != null ? args["accion"].ToString() : "estado";

        if (accion == "aplicar")   { Aplicar();   return; }
        if (accion == "robar")     { Robar();     return; }
        if (accion == "secuencia") { Secuencia(); return; }

        Estado();
    }

    // ─── Aplicar: arma la jerarquía en la escena ──────────────────────────────

    static void Aplicar()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE D: armar la pelota del dribble ===");

        // 1. Raíz con el componente.
        GameObject raiz = GameObject.Find("BallDribble");
        if (raiz == null)
        {
            raiz = new GameObject("BallDribble");
            Undo.RegisterCreatedObjectUndo(raiz, "Crear BallDribble");
            log.AppendLine("raíz BallDribble: creada");
        }
        else log.AppendLine("raíz BallDribble: ya existía");

        raiz.transform.SetParent(null);
        raiz.transform.localScale = Vector3.one;

        // 2. La pelota (esfera hija).
        Transform hija = raiz.transform.Find("Pelota");
        if (hija == null)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Pelota";
            Undo.RegisterCreatedObjectUndo(go, "Crear Pelota");
            hija = go.transform;
            hija.SetParent(raiz.transform, false);
            log.AppendLine("pelota: creada (esfera)");
        }
        else log.AppendLine("pelota: ya existía");

        hija.localPosition = Vector3.zero;
        hija.localRotation = Quaternion.identity;
        hija.localScale    = Vector3.one * 0.44f;

        // 3. Sin física: la pelota es sólo visual, la mueve el script.
        Collider col = hija.GetComponent<Collider>();
        if (col != null) { Object.DestroyImmediate(col); log.AppendLine("collider de la pelota: borrado"); }

        Rigidbody rb = hija.GetComponent<Rigidbody>();
        if (rb != null) { Object.DestroyImmediate(rb); log.AppendLine("rigidbody de la pelota: borrado"); }

        // 4. Mismo material que la pelota del penal.
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial);
        MeshRenderer mr = hija.GetComponent<MeshRenderer>();
        if (mat != null && mr != null)
        {
            mr.sharedMaterial = mat;
            log.AppendLine($"material: {mat.name}");
        }
        else log.AppendLine($"material: NO aplicado (mat={mat != null}, renderer={mr != null})");

        // 5. El componente.
        BallDribble comp = raiz.GetComponent<BallDribble>();
        if (comp == null)
        {
            comp = raiz.AddComponent<BallDribble>();
            log.AppendLine("BallDribble: agregado");
        }
        else log.AppendLine("BallDribble: ya estaba");

        // 6. Referencia a la pelota (campo privado con [SerializeField]).
        var so = new SerializedObject(comp);
        var prop = so.FindProperty("pelota");
        if (prop != null)
        {
            prop.objectReferenceValue = hija;
            so.ApplyModifiedPropertiesWithoutUndo();
            log.AppendLine("referencia 'pelota': asignada");
        }

        // 7. Posición inicial: a los pies del jugador. El jugador se instancia al
        // arrancar la partida, así que en la escena nos guiamos por el punto de spawn.
        var jugador = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (jugador != null)
        {
            raiz.transform.position = jugador.transform.position;
            log.AppendLine($"posición inicial: {jugador.transform.position} (pies del jugador)");
        }
        else
        {
            GameObject spawn = GameObject.Find("PlayerSpawnPoint");
            raiz.transform.position = spawn != null ? spawn.transform.position : Vector3.zero;
            log.AppendLine($"posición inicial: {raiz.transform.position} " +
                           $"({(spawn != null ? "PlayerSpawnPoint" : "origen")})");
        }

        EditorUtility.SetDirty(raiz);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        log.AppendLine("escena guardada");

        Debug.Log(log.ToString());
    }

    // ─── Robar: dispara el tackle desde la sonda ──────────────────────────────

    static void Robar()
    {
        BallDribble comp = BallDribble.Instance;
        if (comp == null) { Debug.Log("[FASE D] no hay BallDribble activo (¿está en Play Mode?)"); return; }

        comp.Robar();
        Debug.Log($"[FASE D] Robar() disparado. TienePelota={comp.TienePelota}");
    }

    // ─── Secuencia: arranque, robo y muestreo en una sola llamada ────────────

    // Campos estáticos: las clases del proyecto viven en el AppDomain y
    // sobreviven entre llamadas, así el tick programado sigue funcionando
    // después de que esta herramienta devuelve el control al editor.
    static BallDribble _sonda;
    static StringBuilder _bitacora;
    static float _t0;
    static float _siguienteMuestra;
    static bool  _yaRobo;
    static bool  _activa;
    static int   _frameMuestreado = -1;

    static void Secuencia()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[FASE D] la secuencia necesita Play Mode");
            return;
        }

        _sonda = BallDribble.Instance;
        if (_sonda == null) _sonda = Object.FindFirstObjectByType<BallDribble>(FindObjectsInactive.Include);
        if (_sonda == null) { Debug.Log("[FASE D] no encontré BallDribble en la escena"); return; }

        // Si el manager quedó en otro estado (menú, penal, game over), arrancamos.
        var gm = RunnerGameManager.Instance;
        if (gm != null && gm.State != GameState.Running) gm.StartGame();

        _bitacora = new StringBuilder();
        _bitacora.AppendLine("=== FASE D: secuencia (robo a los 1,5 s, muestras cada 0,4 s) ===");

        _t0              = Time.realtimeSinceStartup;
        _siguienteMuestra = 0.4f;
        _yaRobo          = false;
        _activa          = true;
        _frameMuestreado = -1;

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!_activa) { EditorApplication.update -= Tick; return; }

        if (!Application.isPlaying) { Cerrar("se salió de Play Mode"); return; }

        float t = Time.realtimeSinceStartup - _t0;

        // Una muestra por frame como máximo, aunque el tick corra varias veces.
        if (t < _siguienteMuestra || Time.frameCount == _frameMuestreado) return;

        _frameMuestreado = Time.frameCount;
        Muestrear(t);
        _siguienteMuestra = t + 0.4f;

        if (!_yaRobo && t >= 1.5f)
        {
            _yaRobo = true;
            _sonda.Robar();
            _bitacora.AppendLine($"  >> t={t:0.00}  Robar() disparado. TienePelota={_sonda.TienePelota}");
        }

        if (t >= 6f) Cerrar(null);
    }

    static void Muestrear(float t)
    {
        Transform pelota  = _sonda.transform.Find("Pelota");
        var jugador = PlayerController.Instance;
        var gm      = RunnerGameManager.Instance;

        string estadoJuego = gm != null ? gm.State.ToString() : "sin manager";

        Vector3 p = pelota != null ? pelota.position : Vector3.zero;
        float distancia = -1f;
        if (pelota != null && jugador != null)
            distancia = Vector2.Distance(new Vector2(p.x, p.z),
                                         new Vector2(jugador.transform.position.x,
                                                     jugador.transform.position.z));

        _bitacora.AppendLine(
            $"t={t:0.00}  estado={estadoJuego}  tienePelota={_sonda.TienePelota}  " +
            $"visible={(pelota != null && pelota.gameObject.activeSelf)}  " +
            $"dist={(distancia >= 0f ? distancia.ToString("0.00") : "s/d")} m  " +
            $"pelota={p}  jugador={(jugador != null ? jugador.transform.position.ToString() : "s/d")}");
    }

    static void Cerrar(string motivo)
    {
        _activa = false;
        EditorApplication.update -= Tick;

        if (motivo != null) _bitacora.AppendLine("cortado: " + motivo);
        _bitacora.AppendLine("=== fin de la secuencia ===");
        Debug.Log(_bitacora.ToString());
    }

    // ─── Estado: dónde está la pelota ────────────────────────────────────────

    static void Estado()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASE D: estado ===");

        var comp    = Object.FindFirstObjectByType<BallDribble>(FindObjectsInactive.Include);
        var jugador = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        log.AppendLine($"BallDribble: {(comp != null ? comp.name : "NO ESTÁ")}  " +
                       $"instancia={(comp != null && BallDribble.Instance == comp)}");

        if (comp == null) { Debug.Log(log.ToString()); return; }

        log.AppendLine($"TienePelota={comp.TienePelota}");

        var gm = RunnerGameManager.Instance;
        log.AppendLine($"estado del juego={(gm != null ? gm.State.ToString() : "sin manager")} " +
                       $"vivo={(jugador != null && jugador.IsAlive)}");

        Transform pelota = comp.transform.Find("Pelota");
        if (pelota == null) { log.AppendLine("no encontré el hijo 'Pelota'"); Debug.Log(log.ToString()); return; }

        Vector3 p = pelota.position;
        log.AppendLine($"pelota: pos={p} visible={pelota.gameObject.activeSelf} " +
                       $"escala={pelota.localScale.x:0.00} rot={pelota.rotation.eulerAngles}");

        if (jugador == null) { log.AppendLine("no hay PlayerController"); Debug.Log(log.ToString()); return; }

        Vector3 j = jugador.transform.position;
        float distancia = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(j.x, j.z));

        log.AppendLine($"jugador: pos={j} velocidad={jugador.CurrentSpeed:0.0}");
        log.AppendLine($"distancia al jugador (plano XZ) = {distancia:0.00} m");
        log.AppendLine($"altura sobre el piso = {p.y - j.y:0.00} m");

        Debug.Log(log.ToString());
    }
}
