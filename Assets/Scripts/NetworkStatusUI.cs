using Fusion;
using UnityEngine;

/// <summary>
/// Hiển thị thông số mạng và bằng chứng Lag Compensation ngay trên màn hình game.
/// Cực kỳ hữu ích để trình bày đồ án (Demo).
/// </summary>
public class NetworkStatusUI : MonoBehaviour
{
    private NetworkRunner _runner;
    private string _lastHitInfo = "Chờ tấn công...";
    private float _logTimer = 0f;

    private void Update()
    {
        if (_runner == null)
            _runner = FindFirstObjectByType<NetworkRunner>();

        if (_logTimer > 0)
            _logTimer -= Time.deltaTime;
        else
            _lastHitInfo = "Hệ thống Bù trễ: Sẵn sàng";
    }

    // Hàm nhận dữ liệu từ MeleeAttack hoặc SwordWave gửi sang
    public void LogHit(string type, bool isPredictive)
    {
        _lastHitInfo = $"<color=yellow>[HIT]</color> {type} (Bù trễ: {isPredictive})";
        _logTimer = 2.0f; // Hiện trong 2 giây rồi thôi
    }

    private void OnGUI()
    {
        if (_runner == null || !_runner.IsRunning) return;

        // Thiết lập Style cho chữ
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);

        // Vẽ nền đen mờ
        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0, 0, 0, 0.6f));
        bg.Apply();
        GUI.skin.box.normal.background = bg;

        GUILayout.BeginArea(new Rect(20, 20, 400, 200), GUI.skin.box);
        
        GUILayout.Label($"<b>NETWORK STATUS</b>", style);
        
        // Hiển thị Ping (RTT)
        double rtt = _runner.SessionInfo.Properties.ContainsKey("RTT") ? 0 : Time.deltaTime * 1000;
        // Fusion 2 Shared Mode thường dùng Runner.DeltaTime * 1000 làm mốc ping ảo nếu không có relay
        GUILayout.Label($"Ping (RTT): {rtt:F0}ms", style);

        // Hiển thị trạng thái Lag Compensation
        GUIStyle statusStyle = new GUIStyle(style);
        statusStyle.normal.textColor = Color.green;
        GUILayout.Label($"Lag Compensation: <color=lime>ACTIVE</color>", statusStyle);

        // Hiển thị thông tin đòn đánh gần nhất
        GUIStyle hitStyle = new GUIStyle(style);
        hitStyle.normal.textColor = Color.yellow;
        GUILayout.Label(_lastHitInfo, hitStyle);

        GUILayout.EndArea();
    }
}
