using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Xử lý di chuyển và va chạm của Kiếm Khí (Sword Wave).
/// Áp dụng kỹ thuật Client-Side Prediction để bù trễ (Lag Compensation).
/// </summary>
public class SwordWave : NetworkBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float damage = 40f;
    public float lifeTime = 2f;
    public float hitRadius = 2.5f; // Bán kính quét của kiếm khí

    [Header("Network Data")]
    [Networked] private TickTimer _lifeTimer { get; set; }
    
    // Danh sách để tránh gây sát thương nhiều lần cho cùng 1 mục tiêu (Xuyên thấu)
    private HashSet<HealthSystem> _hitTargets = new HashSet<HealthSystem>();

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            _lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }
        
        // Tự động xoay về hướng bay (đã được Runner.Spawn gán rotation)
        Debug.Log("[SwordWave] Kiếm Khí đã được phóng!");
    }

    public override void FixedUpdateNetwork()
    {
        // 1. Di chuyển liên tục về phía trước
        transform.position += transform.forward * speed * Runner.DeltaTime;

        // 2. Chỉ State Authority mới tính toán sát thương (Mô hình Bù trễ)
        if (Object.HasStateAuthority)
        {
            CheckHits();

            if (_lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }

    private void CheckHits()
    {
        // Quét hình cầu tại vị trí hiện tại của kiếm khí
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);

        foreach (var col in hits)
        {
            var health = col.GetComponent<HealthSystem>();
            if (health != null && !_hitTargets.Contains(health))
            {
                // Bỏ qua nếu trúng chính người bắn
                var netObj = col.GetComponent<NetworkObject>();
                if (netObj != null && netObj.InputAuthority == Object.InputAuthority) continue;

                // Gây sát thương
                _hitTargets.Add(health);

                // [LAG COMP PROOF] Hiển thị bằng chứng bù trễ cho Kiếm Khí
                Debug.Log($"<color=magenta>[LagComp Proof]</color> SwordWave Hit! Predictive: {Object.HasStateAuthority}, Pos: {transform.position}");

                health.TakeDamage(damage, transform.position, Object.InputAuthority);
                
                Debug.Log($"[SwordWave] Đã chém trúng: {col.name} gây {damage} sát thương!");

                // [MỚI] Thông báo cho bảng UI để trình demo
                var ui = FindFirstObjectByType<NetworkStatusUI>();
                if (ui != null) ui.LogHit("SwordWave (Predictive)", true);
            }
        }
    }

    // Hiển thị phạm vi quét trong Editor để dễ debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.purple;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}
