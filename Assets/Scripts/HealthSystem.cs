using Fusion;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quản lý máu (HP), hiển thị thanh máu lên màn hình (HUD hoặc Billboard UI)
/// và xử lý cái chết / hồi sinh.
/// </summary>
public class HealthSystem : NetworkBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float CurrentHealth { get; set; }

    // Dùng biến local để so sánh lượng máu thay đổi nhằm kích hoạt tính năng "Đứng Hình" (Hit React)
    private float _previousHealth;



    public override void Spawned()
    {
        Debug.Log($"[HealthSystem] Player {Object.Id} Spawned. Tìm kiếm FloatingUIManager...");

        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }

        _previousHealth = maxHealth;

        if (FloatingUIManager.Instance != null)
        {
            Debug.Log("[HealthSystem] OK - Đã thấy FloatingUIManager! Bắt đầu đăng ký.");
            FloatingUIManager.Instance.RegisterPlayer(this);
            FloatingUIManager.Instance.UpdateHealth(this, maxHealth, maxHealth);
        }
        else
        {
            Debug.LogError("[HealthSystem] LỖI: FloatingUIManager.Instance đang NULL! Bạn chưa gắn nó vào HUD hoặc chưa lưu Scene.");
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Xóa thanh máu khi nhân vật biến mất/chết vĩnh viễn
        if (FloatingUIManager.Instance != null)
        {
            FloatingUIManager.Instance.UnregisterPlayer(this);
        }
    }

    /// <summary>
    /// Hàm xử lý nhận sát thương. 
    /// CHỈ được gọi bởi người đánh (State Authority của vũ khí/đạn).
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPosition)
    {
        // Fix: Tránh trường hợp client tự trừ máu của thằng khác. 
        // Trong Shared Mode, state authority cùa HealthSystem có thể thuộc về người bị đánh, 
        // nhưng người đánh cũng có thể Require quyền sửa biến này nếu dùng RPC.
        // Để đơn giản và lag-free: Dùng RPC yêu cầu chủ thể tự trừ máu của mình.
        Rpc_TakeDamage(damage, hitPosition);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_TakeDamage(float damage, Vector3 hitPosition)
    {
        if (CurrentHealth <= 0) return; // Đã chết rồi

        CurrentHealth -= damage;

        // Nếu máu tụt xuống 0 thì xử lý chết
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void OnHealthChanged()
    {
        Debug.Log($"[HealthSystem] Player {Object.Id} máu còn: {CurrentHealth}");

        // 1. Cập nhật UI Thanh Máu Overlay (UI Toolkit)
        if (FloatingUIManager.Instance != null)
        {
            FloatingUIManager.Instance.UpdateHealth(this, CurrentHealth, maxHealth);
        }

        // Tính lượng máu MẤT ĐI
        float damageTaken = _previousHealth - CurrentHealth;
        _previousHealth = CurrentHealth;

        // Nếu thực sự BỊ TRỪ MÁU
        if (damageTaken > 0)
        {
            // 2. Chớp đỏ người (Damage Flash)
            var flasher = GetComponent<DamageFlash>();
            if (flasher != null)
                flasher.Flash();

            Vector3 spawnPos = transform.position + Vector3.up * 1f;

            // 3. Gọi VFX và Text từ Pool
            if (HitEffectManager.Instance != null)
            {
                // Xịt tia nham thạch lửa
                HitEffectManager.Instance.SpawnHitEffect(spawnPos);
                
                // Nảy con số sát thương (-10) lơ lửng trên đầu
                HitEffectManager.Instance.SpawnDamageText(spawnPos + Vector3.up * 0.5f, damageTaken);
            }
        }
    }

    private void Die()
    {
        Debug.Log($"[HealthSystem] Player {Object.Id} BỊ HẠ GỤC!");

        // Lấy Random vị trí hồi sinh
        Vector3 randomRespawnPos = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        
        // Di chuyển xác về vị trí mới
        GetComponent<CharacterController>().enabled = false;
        transform.position = randomRespawnPos;
        GetComponent<CharacterController>().enabled = true;

        // Bơm lại máu
        CurrentHealth = maxHealth;
    }
}
