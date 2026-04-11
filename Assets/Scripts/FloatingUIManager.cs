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
    private Camera _cachedCam;

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
        // Tự động gán lại root nếu lỡ bị thất lạc lúc đầu (thường gặp trong bản build)
        if (_root == null && _uiDoc != null) _root = _uiDoc.rootVisualElement;
        
        if (_root == null) return;

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
            float percent = 0f;

            // [MỚI] ƯU TIÊN HIỂN THỊ GIÁP NỘ (MÀU TÍM, MỐC 200, DÀI HƠN)
            if (health.CurrentRageHealth > 0)
            {
                element.style.width = 220; // Dãn dài thanh máu cho hoành tráng
                percent = Mathf.Clamp01(health.CurrentRageHealth / 200f) * 100f;
                fill.style.backgroundColor = new Color(0.6f, 0.2f, 0.9f); // Màu Tím Vibrant
            }
            else
            {
                element.style.width = 120; // Co lại kích thước thường
                percent = Mathf.Clamp01(currentHp / maxHp) * 100f;
                
                // Đổi màu tùy theo mức máu gốc
                if (percent > 60) fill.style.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                else if (percent > 30) fill.style.backgroundColor = new Color(0.9f, 0.7f, 0.1f);
                else fill.style.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
            }

            fill.style.width = Length.Percent(percent);
            
            // Xử lý hiệu ứng chớp mép màn hình nếu đây là nhân vật của người chơi này (Local Player)
            if (health.HasInputAuthority)
            {
                float normalPercent = Mathf.Clamp01(currentHp / maxHp) * 100f;
                if (normalPercent <= 30f && currentHp > 0)
                {
                    _targetBloodAlpha = 1f - (normalPercent / 30f);
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
        // TÌM CAMERA THÔNG MINH HƠN:
        if (_cachedCam == null || !_cachedCam.gameObject.activeInHierarchy)
        {
            _cachedCam = Camera.main;
            if (_cachedCam == null) _cachedCam = FindFirstObjectByType<Camera>();
        }

        if (_cachedCam == null) return;

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

            // XỬ LÝ LỖI EXE: Nếu là nhân vật chính, ta đẩy nhẹ vị trí ra xa Camera một chút 
            // để tránh bị "nhảy" vào vùng cấm (Near Clip Plane) gây ra tọa độ (0,0)
            bool isLocal = (health.Object != null && health.Object.HasInputAuthority);
            if (isLocal) worldPos += _cachedCam.transform.forward * 0.5f;

            Vector3 viewportPos = _cachedCam.WorldToViewportPoint(worldPos);
            
            if (viewportPos.z < 0 && !isLocal) 
            {
                element.style.display = DisplayStyle.None;
                continue;
            }
            element.style.display = DisplayStyle.Flex;

            Vector2 uiPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _root.panel, worldPos, _cachedCam
            );

            // Căn giữa thanh máu dựa trên chiều rộng thực tế (resolvedStyle.width) 
            // Điều này giúp thanh máu luôn cân bằng kể cả khi dài 120px hay 220px
            float barWidth = element.resolvedStyle.width;
            if (barWidth <= 0) barWidth = (health.CurrentRageHealth > 0) ? 220f : 120f; // Fallback nếu UI chưa render xong
            
            element.style.left = uiPos.x - (barWidth / 2f); 
            element.style.top = uiPos.y - 8f;
        }

        foreach (var kvp in _staminaBars)
        {
            var stamina = kvp.Key;
            var element = kvp.Value;

            // Nằm thấp hơn thanh máu một chút
            Vector3 worldPos = stamina.transform.position + Vector3.up * 2.1f;
            
            // XỬ LÝ LỖI EXE: Đẩy nhẹ ra xa Camera nếu là Local Player
            bool isLocal = (stamina.Object != null && stamina.Object.HasInputAuthority);
            if (isLocal) worldPos += _cachedCam.transform.forward * 0.4f;

            Vector3 viewportPos = _cachedCam.WorldToViewportPoint(worldPos);
            if (viewportPos.z < 0 && !isLocal)
            {
                element.style.display = DisplayStyle.None;
                continue;
            }
            element.style.display = DisplayStyle.Flex;

            Vector2 uiPos = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, worldPos, _cachedCam);

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

            // XỬ LÝ LỖI EXE: Đẩy nhẹ ra xa Camera nếu là Local Player
            bool isLocal = (rage.Object != null && rage.Object.HasInputAuthority);
            if (isLocal) worldPos += _cachedCam.transform.forward * 0.3f;

            Vector3 viewportPos = _cachedCam.WorldToViewportPoint(worldPos);
            if (viewportPos.z < 0 && !isLocal) { element.style.display = DisplayStyle.None; continue; }
            element.style.display = DisplayStyle.Flex;

            // --- CẬP NHẬT TỌA ĐỘ VÀ HIỆU ỨNG NHẤP NHÁY ---
            Vector2 uiPos = RuntimePanelUtils.CameraTransformWorldToPanel(_root.panel, worldPos, _cachedCam);
            element.style.left = uiPos.x; 
            element.style.top = uiPos.y - 32f; 

            // [MỚI] Hiệu ứng Nhấp nháy khi nộ sắp hết (Dưới 20%)
            var fill = element.Q<VisualElement>("rg_fill");
            if (fill != null)
            {
                float percent = (rage.maxRage > 0) ? (rage.CurrentRage / rage.maxRage * 100f) : 0f;
                if (rage.IsRaging && percent < 20f)
                {
                    float flash = 0.4f + 0.6f * Mathf.Sin(Time.time * 20f);
                    fill.style.opacity = flash;
                }
                else
                {
                    fill.style.opacity = 1f;
                }
            }
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 18;
        style.normal.textColor = Color.green;

        float y = 10;

        foreach (var kvp in _rageBars)
        {
            var rage = kvp.Key;
            if (rage == null) continue;
            bool isLocal = (rage.Object != null && rage.Object.HasInputAuthority);
            Vector3 vPos = _cachedCam != null ? _cachedCam.WorldToViewportPoint(rage.transform.position) : Vector3.zero;
            y += 25;
        }
    }
}
