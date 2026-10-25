#if UNITY_EDITOR
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;

/// <summary>
/// SONDA TEMPORAL — junta lo que falta para la Fase 17: como esta armado el
/// prefab del jugador (para poder cambiar el color de la camiseta) y que tiene
/// enganchado el boton PERSONAJES del menu.
/// action = "ver".
/// </summary>
public static class _SondaFase17
{
    public static void Execute(string action)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===== SONDA FASE 17 =====");

        // 0. Dejar la escena como corresponde: menu visible y tienda apagada.
        var uiToo = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiToo != null)
        {
            var menuTr0 = Buscar(uiToo.transform, "MainMenuPanel");
            var shopTr0 = Buscar(uiToo.transform, "ShopPanel");
            if (menuTr0 != null) menuTr0.gameObject.SetActive(true);
            if (shopTr0 != null) shopTr0.gameObject.SetActive(false);
            sb.AppendLine($"escena: menu activo={menuTr0?.gameObject.activeSelf} " +
                          $"tienda activa={shopTr0?.gameObject.activeSelf}");
            EditorUtility.SetDirty(uiToo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            sb.AppendLine("escena guardada");
        }

        // 1. Prefab del jugador
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
        if (prefab == null) sb.AppendLine("no encontre Player.prefab");
        else
        {
            sb.AppendLine("Player.prefab:");
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                string ruta = Ruta(t, prefab.transform);
                var comps = t.GetComponents<Component>();
                var nombres = new StringBuilder();
                foreach (var c in comps) nombres.Append(c.GetType().Name).Append(' ');
                sb.AppendLine($"  - '{ruta}' [{nombres}]");

                var rend = t.GetComponent<Renderer>();
                if (rend != null)
                {
                    foreach (var mat in rend.sharedMaterials)
                    {
                        string props = "";
                        if (mat != null)
                        {
                            if (mat.HasProperty("_BaseColor")) props += " _BaseColor=" + mat.GetColor("_BaseColor");
                            if (mat.HasProperty("_Color"))     props += " _Color=" + mat.GetColor("_Color");
                            if (mat.HasProperty("_BaseMap"))   props += " _BaseMap=" + (mat.GetTexture("_BaseMap") != null);
                        }
                        sb.AppendLine($"      material '{mat?.name}' shader='{mat?.shader?.name}'{props}");

                        if (mat != null && mat.shader != null)
                        {
                            var nombres2 = new StringBuilder();
                            int total = mat.shader.GetPropertyCount();
                            for (int i = 0; i < total; i++)
                            {
                                var tipo = mat.shader.GetPropertyType(i);
                                nombres2.Append(mat.shader.GetPropertyName(i))
                                        .Append('(').Append(tipo).Append(") ");
                            }
                            sb.AppendLine("      propiedades: " + nombres2);
                        }
                    }
                }
            }
        }

        // 2. Panel de personajes
        var charPanel = GameObject.Find("Canvas/CharactersPanel");
        if (charPanel == null)
        {
            var ui = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
            var tr = ui != null ? Buscar(ui.transform, "CharactersPanel") : null;
            charPanel = tr != null ? tr.gameObject : null;
        }
        if (charPanel == null) sb.AppendLine("no encontre Canvas/CharactersPanel");
        else
        {
            var rt = (RectTransform)charPanel.transform;
            sb.AppendLine($"CharactersPanel: rect={rt.rect.size} hijos={rt.childCount} " +
                          $"componentes=[{string.Join(",", System.Array.ConvertAll(charPanel.GetComponents<Component>(), c => c.GetType().Name))}]");
        }

        // 3. Boton PERSONAJES del menu
        var menu = GameObject.Find("Canvas/MainMenuPanel");
        var btnTr = menu != null ? Buscar(menu.transform, "personajes") : null;
        if (btnTr == null) sb.AppendLine("no encontre el boton 'personajes'");
        else
        {
            var btn = btnTr.GetComponent<Button>();
            sb.AppendLine($"boton 'personajes': tiene Button={btn != null}");
            if (btn != null)
            {
                int n = btn.onClick.GetPersistentEventCount();
                sb.AppendLine($"  listeners persistentes={n}");
                for (int i = 0; i < n; i++)
                    sb.AppendLine($"    [{i}] {btn.onClick.GetPersistentTarget(i)?.GetType().Name}." +
                                  $"{btn.onClick.GetPersistentMethodName(i)}");
            }
        }

        Debug.Log(sb.ToString());
    }

    private static Transform Buscar(Transform raiz, string nombre)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }

    private static string Ruta(Transform t, Transform raiz)
    {
        string r = t.name;
        var p = t.parent;
        while (p != null && p != raiz) { r = p.name + "/" + r; p = p.parent; }
        return r;
    }
}
#endif
