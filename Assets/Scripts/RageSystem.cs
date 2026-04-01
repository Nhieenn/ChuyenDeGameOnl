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
    public float rageDrainRate = 12f; // Mất 12 điểm nộ mỗi giây khi đang Nộ (tầm 8-9s)

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

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        // 1. Logic Tiêu hao Nộ khi đang kích hoạt
        if (IsRaging)
        {
            CurrentRage -= rageDrainRate * Runner.DeltaTime;
            if (CurrentRage <= 0)
            {
                CurrentRage = 0;
                IsRaging = false; // Tự động thoát Nộ khi hết điểm
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
        
        // Hồi đầy máu ngay lập tức khi kích hoạt (Cơ hội lật kèo)
        var health = GetComponent<HealthSystem>();
        if (health != null)
        {
            health.CurrentHealth = health.maxHealth;
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
        }
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
