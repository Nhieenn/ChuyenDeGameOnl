using Fusion;
using UnityEngine;

/// <summary>
/// Xử lý vật phẩm có thể thu thập (Ngọc Máu/Nộ).
/// Gắn lên prefab Ngọc (Cần có NetworkObject và Collider isTrigger).
/// </summary>
public class CollectibleOrb : NetworkBehaviour
{
    public enum OrbType { Health, Rage }
    [Header("Settings")]
    public OrbType type;
    public float amount = 25f; // Giá trị hồi phục (25 máu hoặc 25 nộ)

    [Header("Visual Settings")]
    [SerializeField] private MeshRenderer _renderer;
    
    [Networked] private TickTimer _despawnTimer { get; set; }

    public override void Spawned()
    {
        // Cập nhật màu sắc dựa trên loại (Nếu làm chung 1 prefab)
        UpdateVisuals();
        
        // [MỚI] Tự động biến mất sau 30 giây để tránh rác (Backup)
        if (Object.HasStateAuthority)
        {
            _despawnTimer = TickTimer.CreateFromSeconds(Runner, 30f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && _despawnTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
    }

    private void UpdateVisuals()
    {
        if (_renderer == null) _renderer = GetComponentInChildren<MeshRenderer>();
        if (_renderer != null)
        {
            _renderer.material.color = (type == OrbType.Health) ? Color.green : new Color(1f, 0.5f, 0f); // Green or Orange
            _renderer.material.EnableKeyword("_EMISSION");
            _renderer.material.SetColor("_EmissionColor", _renderer.material.color * 2f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Chỉ State Authority mới xử lý logic nhặt để tránh duplicate qua mạng
        if (!Object.HasStateAuthority) return;

        // Kiểm tra xem có phải Player không
        var health = other.GetComponent<HealthSystem>();
        var rage = other.GetComponent<RageSystem>();

        if (health != null || rage != null)
        {
            ApplyEffect(health, rage);
            
            // Thông báo cho Spawner là Ngọc đã bị nhặt (Nếu cần)
            Debug.Log($"[CollectibleOrb] {type} Orb collected by {other.name}");
            
            // Trả về Object Pool
            Runner.Despawn(Object);
        }
    }

    private void ApplyEffect(HealthSystem health, RageSystem rage)
    {
        if (type == OrbType.Health && health != null)
        {
            health.CurrentHealth = Mathf.Min(health.maxHealth, health.CurrentHealth + amount);
        }
        else if (type == OrbType.Rage && rage != null)
        {
            // Nếu có RageSystem trên target
            if (rage != null) rage.AddRage(amount);
            else 
            {
                // Thử tìm trên cùng object với health
                var r = health?.GetComponent<RageSystem>();
                if (r != null) r.AddRage(amount);
            }
        }
    }
}
