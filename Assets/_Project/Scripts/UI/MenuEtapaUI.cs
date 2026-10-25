using UnityEngine;
using TMPro;

/// <summary>
/// Muestra en el menú principal el nombre de la etapa del torneo que toca jugar
/// (Entrenamiento, Fase de grupos, Octavos de final, Cuartos de final,
/// Semifinal o Final).
///
/// Reemplaza el viejo cartel "Nivel 1", que era un número sin significado para
/// el jugador. El texto sale del LevelConfig, así que si se agregan o renombran
/// etapas en el asset, el menú se actualiza solo.
///
/// Va en el GameObject "Canvas/MainMenuPanel/nivel" y usa el TMP_Text del mismo
/// objeto, así no hay que arrastrar nada en el Inspector.
///
/// Carpeta: Assets/_Project/Scripts/UI/
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class MenuEtapaUI : MonoBehaviour
{
    // El texto del cartel, cacheado para no buscarlo en cada refresco.
    private TMP_Text _texto;

    private void Awake()
    {
        _texto = GetComponent<TMP_Text>();
    }

    // Cuando el panel del menú se vuelve a mostrar (por ejemplo al volver de una
    // carrera o al salir de una tienda) el cartel se reescribe con la etapa actual.
    private void OnEnable()
    {
        Refrescar();
    }

    // Start corre después de todos los Awake de la escena, así que acá el
    // ObstacleManager ya existe y nos puede prestar su LevelConfig.
    private void Start()
    {
        Refrescar();
    }

    /// <summary>
    /// Reescribe el cartel con el nombre de la etapa que corresponde al nivel
    /// guardado. Si el ObstacleManager todavía no existe (arranque muy temprano)
    /// no toca nada, para no dejar el cartel vacío.
    /// </summary>
    public void Refrescar()
    {
        if (_texto == null) _texto = GetComponent<TMP_Text>();
        if (_texto == null) return;

        LevelConfig config = ObstacleManager.Configuracion;
        if (config == null) return;

        // SaveManager.Data siempre está disponible: no depende de GameData.
        _texto.text = config.NombreDeEtapa(SaveManager.Data.currentLevel);
    }
}
