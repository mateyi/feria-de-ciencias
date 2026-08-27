using UnityEngine;

/// <summary>
/// Red de seguridad anti-caída. Si el jugador cae por debajo de un nivel Y mínimo
/// (por ejemplo, por atravesar el suelo por un gap de colliders), se lo detecta como
/// fuera de límites y se dispara una recuperación segura:
///  - Reubicar al jugador sobre suelo válido, o
///  - Marcar muerte (game over) si no hay punto de recuperación.
///
/// Esto evita el "loop infinito de caída" y protege contra estados imposibles.
/// </summary>
public class OutOfBoundsGuard : MonoBehaviour
{
    public static OutOfBoundsGuard Instance { get; private set; }

    [Header("Límite inferior")]
    [Tooltip("Si la Y del jugador cae por debajo de este valor, se considera fuera de límites.")]
    [SerializeField] private float killY = -5f;

    [Header("Recuperación")]
    [Tooltip("Altura a la que se reubica al jugador sobre su X/Z actuales.")]
    [SerializeField] private float respawnY = 1f;

    [Tooltip("Si es true, en vez de reubicar, se llama a OnPlayerDied (game over).")]
    [SerializeField] private bool killInsteadOfRecover = false;

    [Tooltip("Cada cuánto (segundos) se chequea, para no hacerlo cada frame.")]
    [SerializeField] private float checkInterval = 0.15f;

    private float _timer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < checkInterval) return;
        _timer = 0f;

        var player = PlayerController.Instance;
        if (player == null) return;

        // Solo cuando se está jugando.
        if (RunnerGameManager.Instance != null && !RunnerGameManager.Instance.IsPlaying) return;

        float y = player.transform.position.y;
        if (y < killY)
        {
            HandleOutOfBounds(player);
        }
    }

    private void HandleOutOfBounds(PlayerController player)
    {
        try
        {
            if (killInsteadOfRecover)
            {
                Debug.LogWarning("[OutOfBoundsGuard] Jugador fuera de límites. Forzando game over.");
                player.Die();
                return;
            }

            // Reubicar sobre suelo válido manteniendo X/Z.
            Vector3 pos = player.transform.position;
            Vector3 safePos = new Vector3(pos.x, respawnY, pos.z);

            // Buscar suelo con raycast hacia abajo desde arriba de la posición actual.
            Vector3 rayStart = new Vector3(pos.x, respawnY + 20f, pos.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 60f))
            {
                safePos.y = hit.point.y + 1f;
            }

            Debug.LogWarning($"[OutOfBoundsGuard] Jugador fuera de límites (Y={pos.y:0.##}). Reubicando a {safePos}.");
            player.Respawn(safePos);
        }
        catch (System.Exception ex)
        {
            // Nunca dejar que una excepción aquí congele/crashee el juego.
            Debug.LogError($"[OutOfBoundsGuard] Error al recuperar al jugador: {ex.Message}");
        }
    }
}
