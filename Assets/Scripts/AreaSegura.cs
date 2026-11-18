using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class AreaSegura : MonoBehaviour
{
    Rect ultima;
    void OnEnable() { Ajustar(); }
    void Update() { if (ultima != Screen.safeArea) Ajustar(); }
    void Ajustar()
    {
        if (Screen.width == 0 || Screen.height == 0) return;
        ultima = Screen.safeArea;
        var rect = (RectTransform)transform;
        rect.anchorMin = new Vector2(ultima.xMin / Screen.width, ultima.yMin / Screen.height);
        rect.anchorMax = new Vector2(ultima.xMax / Screen.width, ultima.yMax / Screen.height);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
