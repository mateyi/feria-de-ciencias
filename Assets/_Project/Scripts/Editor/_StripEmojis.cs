#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// Limpia de la escena los caracteres que la fuente LiberationSans SDF NO tiene
/// (emojis y símbolos con código >= U+2500). Unity los reemplaza por "□" y llena
/// la consola de warnings. Se eliminan del texto; no se cambia nada más.
/// </summary>
public static class _StripEmojis
{
    // A partir de este código empiezan los símbolos/emojis que no están en la fuente.
    private const char Limite = '\u2500';

    public static void Execute()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== LIMPIEZA DE EMOJIS ==========");

        var textos = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int modificados = 0, caracteres = 0;

        foreach (var t in textos)
        {
            if (t == null || string.IsNullOrEmpty(t.text)) continue;

            string original = t.text;
            var limpio = new StringBuilder(original.Length);

            foreach (char c in original)
                if (c < Limite) limpio.Append(c);   // los emojis son surrogates (>= U+D800), tambien caen

            string nuevo = limpio.ToString().Trim();

            if (nuevo != original)
            {
                // Preferimos "Listo" con espacio simple por si el texto era solo un emoji.
                t.text = nuevo;
                EditorUtility.SetDirty(t);
                modificados++;
                caracteres += original.Length - nuevo.Length;
                sb.AppendLine($"   '{GetPath(t.transform)}': \"{original}\" -> \"{nuevo}\"");
            }
        }

        sb.AppendLine($"Textos modificados: {modificados}  (caracteres quitados: {caracteres})");

        // Guardar la escena
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        sb.AppendLine($"Escena guardada: {escena.name}");

        Debug.Log(sb.ToString());
    }

    private static string GetPath(Transform t)
    {
        string s = t.name;
        while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
        return s;
    }
}
#endif
