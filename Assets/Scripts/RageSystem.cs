using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý hệ thống Nộ (Rage).
/// Tích lũy nộ khi đánh hoặc bị đánh. Giải phóng nộ để biến hình cầm kiếm.
/// </summary>
public class RageSystem : NetworkBehaviour
{
    [Header("Rage Settings")]
    public float maxRage = 100f;
    public float rageDrainRate = 6.67f; // Mất 6.67 điểm nộ mỗi giây (Đúng 15s cho 100 nộ)

    [Networked, OnChangedRender(nameof(OnRageChanged))]
    public float CurrentRage { get; set; }

    [Networked, OnChangedRender(nameof(OnIsRagingChanged))]
    public NetworkBool IsRaging { get; set; }

    public override void Spawned()
    {
        // Chỉ reset về 0 nếu trong Inspector bồ để là 0. 
        // Nếu bồ gán 100 để test thì nó sẽ giữ nguyên 100.
        if (Object.HasStateAuthority && CurrentRage == 0)
        {
            CurrentRage = 0f;
            IsRaging = false;
        }
        
        // Cập nhật UI ngay lập tức
        if (FloatingUIManager.Instance != null)
        {
            FloatingUIManager.Instance.UpdateRage(this, CurrentRage, maxRage);
        }
    }

    public override void Render()
    {
        // [FIX BUG] Đảm bảo tạo Thanh Nộ An Toàn bằng cách xác minh trực tiếp với UI Manager mỗi frame đồ họa
        // Giúp Client vào sau vẫn thấy thanh nộ của mình nếu Spawned() chạy quá sớm khi UI chưa load xong
        if (FloatingUIManager.Instance != null && !FloatingUIManager.Instance.HasRegisteredRage(this))
        {
            FloatingUIManager.Instance.UpdateRage(this, CurrentRage, maxRage);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // 1. Logic Tiêu hao Nộ khi đang kích hoạt
        if (IsRaging)
        {
            var health = GetComponent<HealthSystem>();
            CurrentRage -= rageDrainRate * Runner.DeltaTime;
            if (CurrentRageHealthExpired(health))
            {
                CurrentRage = 0;
                IsRaging = false; // Tự động thoát Nộ khi hết điểm hoặc hết máu ảo
                if (health != null) health.CurrentRageHealth = 0;
            }
        }
        else
        {
            // 2. Logic Kích hoạt Nộ (Chỉ khi nộ đầy 100%)
            if (GetInput(out NetworkInputData data))
            {
                if (data.isRagePressed)
                {
                    Debug.Log($"[RageSystem] Đang nhấn phím Nộ. Điểm hiện tại: {CurrentRage}/{maxRage}");
                    if (CurrentRage >= maxRage - 0.05f) // Thêm sai số để tránh lỗi làm tròn số thực
                    {
                        ActivateRage();
                    }
                }
            }

            // HACK ĐỂ TEST: Nhấn phím P (trong Editor) để đầy nộ ngay lập tức
            if (Application.isEditor && Keyboard.current != null && Keyboard.current.pKey.isPressed)
            {
                CurrentRage = maxRage;
            }
        }
    }

    private void ActivateRage()
    {
        IsRaging = true;
        
        // Hồi thêm 25% máu tối đa khi kích hoạt (Cân bằng lại từ 100% xuống 25%)
        var health = GetComponent<HealthSystem>();
        if (health != null)
        {
            float healAmount = health.maxHealth * 0.25f;
            health.CurrentHealth = Mathf.Min(health.maxHealth, health.CurrentHealth + healAmount);
            
            // [MỚI] Cấp Giáp Nộ 200 HP
            health.CurrentRageHealth = 200f;
        }

        Debug.Log("[RageSystem] RAGNAAAAAROOOOK! Kích hoạt Nộ!");
    }

    /// <summary>
    /// Được gọi từ MeleeAttack (đánh trúng) hoặc HealthSystem (bị đánh/đỡ đòn)
    /// </summary>
    public void AddRage(float amount)
    {
        if (IsRaging || !Object.HasStateAuthority) return;

        CurrentRage += amount;
        if (CurrentRage > maxRage) CurrentRage = maxRage;
    }

    /// <summary>
    /// Ép thoát nộ ngay lập tức (Dùng khi bị đánh hết máu lúc đang nộ)
    /// </summary>
    public void ForceExitRage()
    {
        if (Object.HasStateAuthority)
        {
            IsRaging = false;
            CurrentRage = 0;
            
            // Xóa giáp nộ
            var health = GetComponent<HealthSystem>();
            if (health != null) health.CurrentRageHealth = 0;
        }
    }

    private bool CurrentRageHealthExpired(HealthSystem health)
    {
        // Thoát nộ nếu hết thời gian HOẶC hết máu ảo (đã xử lý ở HealthSystem)
        return CurrentRage <= 0;
    }

    private void OnRageChanged()
    {
        // Cập nhật UI Thanh Nộ (Sẽ cấu hình trong FloatingUIManager sau)
        if (FloatingUIManager.Instance != null)
        {
            FloatingUIManager.Instance.UpdateRage(this, CurrentRage, maxRage);
        }
    }

    private void OnIsRagingChanged()
    {
        // Phản hồi Visual/Audio ngay lập tức khi trạng thái Nộ thay đổi
        Debug.Log($"[RageSystem] Trạng thái Nộ: {IsRaging}");
    }
}
