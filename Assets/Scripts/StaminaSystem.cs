using Fusion;
using UnityEngine;

/// <summary>
/// Quản lý hệ thống Thể lực (Stamina) cho việc Chạy nhanh (Sprint), Nhảy, Tấn công và Lướt/Né đòn (Dash/Roll).
/// Xử lý cơ chế Rơi vào trạng thái Kiệt sức (Out of Stamina) dẫn đến Choáng (Stun).
/// </summary>
public class StaminaSystem : NetworkBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 15f; // Tốc độ hồi phục mỗi giây

    [Networked, OnChangedRender(nameof(OnStaminaChanged))]
    public float CurrentStamina { get; set; }

    [Networked] public NetworkBool IsExhausted { get; set; } // Hết 100% thể lực
    [Networked] public NetworkBool IsStunned { get; set; }   // Bị đánh ngất do hết thể lực
    [Networked] private TickTimer _stunTimer { get; set; }   // Thời gian bị choáng
    [Networked] private TickTimer _regenDelayTimer { get; set; } // Chờ 1 thời gian sau hành động thả tay mới hồi thể lực

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentStamina = maxStamina;
            IsExhausted = false;
            IsStunned = false;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // Xử lý Hồi Choáng (Hết 3 giây Stun)
        if (IsStunned)
        {
            if (_stunTimer.Expired(Runner))
            {
                IsStunned = false;
                // Báo cho Animator đứng dậy hoặc thoát Stun
                // (Chỉ thoát Stun logic, Trigger "Stun" thường là hoạt ảnh dài tự động kết thúc hoặc cần SetTrigger "GetUp")
            }
            return; // Đang bị choáng thì không làm và không trừ gì nữa, nhưng cho phép hồi sinh lực
        }

        // Tự động hồi phục Thể lực nếu hết Delay
        if (_regenDelayTimer.ExpiredOrNotRunning(Runner))
        {
            if (CurrentStamina < maxStamina)
            {
                CurrentStamina += staminaRegenRate * Runner.DeltaTime;
                if (CurrentStamina > maxStamina) CurrentStamina = maxStamina;
                
                // Tràn trề sức sống lại rồi
                if (CurrentStamina >= 20f && IsExhausted) 
                {
                    IsExhausted = false;
                }
            }
        }
    }

    public override void Render()
    {
        // Đảm bảo tạo Thanh Thể Lực An Toàn bằng cách xác minh trực tiếp với UI Manager
        if (FloatingUIManager.Instance != null && !FloatingUIManager.Instance.HasRegisteredStamina(this))
        {
            FloatingUIManager.Instance.UpdateStamina(this, CurrentStamina, maxStamina);
        }
    }

    /// <summary>
    /// Tiêu hao thể lực tức thời (Ví dụ: Đấm, Nhảy, Lướt/Roll)
    /// </summary>
    public bool ConsumeStamina(float amount)
    {
        if (IsStunned || CurrentStamina < amount) 
        {
            if (CurrentStamina <= 0.1f) IsExhausted = true;
            return false;
        }

        if (Object.HasStateAuthority)
        {
            CurrentStamina -= amount;
            if (CurrentStamina <= 0f) 
            {
                CurrentStamina = 0f;
                IsExhausted = true;
            }

            // Kích hoạt delay hồi phục (nghỉ 1 giây mới bắt đầu hồi)
            _regenDelayTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        }
        return true;
    }

    /// <summary>
    /// Rút cạn dần thể lực kéo dài (Ví dụ: Đang đè nút Sprint Lên)
    /// </summary>
    public bool DrainStaminaContinually(float amountPerSecond)
    {
        if (IsStunned || CurrentStamina <= 0f)
        {
            IsExhausted = true;
            return false;
        }

        if (Object.HasStateAuthority)
        {
            CurrentStamina -= amountPerSecond * Runner.DeltaTime;
            if (CurrentStamina <= 0f)
            {
                CurrentStamina = 0f;
                IsExhausted = true;
            }
            
            // Liên tục chèn Delay hồi phục
            _regenDelayTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        }
        return true;
    }

    /// <summary>
    /// Bị đấm lúc kiệt sức sẽ bị Choáng
    /// </summary>
    public void TriggerExhaustionStun()
    {
        if (!Object.HasStateAuthority || IsStunned) return;

        IsStunned = true;
        _stunTimer = TickTimer.CreateFromSeconds(Runner, 3f); // Choáng 3 giây
        
        // Phát lệnh Animator Stun
        var anim = GetComponent<PlayerAnimator>();
        if (anim != null) anim.Rpc_TriggerStun();
    }

    private void OnStaminaChanged()
    {
        // Cập nhật UI Thanh Thể Lực (UI Toolkit)
        if (FloatingUIManager.Instance != null)
        {
            FloatingUIManager.Instance.UpdateStamina(this, CurrentStamina, maxStamina);
        }
    }
}
