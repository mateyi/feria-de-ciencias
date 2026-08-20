using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador con un offset fijo, estilo endless runner.
/// Posición suavizada para sensación de velocidad.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset desde el jugador")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -8f);

    [Header("Suavizado")]
    [SerializeField] private float positionSmoothing = 8f;
    [SerializeField] private float rotationSmoothing = 5f;

    [Header("Ángulo de cámara")]
    [SerializeField] private float lookDownAngle = 15f;

    private void LateUpdate()
    {
        if (target == null)
        {
            // Intentar encontrar al jugador automáticamente
            if (PlayerController.Instance != null)
                target = PlayerController.Instance.transform;
            return;
        }

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, positionSmoothing * Time.deltaTime);

        Quaternion desiredRot = Quaternion.Euler(lookDownAngle, 0f, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSmoothing * Time.deltaTime);
    }
}
