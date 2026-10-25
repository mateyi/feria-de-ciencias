using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Text;

/// <summary>
/// Constructor y sonda de las fases E (pausa) y F (brillo, volumen y silencio).
///
/// Acciones (argumento "accion"):
///   aplicar -> arma en el Canvas el panel de pausa, el de ajustes, el velo del
///              brillo y el botón de pausa; engancha todo y guarda la escena.
///   ver     -> (fuera o dentro de Play Mode) reporta cómo quedó armado.
///   probar  -> (Play Mode) prueba la pausa y los ajustes en una sola llamada:
///              arranca la carrera, pausa, mide que el mundo quede congelado,
///              reanuda, abre ajustes, mueve volumen y brillo y los guarda.
///   pausar  -> (Play Mode) deja el juego pausado a la vista, para una captura.
///   ajustes -> (Play Mode) abre el panel de ajustes, para una captura.
///   restaurar -> devuelve volumen y brillo a los valores de fábrica.
///
/// Carpeta: Assets/_Project/Scripts/Editor/
/// </summary>
public static class _FaseEF
{
    public static void Execute(Newtonsoft.Json.Linq.JObject args)
    {
        string accion = args != null && args["accion"] != null ? args["accion"].ToString() : "ver";

        if (accion == "aplicar")   { Aplicar();   return; }
        if (accion == "probar")    { Probar();    return; }
        if (accion == "pausar")    { PausarParaMirar();  return; }
        if (accion == "ajustes")   { AbrirAjustes();     return; }
        if (accion == "restaurar") { RestaurarAjustes(); return; }

        Ver();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  APLICAR
    // ═════════════════════════════════════════════════════════════════════════

    static void Aplicar()
    {
        if (Application.isPlaying)
        {
            Debug.Log("[FASE EF] salí de Play Mode antes de aplicar los cambios.");
            return;
        }

        var log = new StringBuilder();
        log.AppendLine("=== FASES E y F: pausa y ajustes ===");

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.Log("[FASE EF] no encontré el Canvas en la escena"); return; }

        // ─── FASE F: velo del brillo ─────────────────────────────────────────
        // Es una imagen negra de pantalla completa que se dibuja encima de la
        // escena. Se deja PRIMERA en la jerarquía del Canvas para que quede
        // detrás de todos los paneles: oscurece el juego, no la interfaz.
        GameObject velo = PanelSiFalta(canvas.transform, "BrightnessOverlay",
                                       Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        velo.GetComponent<Image>().raycastTarget = false;
        velo.transform.SetSiblingIndex(0);
        log.AppendLine("velo de brillo: listo");

        // ─── FASE E: panel de pausa ──────────────────────────────────────────
        GameObject pausa = PanelSiFalta(canvas.transform, "PausePanel",
                                        Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.78f));
        pausa.transform.SetAsLastSibling();   // se dibuja sobre todo lo demás
        log.AppendLine("panel de pausa: listo");

        GameObject tarjeta = PanelSiFalta(pausa.transform, "Tarjeta",
                                          new Vector2(0.10f, 0.20f), new Vector2(0.90f, 0.80f),
                                          new Color(0.08f, 0.11f, 0.17f, 0.98f));

        TextoSiFalta(tarjeta.transform, "Titulo", "PAUSA", 52,
                     new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.97f),
                     TextAlignmentOptions.Center, true);

        Button botonSeguir = BotonSiFalta(tarjeta.transform, "BotonSeguir", "SEGUIR",
                                          new Vector2(0.14f, 0.44f), new Vector2(0.86f, 0.62f),
                                          new Color(0.16f, 0.62f, 0.32f, 1f), 38);

        Button botonMenu = BotonSiFalta(tarjeta.transform, "BotonMenu", "MENU",
                                        new Vector2(0.14f, 0.20f), new Vector2(0.86f, 0.38f),
                                        new Color(0.28f, 0.33f, 0.45f, 1f), 38);

        // El panel arranca apagado: se muestra sólo cuando el juego se pausa.
        pausa.SetActive(false);

        // ─── FASE E: el botón "≡" del HUD ────────────────────────────────────
        // Se dibuja con tres rectángulos y no con el glifo "≡": la fuente del
        // proyecto no siempre lo tiene y TextMeshPro lo reemplaza por un
        // cuadrado vacío.
        Transform botonPausaT = canvas.transform.Find("GameplayPanel/PauseButton");
        Button botonPausa = null;

        if (botonPausaT == null) log.AppendLine("ERROR: no encontré GameplayPanel/PauseButton");
        else
        {
            botonPausa = botonPausaT.GetComponent<Button>();
            if (botonPausa == null) botonPausa = botonPausaT.gameObject.AddComponent<Button>();
            botonPausa.targetGraphic = botonPausaT.GetComponent<Image>();

            // El texto viejo (el glifo que salía como cuadrado) se saca.
            Transform iconoViejo = botonPausaT.Find("PauseIcon");
            if (iconoViejo != null)
            {
                Object.DestroyImmediate(iconoViejo.gameObject);
                log.AppendLine("ícono de pausa: texto viejo borrado");
            }

            Transform barras = botonPausaT.Find("PauseBars");
            if (barras == null)
            {
                var go = new GameObject("PauseBars", typeof(RectTransform));
                go.transform.SetParent(botonPausaT, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                barras = go.transform;
            }

            // Tres barras horizontales centradas: el "≡" dibujado a mano.
            float[] centros = { 0.72f, 0.50f, 0.28f };
            for (int i = 0; i < centros.Length; i++)
            {
                string nombre = "Barra_" + i;
                if (barras.Find(nombre) != null) continue;

                var barra = new GameObject(nombre, typeof(RectTransform));
                barra.transform.SetParent(barras, false);

                var rt = barra.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.22f, centros[i] - 0.06f);
                rt.anchorMax = new Vector2(0.78f, centros[i] + 0.06f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var img = barra.AddComponent<Image>();
                img.color = Color.white;
                img.raycastTarget = false;
            }

            log.AppendLine("ícono de pausa: tres barras listas");
        }

        // ─── FASE F: panel de ajustes ────────────────────────────────────────
        GameObject ajustes = canvas.transform.Find("SettingsPanel")?.gameObject;

        Slider  sliderVolumen = null;
        Slider  sliderBrillo  = null;
        Button  botonSilencio = null;
        Button  botonVolver   = null;
        TMP_Text etiquetaSilencio = null;

        if (ajustes == null) log.AppendLine("ERROR: no encontré Canvas/SettingsPanel");
        else
        {
            // El panel ya existía pero era invisible (alpha 0). Se le pone un
            // fondo oscuro para que se lea como el resto de las pantallas.
            var fondoPanel = ajustes.GetComponent<Image>();
            if (fondoPanel != null) fondoPanel.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

            TextoSiFalta(ajustes.transform, "Titulo", "AJUSTES", 46,
                         new Vector2(0.10f, 0.80f), new Vector2(0.90f, 0.92f),
                         TextAlignmentOptions.Center, true);

            TextoSiFalta(ajustes.transform, "EtiquetaVolumen", "VOLUMEN", 34,
                         new Vector2(0.08f, 0.66f), new Vector2(0.42f, 0.74f),
                         TextAlignmentOptions.Left, true);

            sliderVolumen = SliderSiFalta(ajustes.transform, "VolumenSlider",
                                          new Vector2(0.40f, 0.66f), new Vector2(0.92f, 0.74f),
                                          new Color(0.20f, 0.80f, 0.45f, 1f));

            TextoSiFalta(ajustes.transform, "EtiquetaBrillo", "BRILLO", 34,
                         new Vector2(0.08f, 0.54f), new Vector2(0.42f, 0.62f),
                         TextAlignmentOptions.Left, true);

            sliderBrillo = SliderSiFalta(ajustes.transform, "BrilloSlider",
                                         new Vector2(0.40f, 0.54f), new Vector2(0.92f, 0.62f),
                                         new Color(0.95f, 0.85f, 0.25f, 1f));

            botonSilencio = BotonSiFalta(ajustes.transform, "BotonSilencio", "SONIDO: SI",
                                         new Vector2(0.22f, 0.38f), new Vector2(0.78f, 0.48f),
                                         new Color(0.18f, 0.22f, 0.30f, 1f), 34);

            Transform etiquetaT = botonSilencio.transform.Find("Label");
            if (etiquetaT != null) etiquetaSilencio = etiquetaT.GetComponent<TMP_Text>();

            botonVolver = BotonSiFalta(ajustes.transform, "BotonVolver", "VOLVER",
                                       new Vector2(0.22f, 0.18f), new Vector2(0.78f, 0.28f),
                                       new Color(0.20f, 0.45f, 0.75f, 1f), 36);

            log.AppendLine("panel de ajustes: listo");
        }

        // ─── Enganches ───────────────────────────────────────────────────────
        // Los botones se conectan por código (Awake de PauseUI y SettingsUI),
        // así no quedan eventos del Inspector que se dupliquen al re-aplicar.
        var pauseUI = canvas.GetComponent<PauseUI>();
        if (pauseUI == null) pauseUI = canvas.AddComponent<PauseUI>();

        Asignar(pauseUI, "botonPausa",  botonPausa,  log);
        Asignar(pauseUI, "botonSeguir", botonSeguir, log);
        Asignar(pauseUI, "botonMenu",   botonMenu,   log);

        var settingsUI = canvas.GetComponent<SettingsUI>();
        if (settingsUI == null) settingsUI = canvas.AddComponent<SettingsUI>();

        Asignar(settingsUI, "volumenSlider",    sliderVolumen,    log);
        Asignar(settingsUI, "brilloSlider",     sliderBrillo,     log);
        Asignar(settingsUI, "botonSilencio",    botonSilencio,    log);
        Asignar(settingsUI, "etiquetaSilencio", etiquetaSilencio, log);
        Asignar(settingsUI, "veloBrillo",       velo.GetComponent<Image>(), log);
        Asignar(settingsUI, "botonVolver",      botonVolver,      log);

        var runner = Object.FindFirstObjectByType<RunnerGameManager>(FindObjectsInactive.Include);
        if (runner != null) Asignar(runner, "pausePanel", pausa, log);
        else log.AppendLine("ERROR: no encontré el RunnerGameManager");

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        log.AppendLine("escena guardada");

        Debug.Log(log.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  VER
    // ═════════════════════════════════════════════════════════════════════════

    static void Ver()
    {
        var log = new StringBuilder();
        log.AppendLine("=== FASES E y F: cómo quedó armado ===");

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.Log("[FASE EF] no encontré el Canvas"); return; }

        Transform pausa    = canvas.transform.Find("PausePanel");
        Transform ajustes  = canvas.transform.Find("SettingsPanel");
        Transform velo     = canvas.transform.Find("BrightnessOverlay");
        Transform pausaBtn = canvas.transform.Find("GameplayPanel/PauseButton");

        log.AppendLine($"PausePanel: {(pausa != null ? (pausa.gameObject.activeSelf ? "existe (visible)" : "existe (apagado)") : "NO ESTÁ")}");
        log.AppendLine($"SettingsPanel: {(ajustes != null ? "existe" : "NO ESTÁ")}");
        log.AppendLine($"BrightnessOverlay: {(velo != null ? $"sibling={velo.GetSiblingIndex()} alpha={velo.GetComponent<Image>().color.a:0.00}" : "NO ESTÁ")}");

        if (pausa != null)
        {
            Transform tarjeta = pausa.Find("Tarjeta");
            log.AppendLine($"  Tarjeta: {(tarjeta != null ? "ok" : "FALTA")}");
            log.AppendLine($"  BotonSeguir: {(tarjeta != null && tarjeta.Find("BotonSeguir") != null ? "ok" : "FALTA")}");
            log.AppendLine($"  BotonMenu: {(tarjeta != null && tarjeta.Find("BotonMenu") != null ? "ok" : "FALTA")}");
        }

        if (pausaBtn != null)
        {
            log.AppendLine($"PauseButton: bars={(pausaBtn.Find("PauseBars") != null)}, " +
                           $"textoViejo={(pausaBtn.Find("PauseIcon") != null)}");
        }

        if (ajustes != null)
        {
            string[] nombres = { "Titulo", "EtiquetaVolumen", "VolumenSlider",
                                 "EtiquetaBrillo", "BrilloSlider", "BotonSilencio", "BotonVolver" };
            foreach (string n in nombres)
                log.AppendLine($"  {n}: {(ajustes.Find(n) != null ? "ok" : "FALTA")}");
        }

        var pauseUI = canvas.GetComponent<PauseUI>();
        log.AppendLine($"PauseUI en Canvas: {(pauseUI != null ? "sí" : "NO")}");
        if (pauseUI != null)
        {
            var so = new SerializedObject(pauseUI);

            // Los nombres se calculan aparte: así no quedan comillas dentro de la
            // interpolación y el archivo se lee igual en cualquier versión de C#.
            string bp = Nombre(so, "botonPausa");
            string bs = Nombre(so, "botonSeguir");
            string bm = Nombre(so, "botonMenu");

            log.AppendLine($"  botonPausa={bp}  botonSeguir={bs}  botonMenu={bm}");
        }

        var settingsUI = canvas.GetComponent<SettingsUI>();
        log.AppendLine($"SettingsUI en Canvas: {(settingsUI != null ? "sí" : "NO")}");
        if (settingsUI != null)
        {
            var so = new SerializedObject(settingsUI);

            string vSlider = Nombre(so, "volumenSlider");
            string bSlider = Nombre(so, "brilloSlider");
            string bSil    = Nombre(so, "botonSilencio");
            string veloRef = Nombre(so, "veloBrillo");
            string bVolver = Nombre(so, "botonVolver");

            log.AppendLine($"  volumenSlider={vSlider}  brilloSlider={bSlider}");
            log.AppendLine($"  botonSilencio={bSil}  veloBrillo={veloRef}  botonVolver={bVolver}");
        }

        var runner = Object.FindFirstObjectByType<RunnerGameManager>(FindObjectsInactive.Include);
        if (runner != null)
        {
            var so = new SerializedObject(runner);
            var prop = so.FindProperty("pausePanel");
            log.AppendLine($"RunnerGameManager.pausePanel = {(prop != null && prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "<NULL>")}");
        }

        log.AppendLine($"AudioListener.volume = {AudioListener.volume:0.00}");
        if (GameData.Instance != null)
            log.AppendLine($"guardado: volumen={GameData.Instance.Volumen:0.00} brillo={GameData.Instance.Brillo:0.00} silenciado={GameData.Instance.Silenciado}");

        Debug.Log(log.ToString());
    }

    static string Nombre(SerializedObject so, string campo)
    {
        var prop = so.FindProperty(campo);
        if (prop == null) return "<sin campo>";
        return prop.objectReferenceValue != null ? prop.objectReferenceValue.name : "<NULL>";
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  PROBAR (Play Mode)
    // ═════════════════════════════════════════════════════════════════════════

    // El editor tarda entre 3 y 30 s por ida y vuelta de herramienta, y una
    // carrera dura menos de eso: por eso todo se prueba dentro de una sola
    // llamada, con un reloj propio.
    static readonly float[] Tiempos = { 1.0f, 1.6f, 2.2f, 2.8f, 3.4f, 3.9f, 4.4f, 4.9f, 5.4f };

    static StringBuilder _log;
    static float _t0;
    static int   _paso;
    static bool  _activa;
    static float _zCongelado;
    static float _scoreCongelado;
    static GameObject _panelPausa;
    static GameObject _panelAjustes;
    static GameObject _velo;
    static Slider _sliderVolumen;
    static Slider _sliderBrillo;
    static Button _botonSilencio;
    static Button _botonVolver;

    static void Probar()
    {
        if (!Application.isPlaying)
        {
            Debug.Log("[FASE EF] la prueba necesita Play Mode");
            return;
        }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.Log("[FASE EF] no encontré el Canvas"); return; }

        Transform t = canvas.transform;
        _panelPausa    = t.Find("PausePanel")?.gameObject;
        _panelAjustes  = t.Find("SettingsPanel")?.gameObject;
        _velo          = t.Find("BrightnessOverlay")?.gameObject;
        _sliderVolumen = t.Find("SettingsPanel/VolumenSlider")?.GetComponent<Slider>();
        _sliderBrillo  = t.Find("SettingsPanel/BrilloSlider")?.GetComponent<Slider>();
        _botonSilencio = t.Find("SettingsPanel/BotonSilencio")?.GetComponent<Button>();
        _botonVolver   = t.Find("SettingsPanel/BotonVolver")?.GetComponent<Button>();

        var runner = RunnerGameManager.Instance;
        if (runner == null) { Debug.Log("[FASE EF] no hay RunnerGameManager activo"); return; }

        if (runner.State != GameState.Running) runner.StartGame();

        _log = new StringBuilder();
        _log.AppendLine("=== FASES E y F: prueba en vivo ===");
        _log.AppendLine($"paneles: pausa={(_panelPausa != null)} ajustes={(_panelAjustes != null)} " +
                        $"velo={(_velo != null)} sliders={(_sliderVolumen != null)}/{(_sliderBrillo != null)}");

        _t0    = Time.realtimeSinceStartup;
        _paso  = 0;
        _activa = true;

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (!_activa) { EditorApplication.update -= Tick; return; }
        if (!Application.isPlaying) { Cerrar("se salió de Play Mode"); return; }

        float t = Time.realtimeSinceStartup - _t0;

        while (_activa && _paso < Tiempos.Length && t >= Tiempos[_paso])
        {
            EjecutarPaso(_paso, t);
            _paso++;
        }

        if (_activa && _paso >= Tiempos.Length) Cerrar(null);
    }

    static void EjecutarPaso(int paso, float t)
    {
        var runner  = RunnerGameManager.Instance;
        var jugador = PlayerController.Instance;

        switch (paso)
        {
            // ── 0: medir el avance normal y pausar ──
            case 0:
                _zCongelado     = jugador != null ? jugador.transform.position.z : 0f;
                _scoreCongelado = runner != null ? runner.Score : 0;
                _log.AppendLine($"[t={t:0.0}] ANTES: estado={runner?.State} timeScale={Time.timeScale:0.0} " +
                                $"zJugador={_zCongelado:0.00} score={_scoreCongelado}");
                runner?.PauseGame();
                break;

            // ── 1: la pausa tiene que congelar el mundo ──
            case 1:
                _log.AppendLine($"[t={t:0.0}] EN PAUSA: Pausado={runner?.Pausado} timeScale={Time.timeScale:0.0} " +
                                $"panelPausa={( _panelPausa != null && _panelPausa.activeSelf)}");
                break;

            case 2:
                float zAhora = jugador != null ? jugador.transform.position.z : 0f;
                _log.AppendLine($"[t={t:0.0}] CONGELADO: zJugador={zAhora:0.00} " +
                                $"delta={Mathf.Abs(zAhora - _zCongelado):0.000} m  " +
                                $"score={(runner != null ? runner.Score : 0)} (antes {_scoreCongelado})");
                runner?.ResumeGame();
                break;

            // ── 2: la carrera vuelve a andar ──
            case 3:
                float zSuelto = jugador != null ? jugador.transform.position.z : 0f;
                _log.AppendLine($"[t={t:0.0}] REANUDADO: Pausado={runner?.Pausado} timeScale={Time.timeScale:0.0} " +
                                $"panelPausa={(_panelPausa != null && _panelPausa.activeSelf)} " +
                                $"zJugador={zSuelto:0.00} (se movió {zSuelto - _zCongelado:0.00} m)");
                break;

            // ── 3: abrir ajustes ──
            case 4:
                UIManager.Instance?.OpenSettings();
                _log.AppendLine($"[t={t:0.0}] AJUSTES ABIERTOS: panel={(_panelAjustes != null && _panelAjustes.activeSelf)} " +
                                $"volumenInicial={AudioListener.volume:0.00}");
                break;

            // ── 4: mover el volumen ──
            case 5:
                if (_sliderVolumen != null) _sliderVolumen.value = 0.35f;
                _log.AppendLine($"[t={t:0.0}] VOLUMEN 0,35 -> AudioListener.volume={AudioListener.volume:0.00}");
                break;

            // ── 5: silenciar ──
            case 6:
                _botonSilencio?.onClick.Invoke();
                _log.AppendLine($"[t={t:0.0}] SILENCIO ON -> AudioListener.volume={AudioListener.volume:0.00} " +
                                $"guardado.silenciado={(GameData.Instance != null && GameData.Instance.Silenciado)}");
                break;

            // ── 6: sacar el silencio y bajar el brillo ──
            case 7:
                _botonSilencio?.onClick.Invoke();
                if (_sliderBrillo != null) _sliderBrillo.value = 0.25f;
                float alpha = _velo != null ? _velo.GetComponent<Image>().color.a : -1f;
                _log.AppendLine($"[t={t:0.0}] SILENCIO OFF + BRILLO 0,25 -> volume={AudioListener.volume:0.00} " +
                                $"alphaVelo={alpha:0.00}");
                break;

            // ── 7: volver (guarda y sale del panel) ──
            case 8:
                _botonVolver?.onClick.Invoke();
                string guardado = GameData.Instance != null
                    ? $"volumen={GameData.Instance.Volumen:0.00} brillo={GameData.Instance.Brillo:0.00} silenciado={GameData.Instance.Silenciado}"
                    : "sin GameData";
                _log.AppendLine($"[t={t:0.0}] VOLVER: panelAjustes={(_panelAjustes != null && _panelAjustes.activeSelf)} " +
                                $"guardado: {guardado}");
                break;
        }
    }

    static void Cerrar(string motivo)
    {
        _activa = false;
        EditorApplication.update -= Tick;

        if (motivo != null) _log.AppendLine("cortado: " + motivo);
        _log.AppendLine("=== fin de la prueba ===");
        Debug.Log(_log.ToString());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  AYUDAS PARA MIRAR
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deja el juego pausado con el panel a la vista (para sacar una captura).
    /// De paso prueba el camino real: hace clic en el botón del HUD, después en
    /// SEGUIR y otra vez en el botón de pausa, así se verifica el enganche de
    /// PauseUI y no sólo el método del manager.
    /// </summary>
    static void PausarParaMirar()
    {
        var runner = RunnerGameManager.Instance;
        if (runner == null) { Debug.Log("[FASE EF] no hay RunnerGameManager activo"); return; }

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.Log("[FASE EF] no encontré el Canvas"); return; }

        var botonPausa  = canvas.transform.Find("GameplayPanel/PauseButton")?.GetComponent<Button>();
        var botonSeguir = canvas.transform.Find("PausePanel/Tarjeta/BotonSeguir")?.GetComponent<Button>();
        var panelPausa  = canvas.transform.Find("PausePanel")?.gameObject;

        if (runner.State != GameState.Running) runner.StartGame();

        var log = new StringBuilder();
        log.AppendLine("=== FASES E y F: clic real en los botones ===");
        log.AppendLine($"clic PAUSA: botón={(botonPausa != null)}");

        botonPausa?.onClick.Invoke();
        log.AppendLine($"  -> Pausado={runner.Pausado} timeScale={Time.timeScale:0.0} " +
                       $"panel={(panelPausa != null && panelPausa.activeSelf)}");

        botonSeguir?.onClick.Invoke();
        log.AppendLine($"clic SEGUIR: botón={(botonSeguir != null)}");
        log.AppendLine($"  -> Pausado={runner.Pausado} timeScale={Time.timeScale:0.0} " +
                       $"panel={(panelPausa != null && panelPausa.activeSelf)}");

        // Se vuelve a pausar para dejar el panel a la vista.
        botonPausa?.onClick.Invoke();
        log.AppendLine($"clic PAUSA otra vez -> Pausado={runner.Pausado} timeScale={Time.timeScale:0.0}");

        Debug.Log(log.ToString());
    }

    /// <summary>Abre el panel de ajustes desde el menú (para sacar una captura).</summary>
    static void AbrirAjustes()
    {
        UIManager.Instance?.OpenSettings();
        Debug.Log("[FASE EF] panel de ajustes abierto para mirar");
    }

    /// <summary>Devuelve el volumen y el brillo a los valores de fábrica y los guarda.</summary>
    static void RestaurarAjustes()
    {
        AudioListener.volume = SaveManager.VolumenPorDefecto;

        if (GameData.Instance != null)
        {
            GameData.Instance.SetVolumen(SaveManager.VolumenPorDefecto);
            GameData.Instance.SetBrillo(SaveManager.BrilloPorDefecto);
            GameData.Instance.SetSilenciado(false);
            GameData.Instance.GuardarAjustes();
        }

        // El velo del brillo lo aplica el panel: se le pide que relea el guardado.
        var canvas = GameObject.Find("Canvas");
        var settings = canvas != null ? canvas.GetComponent<SettingsUI>() : null;
        if (settings != null) settings.CargarAjustes();

        Debug.Log($"[FASE EF] ajustes restaurados: volumen={SaveManager.VolumenPorDefecto:0.00} " +
                  $"brillo={SaveManager.BrilloPorDefecto:0.00}");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  HELPERS DE CONSTRUCCIÓN
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Crea un rectángulo con fondo (o lo devuelve si ya existe).</summary>
    static GameObject PanelSiFalta(Transform padre, string nombre, Vector2 amin, Vector2 amax, Color color)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null) return existente.gameObject;

        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = color;

        return go;
    }

    /// <summary>Crea un texto (o lo devuelve si ya existe).</summary>
    static TMP_Text TextoSiFalta(Transform padre, string nombre, string contenido, float tamano,
                                 Vector2 amin, Vector2 amax, TextAlignmentOptions alineacion, bool negrita)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null) return existente.GetComponent<TMP_Text>();

        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var texto = go.AddComponent<TextMeshProUGUI>();
        texto.text       = contenido;
        texto.fontSize   = tamano;
        texto.alignment  = alineacion;
        texto.fontStyle  = negrita ? FontStyles.Bold : FontStyles.Normal;
        texto.color      = Color.white;
        texto.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) texto.font = TMP_Settings.defaultFontAsset;

        return texto;
    }

    /// <summary>Crea un botón con su etiqueta centrada (o lo devuelve si ya existe).</summary>
    static Button BotonSiFalta(Transform padre, string nombre, string etiqueta,
                               Vector2 amin, Vector2 amax, Color fondo, float tamanoFuente)
    {
        GameObject go = PanelSiFalta(padre, nombre, amin, amax, fondo);

        Button boton = go.GetComponent<Button>();
        if (boton == null) boton = go.AddComponent<Button>();
        boton.targetGraphic = go.GetComponent<Image>();

        if (go.transform.Find("Label") == null)
            TextoSiFalta(go.transform, "Label", etiqueta, tamanoFuente,
                         Vector2.zero, Vector2.one, TextAlignmentOptions.Center, true);

        return boton;
    }

    /// <summary>
    /// Crea un slider horizontal de 0 a 1 (o lo devuelve si ya existe).
    /// La estructura es la misma que arma Unity: un riel, una zona de relleno y
    /// una zona de perilla. El Slider deduce las zonas a partir del padre de
    /// cada rect que se le pasa.
    /// </summary>
    static Slider SliderSiFalta(Transform padre, string nombre, Vector2 amin, Vector2 amax, Color colorRelleno)
    {
        Transform existente = padre.Find(nombre);
        if (existente != null)
        {
            var yaEsta = existente.GetComponent<Slider>();
            if (yaEsta != null) return yaEsta;
        }

        // Riel: también recibe los clics del slider.
        GameObject raiz = PanelSiFalta(padre, nombre, amin, amax, new Color(0.16f, 0.19f, 0.24f, 1f));

        GameObject areaRelleno = PanelSiFalta(raiz.transform, "Fill Area", Vector2.zero, Vector2.one,
                                              new Color(0f, 0f, 0f, 0f));
        areaRelleno.GetComponent<Image>().raycastTarget = false;

        GameObject relleno = PanelSiFalta(areaRelleno.transform, "Fill", Vector2.zero, Vector2.one, colorRelleno);
        relleno.GetComponent<Image>().raycastTarget = false;

        GameObject areaPerilla = PanelSiFalta(raiz.transform, "Handle Slide Area", Vector2.zero, Vector2.one,
                                              new Color(0f, 0f, 0f, 0f));
        areaPerilla.GetComponent<Image>().raycastTarget = false;

        GameObject perilla = PanelSiFalta(areaPerilla.transform, "Handle",
                                          new Vector2(0f, 0f), new Vector2(0f, 1f), Color.white);
        var rtPerilla = perilla.GetComponent<RectTransform>();
        rtPerilla.sizeDelta = new Vector2(32f, 0f);

        var slider = raiz.AddComponent<Slider>();
        slider.targetGraphic = raiz.GetComponent<Image>();
        slider.fillRect      = relleno.GetComponent<RectTransform>();
        slider.handleRect    = rtPerilla;
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = 1f;

        return slider;
    }

    /// <summary>Asigna un campo privado [SerializeField] por nombre.</summary>
    static void Asignar(Object componente, string campo, Object valor, StringBuilder log)
    {
        var so = new SerializedObject(componente);
        var prop = so.FindProperty(campo);

        if (prop == null) { log.AppendLine($"  {campo}: NO existe el campo"); return; }

        prop.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();

        log.AppendLine($"  {campo}: {(valor != null ? valor.name : "<NULL>")}");
    }
}
