using UnityEngine;
using UnityEditor.SceneManagement;

/// <summary>
/// SONDA TEMPORAL. Recarga la escena desde disco (descarta los cambios que
/// dejaron las pruebas anteriores), aplica la pose pedida al arquero y mide
/// dónde quedó cada hueso para poder comprobarlo sin adivinar por la captura.
/// </summary>
public static class _PoseArquero
{
    public static void Execute(string action, string zone, float u)
    {
        if (action == "reload")
        {
            // Descarta todo lo que hayan tocado las pruebas: la escena vuelve al
            // estado guardado (arquero parado y huesos en la pose del modelo).
            EditorSceneManager.OpenScene("Assets/VAMOSBIEN.unity", OpenSceneMode.Single);
            Debug.Log("[PoseArquero] Escena recargada desde disco.");
            return;
        }

        var gk = UnityEngine.Object.FindFirstObjectByType<GoalkeeperAI>(FindObjectsInactive.Include);
        if (gk == null) { Debug.LogError("[PoseArquero] No encontré GoalkeeperAI."); return; }

        if (action == "bind")
        {
            // Pose tal cual viene en el modelo, sin tocar un solo hueso.
            Medir(gk, "BIND");
            return;
        }

        if (action == "reposo")
        {
            gk.ReposoDePrueba();
            Medir(gk, "REPOSO");
            return;
        }

        if (action == "restore")
        {
            gk.RestaurarPoseOriginal();
            var an0 = gk.GetComponent<Animator>();
            if (an0 != null) an0.enabled = true;
            Medir(gk, "RESTAURADA");
            return;
        }

        GoalkeeperAI.SaveZone z = zone switch
        {
            "Left"  => GoalkeeperAI.SaveZone.Left,
            "Right" => GoalkeeperAI.SaveZone.Right,
            _       => GoalkeeperAI.SaveZone.Center,
        };

        gk.PoseDePrueba(z, u);
        Medir(gk, $"{z} u={u:0.00}");
    }

    /// <summary>Registra la posición de los huesos clave de la pose actual.</summary>
    private static void Medir(GoalkeeperAI gk, string etiqueta)
    {
        var izq   = Buscar(gk.transform, "LeftArm");
        var codoI = Buscar(gk.transform, "LeftForeArm");
        var der   = Buscar(gk.transform, "RightArm");
        var codoD = Buscar(gk.transform, "RightForeArm");

        string di = izq != null && codoI != null
            ? (codoI.position - izq.position).normalized.ToString("F2") : "?";
        string dd = der != null && codoD != null
            ? (codoD.position - der.position).normalized.ToString("F2") : "?";

        Debug.Log($"[PoseArquero] {etiqueta} | raíz={gk.transform.position} " +
                  $"rot={gk.transform.eulerAngles} | brazoIzq={di} brazoDer={dd}");

        // Desglose hueso por hueso: sirve para comprobar si los brazos quedan
        // abiertos (manos más afuera que los hombros) o cruzados.
        var hombroI = Buscar(gk.transform, "LeftShoulder");
        var hombroD = Buscar(gk.transform, "RightShoulder");
        var manoI   = Buscar(gk.transform, "LeftHand");
        var manoD   = Buscar(gk.transform, "RightHand");
        var cadera  = Buscar(gk.transform, "Hips");
        var pieI    = Buscar(gk.transform, "LeftToeBase");
        var pieD    = Buscar(gk.transform, "RightToeBase");

        string P(Transform t) => t != null ? t.position.ToString("F2") : "?";

        Debug.Log($"[PoseArquero]   cadera={P(cadera)} hombroI={P(hombroI)} hombroD={P(hombroD)}");
        Debug.Log($"[PoseArquero]   codoI={P(codoI)} codoD={P(codoD)}");
        Debug.Log($"[PoseArquero]   manoI={P(manoI)} manoD={P(manoD)}");
        Debug.Log($"[PoseArquero]   pieI={P(pieI)} pieD={P(pieD)}");

        // Comprobación de que la MALLA va con los huesos: se hornea la malla
        // skinned y se miran sus límites en espacio de mundo. Si los brazos se
        // abren, el ancho en X tiene que crecer; si no, la captura engaña.
        var smr = gk.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (smr != null && smr.sharedMesh != null)
        {
            var copia = new Mesh();
            smr.BakeMesh(copia, true);
            copia.RecalculateBounds();
            Bounds b = copia.bounds;

            // Se pasa a mundo: el SkinnedMeshRenderer cuelga de Armature y tiene
            // escala 0,01, así que los límites en local no se comparan a ojo.
            Matrix4x4 m = smr.transform.localToWorldMatrix;
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = m.MultiplyPoint3x4(new Vector3(
                    (i & 1) == 0 ? b.min.x : b.max.x,
                    (i & 2) == 0 ? b.min.y : b.max.y,
                    (i & 4) == 0 ? b.min.z : b.max.z));
                min = Vector3.Min(min, c);
                max = Vector3.Max(max, c);
            }

            Debug.Log($"[PoseArquero]   malla mundo: min={min.ToString("F2")} max={max.ToString("F2")}");

            // Silueta: dónde está el ancho de la parte de arriba. Con los brazos
            // en V las manos son lo más alto y lo más abierto; con los brazos
            // cruzados lo más alto es la cabeza y el ancho de arriba es chico.
            Vector3[] vs = copia.vertices;
            float techo = max.y - 0.40f;
            float xMin = float.MaxValue, xMax = float.MinValue;
            int cuenta = 0;
            foreach (var v in vs)
            {
                Vector3 w = m.MultiplyPoint3x4(v);
                if (w.y < techo) continue;
                xMin = Mathf.Min(xMin, w.x);
                xMax = Mathf.Max(xMax, w.x);
                cuenta++;
            }
            Debug.Log($"[PoseArquero]   arriba (y>{techo:0.00}): x de {xMin:0.00} a {xMax:0.00} " +
                      $"(ancho {xMax - xMin:0.00}, vértices {cuenta})");

            UnityEngine.Object.DestroyImmediate(copia);
        }

        // Sin esto la ventana de Escena no se vuelve a dibujar (no hay input del
        // usuario), así que una captura devuelve el cuadro viejo y engaña.
        UnityEditor.SceneView.RepaintAll();
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }

    private static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
