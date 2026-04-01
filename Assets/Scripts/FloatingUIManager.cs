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
    private Dictionary<StaminaSystem, VisualElement> _staminaBars = new Dictionary<StaminaSystem, VisualElement>();
    private Dictionary<RageSystem, VisualElement> _rageBars = new Dictionary<RageSystem, VisualElement>();

    // Hiệu ứng máu yếu (Vignette đỏ viền màn hình)
    private VisualElement _bloodyScreen;
    private float _targetBloodAlpha = 0f;
    private float _currentBloodAlpha = 0f;

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
        SetupBloodScreen();
    }

    private void SetupBloodScreen()
    {
        if (_root == null || _bloodyScreen != null) return;
        
        _bloodyScreen = new VisualElement();
        _bloodyScreen.style.position = Position.Absolute;
        _bloodyScreen.style.left = 0; _bloodyScreen.style.right = 0;
        _bloodyScreen.style.top = 0; _bloodyScreen.style.bottom = 0;
        _bloodyScreen.pickingMode = PickingMode.Ignore;
        
        // Viền đỏ dày 40px ở 4 góc màn hình
        _bloodyScreen.style.borderTopWidth = 40;
        _bloodyScreen.style.borderBottomWidth = 40;
        _bloodyScreen.style.borderLeftWidth = 40;
        _bloodyScreen.style.borderRightWidth = 40;
        _bloodyScreen.style.borderTopColor = new Color(1, 0, 0, 0.5f);
        _bloodyScreen.style.borderBottomColor = new Color(1, 0, 0, 0.5f);
        _bloodyScreen.style.borderLeftColor = new Color(1, 0, 0, 0.5f);
        _bloodyScreen.style.borderRightColor = new Color(1, 0, 0, 0.5f);
        
        _bloodyScreen.style.opacity = 0f; // Mặc định ẩn
        _root.Add(_bloodyScreen);
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

    public bool HasRegisteredHealth(HealthSystem health)
    {
        if (_healthBars.TryGetValue(health, out var element))
        {
            // Kiểm tra vô cùng quan trọng: Nếu thẻ UI tồn tại nhưng bị văng khỏi gốc giao diện (Orphaned Zombie)
            // (Thường xảy ra khi UI Toolkit nháy Scene ở frame đầu tiên) -> Buộc phải đẻ lại!
            if (element.panel == null)
            {
                _healthBars.Remove(health);
                return false;
            }
            return true;
        }
        return false;
    }

    public bool HasRegisteredStamina(StaminaSystem stamina)
    {
        if (_staminaBars.TryGetValue(stamina, out var element))
        {
            if (element.panel == null)
            {
                _staminaBars.Remove(stamina);
                return false;
            }
            return true;
        }
        return false;
    }

    public bool HasRegisteredRage(RageSystem rage)
    {
        if (_rageBars.TryGetValue(rage, out var element))
        {
            if (element.panel == null)
            {
                _rageBars.Remove(rage);
                return false;
            }
            return true;
        }
        return false;
    }

    public void UnregisterPlayer(HealthSystem health)
    {
        if (_healthBars.TryGetValue(health, out var element))
        {
            if (_root != null && _root.Contains(element))
                _root.Remove(element);
            _healthBars.Remove(health);
        }
        
        var stamina = health.GetComponent<StaminaSystem>();
        if (stamina != null && _staminaBars.TryGetValue(stamina, out var stElement))
        {
            if (_root != null && _root.Contains(stElement))
                _root.Remove(stElement);
            _staminaBars.Remove(stamina);
        }

        var rage = health.GetComponent<RageSystem>();
        if (rage != null && _rageBars.TryGetValue(rage, out var rgElement))
        {
            if (_root != null && _root.Contains(rgElement))
                _root.Remove(rgElement);
            _rageBars.Remove(rage);
        }
    }

    /// <summary>
    /// StaminaSystem sẽ gọi hàm này khi cập nhật để sinh UI hoặc cập nhật thanh thể lực.
    /// </summary>
    public void UpdateStamina(StaminaSystem stamina, float currentSp, float maxSp)
    {
        if (!_staminaBars.TryGetValue(stamina, out var stElement))
        {
            // Lần đầu gọi sẽ tự tạo Container Thể Lực
            stElement = new VisualElement();
            stElement.pickingMode = PickingMode.Ignore;
            stElement.style.position = Position.Absolute;
            stElement.style.width = 100; // Nhỏ hơn thanh máu xíu
            stElement.style.height = 8;
            stElement.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            
            var stFill = new VisualElement();
            stFill.name = "st_fill";
            stFill.style.height = Length.Percent(100);
            stFill.style.width = Length.Percent(100);
            stFill.style.backgroundColor = new Color(1f, 0.8f, 0f); // Màu vàng
            
            stElement.Add(stFill);
            _root.Add(stElement);
            _staminaBars.Add(stamina, stElement);
        }

        var fill = stElement.Q<VisualElement>("st_fill");
        float percent = Mathf.Clamp01(currentSp / maxSp) * 100f;
        fill.style.width = Length.Percent(percent);
        
        // Cạn kiệt thì đổi màu xám
        fill.style.backgroundColor = percent < 5f ? Color.gray : new Color(1f, 0.8f, 0f);
    }

    /// <summary>
    /// RageSystem sẽ gọi hàm này để cập nhật thanh nộ nổi.
    /// </summary>
    public void UpdateRage(RageSystem rage, float currentRg, float maxRg)
    {
        if (!_rageBars.TryGetValue(rage, out var rgElement))
        {
            Debug.Log($"[FloatingUIManager] Đang tạo thanh nộ dọc cho: {rage.gameObject.name}");
            
            rgElement = new VisualElement();
            rgElement.name = "RageBarRoot";
            rgElement.pickingMode = PickingMode.Ignore;
            rgElement.style.position = Position.Absolute;
            
            // TẤT CẢ NGƯỜI CHƠI ĐỀU DÙNG THANH DỌC (CHIẾN THUẬT)
            rgElement.style.width = 15;  
            rgElement.style.height = 65; 
            rgElement.style.backgroundColor = new Color(0, 0, 0, 0.8f);
            rgElement.style.borderBottomLeftRadius = 4;
            rgElement.style.borderBottomRightRadius = 4;
            rgElement.style.borderTopLeftRadius = 4;
            rgElement.style.borderTopRightRadius = 4;
            rgElement.style.flexDirection = FlexDirection.ColumnReverse;
            
            var rgFill = new VisualElement();
            rgFill.name = "rg_fill";
            rgFill.style.width = Length.Percent(100);
            rgFill.style.height = Length.Percent(0);
            rgFill.style.backgroundColor = new Color(1f, 0.5f, 0f); // Màu cam
            
            rgElement.Add(rgFill);
            _root.Add(rgElement);
            _rageBars.Add(rage, rgElement);
        }

        var fill = rgElement.Q<VisualElement>("rg_fill");
        float percent = Mathf.Clamp(currentRg / maxRg * 100f, 0, 100);
        fill.style.height = Length.Percent(percent);

        // Đỏ khi nộ
        fill.style.backgroundColor = rage.IsRaging ? Color.red : new Color(1f, 0.5f, 0f);
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
            
            // Xử lý hiệu ứng chớp mép màn hình nếu đây là nhân vật của người chơi này (Local Player)
            if (health.HasInputAuthority)
            {
                if (percent <= 30f && currentHp > 0)
                {
                    // Càng thấp máu thì mức TargetAlpha càng đậm (tối đa 1.0)
                    _targetBloodAlpha = 1f - (percent / 30f);
                }
                else
                {
                    _targetBloodAlpha = 0f;
                }
            }
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
            return;
        }

        // --- CẬP NHẬT HIỆU ỨNG NHỊP TIM MÁU YẾU ---
        if (_bloodyScreen != null)
        {
            // Sin curve tạo nhịp đập phập phồng (pulse) 
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 6f);
            _currentBloodAlpha = Mathf.Lerp(_currentBloodAlpha, _targetBloodAlpha * pulse, Time.deltaTime * 5f);
            _bloodyScreen.style.opacity = _currentBloodAlpha;
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

        // --- CẬP NHẬT TỌA ĐỘ THANH THỂ LỰC ---
        foreach (var kvp in _staminaBars)
        {
            var stamina = kvp.Key;
            var element = kvp.Value;

            // Nằm thấp hơn thanh máu một chút
            Vector3 worldPos = stamina.transform.position + Vector3.up * 2.1f;
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);
            if (viewportPos.z < 0)
            {
                element.style.display = DisplayStyle.None;
                continue;
            }
            element.style.display = DisplayStyle.Flex;

            Vector2 uiPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _root.panel, worldPos, Camera.main
            );

            // Rộng 100px nên căn giữa là -50f
            element.style.left = uiPos.x - 50f; 
            element.style.top = uiPos.y; 
        }

        // --- CẬP NHẬT TỌA ĐỘ THANH NỘ ---
        foreach (var kvp in _rageBars)
        {
            var rage = kvp.Key;
            var element = kvp.Value;

            if (rage == null || element == null) continue;

            // Đặt thanh nộ ở vị trí ngang hông (up * 1.0f) và lệch phải (right * 0.7f)
            Vector3 worldPos = rage.transform.position + (Vector3.up * 1.0f) + (rage.transform.right * 0.7f);

            Vector3 viewportPos = Camera.main.WorldToViewportPoint(worldPos);
            if (viewportPos.z < 0) { element.style.display = DisplayStyle.None; continue; }
            element.style.display = DisplayStyle.Flex;

            Vector2 uiPos = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, worldPos, Camera.main);
            
            element.style.left = uiPos.x; 
            element.style.top = uiPos.y - 32f; // Căn giữa thanh dọc (65px / 2)
        }
    }

}
