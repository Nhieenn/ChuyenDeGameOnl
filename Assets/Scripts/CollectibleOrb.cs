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
    public GameObject pickupVFX; // GameObject hoặc Particle sẽ sinh ra khi nhặt
    public AudioClip pickupSFX;  // Âm thanh khi nhặt
    
    [Networked] private TickTimer _despawnTimer { get; set; }
    private Rigidbody _rb;

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody>();
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


    private void OnCollisionEnter(Collision collision)
    {
        // Nếu chạm vào đất (Layer Ground) -> Dừng hẳn để không lăn lung tung
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;

                // Đồng bộ hóa vị trí bay bổng mới sau khi chạm đất
                var visuals = GetComponent<OrbVisuals>();
                if (visuals != null) visuals.ResetStartPos();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Kiểm tra xem thứ chạm vào có phải Player không
        var health = other.GetComponent<HealthSystem>();
        var rage = other.GetComponent<RageSystem>();

        if (health == null && rage == null) return;

        // 2. [QUAN TRỌNG] Chỉ xử lý nếu người chạm vào là CHÍNH BẠN (Local Player)
        // Điều này đảm bảo mỗi người tự nhặt trên máy mình và gửi lệnh xóa lên server
        bool isLocalPlayer = false;
        if (health != null && health.Object != null) isLocalPlayer = health.Object.HasInputAuthority;
        else if (rage != null && rage.Object != null) isLocalPlayer = rage.Object.HasInputAuthority;

        if (!isLocalPlayer) return;

        // 3. Thực hiện hồi chỉ số (Client-side Prediction để mượt hơn)
        ApplyEffect(health, rage);
        PlayPickupEffects();

        Debug.Log($"[CollectibleOrb] {type} Orb collected locally by {other.name}, requesting despawn.");

        // 4. Nếu máy mình có quyền (State Authority) thì xóa luôn, nếu không thì Request quyền rồi xóa
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
        else
        {
            // Trong Shared Mode, chúng ta cần State Authority để Despawn
            // Ta yêu cầu quyền và thực hiện xóa ngay khi có quyền
            Object.RequestStateAuthority();
            Runner.Despawn(Object); 
        }
    }

    private void PlayPickupEffects()
    {
        // Sinh VFX (Nếu có)
        if (pickupVFX != null)
        {
            Instantiate(pickupVFX, transform.position, Quaternion.identity);
        }

        // Phát SFX (Nếu có)
        if (pickupSFX != null)
        {
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position);
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
