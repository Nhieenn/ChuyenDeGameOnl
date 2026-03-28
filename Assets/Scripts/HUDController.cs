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
    private Label _kdaLabel;

    public static HUDController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;
        
        _root = uiDoc.rootVisualElement;
        
        // TÌM VÀ XOÁ CÁC ELEMENT CŨ CỦA HUD NẾU CÓ ĐỂ TRÁNH DUPLICATE, TUYỆT ĐỐI KHÔNG DÙNG _root.Clear() GÂY XÓA THANH MÁU!
        var oldHud = _root.Q<VisualElement>("hud-container");
        if (oldHud != null) _root.Remove(oldHud);

        var hudContainer = new VisualElement { name = "hud-container" };
        hudContainer.style.flexGrow = 1;
        hudContainer.pickingMode = PickingMode.Ignore;
        _root.Add(hudContainer);

        BuildCrosshair(hudContainer);
        BuildKDA_Board(hudContainer);
    }

    private void BuildKDA_Board(VisualElement parent)
    {
        // Khung nền chứa điểm số K/D (Top Right)
        var kdaContainer = new VisualElement();
        kdaContainer.style.position = Position.Absolute;
        kdaContainer.style.top = 20;
        kdaContainer.style.right = 20;
        kdaContainer.style.backgroundColor = new Color(0, 0, 0, 0.7f);
        kdaContainer.style.paddingLeft = 20;
        kdaContainer.style.paddingRight = 20;
        kdaContainer.style.paddingTop = 10;
        kdaContainer.style.paddingBottom = 10;
        kdaContainer.style.borderTopLeftRadius = 8;
        kdaContainer.style.borderTopRightRadius = 8;
        kdaContainer.style.borderBottomLeftRadius = 8;
        kdaContainer.style.borderBottomRightRadius = 8;

        // Label chữ
        _kdaLabel = new Label("K/D: 0 / 0");
        _kdaLabel.style.color = Color.white;
        _kdaLabel.style.fontSize = 24;
        _kdaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        kdaContainer.Add(_kdaLabel);
        parent.Add(kdaContainer);
    }

    private void Update()
    {
        if (_kdaLabel == null || Fusion.NetworkRunner.Instances.Count == 0) return;

        var runner = Fusion.NetworkRunner.Instances[0];
        if (runner == null || !runner.IsRunning) return;

        // Quét lấy điểm của bản thân người chơi để hiển thị lên HUD K/DA
        var myPlayerRef = runner.LocalPlayer;
        var allPlayers = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var hp in allPlayers)
        {
            if (hp.Object != null && hp.Object.InputAuthority == myPlayerRef)
            {
                _kdaLabel.text = $"K/D: {hp.Kills} / {hp.Deaths}";
                break;
            }
        }
    }

    private void BuildCrosshair(VisualElement parent)
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
        parent.Add(wrap);

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
