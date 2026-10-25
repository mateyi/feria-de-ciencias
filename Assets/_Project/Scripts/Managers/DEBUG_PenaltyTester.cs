using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// SCRIPT TEMPORAL DE TESTEO — borrar cuando la transición automática
/// de la META funcione bien.
///
/// Al apretar la tecla P durante el Play, fuerza el inicio del
/// minijuego de penales directamente, sin depender de RunnerGameManager
/// ni de haber cruzado la línea de meta. Útil para probar el minijuego
/// (apuntado con mouse, arquero, resultado) de forma aislada.
///
/// CÓMO USARLO:
/// 1. Creá un GameObject vacío en la escena (click derecho en Hierarchy
///    > Create Empty), llamalo "DEBUG_PenaltyTester".
/// 2. Arrastrale este script como componente.
/// 3. Dale Play, apretá "Jugar" en el menú normal (así se instancia el
///    jugador), y una vez que estés en la escena de carrera, apretá P.
/// 4. Debería activarse el minijuego de penales inmediatamente.
///
/// Carpeta sugerida: Assets/_Project/Scripts/Managers (o cualquier
/// carpeta, es descartable).
/// </summary>
public class DEBUG_PenaltyTester : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            Debug.Log("[DEBUG_PenaltyTester] Forzando inicio del minijuego de penales...");

            if (PenaltyMinigameManager.Instance == null)
            {
                Debug.LogWarning("[DEBUG_PenaltyTester] No se encontró PenaltyMinigameManager.Instance en la escena.");
                return;
            }

            if (PlayerController.Instance == null)
            {
                Debug.LogWarning("[DEBUG_PenaltyTester] No hay un PlayerController.Instance todavía — " +
                    "apretá 'Jugar' en el menú principal primero para que se instancie el jugador.");
                return;
            }

            PenaltyMinigameManager.Instance.BeginPenaltySequence();
        }
    }
}
