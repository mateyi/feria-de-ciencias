using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Controla al jugador durante la fase de runner:
///   - Movimiento automático hacia adelante.
///   - Cambio de carril con swipe/teclado (nuevo Input System).
///   - Salto y gravedad.
///   - Detección de colisiones con obstáculos.
///
/// Cuando el runner termina (META alcanzada), RunnerGameManager llama a
/// SetRunnerEnabled(false) para congelar todo el movimiento sin destruir el objeto.
///
/// Carpeta: Assets/_Project/Scripts/Player/
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────
    public static PlayerController Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────
    [Header("Movimiento")]
    [SerializeField] private float baseForwardSpeed = 8f;
    [SerializeField] private float laneSwitchSpeed  = 12f;
    [SerializeField] private float jumpForce        = 12f;
    [SerializeField] private float gravity          = -25f;

    // ─── Estado público ───────────────────────────────────────────────────────
    public int  CurrentLane { get; private set; } = 1; // 0=izq, 1=centro, 2=der
    public bool IsAlive     { get; private set; } = true;
    public bool IsGrounded  { get; private set; }

    /// <summary>
    /// Cuando es false, el PlayerController ignora todo input y no mueve al personaje.
    /// Se desactiva al cruzar la META para ceder el control al minijuego de penales.
    /// </summary>
    public bool RunnerEnabled { get; private set; } = true;

    /// <summary>
    /// Velocidad actual (aumenta con la stat de velocidad del jugador y con la
    /// mejora "Velocidad" comprada en la tienda).
    /// </summary>
    public float CurrentSpeed =>
        baseForwardSpeed * (1f + (PlayerStats.Instance != null ? PlayerStats.Instance.speed / 200f : 0f))
                         * (1f + (UpgradeManager.Instance != null ? UpgradeManager.Instance.BonusVelocidad : 0f));

    // ─── Estado para el sistema de obstáculos (Fase B) ────────────────────────

    /// <summary>
    /// Metros que el jugador tiene los pies por encima del piso. Es 0 cuando
    /// está apoyado. Lo usan los obstáculos bajos para saber si los saltó.
    /// </summary>
    public float AlturaSobrePiso { get; private set; }

    /// <summary>
    /// True durante el segundo de gracia después de un choque. Mientras está
    /// prendido no se descuenta energía de nuevo (evita daño múltiple).
    /// </summary>
    public bool Invulnerable { get; private set; }

    /// <summary>True si el jugador está en el aire (saltando).</summary>
    public bool EstaEnElAire => !IsGrounded;

    /// <summary>Multiplicador temporal de velocidad (lo usa la ralentización).</summary>
    private float _modificadorVelocidad = 1f;

    /// <summary>Último Y del piso donde estuvo apoyado, para medir el salto.</summary>
    private float _ySuelo;

    // ─── Privados ─────────────────────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3 _velocity;
    private float   _targetX;
    private bool    _isJumping;

    // Input táctil
    private Vector2 _touchStart;
    private bool    _touchActive;

    // Input System actions
    private InputAction _moveAction;
    private InputAction _jumpAction;

    // ─── Unity ────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cc = GetComponent<CharacterController>();

        // Crear acciones de input en código
        _moveAction = new InputAction("Move", InputActionType.Value, "<Keyboard>/leftArrow");
        _moveAction.AddBinding("<Keyboard>/a");
        _moveAction.AddBinding("<Keyboard>/rightArrow");
        _moveAction.AddBinding("<Keyboard>/d");

        _jumpAction = new InputAction("Jump", InputActionType.Button);
        _jumpAction.AddBinding("<Keyboard>/space");
        _jumpAction.AddBinding("<Keyboard>/w");
        _jumpAction.AddBinding("<Keyboard>/upArrow");

        _moveAction.Enable();
        _jumpAction.Enable();

        _jumpAction.performed += _ => TryJump();
    }

    private void OnDestroy()
    {
        _moveAction?.Disable();
        _jumpAction?.Disable();
    }

    private void Start()
    {
        _targetX = LaneSystem.GetLaneX(CurrentLane);
        Vector3 pos = transform.position;
        pos.x = _targetX;
        transform.position = pos;
    }

    private void Update()
    {
        // No hacer nada si el runner está deshabilitado (fase de penal o menú)
        if (!RunnerEnabled) return;
        if (!IsAlive) return;

        // Respetar el estado del GameManager solo cuando existe.
        // Si no hay GameManager (escena de prueba) o el estado es Running, dejar pasar.
        // IMPORTANTE: no bloquear cuando el GameManager aún no inicializó (State == MainMenu
        // antes de que el jugador pulse "Jugar") — en ese caso el PlayerController
        // no debería existir todavía porque se instancia desde StartGame().
        // Bloqueamos únicamente los estados que explícitamente deben detener el runner:
        // PenaltyMinigame, LevelComplete y GameOver.
        if (RunnerGameManager.Instance != null)
        {
            GameState s = RunnerGameManager.Instance.State;
            if (s == GameState.PenaltyMinigame ||
                s == GameState.LevelComplete   ||
                s == GameState.GameOver) return;
        }

        HandleKeyboardInput();
        HandleTouchInput();
        Move();
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame  || Keyboard.current.aKey.wasPressedThisFrame)  MoveLeft();
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)  MoveRight();
        if (Keyboard.current.spaceKey.wasPressedThisFrame      ||
            Keyboard.current.wKey.wasPressedThisFrame          ||
            Keyboard.current.upArrowKey.wasPressedThisFrame)   TryJump();
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _touchStart  = touch.position.ReadValue();
                _touchActive = true;
            }
            else if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Ended && _touchActive)
            {
                _touchActive = false;
                Vector2 delta = touch.position.ReadValue() - _touchStart;
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                {
                    if (delta.x < -50) MoveLeft();
                    else if (delta.x > 50) MoveRight();
                }
                else if (delta.y > 50) TryJump();
            }
        }
    }

    // ─── Movimiento ───────────────────────────────────────────────────────────

    private void Move()
    {
        IsGrounded = _cc.isGrounded;

        if (IsGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;

        float currentX = transform.position.x;
        // La mejora "Control" acelera el cambio de carril.
        float velocidadCarril = laneSwitchSpeed *
            (UpgradeManager.Instance != null ? UpgradeManager.Instance.MultiplicadorCambioCarril : 1f);
        float newX     = Mathf.MoveTowards(currentX, _targetX, velocidadCarril * Time.deltaTime);

        // Velocidad del nivel (la define el LevelConfig de la Fase B) y
        // ralentización temporal por choque con un árbitro, por ejemplo.
        float multiplicadorNivel = ObstacleManager.Instance != null
            ? ObstacleManager.Instance.MultiplicadorVelocidadNivel
            : 1f;

        // Pickup de botines turbo (Fase C): acelera mientras dura el efecto.
        float multiplicadorTurbo = PickupManager.Instance != null
            ? PickupManager.Instance.MultiplicadorVelocidad
            : 1f;

        Vector3 move = new Vector3(
            newX - currentX,
            _velocity.y * Time.deltaTime,
            CurrentSpeed * multiplicadorNivel * multiplicadorTurbo * _modificadorVelocidad
                * (RunnerGameManager.Instance != null ? RunnerGameManager.Instance.SpeedMultiplier : 1f)
                * Time.deltaTime
        );

        _cc.Move(move);

        // ── Altura sobre el piso (para los obstáculos que se pueden saltar) ──
        // Cuando está apoyado guardamos el Y del piso; cuando vuela, la
        // diferencia contra ese piso es lo alto que pasó.
        if (IsGrounded)
        {
            _ySuelo = transform.position.y;
            AlturaSobrePiso = 0f;
        }
        else
        {
            AlturaSobrePiso = Mathf.Max(0f, transform.position.y - _ySuelo);
        }
    }

    public void MoveLeft()
    {
        if (CurrentLane > 0)
        {
            CurrentLane--;
            _targetX = LaneSystem.GetLaneX(CurrentLane);
        }
    }

    public void MoveRight()
    {
        if (CurrentLane < LaneSystem.LaneCount - 1)
        {
            CurrentLane++;
            _targetX = LaneSystem.GetLaneX(CurrentLane);
        }
    }

    private void TryJump()
    {
        // Solo saltar si el runner está activo
        if (!RunnerEnabled) return;
        Jump();
    }

    public void Jump()
    {
        if (IsGrounded && !_isJumping)
        {
            _velocity.y = jumpForce;
            _isJumping  = true;
            StartCoroutine(ResetJump());
        }
    }

    private IEnumerator ResetJump()
    {
        yield return new WaitForSeconds(0.3f);
        _isJumping = false;
    }

    // ─── API pública ──────────────────────────────────────────────────────────

    /// <summary>
    /// Habilita o deshabilita el movimiento del runner.
    /// Llamado por RunnerGameManager al cruzar la META (false) o al reiniciar (true).
    /// Cuando se deshabilita, también detiene la velocidad vertical para que el
    /// personaje no quede flotando.
    /// </summary>
    public void SetRunnerEnabled(bool enabled)
    {
        RunnerEnabled = enabled;
        if (!enabled)
        {
            // Detener velocidad vertical para que el personaje aterrice limpiamente
            _velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Recibe el golpe de un obstáculo: descuenta energía de la barra, da un
    /// segundo de invulnerabilidad, parpadea y (si corresponde) ralentiza.
    ///
    /// Ya NO mata de un toque como el Obstacle viejo: solo resta energía. La
    /// derrota llega cuando la barra queda en cero.
    /// </summary>
    public void RecibirChoque(float danioEnergia, float duracionRalentizacion)
    {
        if (!IsAlive) return;
        if (Invulnerable) return;

        // Escudo comprado en la tienda: absorbe el choque entero.
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.ConsumirEscudo())
        {
            StartCoroutine(Invulnerabilidad());
            return;
        }

        PlayerStats.Instance?.DrainEnergy(danioEnergia);

        // Refrescamos la barra del HUD en el acto (no esperamos al Update).
        if (GameplayHUD.Instance != null && PlayerStats.Instance != null)
            GameplayHUD.Instance.SetEnergy(PlayerStats.Instance.EnergyNormalized);

        StartCoroutine(Invulnerabilidad());
        StartCoroutine(Parpadeo());

        if (duracionRalentizacion > 0.01f)
            StartCoroutine(Ralentizar(duracionRalentizacion));

        // Energía en cero = derrota de nivel.
        if (PlayerStats.Instance != null && PlayerStats.Instance.energy <= 0f)
            Die();
    }

    /// <summary>Deja al jugador invulnerable un segundo (lo configura ObstacleManager).</summary>
    private System.Collections.IEnumerator Invulnerabilidad()
    {
        Invulnerable = true;

        float duracion = ObstacleManager.Instance != null
            ? ObstacleManager.Instance.TiempoInvulnerabilidad
            : 1f;

        yield return new WaitForSeconds(duracion);
        Invulnerable = false;
    }

    /// <summary>Hace parpadear al jugador mientras dura la invulnerabilidad.</summary>
    private System.Collections.IEnumerator Parpadeo()
    {
        // Buscamos todos los renderers del modelo (el jugador puede tener malla
        // skinned + accesorios).
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) yield break;

        const float intervalo = 0.1f;
        float transcurrido = 0f;
        bool visible = false;

        while (transcurrido < (ObstacleManager.Instance != null
                                ? ObstacleManager.Instance.TiempoInvulnerabilidad
                                : 1f))
        {
            visible = !visible;
            foreach (var r in renderers) if (r != null) r.enabled = visible;

            yield return new WaitForSeconds(intervalo);
            transcurrido += intervalo;
        }

        // Siempre termina visible: si queda apagado el jugador desaparece.
        foreach (var r in renderers) if (r != null) r.enabled = true;
    }

    /// <summary>Baja la velocidad un rato (tarjeta amarilla del árbitro, tackle).</summary>
    private System.Collections.IEnumerator Ralentizar(float duracion)
    {
        _modificadorVelocidad = 0.45f;
        yield return new WaitForSeconds(duracion);
        _modificadorVelocidad = 1f;
    }

    /// <summary>Mata al jugador y notifica al GameManager.</summary>
    public void Die()
    {
        if (!IsAlive) return;

        // Escudo: absorbe un choque por nivel comprado.
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.ConsumirEscudo())
            return;

        IsAlive = false;
        RunnerGameManager.Instance?.OnPlayerDied();
    }

    /// <summary>Reaparece al jugador en una posición dada (usado al reiniciar).</summary>
    public void Respawn(Vector3 position)
    {
        IsAlive       = true;
        RunnerEnabled = true;
        CurrentLane   = 1;
        _targetX      = LaneSystem.GetLaneX(CurrentLane);
        _velocity     = Vector3.zero;
        _isJumping    = false;

        // Limpiamos el estado de combate de la Fase B: al reaparecer no queda
        // invulnerable, ralentizado ni parpadeando.
        StopAllCoroutines();
        Invulnerable = false;
        _modificadorVelocidad = 1f;
        AlturaSobrePiso = 0f;
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r != null) r.enabled = true;

        // Desactivar el CharacterController antes de teletransportar para evitar
        // que quede atascado en la geometría.
        if (_cc != null)
        {
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = true;
        }
        else
        {
            transform.position = position;
        }
    }
}
