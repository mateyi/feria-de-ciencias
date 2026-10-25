using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Un obstáculo concreto habilitado dentro de un nivel, con su peso relativo.
/// </summary>
[System.Serializable]
public class EntradaObstaculo
{
    [Tooltip("Datos del obstáculo.")]
    public ObstacleData dato;

    [Tooltip("Peso dentro de ESTE nivel. Más peso = sale más seguido.")]
    [Range(0f, 20f)]
    public float peso = 1f;
}

/// <summary>
/// Configuración de UN nivel de dificultad.
/// </summary>
[System.Serializable]
public class NivelObstaculos
{
    [Header("Identidad")]
    [Range(1, 20)]
    public int nivel = 1;

    public string nombre = "Entrenamiento";

    [Header("Ritmo")]
    [Tooltip("Multiplicador de velocidad del jugador en este nivel. 1 = normal.")]
    [Range(0.5f, 2.5f)]
    public float multiplicadorVelocidad = 1f;

    [Tooltip("Cuántas filas de obstáculos aparecen por segmento de pista (0 = ninguna).")]
    [Range(0f, 4f)]
    public float filasPorSegmento = 1f;

    [Tooltip("Distancia mínima, en metros, entre dos filas. Más margen = más tiempo " +
             "para reaccionar.")]
    [Range(6f, 40f)]
    public float margenEntreFilas = 26f;

    [Tooltip("Probabilidad (0-1) de que una fila ocupe DOS carriles en vez de uno.")]
    [Range(0f, 1f)]
    public float probabilidadDosCarriles = 0f;

    [Header("Obstáculos permitidos")]
    [Tooltip("Lista de obstáculos que pueden aparecer en este nivel, con su peso.")]
    public List<EntradaObstaculo> permitidos = new List<EntradaObstaculo>();
}

/// <summary>
/// ScriptableObject con la dificultad de TODOS los niveles.
///
/// Las seis etapas son las de un Mundial, en orden. Al jugador NO se le habla
/// de "nivel 1, 2, 3": el HUD, el menú y la pantalla de victoria muestran el
/// nombre de la etapa.
///
///   1 Entrenamiento      → conos y barreras simples
///   2 Fase de grupos     → + árbitros quietos
///   3 Octavos de final   → + defensas deslizantes y árbitros móviles
///   4 Cuartos de final   → más densidad, menos margen
///   5 Semifinal          → combos barrera + defensa
///   6 Final              → todo, densidad máxima. Ganarla = campeón del mundo.
///
/// Crear desde: Assets > Create > MundialRunner > LevelConfig
/// Carpeta: Assets/_Project/Scripts/Data/
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "MundialRunner/LevelConfig")]
public class LevelConfig : ScriptableObject
{
    [Tooltip("Un registro por nivel. Si el nivel jugado supera la lista, se usa el último.")]
    public List<NivelObstaculos> niveles = new List<NivelObstaculos>();

    [Header("Ajustes generales")]
    [Tooltip("Segundos de invulnerabilidad después de un choque, para no recibir daño múltiple.")]
    [Range(0f, 3f)]
    public float tiempoInvulnerabilidad = 1f;

    [Tooltip("Velocidad lateral por defecto de los deslizantes, en m/s.")]
    public float velocidadLateralPorDefecto = 7f;

    /// <summary>
    /// Devuelve la configuración del nivel pedido. Si el nivel no existe en la
    /// lista, devuelve el último (para que el juego nunca se quede sin datos).
    /// </summary>
    public NivelObstaculos Obtener(int nivel)
    {
        if (niveles == null || niveles.Count == 0) return null;

        // Buscamos el registro exacto.
        foreach (var n in niveles)
            if (n != null && n.nivel == nivel) return n;

        // Si no está, devolvemos el más alto que no supere el nivel pedido,
        // o el primero si el nivel es más bajo que todos.
        NivelObstaculos mejor = null;
        foreach (var n in niveles)
        {
            if (n == null) continue;
            if (n.nivel <= nivel && (mejor == null || n.nivel > mejor.nivel)) mejor = n;
        }

        if (mejor != null) return mejor;

        // Nivel más bajo que todos: usamos el más chico.
        NivelObstaculos primero = null;
        foreach (var n in niveles)
            if (n != null && (primero == null || n.nivel < primero.nivel)) primero = n;
        return primero;
    }

    // ─── Etapas del torneo ────────────────────────────────────────────────────

    /// <summary>
    /// Número de la última etapa configurada: la FINAL. Ganar ahí es salir
    /// campeón del mundo y el torneo vuelve a empezar.
    /// </summary>
    public int NivelFinal
    {
        get
        {
            if (niveles == null || niveles.Count == 0) return 1;

            int maximo = 1;
            foreach (var n in niveles)
                if (n != null && n.nivel > maximo) maximo = n.nivel;

            return maximo;
        }
    }

    /// <summary>
    /// Nombre deportivo de la etapa que corresponde al nivel pedido
    /// (Entrenamiento, Fase de grupos, Octavos de final, Cuartos de final,
    /// Semifinal, Final). Si el asset quedara sin datos devuelve "Nivel N"
    /// para que la interfaz nunca se quede vacía.
    /// </summary>
    public string NombreDeEtapa(int nivel)
    {
        NivelObstaculos n = Obtener(nivel);
        if (n != null && !string.IsNullOrEmpty(n.nombre)) return n.nombre;

        return "Nivel " + nivel;
    }

    /// <summary>True si el nivel pedido es la FINAL (la última etapa del torneo).</summary>
    public bool EsFinal(int nivel) => nivel >= NivelFinal;

    /// <summary>
    /// Nombre de la etapa que viene después de la indicada. Si ya se jugaba la
    /// final devuelve la primera etapa, porque al salir campeón el torneo
    /// arranca de nuevo.
    /// </summary>
    public string EtapaSiguiente(int nivel)
    {
        if (nivel >= NivelFinal) return NombreDeEtapa(1);
        return NombreDeEtapa(nivel + 1);
    }
}
