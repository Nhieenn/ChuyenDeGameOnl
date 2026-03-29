using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using PlayFab.ClientModels;

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
    private Label _timerLabel; // Đồng hồ đếm lùi Match Time
    
    private bool _isMatchEnded = false;
    private VisualElement _scoreboardRoot;
    private VisualElement _globalList; // Danh sách Global phía bên phải UI

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

        // Đăng ký nhận dữ liệu Bảng vàng từ PlayFab
        PlayFabManager.OnLeaderboardUpdated += OnGlobalLeaderboardReceived;
        
        // TÌM VÀ XOÁ CÁC ELEMENT CŨ CỦA HUD NẾU CÓ ĐỂ TRÁNH DUPLICATE
        var oldHud = _root.Q<VisualElement>("hud-container");
        if (oldHud != null) _root.Remove(oldHud);

        var hudContainer = new VisualElement { name = "hud-container" };
        hudContainer.style.flexGrow = 1;
        hudContainer.pickingMode = PickingMode.Ignore;
        _root.Add(hudContainer);

        BuildCrosshair(hudContainer);
        BuildKDA_Board(hudContainer);
    }

    private void OnDisable()
    {
        PlayFabManager.OnLeaderboardUpdated -= OnGlobalLeaderboardReceived;
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
        kdaContainer.style.flexDirection = FlexDirection.Column; // Để Timer bên dưới KDA
        kdaContainer.style.alignItems = Align.Center;
        kdaContainer.style.borderTopLeftRadius = 8;
        kdaContainer.style.borderTopRightRadius = 8;
        kdaContainer.style.borderBottomLeftRadius = 8;
        kdaContainer.style.borderBottomRightRadius = 8;

        // Label chữ
        _kdaLabel = new Label("K/D: 0 / 0");
        _kdaLabel.style.color = Color.white;
        _kdaLabel.style.fontSize = 24;
        _kdaLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        // Label Đồng hồ trận đấu
        _timerLabel = new Label("10:00");
        _timerLabel.style.color = new Color(1f, 0.8f, 0f); // Màu Vàng
        _timerLabel.style.fontSize = 20;
        _timerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        kdaContainer.Add(_kdaLabel);
        kdaContainer.Add(_timerLabel);
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

        // Cập nhật Đồng Hồ Trận Đấu (Đồng bộ tuyệt đối bằng Server Simulation Time)
        // Set Match Time là 3 phút (180 giây) theo yêu cầu
        float maxMatchTime = 180f; 
        float elapsedTime = 0f;
        
        // Cần bọc Try-Catch vì ở vài Frame đầu tiên tham gia phòng, Server chưa kịp phản hồi cấu hình thời gian
        try
        {
            elapsedTime = (float)runner.SimulationTime;
        }
        catch (System.InvalidOperationException)
        {
            // Bỏ qua Update frame này cho đến khi nhận được dữ liệu thời gian mạng
            return; 
        }

        float remainingTime = Mathf.Max(0f, maxMatchTime - elapsedTime);

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        
        _timerLabel.text = $"{minutes:00}:{seconds:00}";
        
        if (remainingTime <= 0)
        {
            _timerLabel.style.color = Color.red; // Đổi màu đỏ khi hết giờ báo hiệu Endgame
            _timerLabel.text = "00:00 (HẾT GIỜ)";

            if (!_isMatchEnded)
            {
                _isMatchEnded = true;
                BuildEndGameScoreboard();
                
                // Yêu cầu PlayFab tải bảng xếp hạng mới nhất về để hiện lên UI
                if (PlayFabManager.Instance != null) {
                    PlayFabManager.Instance.GetLeaderboard();
                }
            }
        }
    }

    private void BuildEndGameScoreboard()
    {
        // 1. Tạo lớp nền mờ toàn màn hình (Glassmorphism hiệu ứng cho sang)
        _scoreboardRoot = new VisualElement();
        _scoreboardRoot.style.position = Position.Absolute;
        _scoreboardRoot.style.width = new Length(100, LengthUnit.Percent);
        _scoreboardRoot.style.height = new Length(100, LengthUnit.Percent);
        _scoreboardRoot.style.backgroundColor = new Color(0, 0, 0, 0.85f);
        _scoreboardRoot.style.alignItems = Align.Center;
        _scoreboardRoot.style.justifyContent = Justify.Center;
        _root.Add(_scoreboardRoot);

        // 2. Panel trung tâm
        var mainPanel = new VisualElement();
        mainPanel.style.width = new Length(80, LengthUnit.Percent);
        mainPanel.style.height = new Length(70, LengthUnit.Percent);
        mainPanel.style.backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
        mainPanel.style.flexDirection = FlexDirection.Row;
        mainPanel.style.paddingLeft = 20;
        mainPanel.style.paddingRight = 20;
        mainPanel.style.paddingTop = 20;
        mainPanel.style.paddingBottom = 20;
        mainPanel.style.borderTopLeftRadius = 15;
        mainPanel.style.borderTopRightRadius = 15;
        mainPanel.style.borderBottomLeftRadius = 15;
        mainPanel.style.borderBottomRightRadius = 15;
        _scoreboardRoot.Add(mainPanel);

        // --- CỘT TRÁI: KẾT QUẢ TRẬN ĐẤU ---
        var localColumn = new VisualElement { style = { flexGrow = 1, marginRight = 10 } };
        var localTitle = new Label("KẾT QUẢ TRẬN ĐẤU");
        localTitle.style.color = Color.cyan;
        localTitle.style.fontSize = 28;
        localTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        localTitle.style.marginBottom = 15;
        localColumn.Add(localTitle);

        var playerList = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .OrderByDescending(p => p.Kills)
            .ToList();

        foreach (var p in playerList)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 5, paddingLeft = 10, paddingRight = 10, backgroundColor = new Color(1,1,1,0.05f) } };
            var nameLabel = new Label($"Player [Id:{p.Object.InputAuthority.RawEncoded}]");
            var scoreLabel = new Label($"Kills: {p.Kills} | Deaths: {p.Deaths}");
            
            nameLabel.style.color = p.Object.HasInputAuthority ? Color.yellow : Color.white;
            scoreLabel.style.color = Color.white;
            
            row.Add(nameLabel);
            row.Add(scoreLabel);
            localColumn.Add(row);
        }
        mainPanel.Add(localColumn);

        // --- CỘT PHẢI: BẢNG VÀNG THẾ GIỚI ---
        var globalColumn = new VisualElement { style = { flexGrow = 1, marginLeft = 10 } };
        var globalTitle = new Label("BẢNG VÀNG THẾ GIỚI (TOP 10)");
        globalTitle.style.color = new Color(1f, 0.84f, 0f); // Gold
        globalTitle.style.fontSize = 28;
        globalTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        globalTitle.style.marginBottom = 15;
        globalColumn.Add(globalTitle);

        _globalList = new VisualElement();
        _globalList.Add(new Label("Đang tải từ máy chủ..."));
        globalColumn.Add(_globalList);
        
        mainPanel.Add(globalColumn);

        // Nút Thoát Game ở dưới cùng
        var leaveButton = new Button(() => { Application.Quit(); });
        leaveButton.text = "THOÁT GAME (EXIT)";
        leaveButton.style.marginTop = 20;
        leaveButton.style.width = 250;
        leaveButton.style.height = 50;
        leaveButton.style.backgroundColor = Color.red;
        leaveButton.style.color = Color.white;
        leaveButton.style.fontSize = 20;
        _scoreboardRoot.Add(leaveButton);

        // Hiện chuột để người chơi bấm nút (Định danh rõ UnityEngine.Cursor)
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    private void OnGlobalLeaderboardReceived(List<PlayerLeaderboardEntry> leaderboard)
    {
        if (_globalList == null) return;
        _globalList.Clear();

        if (leaderboard == null || leaderboard.Count == 0)
        {
            _globalList.Add(new Label("Chưa có cao thủ nào ghi danh!"));
            return;
        }

        foreach (var item in leaderboard)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 3 } };
            var name = new Label($"#{item.Position + 1} {item.DisplayName}");
            var stat = new Label($"{item.StatValue} Kills");
            
            name.style.color = Color.white;
            stat.style.color = new Color(1f, 0.84f, 0f);
            
            row.Add(name);
            row.Add(stat);
            _globalList.Add(row);
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
