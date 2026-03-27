using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Quản lý thanh máu nổi trên đầu nhân vật (Floating HP Bar) bằng UI Toolkit.
/// Nằm trên HUD GameObject hoặc một UI Document chung.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class FloatingUIManager : MonoBehaviour
{
    public static FloatingUIManager Instance { get; private set; }

    private UIDocument _uiDoc;
    private VisualElement _root;

    // Danh sách các thanh máu của từng người chơi
    private Dictionary<HealthSystem, VisualElement> _healthBars = new Dictionary<HealthSystem, VisualElement>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[FloatingUIManager] Đã khởi tạo thành công (Awake).");
        }
        else Destroy(gameObject);

        _uiDoc = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // Có thể root chưa sẵn sàng ngay Awake, nên gán ở OnEnable
        _root = _uiDoc.rootVisualElement;
    }

    /// <summary>
    /// HealthSystem sẽ gọi hàm này khi nhân vật Spawn ra
    /// </summary>
    public void RegisterPlayer(HealthSystem health)
    {
        if (_root == null)
        {
            Debug.LogError("[FloatingUIManager] LỖI: _root đang NULL, hãy kiểm tra xem component UIDocument có hoạt động không!");
            return;
        }


        // Code tạo giao diện thanh máu (Container viền đen)
        var container = new VisualElement();
        container.pickingMode = PickingMode.Ignore;
        container.style.position = Position.Absolute;
        container.style.width = 120;
        container.style.height = 16;
        container.style.backgroundColor = new Color(0, 0, 0, 0.6f);
        container.style.borderTopLeftRadius = 4;
        container.style.borderTopRightRadius = 4;
        container.style.borderBottomLeftRadius = 4;
        container.style.borderBottomRightRadius = 4;
        container.style.borderBottomWidth = 2;
        container.style.borderTopWidth = 2;
        container.style.borderLeftWidth = 2;
        container.style.borderRightWidth = 2;
        container.style.borderBottomColor = Color.black;
        container.style.borderTopColor = Color.black;
        container.style.borderLeftColor = Color.black;
        container.style.borderRightColor = Color.black;

        // Phần màu bên trong (Fill xanh)
        var fill = new VisualElement();
        fill.name = "fill";
        fill.style.height = Length.Percent(100);
        fill.style.width = Length.Percent(100);
        fill.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f); // Xanh lá
        
        Debug.Log($"[FloatingUIManager] Đã tạo xong thanh máu cho Player {health.Object?.Id}");
        
        container.Add(fill);
        _root.Add(container);

        _healthBars.Add(health, container);
    }

    /// <summary>
    /// HealthSystem gọi khi nhân vật bị xóa
    /// </summary>
    public void UnregisterPlayer(HealthSystem health)
    {
        if (_healthBars.TryGetValue(health, out var element))
        {
            if (_root != null && _root.Contains(element))
                _root.Remove(element);
            _healthBars.Remove(health);
        }
    }

    /// <summary>
    /// Cập nhật % thanh máu khi nhận sát thương
    /// </summary>
    public void UpdateHealth(HealthSystem health, float currentHp, float maxHp)
    {
        if (_healthBars.TryGetValue(health, out var element))
        {
            var fill = element.Q<VisualElement>("fill");
            float percent = Mathf.Clamp01(currentHp / maxHp) * 100f;
            fill.style.width = Length.Percent(percent);

            // Đổi màu tùy theo mức máu
            if (percent > 60) fill.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            else if (percent > 30) fill.style.backgroundColor = new Color(0.9f, 0.7f, 0.1f);
            else fill.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
            
            Debug.Log($"[FloatingUIManager] Cập nhật máu Player {health.Object?.Id} -> {percent}%");
        }
    }

    private void LateUpdate()
    {
        if (Camera.main == null)
        {
            // Debug.LogWarning("[FloatingUIManager] Không tìm thấy Camera.main!");
            return;
        }
        if (_root == null || _root.panel == null)
        {
            // Debug.LogWarning("[FloatingUIManager] UIDocument Root hoặc Panel chưa sẵn sàng!");
            return;
        }

        foreach (var kvp in _healthBars)
        {
            var health = kvp.Key;
            var element = kvp.Value;

            // Vị trí trên đỉnh đầu nhân vật (cao lên 2.2 units)
            Vector3 worldPos = health.transform.position + Vector3.up * 2.2f;

            // Kiểm tra xem vị trí đó có nằm sau lưng camera không
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);
            if (viewportPos.z < 0)
            {
                element.style.display = DisplayStyle.None;
                continue;
            }
            element.style.display = DisplayStyle.Flex;

            // UI Toolkit API: Đổi tọa độ Thế Giới thành tọa độ UI Panel
            Vector2 uiPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _root.panel, worldPos, Camera.main
            );

            // Căn giữa thanh máu (offset nửa chiều rộng và nửa chiều cao)
            element.style.left = uiPos.x - 60f; 
            element.style.top = uiPos.y - 8f;
        }
    }
}
