using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Controla al jugador: movimiento automático hacia adelante, cambio de carril
/// con swipe/teclado usando el nuevo Input System, y detección de colisiones.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movimiento")]
    [SerializeField] private float baseForwardSpeed = 8f;
    [SerializeField] private float laneSwitchSpeed  = 12f;
    [SerializeField] private float jumpForce        = 12f;
    [SerializeField] private float gravity          = -25f;

    [Header("Estado")]
    public int CurrentLane { get; private set; } = 1; // 0=izq, 1=centro, 2=der
    public bool IsAlive { get; private set; } = true;
    public bool IsGrounded { get; private set; }

    // Velocidad actual (puede aumentar con stats)
    public float CurrentSpeed => baseForwardSpeed * (1f + (PlayerStats.Instance != null ? PlayerStats.Instance.speed / 200f : 0f));

    private CharacterController _cc;
    private Vector3 _velocity;
    private float _targetX;
    private bool _isJumping;

    // Input táctil
    private Vector2 _touchStart;
    private bool _touchActive;

    // Input System actions (leídas directamente)
    private InputAction _moveAction;
    private InputAction _jumpAction;

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

        // Callbacks
        _jumpAction.performed += _ => Jump();
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
        if (!IsAlive) return;
        if (RunnerGameManager.Instance != null && !RunnerGameManager.Instance.IsPlaying) return;

        HandleKeyboardInput();
        HandleTouchInput();
        Move();
    }

    private void HandleKeyboardInput()
    {
        // Flechas / WASD con el nuevo Input System
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame  || Keyboard.current.aKey.wasPressedThisFrame)  MoveLeft();
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)  MoveRight();
            if (Keyboard.current.spaceKey.wasPressedThisFrame      || Keyboard.current.wKey.wasPressedThisFrame   ||
                Keyboard.current.upArrowKey.wasPressedThisFrame)   Jump();
        }
    }

    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;

        var touches = Touchscreen.current.touches;
        foreach (var touch in touches)
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
                else if (delta.y > 50) Jump();
            }
        }
    }

    private void Move()
    {
        IsGrounded = _cc.isGrounded;

        if (IsGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        // Gravedad
        _velocity.y += gravity * Time.deltaTime;

        // Movimiento lateral suave
        float currentX = transform.position.x;
        float newX = Mathf.MoveTowards(currentX, _targetX, laneSwitchSpeed * Time.deltaTime);

        Vector3 move = new Vector3(
            newX - currentX,
            _velocity.y * Time.deltaTime,
            CurrentSpeed * Time.deltaTime
        );

        _cc.Move(move);
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

    public void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;
        RunnerGameManager.Instance?.OnPlayerDied();
    }

    public void Respawn(Vector3 position)
    {
        IsAlive     = true;
        CurrentLane = 1;
        _targetX    = LaneSystem.GetLaneX(CurrentLane);
        _velocity   = Vector3.zero;
        transform.position = position;
    }
}
