using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botones de la pausa (Fase E).
///
/// Qué hace:
///   - El botón "≡" del HUD (Canvas/GameplayPanel/PauseButton) pide la pausa.
///   - El botón SEGUIR del panel de pausa la saca.
///   - El botón MENU cierra la partida y vuelve al menú principal.
///
/// Los botones se enganchan por código en Awake y no con eventos del Inspector,
/// así el panel puede arrancar apagado y quedar enganchado igual.
///
/// Va en el GameObject Canvas (siempre activo), porque el panel de pausa
/// empieza apagado y su Awake no correría hasta mostrarlo.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
public class PauseUI : MonoBehaviour
{
    [Header("Botones")]
    [Tooltip("El botón de pausa del HUD (el de las tres rayas).")]
    [SerializeField] private Button botonPausa;

    [Tooltip("Botón SEGUIR del panel de pausa.")]
    [SerializeField] private Button botonSeguir;

    [Tooltip("Botón MENU del panel de pausa.")]
    [SerializeField] private Button botonMenu;

    private void Awake()
    {
        if (botonPausa)  botonPausa.onClick.AddListener(Pausar);
        if (botonSeguir) botonSeguir.onClick.AddListener(Reanudar);
        if (botonMenu)   botonMenu.onClick.AddListener(VolverAlMenu);
    }

    private void Pausar()       => RunnerGameManager.Instance?.PauseGame();
    private void Reanudar()     => RunnerGameManager.Instance?.ResumeGame();
    private void VolverAlMenu() => RunnerGameManager.Instance?.VolverAlMenuDesdePausa();
}
