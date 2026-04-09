using Fusion;
using UnityEngine;

/// <summary>
/// Quản lý việc sinh Ngọc tại trung tâm bản đồ mỗi 35 giây.
/// Gắn lên một GameObject trong scene (Ví cả GameSession hoặc Manager).
/// </summary>
public class CenterOrbSpawner : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab Ngọc (Có CollectibleOrb component)")]
    public NetworkObject healthOrbPrefab;
    public NetworkObject rageOrbPrefab;
    
    [Tooltip("Thời gian chờ giữa 2 lần spawn (giây)")]
    public float spawnInterval = 35f;
    
    [Tooltip("Độ cao bắt đầu rơi (mét)")]
    public float spawnHeight = 15f;

    [Networked] private TickTimer _spawnTimer { get; set; }
    [Networked] private NetworkObject _activeOrb { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // Bắt đầu đếm ngược ngay khi trận đấu bắt đầu
            _spawnTimer = TickTimer.CreateFromSeconds(Runner, 5f); // 5s đầu cho mượt
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Chỉ State Authority (Chủ phòng / Host) mới được quyền sinh vật phẩm
        if (!Object.HasStateAuthority) return;

        // Nếu đã có Ngọc trên sân và nó vẫn còn sống (IsValid) thì KHÔNG đếm tiếp
        if (_activeOrb != null && _activeOrb.IsValid)
        {
            return;
        }

        // Nếu hết thời gian chờ -> Sinh Ngọc mới
        if (_spawnTimer.Expired(Runner))
        {
            SpawnNewOrb();
            
            // Đặt lại bộ đếm 35 giây
            _spawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    private void SpawnNewOrb()
    {
        // Chọn ngẫu nhiên loại Ngọc (50/50)
        NetworkObject prefab = (Random.value > 0.5f) ? healthOrbPrefab : rageOrbPrefab;
        
        if (prefab == null) 
        {
            Debug.LogWarning("[CenterOrbSpawner] LỖI: Chưa gán Prefab Ngọc trong Inspector!");
            return;
        }

        // Vị trí rơi: (0, 15, 0) để nó rơi xuống (0, 0, 0)
        Vector3 spawnPos = new Vector3(0, spawnHeight, 0);
        
        Debug.Log($"[CenterOrbSpawner] Đang thả một viên Ngọc {prefab.name} từ độ cao {spawnHeight}m xuống trung tâm!");
        
        // Spawn thông qua Runner (Sẽ được ObjectPoolManager tự động thu hồi/tạo mới)
        _activeOrb = Runner.Spawn(prefab, spawnPos, Quaternion.identity);
    }
}
