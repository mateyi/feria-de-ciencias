#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Text;

/// <summary>
/// SONDA DE LA MIRA (solo lectura). Espera a que el penal esté en "Apuntando"
/// y vuelca todos los datos del RectTransform y de la Image del AimReticle,
/// para entender por qué no se dibuja en pantalla.
/// </summary>
public static class _SondaMira
{
    private static float _t0;
    private static bool  _hecho;

    public static void Execute()
    {
        _t0 = Time.realtimeSinceStartup;
        _hecho = false;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!Application.isPlaying) { EditorApplication.update -= Tick; return; }

        var pmm = PenaltyMinigameManager.Instance;
        if (pmm == null) return;

        // Arrancar el penal si todavía no empezó.
        if (pmm.Estado == PenaltyMinigameManager.PenaltyState.Inactivo)
        {
            if (Time.realtimeSinceStartup - _t0 > 0.3f) pmm.IniciarModoPenalDebug();
            return;
        }

        if (pmm.Estado != PenaltyMinigameManager.PenaltyState.Apuntando) return;
        if (_hecho) return;
        if (Time.realtimeSinceStartup - _t0 < 1.8f) return;

        _hecho = true;
        EditorApplication.update -= Tick;

        var sb = new StringBuilder();
        sb.AppendLine("========== SONDA MIRA ==========");

        var so = new SerializedObject(pmm);
        var rt = so.FindProperty("aimReticle")?.objectReferenceValue as RectTransform;

        if (rt == null) { sb.AppendLine("aimReticle = NULL"); Debug.Log(sb.ToString()); return; }

        sb.AppendLine($"Objeto: '{GetPath(rt)}'");
        sb.AppendLine($"  activeSelf={rt.gameObject.activeSelf} activeInHierarchy={rt.gameObject.activeInHierarchy}");
        sb.AppendLine($"  siblingIndex={rt.GetSiblingIndex()} / {rt.parent.childCount - 1}");
        sb.AppendLine($"  rect={rt.rect} sizeDelta={rt.sizeDelta} localScale={rt.localScale}");
        sb.AppendLine($"  pivot={rt.pivot} anchorMin={rt.anchorMin} anchorMax={rt.anchorMax}");
        sb.AppendLine($"  position={rt.position} anchoredPosition={rt.anchoredPosition}");
        sb.AppendLine($"  Screen = {Screen.width} x {Screen.height}");

        var canvas = rt.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            sb.AppendLine($"Canvas '{canvas.name}': renderMode={canvas.renderMode} scaleFactor={canvas.scaleFactor} " +
                          $"pixelRect={canvas.pixelRect} sortingOrder={canvas.sortingOrder}");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
                sb.AppendLine($"  CanvasScaler: mode={scaler.uiScaleMode} ref={scaler.referenceResolution} match={scaler.matchWidthOrHeight}");
            sb.AppendLine($"  Canvas RectTransform rect={canvas.GetComponent<RectTransform>().rect}");
        }

        var img = rt.GetComponent<Image>();
        if (img == null) sb.AppendLine("  Image = NULL");
        else
        {
            sb.AppendLine($"  Image: enabled={img.enabled} sprite={(img.sprite != null ? img.sprite.name : "NULL")} " +
                          $"color={img.color} type={img.type} preserveAspect={img.preserveAspect}");
            if (img.sprite != null)
                sb.AppendLine($"    sprite.rect={img.sprite.rect} ppu={img.sprite.pixelsPerUnit} " +
                              $"texture={(img.sprite.texture != null ? img.sprite.texture.width + "x" + img.sprite.texture.height : "NULL")}");
            sb.AppendLine($"    material={(img.material != null ? img.material.name : "NULL")} raycastTarget={img.raycastTarget}");
        }

        var cr = rt.GetComponent<CanvasRenderer>();
        if (cr != null) sb.AppendLine($"  CanvasRenderer: cull={cr.cull} hasPopInstruction={cr.hasPopInstruction}");

        // ¿El padre está activo?
        var padre = rt.parent;
        while (padre != null)
        {
            sb.AppendLine($"  Ancestro '{padre.name}' activeSelf={padre.gameObject.activeSelf} " +
                          $"scale={padre.localScale}");
            padre = padre.parent;
        }

        sb.AppendLine("================================");
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
