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

    [Networked] public int Kills { get; set; }
    [Networked] public int Deaths { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }

    // Quản lý thời gian nằm gục chờ hồi sinh
    [Networked] private TickTimer _respawnTimer { get; set; }

    // Dùng biến local để so sánh lượng máu thay đổi nhằm kích hoạt tính năng "Đứng Hình" (Hit React)
    private float _previousHealth;



    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
        }
        _previousHealth = maxHealth;
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
    public void TakeDamage(float damage, Vector3 attackerPos, PlayerRef attackerRef)
    {
        if (IsDead) return;
        Rpc_TakeDamage(damage, attackerPos, attackerRef);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_TakeDamage(float damage, Vector3 attackerPos, PlayerRef attackerRef)
    {
        if (CurrentHealth <= 0 || IsDead) return; // Đã chết rồi

        var movement = GetComponent<PlayerMovement>();
        var stamina = GetComponent<StaminaSystem>();

        // [MỚI] KIỂM TRA ĐỠ ĐÒN (BLOCKING)
        bool isHitOnShield = false;
        if (movement != null && movement.IsBlocking)
        {
            damage *= 0.5f; // Giảm 50% sát thương nhận vào nếu dựng khiên
            isHitOnShield = true;
            
            // Gõ vào khiên sẽ làm hao mòn nghiêm trọng Thể Lực của nạn nhân
            if (stamina != null)
            {
                stamina.ConsumeStamina(15f);
            }
        }

        // Xử lý nảy lùi lại (Knockback)
        if (movement != null)
        {
            Vector3 pushDir = (transform.position - attackerPos).normalized;
            pushDir.y = 0.5f; // Hơi nảy nhẹ lên trên một chút cho có cảm giác chấn động
            
            // Nếu có Khiên, lùi ít đi 1 chút để tạo cảm giác chấn thủ vững vàng
            float knockForce = isHitOnShield ? 6f : 12f;
            movement.ApplyKnockback(pushDir * knockForce); 
        }

        // Tính toán hướng bị đánh (để suy ra hoạt ảnh HitF, HitB, HitL, HitR)
        Vector3 hitDirection = (attackerPos - transform.position).normalized;
        hitDirection.y = 0;
        
        // Góc giữa hướng mặt của mình và hướng đòn đánh bay tới
        float angle = Vector3.SignedAngle(transform.forward, hitDirection, Vector3.up);
        
        // Quy đổi góc sang Float cho Blend Tree hoặc Animator
        // Quy ước: 0=Front, 1=Right, 2=Back, 3=Left (Tuỳ chỉnh theo cách bạn setup Animator)
        float hitX = 0f;
        if (angle > -45f && angle <= 45f) hitX = 0f; // Bị đấm trước mặt (Front) -> Mình lùi ra sau
        else if (angle > 45f && angle <= 135f) hitX = 1f; // Bị đấm bên Phải
        else if (angle < -45f && angle >= -135f) hitX = 3f; // Bị đấm bên Trái
        else hitX = 2f; // Bị đấm sau lưng (Back) -> Mình ngã nhào tới trước

        // Báo cho Animator giật người
        GetComponent<PlayerAnimator>()?.TriggerHit(hitX);

        // Kiểm tra Hết Thể Lực -> Bị Choáng! (Tính năng "Vỡ Khiên" ăn theo logic này cực kì hoàn hảo)
        if (stamina != null && stamina.IsExhausted && !stamina.IsStunned)
        {
            stamina.TriggerExhaustionStun();
        }

        CurrentHealth -= damage;

        // Nếu máu tụt xuống 0 thì xử lý chết
        if (CurrentHealth <= 0)
        {
            Die(attackerRef);
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

    public override void FixedUpdateNetwork()
    {
        // Hệ thống Hồi sinh tự động sau 3 giây (chỉ chạy trên StateAuthority)
        if (Object.HasStateAuthority && IsDead && _respawnTimer.Expired(Runner))
        {
            Respawn();
        }
    }

    public override void Render()
    {
        // Liên tục kiểm tra xem Floating UI MỚI NHẤT đã vẽ thanh máu của mình chưa
        // Cần thiết vì Scene Reload của Client sẽ tiêu hủy UI Document của Scene Cũ
        if (FloatingUIManager.Instance != null && !FloatingUIManager.Instance.HasRegisteredHealth(this))
        {
            FloatingUIManager.Instance.RegisterPlayer(this);
            FloatingUIManager.Instance.UpdateHealth(this, CurrentHealth, maxHealth);
        }
    }

    private void Die(PlayerRef killerRef)
    {
        if (IsDead) return;
        Debug.Log($"[HealthSystem] Player {Object.Id} BỊ HẠ GỤC!");
        
        IsDead = true;
        Deaths += 1; // Tăng số lần chết

        // Báo cho toàn bản đồ biết ai là người vung đấm chốt hạ
        // Đây là cách vượt qua bộ luật StateAuthority gắt gao của Fusion
        Rpc_BroadcastDeath(killerRef);

        // Báo Animator nằm xuống đất
        GetComponent<PlayerAnimator>()?.Rpc_TriggerKnockdown();

        // Xóa thanh máu tạm thời
        if (FloatingUIManager.Instance != null)
            FloatingUIManager.Instance.UpdateHealth(this, 0, maxHealth);

        // Hẹn giờ 3 giây sau dậy
        _respawnTimer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    // [MỚI - ĐẤU NỐI PLAYFAB]
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_BroadcastDeath(PlayerRef killer)
    {
        // Kiểm tra xem TRÊN BẢN THÂN CHIẾC MÁY HIỆN TẠI (LocalPlayer), mình có phải là thằng giết người không?
        if (killer != PlayerRef.None && Runner.LocalPlayer == killer)
        {
            // Mình chính là tên sát thủ! Giờ thì đi tìm biến Nhân Hình của mình (HealthSystem)
            var allPlayers = FindObjectsByType<HealthSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var hp in allPlayers)
            {
                if (hp.Object != null && hp.Object.InputAuthority == Runner.LocalPlayer)
                {
                    // Được quyền tự do thao tác sửa máu/kill vì mình LÀ CHỦ NHÂN thực sự trên máy tính này
                    hp.Kills += 1;
                    
                    // GỘP NGAY ĐIỂM SỐ LÊN MÁY CHỦ BẢNG XẾP HẠNG MICROSOFT ĐÁM MÂY!
                    if (PlayFabManager.Instance != null)
                    {
                        PlayFabManager.Instance.SendLeaderboard(hp.Kills);
                    }
                    else
                    {
                        Debug.LogWarning("[PlayFab] Không tìm thấy PlayFabManager để gửi điểm Kills.");
                    }
                    
                    break;
                }
            }
        }
    }

    private void Respawn()
    {
        IsDead = false;

        // Báo Animator đứng dậy
        GetComponent<PlayerAnimator>()?.Rpc_TriggerGetUp();

        // Lấy Random vị trí hồi sinh
        Vector3 randomRespawnPos = new Vector3(Random.Range(-5f, 5f), 1f, Random.Range(-5f, 5f));
        
        // Di chuyển xác về vị trí mới
        var controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            transform.position = randomRespawnPos;
            controller.enabled = true;
        }

        // Bơm lại máu
        CurrentHealth = maxHealth;
    }
}
