using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// HUD crosshair bằng UI Toolkit.
/// Gắn vào HUD GameObject. UIDocument cần Panel Settings với Sort Order = 10.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class HUDController : MonoBehaviour
{
    [Header("Crosshair")]
    public float size = 18f;
    public float gap = 5f;
    public float thickness = 2f;
    public Color color = Color.white;

    private VisualElement _root;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.Clear();
        _root.style.flexGrow = 1;
        _root.pickingMode = PickingMode.Ignore;
        BuildCrosshair();
    }

    private void BuildCrosshair()
    {
        var sc = new StyleColor(color);

        // Wrapper căn giữa
        var wrap = new VisualElement
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                left = new Length(50, LengthUnit.Percent),
                top  = new Length(50, LengthUnit.Percent),
                width = 0,
                height = 0,
            }
        };
        _root.Add(wrap);

        float h = gap / 2f;

        // Trái
        AddBar(wrap, sc, -(h + size), -thickness / 2f, size, thickness);
        // Phải
        AddBar(wrap, sc, h, -thickness / 2f, size, thickness);
        // Trên
        AddBar(wrap, sc, -thickness / 2f, -(h + size), thickness, size);
        // Dưới
        AddBar(wrap, sc, -thickness / 2f, h, thickness, size);

        // Dot trung tâm
        float d = thickness + 1f;
        AddBar(wrap, sc, -d / 2f, -d / 2f, d, d);
    }

    private void AddBar(VisualElement parent, StyleColor c, float l, float t, float w, float h)
    {
        var el = new VisualElement
        {
            pickingMode = PickingMode.Ignore,
            style =
            {
                position = Position.Absolute,
                left = l, top = t, width = w, height = h,
                backgroundColor = c,
            }
        };
        parent.Add(el);
    }
}
