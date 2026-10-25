#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// SONDA TEMPORAL (Fase 13). Instancia un segmento de decisión en la escena,
/// informa qué decisión le tocó y qué iconos quedaron puestos en cada cartel,
/// para poder mirarlo con una captura. Con action="limpiar" lo borra.
/// </summary>
public static class _VerCartel
{
    const string Nombre = "__CartelDePrueba";

    public static void Execute(string action)
    {
        var viejo = GameObject.Find(Nombre);
        if (viejo != null) Object.DestroyImmediate(viejo);

        if (action == "limpiar")
        {
            Debug.Log("[VerCartel] Segmento de prueba eliminado.");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Segment_Decision.prefab");
        if (prefab == null) { Debug.LogError("[VerCartel] no encontré el prefab"); return; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = Nombre;
        go.transform.position = new Vector3(0f, 0f, -2f);
        go.transform.rotation = Quaternion.identity;

        var punto = go.GetComponentInChildren<DecisionPoint>(true);
        var iconoIzq = Buscar(go.transform, "Icono_Cartel_IZQ")?.GetComponent<SpriteRenderer>();
        var iconoDer = Buscar(go.transform, "Icono_Cartel_DER")?.GetComponent<SpriteRenderer>();

        string spriteIzq = iconoIzq != null && iconoIzq.sprite != null ? iconoIzq.sprite.name : "NULL";
        string spriteDer = iconoDer != null && iconoDer.sprite != null ? iconoDer.sprite.name : "NULL";

        Debug.Log($"[VerCartel] instanciado en z=-2. IconoIZQ={spriteIzq} IconoDER={spriteDer} " +
                  $"DecisionPoint={(punto != null)}");

        // Nombrar la decisión elegida por el sorteo de Awake.
        var so = new SerializedObject(punto);

        // En edición Awake NO corre, así que forzamos una decisión concreta y
        // volvemos a aplicar los carteles por reflexión para ver el resultado
        // real (color verde/rojo + icono) en la captura.
        var data = AssetDatabase.LoadAssetAtPath<DecisionData>(
            "Assets/_Project/Data/Decisiones/Decision_Powerade_Cerveza.asset");
        if (data != null)
        {
            so.FindProperty("decisionData").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();

            var metodo = typeof(DecisionPoint).GetMethod("AplicarCarteles",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            metodo?.Invoke(punto, null);
        }

        var prop = so.FindProperty("decisionData");
        var elegida = prop != null ? prop.objectReferenceValue as DecisionData : null;
        if (elegida != null)
            Debug.Log($"[VerCartel] decisión mostrada: {elegida.name} | " +
                      $"izq='{elegida.leftLabel}' pos={elegida.leftIsPositive} | " +
                      $"der='{elegida.rightLabel}' pos={elegida.rightIsPositive}");

        Selection.activeGameObject = go;
    }

    static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
#endif
