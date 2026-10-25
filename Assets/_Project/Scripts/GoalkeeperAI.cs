using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IA del arquero rival para el minijuego de penales.
///
/// Elige una columna (izquierda / centro / derecha) y se estira hacia ella.
/// Tiene DOS formas de animarse:
///
///   1. ANIMATOR (preferida cuando el rig está listo): si el GameObject tiene un
///      Animator con Controller Y Avatar, dispara el trigger correspondiente
///      (DiveLeft / DiveRight / CenterSave).
///
///   2. ANIMACIÓN PROCEDURAL POR HUESOS (la que se usa hoy, porque el Avatar
///      todavía no está configurado): mueve la raíz Y posa cada hueso del
///      esqueleto (columna, brazos, piernas y cuello) para que la estirada se
///      entienda: antepié de carga, salto en arco, estirada completa con los
///      brazos apuntando a la pelota, y caída al piso.
///
/// Antes la animación sólo deslizaba y rotaba la RAÍZ: los brazos quedaban
/// pegados al cuerpo y no se leía para qué lado se tiraba. Ahora se posan los
/// huesos, que es lo que realmente se ve.
///
/// En reposo el arquero también se mueve un poco (balanceo de peso), para que
/// no parezca una estatua mientras el jugador apunta.
///
/// Carpeta: Assets/_Project/Scripts/
/// </summary>
public class GoalkeeperAI : MonoBehaviour
{
    /// <summary>Las 3 zonas que puede cubrir el arquero.</summary>
    public enum SaveZone { Left, Center, Right }

    [Header("Animator triggers")]
    [Tooltip("Trigger para estirarse a la izquierda.")]
    [SerializeField] private string diveLeftTrigger = "DiveLeft";

    [Tooltip("Trigger para estirarse a la derecha.")]
    [SerializeField] private string diveRightTrigger = "DiveRight";

    [Tooltip("Trigger para atajar un tiro al centro.")]
    [SerializeField] private string centerSaveTrigger = "CenterSave";

    [Tooltip("Trigger para volver al estado de reposo.")]
    [SerializeField] private string resetTrigger = "ToIdle";

    [Header("Animación procedural (se usa si el Animator no tiene Avatar)")]
    [Tooltip("Fuerza la animación procedural incluso si el Animator está completo.")]
    [SerializeField] private bool forzarEstiradaPorCodigo = false;

    [Tooltip("Cuántos metros se desplaza el arquero hacia el costado.")]
    [SerializeField] private float desplazamientoLateral = 1.45f;

    [Tooltip("Altura máxima del salto, en metros.")]
    [SerializeField] private float alturaMaximaVuelo = 0.72f;

    [Tooltip("Inclinación máxima del cuerpo al estirarse (90 = acostado del todo).")]
    [SerializeField] private float inclinacionMaximaGrados = 88f;

    [Tooltip("Segundos que tarda en llegar a la estirada completa.")]
    [SerializeField] private float duracionEstirada = 0.42f;

    [Tooltip("Segundos que tarda en caer al piso después de la estirada.")]
    [SerializeField] private float duracionAterrizaje = 0.38f;

    [Tooltip("Segundos que tarda en volver a la pose de reposo.")]
    [SerializeField] private float duracionRetorno = 0.30f;

    [Header("Reposo (balanceo mientras el jugador apunta)")]
    [Tooltip("Activa el balanceo de peso en reposo. Se apaga solo si usa el Animator.")]
    [SerializeField] private bool usarIdleProcedural = true;

    [Tooltip("Cuánto se mueve de costado en reposo, en metros.")]
    [SerializeField] private float amplitudBalanceo = 0.045f;

    [Tooltip("Velocidad del balanceo en reposo, en ciclos por segundo.")]
    [SerializeField] private float frecuenciaBalanceo = 0.55f;

    // ─── Pose ─────────────────────────────────────────────────────────────────
    // Todos los valores están normalizados 0..1 salvo 'pitch', que va en grados.
    // Así las tablas de abajo se leen fácil y se pueden retocar sin tocar la
    // lógica: es la parte que conviene mirar para entender la animación.

    private struct PoseArquero
    {
        public float lateral;   // 0..1 → cuánto se corre al costado
        public float alto;      // 0..1 → cuánto salta
        public float roll;      // 0..1 → cuánto se acuesta de costado
        public float pitch;     // grados → se inclina adelante (-) o atrás (+)
        public float brazos;    // 0..1 → cuánto estira los brazos
        public float piernas;   // 0..1 → cuánto arrastra las piernas
        public float flexion;   // 0..1 → cuánto se agacha
        public float curva;     // 0..1 → cuánto curva la columna

        public static PoseArquero Mezclar(PoseArquero a, PoseArquero b, float t)
        {
            return new PoseArquero
            {
                lateral = Mathf.Lerp(a.lateral, b.lateral, t),
                alto    = Mathf.Lerp(a.alto,    b.alto,    t),
                roll    = Mathf.Lerp(a.roll,    b.roll,    t),
                pitch   = Mathf.Lerp(a.pitch,   b.pitch,   t),
                brazos  = Mathf.Lerp(a.brazos,  b.brazos,  t),
                piernas = Mathf.Lerp(a.piernas, b.piernas, t),
                flexion = Mathf.Lerp(a.flexion, b.flexion, t),
                curva   = Mathf.Lerp(a.curva,   b.curva,   t),
            };
        }
    }

    /// <summary>Atajo para escribir las tablas de poses de forma compacta.</summary>
    private static PoseArquero P(float lateral, float alto, float roll, float pitch,
                                 float brazos, float piernas, float flexion, float curva)
    {
        return new PoseArquero
        {
            lateral = lateral, alto = alto, roll = roll, pitch = pitch,
            brazos = brazos, piernas = piernas, flexion = flexion, curva = curva,
        };
    }

    // ── Tablas: estirada a un costado ────────────────────────────────────────
    // u=0 → parado | u≈0.16 → antepié (se agacha y va al lado contrario)
    // u≈0.38 → despegue | u≈0.68 → estirada máxima | u=1 → fin del vuelo
    private static readonly float[] U_LADO = { 0.00f, 0.16f, 0.38f, 0.68f, 1.00f };
    private static readonly PoseArquero[] P_LADO =
    {
        P( 0.00f,  0.00f,  0.00f,   0f, 0.00f, 0.00f, 0.00f,  0.00f),
        P(-0.08f, -0.10f, -0.15f,   0f, 0.10f, 0.20f, 0.85f, -0.10f),
        P( 0.55f,  0.55f,  0.50f,   0f, 0.70f, 0.70f, 0.35f,  0.45f),
        P( 0.90f,  0.70f,  0.88f,   0f, 1.00f, 0.95f, 0.15f,  0.80f),
        P( 1.00f,  0.62f,  1.00f,   0f, 1.00f, 1.00f, 0.08f,  0.90f),
    };

    // ── Tablas: estirada al centro (blocaje de frente) ───────────────────────
    private static readonly float[] U_CENTRO = { 0.00f, 0.16f, 0.45f, 1.00f };
    private static readonly PoseArquero[] P_CENTRO =
    {
        P(0f,  0.00f, 0f,   0f, 0.00f, 0.00f, 0.00f, 0.00f),
        P(0f, -0.14f, 0f,   6f, 0.15f, 0.35f, 0.95f, 0.05f),
        P(0f,  0.72f, 0f,  -8f, 0.85f, 0.25f, 0.15f, 0.10f),
        P(0f,  0.85f, 0f, -12f, 1.00f, 0.20f, 0.06f, 0.15f),
    };

    // ── Tablas: caída al piso después de la estirada ─────────────────────────
    private static readonly float[] U_ATERRIZA = { 0.00f, 0.45f, 1.00f };

    private static readonly PoseArquero[] P_LADO_ATERRIZA =
    {
        P(1.00f, 0.62f, 1.00f, 0f, 1.00f, 1.00f, 0.08f, 0.90f),
        P(0.98f, 0.16f, 0.85f, 0f, 0.80f, 0.60f, 0.45f, 0.60f),
        P(0.96f, 0.02f, 0.62f, 0f, 0.50f, 0.30f, 0.70f, 0.30f),
    };

    private static readonly PoseArquero[] P_CENTRO_ATERRIZA =
    {
        P(0f, 0.85f, 0f, -12f, 1.00f, 0.20f, 0.06f, 0.15f),
        P(0f, 0.12f, 0f,  -8f, 0.80f, 0.40f, 0.60f, 0.15f),
        P(0f, 0.00f, 0f,  -4f, 0.55f, 0.30f, 0.80f, 0.08f),
    };

    // ─── Estado interno ───────────────────────────────────────────────────────

    private enum Modo { Reposo, Estirada, Aterrizaje, Tendido, Volviendo }

    private Animator _animator;

    // Pose de reposo del arquero (se captura al arrancar para poder volver).
    private Vector3    _posicionBase;
    private Quaternion _rotacionBase;
    private bool       _poseBaseCapturada;

    // Esqueleto cacheado. Guardamos la pose original de cada hueso para poder
    // reconstruir la pose desde cero en cada frame (evita que se vaya acumulando
    // el error de las rotaciones).
    private readonly List<Transform> _huesos = new List<Transform>();
    private Vector3[]    _huesosPos;
    private Quaternion[] _huesosRot;

    // Referencias directas a los huesos que usa la pose.
    private Transform _hips, _spine02, _spine01, _spine, _neck, _head;
    private Transform _leftArm, _leftForeArm, _leftHand;
    private Transform _rightArm, _rightForeArm, _rightHand;
    private Transform _leftUpLeg, _leftLeg, _rightUpLeg, _rightLeg;

    private Modo  _modo = Modo.Reposo;
    private float _tiempoModo;
    private float _direccion;                 // -1 izquierda | 0 centro | +1 derecha
    private PoseArquero _poseActual;
    private PoseArquero _poseFinalEstirada;
    private PoseArquero _poseOrigenVuelta;

    /// <summary>Zona que eligió cubrir el arquero en el último penal.</summary>
    public SaveZone ChosenZone { get; private set; }

    /// <summary>
    /// True cuando el Animator puede usarse de verdad: tiene Controller Y Avatar
    /// válido. Sin Avatar, un rig Humanoid no retargetea y los clips no se
    /// reproducen (el arquero se queda quieto). En ese caso usamos la animación
    /// procedural por huesos.
    /// </summary>
    private bool TieneAnimatorUtilizable =>
        !forzarEstiradaPorCodigo &&
        _animator != null &&
        _animator.runtimeAnimatorController != null &&
        _animator.avatar != null;

    /// <summary>
    /// True cuando ya tenemos TODOS los huesos que necesita la pose.
    /// Se calcula a partir de las referencias reales (no de un bool aparte) para
    /// que no pueda quedar "listo" con el esqueleto vacío.
    /// </summary>
    private bool EsqueletoListo =>
        _huesos.Count > 0 &&
        _huesosPos != null &&
        _huesosPos.Length == _huesos.Count &&
        _hips != null && _leftArm != null && _rightArm != null &&
        _leftUpLeg != null && _rightUpLeg != null;

    // ─── Unity ────────────────────────────────────────────────────────────────

    private void Awake() => Inicializar();

    private void OnDisable()
    {
        // Al desactivarse (fin del nivel, recarga de escena) dejamos el arquero
        // parado, para que no quede acostado si se reutiliza.
        if (_huesos.Count > 0) RestaurarHuesos();
    }

    private void Update()
    {
        if (TieneAnimatorUtilizable) return;
        if (!EsqueletoListo) { Inicializar(); if (!EsqueletoListo) return; }

        _tiempoModo += Time.deltaTime;

        switch (_modo)
        {
            case Modo.Reposo:
                AplicarPose(PoseDeReposo(), 0f, esReposo: true);
                break;

            case Modo.Estirada:
            {
                float u = Mathf.Clamp01(_tiempoModo / Mathf.Max(0.05f, duracionEstirada));
                bool  alCentro = Mathf.Approximately(_direccion, 0f);

                PoseArquero p = Interpolar(
                    alCentro ? P_CENTRO : P_LADO,
                    alCentro ? U_CENTRO : U_LADO,
                    u);

                AplicarPose(p, _direccion, esReposo: false);
                _poseFinalEstirada = p;

                if (u >= 1f) { _modo = Modo.Aterrizaje; _tiempoModo = 0f; }
                break;
            }

            case Modo.Aterrizaje:
            {
                float u = Mathf.Clamp01(_tiempoModo / Mathf.Max(0.05f, duracionAterrizaje));
                bool  alCentro = Mathf.Approximately(_direccion, 0f);

                PoseArquero p = Interpolar(
                    alCentro ? P_CENTRO_ATERRIZA : P_LADO_ATERRIZA,
                    U_ATERRIZA,
                    u);

                AplicarPose(p, _direccion, esReposo: false);
                _poseFinalEstirada = p;

                if (u >= 1f) { _modo = Modo.Tendido; _tiempoModo = 0f; }
                break;
            }

            case Modo.Tendido:
                // Se queda en el piso hasta que el manager pida volver al reposo.
                AplicarPose(_poseFinalEstirada, _direccion, esReposo: false);
                break;

            case Modo.Volviendo:
            {
                float u = Mathf.Clamp01(_tiempoModo / Mathf.Max(0.05f, duracionRetorno));
                float s = Mathf.SmoothStep(0f, 1f, u);

                PoseArquero p = PoseArquero.Mezclar(_poseOrigenVuelta, PoseDeReposo(), s);
                AplicarPose(p, Mathf.Lerp(_direccion, 0f, s), esReposo: true);

                if (u >= 1f)
                {
                    _modo = Modo.Reposo;
                    _tiempoModo = 0f;
                    _direccion = 0f;
                }
                break;
            }
        }
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reacciona a un penal eligiendo una zona al azar.
    /// Devuelve la zona que decidió cubrir (para que la lógica compare con el tiro).
    /// </summary>
    public SaveZone React()
    {
        ChosenZone = (SaveZone)Random.Range(0, 3);
        EjecutarEstirada(ChosenZone);
        return ChosenZone;
    }

    /// <summary>
    /// Reacciona hacia una zona concreta (se usa cuando el arquero "adivina"
    /// la columna del tiro, o para probar a mano).
    /// </summary>
    public void React(SaveZone zone)
    {
        ChosenZone = zone;
        EjecutarEstirada(zone);
    }

    /// <summary>
    /// Vuelve a la posición de reposo entre intentos, tanto por Animator
    /// como por animación procedural.
    /// </summary>
    public void ResetToIdle()
    {
        if (TieneAnimatorUtilizable)
        {
            // Si hay controller, avisamos por trigger.
            _animator.SetTrigger(resetTrigger);
            return;
        }

        // Volvemos suavemente desde la pose en la que esté (aunque esté tirado).
        Inicializar();
        _poseOrigenVuelta = _poseActual;
        _modo = Modo.Volviendo;
        _tiempoModo = 0f;
    }

    /// <summary>
    /// Deja al arquero congelado en una pose del vuelo. Pensado para revisar la
    /// animación desde el editor sin tener que jugar. No lo usa el minijuego.
    /// </summary>
    public void PoseDePrueba(SaveZone zone, float u01)
    {
        Inicializar();

        _direccion = zone switch
        {
            SaveZone.Left  => -1f,
            SaveZone.Right =>  1f,
            _              =>  0f,
        };

        bool alCentro = Mathf.Approximately(_direccion, 0f);
        PoseArquero p = Interpolar(
            alCentro ? P_CENTRO : P_LADO,
            alCentro ? U_CENTRO : U_LADO,
            Mathf.Clamp01(u01));

        AplicarPose(p, _direccion, esReposo: false);
    }

    /// <summary>
    /// Deja al arquero en la pose de reposo procedural, con el balanceo ya
    /// aplicado. Sólo para revisarla desde el editor: en el juego la aplica
    /// Update() sola mientras está en Modo.Reposo. No la usa el minijuego.
    /// </summary>
    public void ReposoDePrueba()
    {
        Inicializar();
        _direccion = 0f;
        AplicarPose(PoseDeReposo(), 0f, esReposo: true);
    }

    /// <summary>Devuelve el arquero a la pose original del modelo (para el editor).</summary>
    public void RestaurarPoseOriginal()
    {
        Inicializar();

        if (_huesos.Count > 0) RestaurarHuesos();
        if (_poseBaseCapturada)
            transform.SetPositionAndRotation(_posicionBase, _rotacionBase);

        _modo = Modo.Reposo;
        _tiempoModo = 0f;
        _direccion = 0f;
        _poseActual = PoseDeReposo();
        _poseFinalEstirada = _poseActual;
    }

    // ─── Inicialización y esqueleto ───────────────────────────────────────────

    private void Inicializar()
    {
        if (_animator == null) _animator = GetComponent<Animator>();

        // La pose de reposo se captura UNA sola vez, antes de cualquier animación.
        if (!_poseBaseCapturada)
        {
            _posicionBase = transform.position;
            _rotacionBase = transform.rotation;
            _poseBaseCapturada = true;
        }

        // El esqueleto se cachea siempre que falte (se autocura si quedó vacío).
        if (!EsqueletoListo)
        {
            CachearEsqueleto();

            // Si el Animator no puede animar de verdad lo apagamos, así no compite
            // con nosotros escribiendo los mismos huesos.
            if (_animator != null && !TieneAnimatorUtilizable)
                _animator.enabled = false;

            // Evita que el cuerpo desaparezca cuando se estira fuera del bounds
            // calculado por el SkinnedMeshRenderer.
            var malla = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (malla != null) malla.updateWhenOffscreen = true;
        }

        _poseActual = PoseDeReposo();
        _poseFinalEstirada = _poseActual;
    }

    private void CachearEsqueleto()
    {
        _huesos.Clear();
        ReiniciarReferencias();
        _huesosPos = null;
        _huesosRot = null;

        // GetComponentsInChildren devuelve los huesos de padre a hijo, que es
        // justo el orden que necesitamos para posarlos.
        Transform[] todos = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            if (todos[i] != null && todos[i] != transform) _huesos.Add(todos[i]);
        }

        if (_huesos.Count == 0) return;

        _huesosPos = new Vector3[_huesos.Count];
        _huesosRot = new Quaternion[_huesos.Count];
        for (int i = 0; i < _huesos.Count; i++)
        {
            _huesosPos[i] = _huesos[i].localPosition;
            _huesosRot[i] = _huesos[i].localRotation;
            GuardarReferencia(_huesos[i].name, _huesos[i]);
        }
    }

    private void ReiniciarReferencias()
    {
        _hips = _spine02 = _spine01 = _spine = _neck = _head = null;
        _leftArm = _leftForeArm = _leftHand = null;
        _rightArm = _rightForeArm = _rightHand = null;
        _leftUpLeg = _leftLeg = _rightUpLeg = _rightLeg = null;
    }

    /// <summary>Asocia cada hueso del rig con la referencia que usa la pose.</summary>
    private void GuardarReferencia(string nombre, Transform h)
    {
        switch (nombre)
        {
            case "Hips":         _hips         = h; break;
            case "Spine02":      _spine02      = h; break;
            case "Spine01":      _spine01      = h; break;
            case "Spine":        _spine        = h; break;
            case "neck":         _neck         = h; break;
            case "Head":         _head         = h; break;
            case "LeftArm":      _leftArm      = h; break;
            case "LeftForeArm":  _leftForeArm  = h; break;
            case "LeftHand":     _leftHand     = h; break;
            case "RightArm":     _rightArm     = h; break;
            case "RightForeArm": _rightForeArm = h; break;
            case "RightHand":    _rightHand    = h; break;
            case "LeftUpLeg":    _leftUpLeg    = h; break;
            case "LeftLeg":      _leftLeg      = h; break;
            case "RightUpLeg":   _rightUpLeg   = h; break;
            case "RightLeg":     _rightLeg     = h; break;
        }
    }

    /// <summary>Devuelve todos los huesos a la pose original del modelo.</summary>
    private void RestaurarHuesos()
    {
        if (_huesosPos == null || _huesosPos.Length != _huesos.Count) return;

        for (int i = 0; i < _huesos.Count; i++)
        {
            if (_huesos[i] == null) continue;
            _huesos[i].localPosition = _huesosPos[i];
            _huesos[i].localRotation = _huesosRot[i];
        }
    }

    // ─── Aplicación de la pose ────────────────────────────────────────────────

    /// <summary>
    /// Aplica una pose completa: primero la raíz (posición y rotación globales),
    /// después los huesos, siempre de padre a hijo.
    /// </summary>
    private void AplicarPose(PoseArquero p, float direccion, bool esReposo)
    {
        if (!EsqueletoListo) Inicializar();

        // ── 1. Raíz: se corre al costado, salta y se acuesta ─────────────────
        Vector3 posicion = _posicionBase
            + new Vector3(direccion * p.lateral * desplazamientoLateral,
                          p.alto * alturaMaximaVuelo,
                          0f)
            - Vector3.up * (p.flexion * 0.13f);   // el agachado baja el cuerpo

        // roll positivo del parámetro = se acuesta hacia 'direccion'.
        // Girando alrededor del eje Z del mundo, un ángulo negativo inclina el
        // "arriba" del arquero hacia +X. Por eso el signo va invertido.
        float rollGrados = -direccion * p.roll * inclinacionMaximaGrados;

        Quaternion rotacion = Quaternion.AngleAxis(rollGrados, Vector3.forward)
                            * Quaternion.AngleAxis(p.pitch, Vector3.right)
                            * _rotacionBase;

        transform.SetPositionAndRotation(posicion, rotacion);
        _poseActual = p;

        // Sin esqueleto sólo se puede mover la raíz.
        if (!EsqueletoListo) return;

        // ── 2. Volvemos los huesos a cero y posamos desde ahí ────────────────
        RestaurarHuesos();

        // Ejes del cuerpo YA rotado: la flexión de piernas y columna se hace
        // sobre el eje hombro-a-hombro, así funciona igual estirado o parado.
        Vector3 ejeLateral = transform.right;

        // ── 3. Columna: se curva hacia el lado del vuelo ─────────────────────
        RotarMundo(_spine02, ejeLateral, p.curva *  9f);
        RotarMundo(_spine01, ejeLateral, p.curva * 11f);
        RotarMundo(_spine,   ejeLateral, p.curva *  9f);

        // ── 4. Piernas ───────────────────────────────────────────────────────
        // flexion → rodillas adelante (se agacha).
        // piernas → piernas hacia atrás (arrastre del vuelo).
        float flex  = p.flexion * 55f;
        float trail = p.piernas * 42f;

        RotarMundo(_leftUpLeg,  ejeLateral, -flex + trail);
        RotarMundo(_rightUpLeg, ejeLateral, -flex + trail);
        RotarMundo(_leftLeg,    ejeLateral,  flex * 1.6f - trail * 0.5f);
        RotarMundo(_rightLeg,   ejeLateral,  flex * 1.6f - trail * 0.5f);

        // ── 5. Brazos: apuntan hacia donde va la pelota ──────────────────────
        // Ésta es la parte que hace que se entienda para qué lado se tira.
        Vector3 dirBrazoIzq, dirBrazoDer;
        float   pesoBrazos;

        if (esReposo)
        {
            // Pose lista: brazos abiertos y un poco adelante, codos flexionados.
            // OJO con el signo de X: el modelo mira hacia -Z, así que la IZQUIERDA
            // del arquero es +X (en la pose de bind su brazo izquierdo cuelga
            // hacia +X). Con el signo al revés los brazos se cruzan adelante.
            dirBrazoIzq = new Vector3( 0.80f, -0.55f, -0.35f);
            dirBrazoDer = new Vector3(-0.80f, -0.55f, -0.35f);
            pesoBrazos  = 0.55f;
        }
        else if (Mathf.Approximately(direccion, 0f))
        {
            // Blocaje central: las dos manos suben abiertas en "V" (como cuando
            // el arquero se hace grande). Se lee perfecto desde la cámara del
            // penal, que mira al arco de frente.
            // X positiva = izquierda del arquero, igual que en la pose de reposo.
            dirBrazoIzq = new Vector3( 0.55f, 0.80f, -0.25f);
            dirBrazoDer = new Vector3(-0.55f, 0.80f, -0.25f);
            pesoBrazos  = p.brazos;
        }
        else
        {
            // Tiro al costado: las dos manos al palo (palomita clásica).
            dirBrazoIzq = dirBrazoDer = new Vector3(direccion, 0.14f, -0.32f);
            pesoBrazos  = p.brazos;
        }

        dirBrazoIzq.Normalize();
        dirBrazoDer.Normalize();

        // Se alinea CADA segmento con su hueso hijo: hombro→codo y codo→mano.
        // Alineando directo hombro→mano el codo queda torcido y después, al
        // corregir el antebrazo, la mano se sale de la línea y el brazo queda
        // doblado apuntando a cualquier lado.
        ApuntarHueso(_leftArm,      _leftForeArm,  dirBrazoIzq, pesoBrazos);
        ApuntarHueso(_leftForeArm,  _leftHand,     dirBrazoIzq, pesoBrazos);
        ApuntarHueso(_rightArm,     _rightForeArm, dirBrazoDer, pesoBrazos);
        ApuntarHueso(_rightForeArm, _rightHand,    dirBrazoDer, pesoBrazos);

        // ── 6. Cabeza: mira la pelota ────────────────────────────────────────
        ApuntarHueso(_neck, _head,
                     Mathf.Approximately(direccion, 0f)
                        ? new Vector3(0f, 0f, -1f)
                        : new Vector3(direccion * 0.6f, 0.1f, -0.8f),
                     esReposo ? 0f : Mathf.Clamp01(p.brazos) * 0.45f);
    }

    /// <summary>Rota un hueso alrededor de un eje del MUNDO (no del hueso).</summary>
    private static void RotarMundo(Transform hueso, Vector3 eje, float grados)
    {
        if (hueso == null || Mathf.Approximately(grados, 0f)) return;
        if (eje.sqrMagnitude < 1e-6f) return;

        hueso.rotation = Quaternion.AngleAxis(grados, eje.normalized) * hueso.rotation;
    }

    /// <summary>
    /// Rota un hueso para que el vector hueso→extremo apunte en 'direccion'.
    /// Se aplica sólo una fracción del giro según 'peso' (0 = no tocar, 1 = todo).
    /// Funciona sin saber cómo están orientados los ejes internos del rig.
    /// </summary>
    private static void ApuntarHueso(Transform hueso, Transform extremo,
                                     Vector3 direccion, float peso)
    {
        if (hueso == null || extremo == null) return;
        if (peso <= 0.001f) return;

        Vector3 actual = extremo.position - hueso.position;
        if (actual.sqrMagnitude < 1e-8f) return;

        Quaternion delta = Quaternion.FromToRotation(actual.normalized, direccion.normalized);

        if (peso < 0.999f)
            delta = Quaternion.Slerp(Quaternion.identity, delta, Mathf.Clamp01(peso));

        hueso.rotation = delta * hueso.rotation;
    }

    // ─── Tablas ───────────────────────────────────────────────────────────────

    /// <summary>Pose de reposo con el balanceo de peso ya aplicado.</summary>
    private PoseArquero PoseDeReposo()
    {
        // Pose base: agachado leve, brazos listos, columna apenas curvada.
        PoseArquero p = P(0f, 0f, 0f, -3f, 0f, 0f, 0.22f, 0.06f);

        if (!usarIdleProcedural) return p;

        float fase = Time.time * frecuenciaBalanceo * Mathf.PI * 2f;
        float onda = Mathf.Sin(fase);

        // Se pasa el peso de un pie al otro y respira (sube y baja poco).
        p.lateral = onda * (amplitudBalanceo / Mathf.Max(0.01f, desplazamientoLateral));
        p.alto    = Mathf.Abs(Mathf.Sin(fase * 2f)) * 0.02f;
        p.roll    = onda * 0.055f;
        p.curva   = 0.06f + onda * 0.03f;
        p.flexion = 0.22f + Mathf.Abs(onda) * 0.05f;

        return p;
    }

    /// <summary>Interpola la pose entre las claves de una tabla.</summary>
    private static PoseArquero Interpolar(PoseArquero[] poses, float[] tiempos, float u)
    {
        if (poses == null || poses.Length == 0) return default;
        if (poses.Length == 1) return poses[0];

        u = Mathf.Clamp01(u);

        for (int i = 0; i < tiempos.Length - 1; i++)
        {
            if (u > tiempos[i + 1]) continue;

            float tramo = Mathf.Max(0.0001f, tiempos[i + 1] - tiempos[i]);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - tiempos[i]) / tramo));
            return PoseArquero.Mezclar(poses[i], poses[i + 1], t);
        }

        return poses[poses.Length - 1];
    }

    // ─── Disparo de la estirada ───────────────────────────────────────────────

    private void EjecutarEstirada(SaveZone zone)
    {
        if (TieneAnimatorUtilizable)
        {
            // ── Camino preferido: Animator ───────────────────────────────────
            switch (zone)
            {
                case SaveZone.Left:   _animator.SetTrigger(diveLeftTrigger);   break;
                case SaveZone.Right:  _animator.SetTrigger(diveRightTrigger);  break;
                case SaveZone.Center: _animator.SetTrigger(centerSaveTrigger); break;
            }
            return;
        }

        // ── Animación procedural por huesos ──────────────────────────────────
        Inicializar();
        if (!EsqueletoListo) return;

        _direccion = zone switch
        {
            SaveZone.Left  => -1f,
            SaveZone.Right =>  1f,
            _              =>  0f,
        };

        _poseOrigenVuelta = _poseActual;
        _modo = Modo.Estirada;
        _tiempoModo = 0f;
    }
}
