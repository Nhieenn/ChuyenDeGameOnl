using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;

/// <summary>
/// Quản lý Hệ thống Cơ sở dữ liệu Đám mây PlayFab (Đăng nhập, Lưu tên, Xếp hạng Kills).
/// Script này tự động chạy ẩn, không cần kéo thả vào Scene.
/// </summary>
public class PlayFabManager : MonoBehaviour
{
    public static PlayFabManager Instance { get; private set; }

    // Sự kiện Gửi danh sách Bảng Xếp Hạng cho bên UI xử lý (HUDController)
    public static event Action<List<PlayerLeaderboardEntry>> OnLeaderboardUpdated;

    // Dùng Attribute này để tự động chích script vào Game ngay khi vừa hiện Logo Unity
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInitialize()
    {
        if (Instance == null)
        {
            var go = new GameObject("PlayFabManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PlayFabManager>();
            
            // Xúc tiến đăng nhập tự động ngay lập tức
            Instance.Login();
        }
    }

    public void Login()
    {
        Debug.Log("[PlayFab] Đang tiến hành kết nối CSDL Đám mây...");
        // Khóa chết Title ID bằng ID THẬT CỦA BẠN (13CAE3)
        PlayFabSettings.staticSettings.TitleId = "13CAE3";

        // Tránh trùng lặp ID khi test 2 cửa sổ trên cùng 1 máy tính (Editor đụng Exe)
        string deviceId = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
        deviceId += "_EDITOR_PC";
#endif

        var request = new LoginWithCustomIDRequest
        {
            // Dùng ngay Mã độc nhất phần cứng máy tính để làm Tài khoản ẩn danh
            CustomId = deviceId,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };

        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnLoginFailure);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log($"[PlayFab] ĐĂNG NHẬP THÀNH CÔNG! Chào mừng Mã tài khoản: {result.PlayFabId}");
        
        // Cấp ngẫu nhiên 1 cái tên xịn xò nếu acc mới tạo
        if (result.NewlyCreated || result.InfoResultPayload.PlayerProfile == null || string.IsNullOrEmpty(result.InfoResultPayload.PlayerProfile.DisplayName))
        {
            string randomName = "SieuSao_" + UnityEngine.Random.Range(1000, 9999);
            SubmitName(randomName);
        }
        else 
        {
            Debug.Log($"[PlayFab] Tên hiển thị người chơi: {result.InfoResultPayload.PlayerProfile.DisplayName}");
        }

        // Tải thử bảng xếp hạng ngay khi login thành công để báo cáo log ra màn hình
        GetLeaderboard();
    }

    private void SubmitName(string name)
    {
        var req = new UpdateUserTitleDisplayNameRequest { DisplayName = name };
        PlayFabClientAPI.UpdateUserTitleDisplayName(req, res => {
            Debug.Log($"[PlayFab] Đã tự động cập nhật Tên mới: {res.DisplayName}");
        }, err => Debug.LogError(err.GenerateErrorReport()));
    }

    private void OnLoginFailure(PlayFabError error)
    {
        Debug.LogWarning($"[PlayFab] LỖI ĐĂNG NHẬP. ErrorCode={error.Error} ({(int)error.Error}): {error.GenerateErrorReport()}");
    }

    /// <summary>
    /// Hàm nhận Báo Mạng, gọi để bắn thẳng Điểm Kills vào Server
    /// </summary>
    public void SendLeaderboard(int kills)
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            Debug.LogWarning("[PlayFab] Chưa kết nối tới Đám mây. Bỏ qua lệnh gửi điểm!");
            return;
        }

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    // Tên Cột Bảng Xếp Hạng. (Nếu trên Playfab chưa có Cột này, nó sẽ tự động tạo luôn!)
                    StatisticName = "Top_Killers",
                    Value = kills
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, 
            res => {
                Debug.Log($"[PlayFab] (+1 Mạng) Đã lưu mốc {kills} Kills lên Đám Mây thành công rực rỡ!");
                // Cập nhật lại list điểm trong Console sau mỗi lúc có người hi sinh
                GetLeaderboard();
            }, 
            err => Debug.LogError("[PlayFab] Lỗi Gửi Điểm: " + err.GenerateErrorReport()));
    }

    /// <summary>
    /// Vét 10 người đỉnh nhất từ Server xuống 
    /// </summary>
    public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Top_Killers",
            StartPosition = 0,
            MaxResultsCount = 10
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, 
            err => Debug.LogError("[PlayFab] Lỗi Tải Xếp hạng: " + err.GenerateErrorReport()));
    }

    private void OnLeaderboardGet(GetLeaderboardResult result)
    {
        Debug.Log("=========================================");
        Debug.Log("🏆 BẢNG VÀNG XẾP HẠNG TOP KILLERS 🏆");
        if (result.Leaderboard.Count == 0)
        {
            Debug.Log("Chưa có Huyết Kiếm nào được rút ra! Bảng còn trống.");
        }
        else
        {
            foreach (var item in result.Leaderboard)
            {
                Debug.Log($" Hạng {item.Position + 1}: {item.DisplayName} - {item.StatValue} Kills");
            }
        }
        
        // Gọi bộ phát Loa thông báo cho Giao diện UI biết là đã có Cập Nhật Bảng xếp hạng mới!
        OnLeaderboardUpdated?.Invoke(result.Leaderboard);

        Debug.Log("=========================================");
    }
}
